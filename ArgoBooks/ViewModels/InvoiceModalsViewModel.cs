using ArgoBooks.Localization;
using ArgoBooks.Services;
using System.Collections.ObjectModel;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Common;
using ArgoBooks.Core.Models.Invoices;
using ArgoBooks.Core.Models.Transactions;
using ArgoBooks.Core.Models.Portal;
using ArgoBooks.Core.Services.InvoiceTemplates;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for invoice modals (Create, Edit, Delete, Filter, History).
/// </summary>
public partial class InvoiceModalsViewModel : ViewModelBase
{
    #region Events

    public event EventHandler? InvoiceSaved;
    public event EventHandler? InvoiceDeleted;
    public event EventHandler? FiltersApplied;
    public event EventHandler? FiltersCleared;

    #endregion

    #region Modal State

    [ObservableProperty]
    private bool _isCreateEditModalOpen;

    [ObservableProperty]
    private bool _isDeleteConfirmOpen;

    [ObservableProperty]
    private bool _isFilterModalOpen;

    [ObservableProperty]
    private bool _isHistoryModalOpen;

    [ObservableProperty]
    private bool _isShowingPreview;

    [ObservableProperty]
    private bool _isEditMode;

    [ObservableProperty]
    private bool _allowPreview;

    /// <summary>
    /// Whether the invoice is being created from a rental record.
    /// When true, rental-derived fields (customer, line items) are read-only.
    /// </summary>
    [ObservableProperty]
    private bool _isFromRental;

    /// <summary>
    /// Whether the invoice is being created from a revenue record.
    /// When true, revenue-derived fields (customer, line items) are read-only.
    /// </summary>
    [ObservableProperty]
    private bool _isFromRevenue;

    /// <summary>
    /// Whether the invoice was created from an external source (rental or revenue).
    /// When true, customer, dates, and line item fields are read-only in the modal.
    /// </summary>
    public bool IsFromExternalSource => IsFromRental || IsFromRevenue;

    partial void OnIsFromRentalChanged(bool value)
    {
        OnPropertyChanged(nameof(IsFromExternalSource));
    }

    partial void OnIsFromRevenueChanged(bool value)
    {
        OnPropertyChanged(nameof(IsFromExternalSource));
    }

    /// <summary>
    /// Whether the modal is in view-only mode (read-only preview of an existing invoice).
    /// </summary>
    [ObservableProperty]
    private bool _isViewOnly;

    [ObservableProperty]
    private string _modalTitle = "Create Invoice";

    [ObservableProperty]
    private string _saveButtonText = "Create Invoice";

    /// <summary>
    /// Gets whether to show the edit form content.
    /// </summary>
    public bool ShowEditContent => !IsShowingPreview && !IsShowingSuccess;

    /// <summary>
    /// Gets whether to show the preview content.
    /// </summary>
    public bool ShowPreviewContent => IsShowingPreview && !IsShowingSuccess;

    /// <summary>
    /// Gets the modal width based on current state.
    /// </summary>
    public double ModalWidth => IsShowingSuccess ? 400 : (IsShowingPreview ? 850 : 750);

    /// <summary>
    /// Gets the modal height based on current state.
    /// </summary>
    public double ModalHeight => IsShowingSuccess ? 380 : 700;

