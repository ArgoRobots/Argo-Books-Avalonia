using ArgoBooks.Localization;
using ArgoBooks.Services;
using ArgoBooks.Views;
using System.Collections.ObjectModel;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Common;
using ArgoBooks.Core.Models.Invoices;
using ArgoBooks.Core.Models.Transactions;
using ArgoBooks.Core.Services;
using ArgoBooks.Core.Services.InvoiceTemplates;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
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
    private bool _isPreviewModalOpen;

    [ObservableProperty]
    private bool _isEditMode;

    [ObservableProperty]
    private string _modalTitle = "Create Invoice";

    [ObservableProperty]
    private string _saveButtonText = "Create Invoice";

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

    #region Delete Confirmation

    [ObservableProperty]
    private string _deleteInvoiceId = string.Empty;

    [ObservableProperty]
    private string _deleteInvoiceAmount = string.Empty;

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
            var defaultTemplates = Core.Services.InvoiceTemplates.InvoiceTemplateFactory.CreateDefaultTemplates();
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
            var defaultTemplates = Core.Services.InvoiceTemplates.InvoiceTemplateFactory.CreateDefaultTemplates();
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
        var renderer = new Core.Services.InvoiceTemplates.InvoiceHtmlRenderer();
        var companyData = App.CompanyManager?.CompanyData;
        if (companyData != null)
        {
            var currencySymbol = Services.CurrencyService.GetSymbol(companySettings.Localization.Currency);
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
        await Core.Services.InvoiceTemplates.InvoicePreviewService.PreviewInBrowserAsync(PreviewHtml, "invoice-preview");
    }

    [RelayCommand]
    private void OpenPreviewModal()
    {
        // Validation before showing preview
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

        // Update preview properties
        OnPropertyChanged(nameof(PreviewCustomerName));
        OnPropertyChanged(nameof(PreviewIssueDate));
        OnPropertyChanged(nameof(PreviewDueDate));

        // Generate HTML preview using the same renderer as the template designer
        GeneratePreviewHtml();

        IsCreateEditModalOpen = false;
        IsPreviewModalOpen = true;
    }

    [RelayCommand]
    private void ClosePreviewModal()
    {
        IsPreviewModalOpen = false;
        IsCreateEditModalOpen = true;
    }

    [RelayCommand]
    private async Task CreateAndSendInvoice()
    {
        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null) return;

        // Validate that we have a template selected
        if (SelectedTemplate?.Template == null)
        {
            await ShowSendErrorAsync("Please select an invoice template.".Translate());
            return;
        }

        // Check if email API is configured
        if (!InvoiceEmailSettings.IsConfigured)
        {
            await ShowSendErrorAsync($"{"Email API is not configured. Please add".Translate()} {InvoiceEmailSettings.ApiEndpointEnvVar} {"and".Translate()} {InvoiceEmailSettings.ApiKeyEnvVar} {"to your .env file.".Translate()}");
            return;
        }

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
            Status = InvoiceStatus.Pending, // Start as pending until email is sent
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

        // Try to send the email
        try
        {
            var emailService = new InvoiceEmailService();
            var emailSettings = companyData.Settings.InvoiceEmail;
            var currencySymbol = CurrencyService.GetSymbol(companyData.Settings.Localization.Currency);

            var response = await emailService.SendInvoiceAsync(
                invoice,
                SelectedTemplate.Template,
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
                Details = $"Invoice sent to {SelectedCustomer.Email}",
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            await ShowSendErrorAsync($"{"Failed to send invoice:".Translate()} {ex.Message}");
            return;
        }

        // Create undo action
        var action = new DelegateAction(
            $"Create and send invoice {invoiceId}",
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
        IsPreviewModalOpen = false;
        ResetForm();
    }

    private async Task ShowSendErrorAsync(string message)
    {
        var messageBoxService = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? (desktop.MainWindow as MainWindow)?.MessageBoxService
            : null;

        if (messageBoxService != null)
        {
            await messageBoxService.ShowErrorAsync("Invoice Send Failed".Translate(), message);
        }
    }

    #endregion

    #region Save Invoice

    [RelayCommand]
    private void CloseCreateEditModal()
    {
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
