using ArgoBooks.Localization;
using ArgoBooks.Services;
using ArgoBooks.Views;
using System.Collections.ObjectModel;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Common;
using ArgoBooks.Core.Models.Invoices;
using ArgoBooks.Core.Models.Transactions;
using ArgoBooks.Core.Services.InvoiceTemplates;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
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

    public ObservableCollection<CustomerOption> CustomerOptions { get; } = [];

    public ObservableCollection<ProductOption> ProductOptions { get; } = [];

    public ObservableCollection<string> StatusOptions { get; } = ["Draft", "Pending", "Sent", "Partial", "Paid", "Cancelled"];

    public ObservableCollection<InvoiceTemplate> TemplateOptions { get; } = [];

    [ObservableProperty]
    private InvoiceTemplate? _selectedTemplate;

    // Computed totals
    public decimal Subtotal => LineItems.Sum(i => i.Amount);
    public decimal TaxAmount => Subtotal * (TaxRate / 100m);
    public decimal Total => Subtotal + TaxAmount;

    public string SubtotalFormatted => $"${Subtotal:N2}";
    public string TaxAmountFormatted => $"${TaxAmount:N2}";
    public string TotalFormatted => $"${Total:N2}";

    partial void OnTaxRateChanged(decimal value)
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

        // Load products for sales/invoices (use UnitPrice for selling price)
        foreach (var product in companyData.Products.OrderBy(p => p.Name))
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
                "Only draft invoices can be continued. Use Edit for other invoice statuses.",
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

        // Populate line items
        LineItems.Clear();
        foreach (var lineItem in invoice.LineItems)
        {
            var displayItem = new LineItemDisplayModel
            {
                SelectedProduct = ProductOptions.FirstOrDefault(p => p.Id == lineItem.ProductId),
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
        IsCreateEditModalOpen = true;
    }

    #endregion

    #region Edit Modal

    public void OpenEditModal(InvoiceDisplayItem? item)
    {
        if (item == null) return;

        LoadCustomerOptions(includeAllOption: false);
        LoadProductOptions();

        var invoice = App.CompanyManager?.CompanyData?.Invoices.FirstOrDefault(i => i.Id == item.Id);
        if (invoice == null) return;

        // Prevent editing invoices that have been sent
        if (invoice.Status != InvoiceStatus.Draft && invoice.Status != InvoiceStatus.Pending)
        {
            App.AddNotification(
                "Cannot Edit Invoice",
                "Invoices that have been sent cannot be modified. Only Draft and Pending invoices can be edited.",
                NotificationType.Warning);
            return;
        }

        _editingInvoiceId = invoice.Id;
        IsEditMode = true;
        AllowPreview = false; // Edit mode doesn't show preview
        ModalTitle = $"Edit Invoice {invoice.Id}";
        SaveButtonText = "Save Changes";

        // Populate form
        SelectedCustomer = CustomerOptions.FirstOrDefault(c => c.Id == invoice.CustomerId);
        ModalIssueDate = new DateTimeOffset(invoice.IssueDate);
        ModalDueDate = new DateTimeOffset(invoice.DueDate);
        ModalStatus = invoice.Status.ToString();
        ModalNotes = invoice.Notes;
        TaxRate = invoice.TaxRate;

        // Populate line items
        LineItems.Clear();
        foreach (var lineItem in invoice.LineItems)
        {
            var displayItem = new LineItemDisplayModel
            {
                Description = lineItem.Description,
                Quantity = lineItem.Quantity,
                UnitPrice = lineItem.UnitPrice
            };
            displayItem.PropertyChanged += (_, _) => UpdateTotals();
            LineItems.Add(displayItem);
        }

        UpdateTotals();
        HasCustomerError = false;
        ValidationMessage = string.Empty;
        HasValidationMessage = false;
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

        // Create undo action
        var deletedInvoice = invoice;
        var action = new DelegateAction(
            $"Delete invoice {invoice.Id}",
            () =>
            {
                companyData?.Invoices.Add(deletedInvoice);
                InvoiceDeleted?.Invoke(this, EventArgs.Empty);
            },
            () =>
            {
                companyData?.Invoices.Remove(deletedInvoice);
                InvoiceDeleted?.Invoke(this, EventArgs.Empty);
            });

        // Remove the invoice
        companyData?.Invoices.Remove(invoice);

        // Record undo action and mark as changed
        App.UndoRedoManager.RecordAction(action);
        App.CompanyManager?.MarkAsChanged();

        InvoiceDeleted?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Filter Modal

    public void OpenFilterModal()
    {
        LoadCustomerOptions(includeAllOption: true);
        IsFilterModalOpen = true;
    }

    [RelayCommand]
    private void CloseFilterModal()
    {
        IsFilterModalOpen = false;
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
        FilterStatus = "All";
        FilterSelectedCustomer = null;
        FilterCustomerId = null;
        FilterAmountMin = null;
        FilterAmountMax = null;
        FilterIssueDateFrom = null;
        FilterIssueDateTo = null;
        FilterDueDateFrom = null;
        FilterDueDateTo = null;
        FiltersCleared?.Invoke(this, EventArgs.Empty);
        CloseFilterModal();
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
            InvoiceNumber = $"INV-{DateTime.Now:yyyy}-XXXXX",
            CustomerId = SelectedCustomer?.Id ?? string.Empty,
            IssueDate = ModalIssueDate?.DateTime ?? DateTime.Now,
            DueDate = ModalDueDate?.DateTime ?? DateTime.Now.AddDays(30),
            TaxRate = TaxRate,
            Notes = ModalNotes,
            Status = InvoiceStatus.Draft,
            LineItems = LineItems.Select(li => new LineItem
            {
                Description = li.Description,
                Quantity = li.Quantity,
                UnitPrice = li.UnitPrice
            }).ToList()
        };

        // Calculate totals
        previewInvoice.Subtotal = previewInvoice.LineItems.Sum(li => li.Quantity * li.UnitPrice);
        previewInvoice.TaxAmount = previewInvoice.Subtotal * (TaxRate / 100m);
        previewInvoice.Total = previewInvoice.Subtotal + previewInvoice.TaxAmount;
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
            // Validate that all line items have a product selected
            foreach (var lineItem in LineItems)
            {
                if (lineItem.SelectedProduct == null)
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

        // Check if email API is configured
        if (!InvoiceEmailSettings.IsConfigured)
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
            invoice.Notes = ModalNotes;
            invoice.Status = InvoiceStatus.Pending;
            invoice.UpdatedAt = DateTime.Now;
            invoice.LineItems = LineItems.Select(i => new LineItem
            {
                Description = i.Description,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                TaxRate = 0
            }).ToList();
        }
        else
        {
            // Generate new invoice ID for new invoices
            var nextNumber = (companyData.Invoices.Count) + 1;
            var invoiceId = $"INV-{DateTime.Now:yyyy}-{nextNumber:D5}";

            invoice = new Invoice
            {
                Id = invoiceId,
                InvoiceNumber = invoiceId,
                CustomerId = SelectedCustomer!.Id!,
                IssueDate = ModalIssueDate?.DateTime ?? DateTime.Now,
                DueDate = ModalDueDate?.DateTime ?? DateTime.Now.AddDays(30),
                TaxRate = TaxRate,
                Notes = ModalNotes,
                Status = InvoiceStatus.Pending,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                LineItems = LineItems.Select(i => new LineItem
                {
                    Description = i.Description,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    TaxRate = 0
                }).ToList()
            };
        }

        // Try to send the email
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

            // Email sent successfully - update invoice status
            invoice.Status = InvoiceStatus.Sent;
            invoice.History.Add(new InvoiceHistoryEntry
            {
                Action = "Sent",
                Details = $"Invoice sent to {customer.Email}",
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            await ShowSendErrorAsync($"{"Failed to send invoice:".Translate()} {ex.Message}");
            return;
        }

        // Create undo action based on whether we're continuing a draft or creating new
        DelegateAction action;
        if (isContinuingDraft)
        {
            // For continued drafts, we just updated the existing invoice
            // Undo would need to restore the previous state (simplified: just mark as changed)
            action = new DelegateAction(
                $"Send invoice {invoice.Id}",
                () =>
                {
                    invoice.Status = InvoiceStatus.Draft;
                    InvoiceSaved?.Invoke(this, EventArgs.Empty);
                },
                () =>
                {
                    invoice.Status = InvoiceStatus.Sent;
                    InvoiceSaved?.Invoke(this, EventArgs.Empty);
                });
        }
        else
        {
            // For new invoices, undo removes the invoice
            action = new DelegateAction(
                $"Create and send invoice {invoice.Id}",
                () =>
                {
                    companyData.Invoices.Remove(invoice);
                    InvoiceSaved?.Invoke(this, EventArgs.Empty);
                },
                () =>
                {
                    companyData.Invoices.Add(invoice);
                    InvoiceSaved?.Invoke(this, EventArgs.Empty);
                });

            // Add the new invoice to the collection
            companyData.Invoices.Add(invoice);
        }

        // Record undo action and mark as changed
        App.UndoRedoManager.RecordAction(action);
        App.CompanyManager?.MarkAsChanged();

        InvoiceSaved?.Invoke(this, EventArgs.Empty);

        // Show success animation instead of closing immediately
        SuccessTitle = "Invoice Sent!".Translate();
        SuccessMessage = "Your invoice has been sent to {0}".TranslateFormat(customer.Email);
        IsShowingPreview = false;
        IsShowingSuccess = true;
    }

    private Task ShowSendErrorAsync(string message)
    {
        // Show inline error banner instead of message box (HTML renderer causes deadlock with message box)
        SendErrorMessage = message;
        HasSendError = true;

        // Also show error notification
        App.AddNotification(
            "Invoice Send Failed".Translate(),
            message,
            NotificationType.Error);

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

    [RelayCommand]
    private void CloseSuccessAndFinish()
    {
        IsShowingSuccess = false;
        IsShowingPreview = false;
        IsCreateEditModalOpen = false;
        ResetForm();
    }

    [RelayCommand]
    private void SaveAsDraft()
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
            Notes = ModalNotes,
            Status = InvoiceStatus.Draft,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            LineItems = LineItems.Select(i => new LineItem
            {
                Description = i.Description,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                TaxRate = 0
            }).ToList()
        };

        // Create undo action
        var action = new DelegateAction(
            $"Add draft invoice {invoiceId}",
            () =>
            {
                companyData.Invoices.Remove(invoice);
                InvoiceSaved?.Invoke(this, EventArgs.Empty);
            },
            () =>
            {
                companyData.Invoices.Add(invoice);
                InvoiceSaved?.Invoke(this, EventArgs.Empty);
            });

        // Add the invoice
        companyData.Invoices.Add(invoice);

        // Record undo action and mark as changed
        App.UndoRedoManager.RecordAction(action);
        App.CompanyManager?.MarkAsChanged();

        InvoiceSaved?.Invoke(this, EventArgs.Empty);
        CloseCreateEditModal();
    }

    [RelayCommand]
    private void SaveInvoice()
    {
        // Clear previous validation errors first
        HasCustomerError = false;
        ValidationMessage = string.Empty;
        HasValidationMessage = false;
        foreach (var lineItem in LineItems)
        {
            lineItem.HasProductError = false;
        }

        // Validation
        if (SelectedCustomer == null || string.IsNullOrEmpty(SelectedCustomer.Id))
        {
            HasCustomerError = true;
            ValidationMessage = "Please select a customer".Translate();
            HasValidationMessage = true;
            return;
        }

        if (LineItems.Count == 0)
        {
            ValidationMessage = "Please add at least one line item".Translate();
            HasValidationMessage = true;
            return;
        }

        // Validate that all line items have a product selected
        var hasProductErrors = false;
        foreach (var lineItem in LineItems)
        {
            if (lineItem.SelectedProduct == null)
            {
                lineItem.HasProductError = true;
                hasProductErrors = true;
            }
        }

        if (hasProductErrors)
        {
            ValidationMessage = "Please select a product for all line items".Translate();
            HasValidationMessage = true;
            return;
        }

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null) return;

        if (IsEditMode)
        {
            SaveEditedInvoice(companyData);
        }
        else
        {
            SaveNewInvoice(companyData);
        }

        CloseCreateEditModal();
    }

    private void SaveNewInvoice(CompanyData companyData)
    {
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
            Notes = ModalNotes,
            Status = InvoiceStatus.Draft,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            LineItems = LineItems.Select(i => new LineItem
            {
                Description = i.Description,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                TaxRate = 0
            }).ToList()
        };

        // Create undo action
        var action = new DelegateAction(
            $"Add invoice {invoiceId}",
            () =>
            {
                companyData.Invoices.Remove(invoice);
                InvoiceSaved?.Invoke(this, EventArgs.Empty);
            },
            () =>
            {
                companyData.Invoices.Add(invoice);
                InvoiceSaved?.Invoke(this, EventArgs.Empty);
                App.CheckAndNotifyInvoiceOverdue(invoice);
            });

        // Add the invoice
        companyData.Invoices.Add(invoice);

        // Record undo action and mark as changed
        App.UndoRedoManager.RecordAction(action);
        App.CompanyManager?.MarkAsChanged();

        InvoiceSaved?.Invoke(this, EventArgs.Empty);
        App.CheckAndNotifyInvoiceOverdue(invoice);
    }

    private void SaveEditedInvoice(CompanyData companyData)
    {
        var invoice = companyData.Invoices.FirstOrDefault(i => i.Id == _editingInvoiceId);
        if (invoice == null) return;

        // Store original values for undo
        var originalCustomerId = invoice.CustomerId;
        var originalIssueDate = invoice.IssueDate;
        var originalDueDate = invoice.DueDate;
        var originalStatus = invoice.Status;
        var originalNotes = invoice.Notes;
        var originalTaxRate = invoice.TaxRate;
        var originalLineItems = invoice.LineItems.ToList();

        // Apply changes
        invoice.CustomerId = SelectedCustomer!.Id!;
        invoice.IssueDate = ModalIssueDate?.DateTime ?? DateTime.Now;
        invoice.DueDate = ModalDueDate?.DateTime ?? DateTime.Now.AddDays(30);
        invoice.Status = Enum.Parse<InvoiceStatus>(ModalStatus);
        invoice.Notes = ModalNotes;
        invoice.TaxRate = TaxRate;
        invoice.UpdatedAt = DateTime.Now;
        invoice.LineItems = LineItems.Select(i => new LineItem
        {
            Description = i.Description,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            TaxRate = 0
        }).ToList();

        // Create undo action
        var action = new DelegateAction(
            $"Edit invoice {_editingInvoiceId}",
            () =>
            {
                invoice.CustomerId = originalCustomerId;
                invoice.IssueDate = originalIssueDate;
                invoice.DueDate = originalDueDate;
                invoice.Status = originalStatus;
                invoice.Notes = originalNotes;
                invoice.TaxRate = originalTaxRate;
                invoice.LineItems = originalLineItems;
                InvoiceSaved?.Invoke(this, EventArgs.Empty);
            },
            () =>
            {
                invoice.CustomerId = SelectedCustomer!.Id!;
                invoice.IssueDate = ModalIssueDate?.DateTime ?? DateTime.Now;
                invoice.DueDate = ModalDueDate?.DateTime ?? DateTime.Now.AddDays(30);
                invoice.Status = Enum.Parse<InvoiceStatus>(ModalStatus);
                invoice.Notes = ModalNotes;
                invoice.TaxRate = TaxRate;
                invoice.LineItems = LineItems.Select(i => new LineItem
                {
                    Description = i.Description,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    TaxRate = 0
                }).ToList();
                InvoiceSaved?.Invoke(this, EventArgs.Empty);
                App.CheckAndNotifyInvoiceOverdue(invoice);
            });

        // Record undo action and mark as changed
        App.UndoRedoManager.RecordAction(action);
        App.CompanyManager?.MarkAsChanged();

        InvoiceSaved?.Invoke(this, EventArgs.Empty);
        App.CheckAndNotifyInvoiceOverdue(invoice);
    }

    private void ResetForm()
    {
        _editingInvoiceId = string.Empty;
        SelectedCustomer = null;
        ModalIssueDate = DateTimeOffset.Now;
        ModalDueDate = DateTimeOffset.Now.AddDays(30);
        ModalStatus = "Draft";
        ModalNotes = string.Empty;
        TaxRate = 0;
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
    private decimal _quantity = 1;

    [ObservableProperty]
    private decimal _unitPrice;

    [ObservableProperty]
    private bool _hasProductError;

    public decimal Amount => Quantity * UnitPrice;
    public string AmountFormatted => $"${Amount:N2}";

    partial void OnSelectedProductChanged(ProductOption? value)
    {
        if (value != null)
        {
            Description = value.Name;
            UnitPrice = value.UnitPrice;
            HasProductError = false;
        }
    }

    partial void OnQuantityChanged(decimal value)
    {
        OnPropertyChanged(nameof(Amount));
        OnPropertyChanged(nameof(AmountFormatted));
    }

    partial void OnUnitPriceChanged(decimal value)
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