    partial void OnIsShowingPreviewChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowEditContent));
        OnPropertyChanged(nameof(ShowPreviewContent));
        OnPropertyChanged(nameof(ModalWidth));
        OnPropertyChanged(nameof(ModalHeight));
    }

    partial void OnIsShowingSuccessChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowEditContent));
        OnPropertyChanged(nameof(ShowPreviewContent));
        OnPropertyChanged(nameof(ModalWidth));
        OnPropertyChanged(nameof(ModalHeight));
    }

    #endregion

    #region Create/Edit Modal Fields

    private string _editingInvoiceId = string.Empty;

    [ObservableProperty]
    private CustomerOption? _selectedCustomer;

    [ObservableProperty]
    private bool _hasCustomerError;

    partial void OnSelectedCustomerChanged(CustomerOption? value)
    {
        // Clear customer error when a valid customer is selected
        if (value != null && !string.IsNullOrEmpty(value.Id))
        {
            HasCustomerError = false;
        }
    }

    [ObservableProperty]
    private DateTimeOffset? _modalIssueDate = DateTimeOffset.Now;

    [ObservableProperty]
    private DateTimeOffset? _modalDueDate = DateTimeOffset.Now.AddDays(30);

    [ObservableProperty]
    private string _modalStatus = "Draft";

    [ObservableProperty]
    private string _modalNotes = string.Empty;

    [ObservableProperty]
    private decimal _taxRate;

    [ObservableProperty]
    private decimal _securityDeposit;

    [ObservableProperty]
    private string _customFeeLabel = string.Empty;

    [ObservableProperty]
    private decimal _customFeeAmount;

    [ObservableProperty]
    private bool _customFeeIsPercent;

    [ObservableProperty]
    private decimal _discountAmount;

    [ObservableProperty]
    private bool _discountIsPercent;

    [ObservableProperty]
    private string _validationMessage = string.Empty;

    [ObservableProperty]
    private bool _hasValidationMessage;

    [ObservableProperty]
    private string _sendErrorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasSendError;

    [ObservableProperty]
    private bool _isShowingSuccess;

    [ObservableProperty]
    private string _successTitle = "Invoice Sent!";

    [ObservableProperty]
    private string _successMessage = string.Empty;

    public ObservableCollection<LineItemDisplayModel> LineItems { get; } = [];

    /// <summary>
    /// Returns true if any data has been entered in the Create modal.
    /// When opened from a rental, pre-filled data doesn't count — only user-editable fields do.
    /// </summary>
    public bool HasEnteredData =>
        IsFromExternalSource
            ? !string.IsNullOrWhiteSpace(ModalNotes) || TaxRate > 0
            : SelectedCustomer != null ||
              !string.IsNullOrWhiteSpace(ModalNotes) ||
              TaxRate > 0 ||
              SecurityDeposit > 0 ||
              LineItems.Any(i => !string.IsNullOrWhiteSpace(i.Description) || i.SelectedProduct != null || (i.UnitPrice ?? 0) > 0);

    // Original values for change detection in edit mode
    private string? _originalCustomerId;
    private DateTimeOffset? _originalIssueDate;
    private DateTimeOffset? _originalDueDate;
    private string _originalStatus = "Draft";
    private string _originalNotes = string.Empty;
    private decimal _originalTaxRate;
    private decimal _originalSecurityDeposit;
    private List<(string? ProductId, string Description, decimal? Quantity, decimal? UnitPrice)> _originalLineItems = [];

    /// <summary>
    /// Returns true if any changes have been made in the Edit modal compared to original values.
    /// </summary>
    public bool HasEditModalChanges
    {
        get
        {
            if (SelectedCustomer?.Id != _originalCustomerId) return true;
            if (ModalIssueDate != _originalIssueDate) return true;
            if (ModalDueDate != _originalDueDate) return true;
            if (ModalStatus != _originalStatus) return true;
            if (ModalNotes != _originalNotes) return true;
            if (TaxRate != _originalTaxRate) return true;
            if (SecurityDeposit != _originalSecurityDeposit) return true;

            // Compare line items
            if (LineItems.Count != _originalLineItems.Count) return true;
            for (int i = 0; i < LineItems.Count; i++)
            {
                var current = LineItems[i];
                var original = _originalLineItems[i];
                if (current.SelectedProduct?.Id != original.ProductId ||
                    current.Description != original.Description ||
                    current.Quantity != original.Quantity ||
                    current.UnitPrice != original.UnitPrice)
                    return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Captures the current form state as original values for change detection.
    /// </summary>
    private void CaptureOriginalValues()
    {
        _originalCustomerId = SelectedCustomer?.Id;
        _originalIssueDate = ModalIssueDate;
        _originalDueDate = ModalDueDate;
        _originalStatus = ModalStatus;
        _originalNotes = ModalNotes;
        _originalTaxRate = TaxRate;
        _originalSecurityDeposit = SecurityDeposit;
        _originalLineItems = LineItems.Select(li => (li.SelectedProduct?.Id, li.Description, li.Quantity, li.UnitPrice)).ToList();
    }

    public ObservableCollection<CustomerOption> CustomerOptions { get; } = [];

    public ObservableCollection<ProductOption> ProductOptions { get; } = [];

    public ObservableCollection<string> StatusOptions { get; } = ["Draft", "Pending", "Sent", "Partial", "Paid", "Cancelled"];

    public ObservableCollection<InvoiceTemplate> TemplateOptions { get; } = [];

    [ObservableProperty]
    private InvoiceTemplate? _selectedTemplate;

    // Computed totals
    public decimal Subtotal => LineItems.Sum(i => i.Amount);
    public decimal TaxAmount => Subtotal * (TaxRate / 100m);
    public decimal CustomFeeCalculated => CustomFeeIsPercent ? Subtotal * (CustomFeeAmount / 100m) : CustomFeeAmount;
    public decimal DiscountCalculated => DiscountIsPercent ? Subtotal * (DiscountAmount / 100m) : DiscountAmount;
    public decimal Total => Subtotal + TaxAmount + SecurityDeposit + CustomFeeCalculated - DiscountCalculated;

    public string SubtotalFormatted => $"${Subtotal:N2}";
    public string TaxAmountFormatted => $"${TaxAmount:N2}";
    public string SecurityDepositFormatted => $"${SecurityDeposit:N2}";
    public string CustomFeeCalculatedFormatted => $"+${CustomFeeCalculated:N2}";
    public string DiscountCalculatedFormatted => $"-${DiscountCalculated:N2}";
    public string TotalFormatted => $"${Total:N2}";

    public bool HasSecurityDeposit => SecurityDeposit > 0;
    public bool HasCustomFee => CustomFeeAmount > 0;
    public bool HasDiscount => DiscountAmount > 0;

    partial void OnSelectedTemplateChanged(InvoiceTemplate? value)
    {
        // Pre-fill notes from template's default notes when creating (not editing)
        if (!IsEditMode && value != null && !string.IsNullOrWhiteSpace(value.DefaultNotes) && string.IsNullOrWhiteSpace(ModalNotes))
        {
            ModalNotes = value.DefaultNotes;
        }
    }

    partial void OnTaxRateChanged(decimal value)
    {
        UpdateTotals();
    }

    partial void OnSecurityDepositChanged(decimal value)
    {
        UpdateTotals();
    }

    partial void OnCustomFeeAmountChanged(decimal value)
    {
        UpdateTotals();
    }

    partial void OnCustomFeeIsPercentChanged(bool value)
    {
        UpdateTotals();
    }

    partial void OnDiscountAmountChanged(decimal value)
    {
        UpdateTotals();
    }

    partial void OnDiscountIsPercentChanged(bool value)
    {
        UpdateTotals();
    }

    private void UpdateTotals()
    {
        OnPropertyChanged(nameof(Subtotal));
        OnPropertyChanged(nameof(TaxAmount));
        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(SubtotalFormatted));
        OnPropertyChanged(nameof(TaxAmountFormatted));
        OnPropertyChanged(nameof(SecurityDepositFormatted));
        OnPropertyChanged(nameof(HasSecurityDeposit));
        OnPropertyChanged(nameof(CustomFeeCalculated));
        OnPropertyChanged(nameof(CustomFeeCalculatedFormatted));
        OnPropertyChanged(nameof(HasCustomFee));
        OnPropertyChanged(nameof(DiscountCalculated));
        OnPropertyChanged(nameof(DiscountCalculatedFormatted));
        OnPropertyChanged(nameof(HasDiscount));
        OnPropertyChanged(nameof(TotalFormatted));
    }

    [RelayCommand]
    private void AddLineItem()
    {
        var item = new LineItemDisplayModel
        {
            Description = string.Empty,
            Quantity = 1,
            UnitPrice = 0
        };
        item.PropertyChanged += (_, _) => UpdateTotals();
        LineItems.Add(item);
        UpdateTotals();
    }

    [RelayCommand]
    private void RemoveLineItem(LineItemDisplayModel? item)
    {
        if (item != null)
        {
            LineItems.Remove(item);
            UpdateTotals();
        }
    }

    /// <summary>
    /// Navigates to Customers page and opens the create customer modal.
    /// </summary>
    [RelayCommand]
    private void NavigateToCreateCustomer()
    {
        // Close the current modal
        IsCreateEditModalOpen = false;

        // Navigate to Customers page with openAddModal parameter
        App.NavigationService?.NavigateTo("Customers", new Dictionary<string, object?> { { "openAddModal", true } });
    }

    /// <summary>
    /// Navigates to Products page and opens the create product modal.
    /// </summary>
    [RelayCommand]
    private void NavigateToCreateProduct()
    {
        // Close the current modal
        IsCreateEditModalOpen = false;

        // Navigate to Products page with openAddModal parameter (Sales tab)
        App.NavigationService?.NavigateTo("Products", new Dictionary<string, object?> { { "openAddModal", true }, { "selectedTabIndex", 1 } });
    }

    #endregion

    #region Filter Modal Fields

    [ObservableProperty]
    private string _filterStatus = "All";

    [ObservableProperty]
    private CustomerOption? _filterSelectedCustomer;

    [ObservableProperty]
    private string? _filterCustomerId;

    [ObservableProperty]
    private string? _filterAmountMin;

    [ObservableProperty]
    private string? _filterAmountMax;

    [ObservableProperty]
    private DateTimeOffset? _filterIssueDateFrom;

    [ObservableProperty]
    private DateTimeOffset? _filterIssueDateTo;

    [ObservableProperty]
    private DateTimeOffset? _filterDueDateFrom;

    [ObservableProperty]
    private DateTimeOffset? _filterDueDateTo;

    public ObservableCollection<string> StatusFilterOptions { get; } = ["All", "Draft", "Pending", "Sent", "Partial", "Paid", "Overdue", "Cancelled"];

    // Original filter values for change detection (captured when modal opens)
    private string _originalFilterStatus = "All";
    private string? _originalFilterCustomerId;
    private string? _originalFilterAmountMin;
    private string? _originalFilterAmountMax;
    private DateTimeOffset? _originalFilterIssueDateFrom;
    private DateTimeOffset? _originalFilterIssueDateTo;
    private DateTimeOffset? _originalFilterDueDateFrom;
    private DateTimeOffset? _originalFilterDueDateTo;

    /// <summary>
    /// Returns true if any filter has been changed from the state when the modal was opened.
    /// </summary>
    public bool HasFilterModalChanges =>
        FilterStatus != _originalFilterStatus ||
        FilterSelectedCustomer?.Id != _originalFilterCustomerId ||
        FilterAmountMin != _originalFilterAmountMin ||
        FilterAmountMax != _originalFilterAmountMax ||
        FilterIssueDateFrom != _originalFilterIssueDateFrom ||
        FilterIssueDateTo != _originalFilterIssueDateTo ||
        FilterDueDateFrom != _originalFilterDueDateFrom ||
        FilterDueDateTo != _originalFilterDueDateTo;

    /// <summary>
    /// Captures the current filter state as original values for change detection.
    /// </summary>
    private void CaptureOriginalFilterValues()
    {
        _originalFilterStatus = FilterStatus;
        _originalFilterCustomerId = FilterSelectedCustomer?.Id;
        _originalFilterAmountMin = FilterAmountMin;
        _originalFilterAmountMax = FilterAmountMax;
        _originalFilterIssueDateFrom = FilterIssueDateFrom;
        _originalFilterIssueDateTo = FilterIssueDateTo;
        _originalFilterDueDateFrom = FilterDueDateFrom;
        _originalFilterDueDateTo = FilterDueDateTo;
    }

    #endregion

    #region History Modal

    [ObservableProperty]
    private string _historyInvoiceId = string.Empty;

    public ObservableCollection<InvoiceHistoryDisplayItem> HistoryItems { get; } = [];

    #endregion

    #region Constructor

    public InvoiceModalsViewModel()
    {
        LoadCustomerOptions(includeAllOption: false);
        LoadProductOptions();
    }

    private void LoadCustomerOptions(bool includeAllOption = false)
    {
        CustomerOptions.Clear();

        if (includeAllOption)
        {
            CustomerOptions.Add(new CustomerOption { Id = null, Name = "All Customers" });
        }

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData?.Customers == null)
            return;

        foreach (var customer in companyData.Customers.OrderBy(c => c.Name))
        {
            CustomerOptions.Add(new CustomerOption { Id = customer.Id, Name = customer.Name });
        }
    }

    private void LoadProductOptions()
    {
        ProductOptions.Clear();

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData?.Products == null)
            return;

        // Load only revenue products for invoices (exclude expense/rental products)
        foreach (var product in companyData.Products
                     .Where(p => p.Type == CategoryType.Revenue)
                     .OrderBy(p => p.Name))
        {
            ProductOptions.Add(new ProductOption
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                UnitPrice = product.UnitPrice
            });
        }
    }

    private void LoadTemplateOptions()
    {
        TemplateOptions.Clear();

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData?.InvoiceTemplates == null || companyData.InvoiceTemplates.Count == 0)
        {
            // Create default templates if none exist
            var defaultTemplates = InvoiceTemplateFactory.CreateDefaultTemplates();
            foreach (var template in defaultTemplates)
            {
                TemplateOptions.Add(template);
            }
            SelectedTemplate = TemplateOptions.FirstOrDefault(t => t.IsDefault) ?? TemplateOptions.FirstOrDefault();
            return;
        }

        foreach (var template in companyData.InvoiceTemplates.OrderBy(t => t.Name))
        {
            TemplateOptions.Add(template);
        }

        // Select default template or first one
        SelectedTemplate = TemplateOptions.FirstOrDefault(t => t.IsDefault) ?? TemplateOptions.FirstOrDefault();
    }

    #endregion

    #region Create Modal

    public void OpenCreateModal()
    {
        LoadCustomerOptions(includeAllOption: false);
        LoadProductOptions();
        LoadTemplateOptions();
        ResetForm();
        IsEditMode = false;
        AllowPreview = true;
        ModalTitle = "Create Invoice";
        SaveButtonText = "Preview";
        IsCreateEditModalOpen = true;
    }

    /// <summary>
    /// Opens the create invoice modal pre-populated from a rental record.
    /// </summary>
    public void OpenCreateFromRental(string rentalRecordId)
    {
        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null) return;

        var rental = companyData.Rentals.FirstOrDefault(r => r.Id == rentalRecordId);
        if (rental == null) return;

        var rentalItem = companyData.RentalInventory.FirstOrDefault(i => i.Id == rental.RentalItemId);
        var itemName = rentalItem?.Name ?? "Unknown Item";

        // Open the standard create modal (loads options, resets form)
        OpenCreateModal();

        // Mark as from rental so rental-derived fields are read-only
        IsFromRental = true;

        // Pre-select the customer
        SelectedCustomer = CustomerOptions.FirstOrDefault(c => c.Id == rental.CustomerId);

        // Calculate rental cost
        var endDate = rental.ReturnDate ?? DateTime.Today;
        var days = (int)(endDate - rental.StartDate).TotalDays;
        if (days < 1) days = 1;

        var totalCost = rental.RateType switch
        {
            RateType.Daily => rental.RateAmount * days * rental.Quantity,
            RateType.Weekly => rental.RateAmount * (decimal)Math.Ceiling(days / 7.0) * rental.Quantity,
            RateType.Monthly => rental.RateAmount * (decimal)Math.Ceiling(days / 30.0) * rental.Quantity,
            _ => rental.RateAmount * days * rental.Quantity
        };

        // Replace the default line item with rental charge
        LineItems.Clear();

        // Try to match rental item to a product by name, or create a synthetic one
        var matchedProduct = ProductOptions.FirstOrDefault(p =>
            string.Equals(p.Name, itemName, StringComparison.OrdinalIgnoreCase));

        if (matchedProduct == null)
        {
            // Create a synthetic product option from the rental item so the dropdown shows the item
            matchedProduct = new ProductOption
            {
                Id = rentalItem?.Id ?? rental.RentalItemId,
                Name = itemName,
                Description = itemName,
                UnitPrice = totalCost
            };
            ProductOptions.Add(matchedProduct);
        }

        var rentalLineItem = new LineItemDisplayModel
        {
            SelectedProduct = matchedProduct,
            Description = $"Rental: {itemName} ({rental.RateType} @ ${rental.RateAmount:N2} x {rental.Quantity})",
            Quantity = 1,
            UnitPrice = totalCost,
            RentalRecordId = rental.Id
        };
        rentalLineItem.PropertyChanged += (_, _) => UpdateTotals();
        LineItems.Add(rentalLineItem);

        // Store security deposit separately (not as a line item)
        SecurityDeposit = rental.SecurityDeposit;

        UpdateTotals();
    }

    /// <summary>
    /// Opens the create invoice modal pre-populated from a revenue record.
    /// </summary>
    public void OpenCreateFromRevenue(string revenueId)
    {
        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null) return;

        var revenue = companyData.Revenues.FirstOrDefault(r => r.Id == revenueId);
        if (revenue == null) return;

        // Open the standard create modal (loads options, resets form)
        OpenCreateModal();

        // Mark as from revenue so revenue-derived fields are read-only
        IsFromRevenue = true;

        // Pre-select the customer
        SelectedCustomer = CustomerOptions.FirstOrDefault(c => c.Id == revenue.CustomerId);

        // Replace the default line item with revenue line items
        LineItems.Clear();
        foreach (var li in revenue.LineItems)
        {
            var matchedProduct = ProductOptions.FirstOrDefault(p => p.Id == li.ProductId);
            var lineItem = new LineItemDisplayModel
            {
                SelectedProduct = matchedProduct,
                Description = li.Description,
                Quantity = li.Quantity,
                UnitPrice = li.UnitPrice,
                RevenueRecordId = revenue.Id
            };
            lineItem.PropertyChanged += (_, _) => UpdateTotals();
            LineItems.Add(lineItem);
        }

        // Set tax rate from revenue
        TaxRate = revenue.TaxRate;

        UpdateTotals();
    }

    /// <summary>
    /// Opens a read-only preview of an existing invoice.
    /// </summary>
    public void OpenViewInvoice(string? invoiceId)
    {
        if (string.IsNullOrEmpty(invoiceId)) return;

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null) return;

        var invoice = companyData.Invoices.FirstOrDefault(i => i.Id == invoiceId);
        if (invoice == null) return;

        // Get the template
        var template = companyData.InvoiceTemplates.FirstOrDefault(t => t.IsDefault);
        if (template == null)
        {
            var defaultTemplates = InvoiceTemplateFactory.CreateDefaultTemplates();
            template = defaultTemplates.FirstOrDefault(t => t.IsDefault) ?? defaultTemplates.First();
        }

        var renderer = new InvoiceHtmlRenderer();
        var currencySymbol = CurrencyService.GetSymbol(companyData.Settings.Localization.Currency);
        PreviewHtml = renderer.RenderInvoice(invoice, template, companyData, currencySymbol);

        // Open modal in view-only preview mode
        ResetForm();
        IsViewOnly = true;
        IsShowingPreview = true;
        ModalTitle = "View Invoice";
        IsCreateEditModalOpen = true;
    }

    /// <summary>
    /// Opens the create modal populated with draft invoice data for continuing work.
    /// Unlike edit mode, this shows the Preview button and allows sending the invoice.
    /// </summary>
    public void ContinueDraftInvoice(InvoiceDisplayItem? item)
    {
        if (item == null) return;

        LoadCustomerOptions(includeAllOption: false);
        LoadProductOptions();
        LoadTemplateOptions();

        var invoice = App.CompanyManager?.CompanyData?.Invoices.FirstOrDefault(i => i.Id == item.Id);
        if (invoice == null) return;

        // Only allow continuing draft invoices
        if (invoice.Status != InvoiceStatus.Draft)
        {
            App.AddNotification(
                "Cannot Continue Invoice",
                "Only draft invoices can be continued.",
                NotificationType.Warning);
            return;
        }

        _editingInvoiceId = invoice.Id;
        IsEditMode = true; // We're editing an existing invoice
        AllowPreview = true; // Show Preview button like create mode
        ModalTitle = "Continue Invoice";
        SaveButtonText = "Preview"; // Show Preview button like create mode

        // Populate form
        SelectedCustomer = CustomerOptions.FirstOrDefault(c => c.Id == invoice.CustomerId);
        ModalIssueDate = new DateTimeOffset(invoice.IssueDate);
        ModalDueDate = new DateTimeOffset(invoice.DueDate);
        ModalStatus = invoice.Status.ToString();
        ModalNotes = invoice.Notes;
        TaxRate = invoice.TaxRate;
        SecurityDeposit = invoice.SecurityDeposit;
        CustomFeeLabel = invoice.CustomFeeLabel;
        CustomFeeAmount = invoice.CustomFeeAmount;
        CustomFeeIsPercent = invoice.CustomFeeIsPercent;
        DiscountAmount = invoice.DiscountAmount;
        DiscountIsPercent = invoice.DiscountIsPercent;

        // Populate line items
        LineItems.Clear();
        foreach (var lineItem in invoice.LineItems)
        {
            var displayItem = new LineItemDisplayModel
            {
                SelectedProduct = ProductOptions.FirstOrDefault(p => p.Id == lineItem.ProductId),
                RentalRecordId = lineItem.RentalRecordId,
                RevenueRecordId = lineItem.RevenueRecordId,
                Description = lineItem.Description,
                Quantity = lineItem.Quantity,
                UnitPrice = lineItem.UnitPrice
            };
            displayItem.PropertyChanged += (_, _) => UpdateTotals();
            LineItems.Add(displayItem);
        }

        // Add a default line item if none exist
        if (LineItems.Count == 0)
        {
            AddLineItem();
        }

        UpdateTotals();
        HasCustomerError = false;
        ValidationMessage = string.Empty;
        HasValidationMessage = false;

        // Capture original values for change detection
        CaptureOriginalValues();

        IsCreateEditModalOpen = true;
    }

    #endregion

    #region Delete Confirmation

    public async void OpenDeleteConfirm(InvoiceDisplayItem? item)
    {
        if (item == null) return;

        var dialog = App.ConfirmationDialog;
        if (dialog == null) return;

        var result = await dialog.ShowAsync(new ConfirmationDialogOptions
        {
            Title = "Delete Invoice".Translate(),
            Message = "Are you sure you want to delete this invoice?\n\nInvoice: {0}\nAmount: {1}".TranslateFormat(item.Id, item.TotalFormatted),
            PrimaryButtonText = "Delete".Translate(),
            CancelButtonText = "Cancel".Translate(),
            IsPrimaryDestructive = true
        });

        if (result != ConfirmationResult.Primary) return;

        var companyData = App.CompanyManager?.CompanyData;

        var invoice = companyData?.Invoices.FirstOrDefault(i => i.Id == item.Id);
        if (invoice == null) return;

        companyData?.Invoices.Remove(invoice);
        InvoiceDeleted?.Invoke(this, EventArgs.Empty);

        // Auto-save immediately
        if (App.CompanyManager != null)
        {
            try { await App.CompanyManager.SaveCompanyAsync(); }
            catch { /* non-fatal */ }
        }
    }

    #endregion

    #region Filter Modal

    public void OpenFilterModal()
    {
        LoadCustomerOptions(includeAllOption: true);
        CaptureOriginalFilterValues();
        IsFilterModalOpen = true;
    }

    [RelayCommand]
    private void CloseFilterModal()
    {
        IsFilterModalOpen = false;
    }

    /// <summary>
    /// Requests to close the Filter modal, showing confirmation if filter values have been changed.
    /// </summary>
    [RelayCommand]
    private async Task RequestCloseFilterModalAsync()
    {
        if (HasFilterModalChanges)
        {
            var dialog = App.ConfirmationDialog;
            if (dialog != null)
            {
                var result = await dialog.ShowAsync(new ConfirmationDialogOptions
                {
                    Title = "Discard Changes?".Translate(),
                    Message = "You have unapplied filter changes. Are you sure you want to close?".Translate(),
                    PrimaryButtonText = "Discard".Translate(),
                    CancelButtonText = "Cancel".Translate(),
                    IsPrimaryDestructive = true
                });

                if (result != ConfirmationResult.Primary)
                    return;
            }

            // Restore filter values to the state when modal was opened
            FilterStatus = _originalFilterStatus;
            FilterSelectedCustomer = CustomerOptions.FirstOrDefault(c => c.Id == _originalFilterCustomerId);
            FilterAmountMin = _originalFilterAmountMin;
            FilterAmountMax = _originalFilterAmountMax;
            FilterIssueDateFrom = _originalFilterIssueDateFrom;
            FilterIssueDateTo = _originalFilterIssueDateTo;
            FilterDueDateFrom = _originalFilterDueDateFrom;
            FilterDueDateTo = _originalFilterDueDateTo;
        }

        CloseFilterModal();
    }

    [RelayCommand]
    private void ApplyFilters()
    {
        // Update FilterCustomerId from selected customer
        FilterCustomerId = FilterSelectedCustomer?.Id;
        FiltersApplied?.Invoke(this, EventArgs.Empty);
        CloseFilterModal();
    }

    [RelayCommand]
    private void ClearFilters()
    {
        ResetFilterDefaults();
        FiltersCleared?.Invoke(this, EventArgs.Empty);
        CloseFilterModal();
    }

    private void ResetFilterDefaults()
    {
        FilterStatus = "All";
        FilterSelectedCustomer = null;
        FilterCustomerId = null;
        FilterAmountMin = null;
        FilterAmountMax = null;
        FilterIssueDateFrom = null;
        FilterIssueDateTo = null;
        FilterDueDateFrom = null;
        FilterDueDateTo = null;
    }

    #endregion

    #region History Modal

    public void OpenHistoryModal(InvoiceDisplayItem? item)
    {
        if (item == null) return;

        var invoice = App.CompanyManager?.CompanyData?.Invoices.FirstOrDefault(i => i.Id == item.Id);
        if (invoice == null) return;

        HistoryInvoiceId = invoice.Id;
        HistoryItems.Clear();

        // Build history from invoice data
        var historyEntries = new List<InvoiceHistoryDisplayItem>
        {
            new()
            {
                ActionType = "Created",
                Description = "Invoice created",
                DateTime = invoice.CreatedAt
            }
        };

        // Add payment history
        var payments = App.CompanyManager?.CompanyData?.Payments
            .Where(p => p.InvoiceId == invoice.Id)
            .OrderBy(p => p.Date)
            .ToList() ?? [];

        foreach (var payment in payments)
        {
            historyEntries.Add(new InvoiceHistoryDisplayItem
            {
                ActionType = "Payment",
                Description = $"Payment of ${payment.Amount:N2} received via {payment.PaymentMethod}",
                DateTime = payment.Date
            });
        }

        // Add status changes from history if available
        if (invoice.History.Count > 0)
        {
            foreach (var entry in invoice.History.OrderBy(h => h.Timestamp))
            {
                historyEntries.Add(new InvoiceHistoryDisplayItem
                {
                    ActionType = entry.Action,
                    Description = entry.Details ?? entry.Action,
                    DateTime = entry.Timestamp
                });
            }
        }

        // Sort by date descending and mark last item
        var sortedHistory = historyEntries.OrderByDescending(h => h.DateTime).ToList();
        for (var i = 0; i < sortedHistory.Count; i++)
        {
            sortedHistory[i].IsLast = i == sortedHistory.Count - 1;
            HistoryItems.Add(sortedHistory[i]);
        }

        IsHistoryModalOpen = true;
    }

    [RelayCommand]
    private void CloseHistoryModal()
    {
        IsHistoryModalOpen = false;
        HistoryInvoiceId = string.Empty;
        HistoryItems.Clear();
    }

    #endregion

    #region Preview Modal

    public string PreviewCustomerName => SelectedCustomer?.Name ?? "No customer selected";
    public string PreviewIssueDate => ModalIssueDate?.ToString("MMMM d, yyyy") ?? "-";
    public string PreviewDueDate => ModalDueDate?.ToString("MMMM d, yyyy") ?? "-";

    [ObservableProperty]
    private string _previewHtml = string.Empty;

    private void GeneratePreviewHtml()
    {
        var template = SelectedTemplate;
        if (template == null)
        {
            // Use default template if none selected
            var defaultTemplates = InvoiceTemplateFactory.CreateDefaultTemplates();
            template = defaultTemplates.FirstOrDefault(t => t.IsDefault) ?? defaultTemplates.First();
        }

        var companySettings = App.CompanyManager?.CompanyData?.Settings ?? new();

        // Create a preview invoice from current form data
        var previewInvoice = new Invoice
        {
            Id = "INV-PREVIEW",
            InvoiceNumber = PeekNextInvoiceNumber(),
            CustomerId = SelectedCustomer?.Id ?? string.Empty,
            IssueDate = ModalIssueDate?.DateTime ?? DateTime.Now,
            DueDate = ModalDueDate?.DateTime ?? DateTime.Now.AddDays(30),
            TaxRate = TaxRate,
            CustomFeeLabel = CustomFeeLabel,
            CustomFeeAmount = CustomFeeAmount,
            CustomFeeIsPercent = CustomFeeIsPercent,
            DiscountAmount = DiscountAmount,
            DiscountIsPercent = DiscountIsPercent,
            Notes = ModalNotes,
            Status = InvoiceStatus.Draft,
            LineItems = LineItems.Select(li => new LineItem
            {
                Description = li.Description,
                Quantity = li.Quantity ?? 0,
                UnitPrice = li.UnitPrice ?? 0
            }).ToList()
        };

        // Calculate totals
        previewInvoice.Subtotal = previewInvoice.LineItems.Sum(li => li.Quantity * li.UnitPrice);
        previewInvoice.TaxAmount = previewInvoice.Subtotal * (TaxRate / 100m);
        previewInvoice.SecurityDeposit = SecurityDeposit;
        previewInvoice.Total = previewInvoice.Subtotal + previewInvoice.TaxAmount + SecurityDeposit + CustomFeeCalculated - DiscountCalculated;
        previewInvoice.Balance = previewInvoice.Total;

        // Render HTML using the same renderer as the template designer
        var renderer = new InvoiceHtmlRenderer();
        var companyData = App.CompanyManager?.CompanyData;
        if (companyData != null)
        {
            var currencySymbol = CurrencyService.GetSymbol(companySettings.Localization.Currency);
            PreviewHtml = renderer.RenderInvoice(previewInvoice, template, companyData, currencySymbol);
        }
        else
        {
            PreviewHtml = "<p>Unable to generate preview</p>";
        }
    }

    [RelayCommand]
    private async Task OpenPreviewInBrowser()
    {
        if (string.IsNullOrEmpty(PreviewHtml)) return;
        await InvoicePreviewService.PreviewInBrowserAsync(PreviewHtml, "invoice-preview");
    }

    [RelayCommand]
    private void OpenPreviewModal()
    {
        // Clear previous validation errors first
        HasCustomerError = false;
        ValidationMessage = string.Empty;
        HasValidationMessage = false;
        HasSendError = false;
        SendErrorMessage = string.Empty;
        foreach (var lineItem in LineItems)
        {
            lineItem.HasProductError = false;
        }

        // Collect all validation errors
        var hasErrors = false;
        var errorMessages = new List<string>();

        // Skip customer/line-item checks when created from external source (fields are pre-populated and hidden)
        if (!IsFromExternalSource)
        {
            // Check customer
            if (SelectedCustomer == null || string.IsNullOrEmpty(SelectedCustomer.Id))
            {
                HasCustomerError = true;
                errorMessages.Add("Please select a customer".Translate());
                hasErrors = true;
            }

            // Check line items
            if (LineItems.Count == 0)
            {
                errorMessages.Add("Please add at least one line item".Translate());
                hasErrors = true;
            }
            else
            {
                // Validate that all line items have a product selected (rental line items are exempt)
                foreach (var lineItem in LineItems)
                {
                    if (lineItem.SelectedProduct == null && string.IsNullOrEmpty(lineItem.RentalRecordId) && string.IsNullOrEmpty(lineItem.RevenueRecordId))
                    {
                        lineItem.HasProductError = true;
                        hasErrors = true;
                    }
                }

                if (LineItems.Any(li => li.HasProductError))
                {
                    errorMessages.Add("Please select a product for all line items".Translate());
                }
            }
        }

        // Show errors if any
        if (hasErrors)
        {
            ValidationMessage = string.Join(" ", errorMessages);
            HasValidationMessage = true;
            return;
        }

        // Update preview properties
        OnPropertyChanged(nameof(PreviewCustomerName));
        OnPropertyChanged(nameof(PreviewIssueDate));
        OnPropertyChanged(nameof(PreviewDueDate));

        // Generate HTML preview using the same renderer as the template designer
        GeneratePreviewHtml();

        // Show preview in the same modal instead of opening a new one
        IsShowingPreview = true;
    }

    [RelayCommand]
    private void ClosePreviewModal()
    {
        // Return to edit mode in the same modal
        IsShowingPreview = false;
    }

    [RelayCommand]
    private async Task CreateAndSendInvoice()
    {
        System.Diagnostics.Debug.WriteLine("CreateAndSendInvoice: START");

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null) return;

        System.Diagnostics.Debug.WriteLine("CreateAndSendInvoice: Got companyData");

        // Validate that we have a template selected
        if (SelectedTemplate == null)
        {
            System.Diagnostics.Debug.WriteLine("CreateAndSendInvoice: No template, calling ShowSendErrorAsync");
            await ShowSendErrorAsync("Please select an invoice template.".Translate());
            return;
        }

        System.Diagnostics.Debug.WriteLine("CreateAndSendInvoice: Template OK, checking IsConfigured");
        System.Diagnostics.Debug.WriteLine($"CreateAndSendInvoice: InvoiceEmailSettings.IsConfigured = {InvoiceEmailSettings.IsConfigured}");

        // Check if email API is configured (only required when portal is NOT configured,
        // since the portal server handles email delivery via sendEmail: true)
        if (!PortalSettings.IsConfigured && !InvoiceEmailSettings.IsConfigured)
        {
            System.Diagnostics.Debug.WriteLine("CreateAndSendInvoice: Not configured, calling ShowSendErrorAsync");
            await ShowSendErrorAsync($"{"Email API is not configured. Please add".Translate()} {InvoiceEmailSettings.ApiKeyEnvVar} {"to your .env file.".Translate()}");
            return;
        }

        // Get the customer for email
        var customer = companyData.GetCustomer(SelectedCustomer!.Id!);
        if (customer == null || string.IsNullOrWhiteSpace(customer.Email))
        {
            await ShowSendErrorAsync("Customer does not have an email address.".Translate());
            return;
        }

        // Prevent sending invoices with a total of $0 or less
        if (Total <= 0)
        {
            await ShowSendErrorAsync("Cannot send an invoice for $0 or less.".Translate());
            return;
        }

        // Check if we're continuing a draft invoice or creating a new one
        var isContinuingDraft = !string.IsNullOrEmpty(_editingInvoiceId) && AllowPreview;
        Invoice invoice;
        Invoice? existingDraft = null;

        if (isContinuingDraft)
        {
            // Find the existing draft invoice
            existingDraft = companyData.Invoices.FirstOrDefault(i => i.Id == _editingInvoiceId);
            if (existingDraft == null)
            {
                await ShowSendErrorAsync("Could not find the draft invoice.".Translate());
                return;
            }

            // Update the existing invoice
            invoice = existingDraft;
            invoice.CustomerId = SelectedCustomer!.Id!;
            invoice.IssueDate = ModalIssueDate?.DateTime ?? DateTime.Now;
            invoice.DueDate = ModalDueDate?.DateTime ?? DateTime.Now.AddDays(30);
            invoice.TaxRate = TaxRate;
            invoice.SecurityDeposit = SecurityDeposit;
            invoice.CustomFeeLabel = CustomFeeLabel;
            invoice.CustomFeeAmount = CustomFeeAmount;
            invoice.CustomFeeIsPercent = CustomFeeIsPercent;
            invoice.DiscountAmount = DiscountAmount;
            invoice.DiscountIsPercent = DiscountIsPercent;
            invoice.Notes = ModalNotes;
            invoice.Status = InvoiceStatus.Pending;
            invoice.UpdatedAt = DateTime.Now;
            invoice.LineItems = LineItems.Select(i => new LineItem
            {
                ProductId = i.SelectedProduct?.Id,
                RentalRecordId = i.RentalRecordId,
                RevenueRecordId = i.RevenueRecordId,
                Description = i.Description,
                Quantity = i.Quantity ?? 0,
                UnitPrice = i.UnitPrice ?? 0,
                TaxRate = 0
            }).ToList();
        }
        else
        {
            // Generate new invoice ID using IdGenerator
            var idGenerator = new IdGenerator(companyData);
            var invoiceId = idGenerator.NextInvoiceId();
            var invoiceNumber = idGenerator.NextInvoiceNumber();

            invoice = new Invoice
            {
                Id = invoiceId,
                InvoiceNumber = invoiceNumber,
                CustomerId = SelectedCustomer!.Id!,
                IssueDate = ModalIssueDate?.DateTime ?? DateTime.Now,
                DueDate = ModalDueDate?.DateTime ?? DateTime.Now.AddDays(30),
                TaxRate = TaxRate,
                SecurityDeposit = SecurityDeposit,
                CustomFeeLabel = CustomFeeLabel,
                CustomFeeAmount = CustomFeeAmount,
                CustomFeeIsPercent = CustomFeeIsPercent,
                DiscountAmount = DiscountAmount,
                DiscountIsPercent = DiscountIsPercent,
                Notes = ModalNotes,
                Status = InvoiceStatus.Pending,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                LineItems = LineItems.Select(i => new LineItem
                {
                    ProductId = i.SelectedProduct?.Id,
                    RentalRecordId = i.RentalRecordId,
                    Description = i.Description,
                    Quantity = i.Quantity ?? 0,
                    UnitPrice = i.UnitPrice ?? 0,
                    TaxRate = 0
                }).ToList()
            };
        }

        // Calculate invoice totals
        invoice.Subtotal = invoice.LineItems.Sum(li => li.Quantity * li.UnitPrice);
        invoice.TaxAmount = invoice.Subtotal * (invoice.TaxRate / 100m);
        var feeCalc = invoice.CustomFeeIsPercent ? invoice.Subtotal * (invoice.CustomFeeAmount / 100m) : invoice.CustomFeeAmount;
        var discCalc = invoice.DiscountIsPercent ? invoice.Subtotal * (invoice.DiscountAmount / 100m) : invoice.DiscountAmount;
        invoice.Total = invoice.Subtotal + invoice.TaxAmount + invoice.SecurityDeposit + feeCalc - discCalc;
        invoice.Balance = invoice.Total - invoice.AmountPaid;

        // Publish and send: portal handles both publishing and email delivery via sendEmail: true.
        // When portal is not configured, fall back to desktop email sending.
        if (PortalSettings.IsConfigured)
        {
            try
            {
                var portalService = App.PaymentPortalService;
                if (portalService != null)
                {
                    var currencySymbol = CurrencyService.GetSymbol(companyData.Settings.Localization.Currency);
                    var publishResponse = await portalService.PublishInvoiceAsync(
                        invoice, companyData, SelectedTemplate, currencySymbol);
                    if (publishResponse.Success)
                    {
                        invoice.History.Add(new InvoiceHistoryEntry
                        {
                            Action = "Published to Portal",
                            Details = "Invoice published to online payment portal",
                            Timestamp = DateTime.UtcNow
                        });

                        if (publishResponse.EmailSent)
                        {
                            invoice.History.Add(new InvoiceHistoryEntry
                            {
                                Action = "Email Sent",
                                Details = $"Invoice notification emailed to {customer.Email} by portal",
                                Timestamp = DateTime.UtcNow
                            });
                        }

                        // Update local provider state from the server's response so the
                        // desktop app stays in sync with which methods are available.
                        if (publishResponse.PaymentMethods != null)
                        {
                            PaymentProviderService.UpdateFromPaymentMethods(publishResponse.PaymentMethods);
                        }
                    }
                    else
                    {
                        var detail = !string.IsNullOrEmpty(publishResponse.Message)
                            ? publishResponse.Message
                            : "The payment portal did not return a payment link.";
                        await ShowSendErrorAsync(
                            $"{"Failed to publish invoice to payment portal:".Translate()} {detail}");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                await ShowSendErrorAsync(
                    $"{"Failed to publish invoice to payment portal:".Translate()} {ex.Message}");
                return;
            }
        }
        else
        {
            // No portal configured - send email directly from the desktop app
            try
            {
                var emailService = new InvoiceEmailService();
                var emailSettings = companyData.Settings.InvoiceEmail;
                var currencySymbol = CurrencyService.GetSymbol(companyData.Settings.Localization.Currency);

                var response = await emailService.SendInvoiceAsync(
                    invoice,
                    SelectedTemplate,
                    companyData,
                    emailSettings,
                    currencySymbol);

                if (!response.Success)
                {
                    await ShowSendErrorAsync(response.Message);
                    return;
                }
            }
            catch (Exception ex)
            {
                await ShowSendErrorAsync($"{"Failed to send invoice:".Translate()} {ex.Message}");
                return;
            }
        }

        // Update invoice status
        invoice.Status = InvoiceStatus.Sent;
        invoice.History.Add(new InvoiceHistoryEntry
        {
            Action = "Sent",
            Details = $"Invoice sent to {customer.Email}",
            Timestamp = DateTime.UtcNow
        });

        if (!isContinuingDraft)
        {
            // Add the new invoice to the collection and link to rentals
            companyData.Invoices.Add(invoice);
            LinkInvoiceToRentals(invoice, companyData);
            LinkInvoiceToRevenue(invoice, companyData);
        }

        InvoiceSaved?.Invoke(this, EventArgs.Empty);

        // Show success animation instead of closing immediately
        SuccessTitle = "Invoice Sent!".Translate();
        SuccessMessage = "Your invoice has been sent to {0}".TranslateFormat(customer.Email);
        IsShowingPreview = false;
        IsShowingSuccess = true;
    }

    private string PeekNextInvoiceNumber()
    {
        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null)
            return $"#INV-{DateTime.Now:yyyy}-001";

        // If editing a draft, use its existing number
        if (!string.IsNullOrEmpty(_editingInvoiceId))
        {
            var draft = companyData.Invoices.FirstOrDefault(i => i.Id == _editingInvoiceId);
            if (draft != null)
                return draft.InvoiceNumber;
        }

        var idGenerator = new IdGenerator(companyData);
        return idGenerator.PeekNextInvoice().Number;
    }

    private Task ShowSendErrorAsync(string message)
    {
        // Show inline error banner instead of message box (HTML renderer causes deadlock with message box)
        SendErrorMessage = message;
        HasSendError = true;

        return Task.CompletedTask;
    }

    [RelayCommand]
    private void DismissSendError()
    {
        HasSendError = false;
        SendErrorMessage = string.Empty;
    }

    #endregion

    #region Save Invoice

    [RelayCommand]
    private void CloseCreateEditModal()
    {
        IsCreateEditModalOpen = false;
        IsShowingPreview = false;
        IsShowingSuccess = false;
        ResetForm();
    }

    /// <summary>
    /// Requests to close the Create/Edit modal, showing confirmation if data was entered (Add mode) or changed (Edit mode).
    /// </summary>
    [RelayCommand]
    private async Task RequestCloseCreateEditModalAsync()
    {
        // Don't show confirmation if showing success screen or in view-only mode
        if (IsShowingSuccess || IsViewOnly)
        {
            CloseCreateEditModal();
            return;
        }

        // In edit mode, check if changes were made; in add mode, check if data was entered
        var hasUnsavedWork = IsEditMode ? HasEditModalChanges : HasEnteredData;

        if (hasUnsavedWork)
        {
            var dialog = App.ConfirmationDialog;
            if (dialog != null)
            {
                // Hide the WebView so the confirmation dialog renders above it (airspace issue)
                var wasShowingPreview = IsShowingPreview;
                if (wasShowingPreview)
                    IsShowingPreview = false;

                var message = IsEditMode
                    ? "You have unsaved changes that will be lost. Are you sure you want to close?".Translate()
                    : "You have entered data that will be lost. Are you sure you want to close?".Translate();

                var result = await dialog.ShowAsync(new ConfirmationDialogOptions
                {
                    Title = "Discard Changes?".Translate(),
                    Message = message,
                    PrimaryButtonText = "Discard".Translate(),
                    CancelButtonText = "Cancel".Translate(),
                    IsPrimaryDestructive = true
                });

                if (result != ConfirmationResult.Primary)
                {
                    if (wasShowingPreview)
                        IsShowingPreview = true;
                    return;
                }
            }
        }

        CloseCreateEditModal();
    }

    [RelayCommand]
    private void CloseSuccessAndFinish()
    {
        IsShowingSuccess = false;
        IsShowingPreview = false;
        IsCreateEditModalOpen = false;
        ResetForm();
    }

    [RelayCommand]
    private async Task SaveAsDraft()
    {
        // Validation - less strict for drafts
        if (SelectedCustomer == null || string.IsNullOrEmpty(SelectedCustomer.Id))
        {
            HasCustomerError = true;
            ValidationMessage = "Please select a customer".Translate();
            HasValidationMessage = true;
            return;
        }

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null) return;

        // Generate invoice ID
        var nextNumber = (companyData.Invoices.Count) + 1;
        var invoiceId = $"INV-{DateTime.Now:yyyy}-{nextNumber:D5}";

        var invoice = new Invoice
        {
            Id = invoiceId,
            InvoiceNumber = invoiceId,
            CustomerId = SelectedCustomer!.Id!,
            IssueDate = ModalIssueDate?.DateTime ?? DateTime.Now,
            DueDate = ModalDueDate?.DateTime ?? DateTime.Now.AddDays(30),
            TaxRate = TaxRate,
            SecurityDeposit = SecurityDeposit,
            CustomFeeLabel = CustomFeeLabel,
            CustomFeeAmount = CustomFeeAmount,
            CustomFeeIsPercent = CustomFeeIsPercent,
            DiscountAmount = DiscountAmount,
            DiscountIsPercent = DiscountIsPercent,
            Notes = ModalNotes,
            Status = InvoiceStatus.Draft,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            LineItems = LineItems.Select(i => new LineItem
            {
                ProductId = i.SelectedProduct?.Id,
                RentalRecordId = i.RentalRecordId,
                RevenueRecordId = i.RevenueRecordId,
                Description = i.Description,
                Quantity = i.Quantity ?? 0,
                UnitPrice = i.UnitPrice ?? 0,
                TaxRate = 0
            }).ToList()
        };

        // Add the invoice and link to rentals
        companyData.Invoices.Add(invoice);
        LinkInvoiceToRentals(invoice, companyData);

        InvoiceSaved?.Invoke(this, EventArgs.Empty);
        CloseCreateEditModal();

        // Auto-save immediately
        if (App.CompanyManager != null)
        {
            try { await App.CompanyManager.SaveCompanyAsync(); }
            catch { /* non-fatal */ }
        }
    }


    /// <summary>
    /// Links an invoice to any rental records referenced by its line items.
    /// </summary>
    private static void LinkInvoiceToRentals(Invoice invoice, CompanyData companyData)
    {
        foreach (var rentalId in invoice.LineItems
                     .Where(li => !string.IsNullOrEmpty(li.RentalRecordId))
                     .Select(li => li.RentalRecordId!)
                     .Distinct())
        {
            var rental = companyData.Rentals.FirstOrDefault(r => r.Id == rentalId);
            if (rental != null && !rental.InvoiceIds.Contains(invoice.Id))
                rental.InvoiceIds.Add(invoice.Id);
        }
    }

    /// <summary>
    /// Unlinks an invoice from any rental records referenced by its line items.
    /// </summary>
    private static void UnlinkInvoiceFromRentals(Invoice invoice, CompanyData companyData)
    {
        foreach (var rentalId in invoice.LineItems
                     .Where(li => !string.IsNullOrEmpty(li.RentalRecordId))
                     .Select(li => li.RentalRecordId!)
                     .Distinct())
        {
            var rental = companyData.Rentals.FirstOrDefault(r => r.Id == rentalId);
            rental?.InvoiceIds.Remove(invoice.Id);
        }
    }

    /// <summary>
    /// Links an invoice to any revenue records referenced by its line items.
    /// </summary>
    private static void LinkInvoiceToRevenue(Invoice invoice, CompanyData companyData)
    {
        foreach (var revenueId in invoice.LineItems
                     .Where(li => !string.IsNullOrEmpty(li.RevenueRecordId))
                     .Select(li => li.RevenueRecordId!)
                     .Distinct())
        {
            var revenue = companyData.Revenues.FirstOrDefault(r => r.Id == revenueId);
            if (revenue != null)
                revenue.InvoiceId = invoice.Id;
        }
    }

    /// <summary>
    /// Unlinks an invoice from any revenue records referenced by its line items.
    /// </summary>
    private static void UnlinkInvoiceFromRevenue(Invoice invoice, CompanyData companyData)
    {
        foreach (var revenueId in invoice.LineItems
                     .Where(li => !string.IsNullOrEmpty(li.RevenueRecordId))
                     .Select(li => li.RevenueRecordId!)
                     .Distinct())
        {
            var revenue = companyData.Revenues.FirstOrDefault(r => r.Id == revenueId);
            if (revenue != null && revenue.InvoiceId == invoice.Id)
                revenue.InvoiceId = null;
        }
    }

    private void ResetForm()
    {
        _editingInvoiceId = string.Empty;
        IsFromRental = false;
        IsFromRevenue = false;
        IsViewOnly = false;
        SelectedCustomer = null;
        ModalIssueDate = DateTimeOffset.Now;
        ModalDueDate = DateTimeOffset.Now.AddDays(30);
        ModalStatus = "Draft";
        ModalNotes = string.Empty;
        TaxRate = 0;
        SecurityDeposit = 0;
        CustomFeeLabel = string.Empty;
        CustomFeeAmount = 0;
        CustomFeeIsPercent = false;
        DiscountAmount = 0;
        DiscountIsPercent = false;
        LineItems.Clear();
        AddLineItem(); // Add one default line item
        HasCustomerError = false;
        ValidationMessage = string.Empty;
        HasValidationMessage = false;
        HasSendError = false;
        SendErrorMessage = string.Empty;
        IsShowingSuccess = false;
        SuccessTitle = "Invoice Sent!".Translate();
        SuccessMessage = string.Empty;
    }

    #endregion
}

/// <summary>
/// Display model for line items in the invoice form.
/// </summary>
public partial class LineItemDisplayModel : ObservableObject
{
    [ObservableProperty]
    private ProductOption? _selectedProduct;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private decimal? _quantity = 1;

    [ObservableProperty]
    private decimal? _unitPrice;

    [ObservableProperty]
    private bool _hasProductError;

    [ObservableProperty]
    private string? _rentalRecordId;

    [ObservableProperty]
    private string? _revenueRecordId;

    public decimal Amount => (Quantity ?? 0) * (UnitPrice ?? 0);
    public string AmountFormatted => $"${Amount:N2}";

    partial void OnSelectedProductChanged(ProductOption? value)
    {
        if (value != null)
        {
            Description = value.Name;
            if (UnitPrice is null or 0)
                UnitPrice = value.UnitPrice;
            HasProductError = false;
        }
    }

    partial void OnQuantityChanged(decimal? value)
    {
        OnPropertyChanged(nameof(Amount));
        OnPropertyChanged(nameof(AmountFormatted));
    }

    partial void OnUnitPriceChanged(decimal? value)
    {
        OnPropertyChanged(nameof(Amount));
        OnPropertyChanged(nameof(AmountFormatted));
    }
}

/// <summary>
/// Display model for invoice history items.
/// </summary>
public class InvoiceHistoryDisplayItem
{
    public string ActionType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime DateTime { get; set; }
    public bool IsLast { get; set; }

    public string DateTimeFormatted => TimeZoneService.FormatDateTime(DateTime);
}
