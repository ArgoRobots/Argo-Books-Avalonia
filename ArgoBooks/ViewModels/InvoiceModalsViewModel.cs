using System.Collections.ObjectModel;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Common;
using ArgoBooks.Core.Models.Transactions;
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
    private decimal _taxRate = 0;

    [ObservableProperty]
    private string _validationMessage = string.Empty;

    [ObservableProperty]
    private bool _hasValidationMessage;

    public ObservableCollection<LineItemDisplayModel> LineItems { get; } = [];

    public ObservableCollection<CustomerOption> CustomerOptions { get; } = [];

    public ObservableCollection<string> StatusOptions { get; } = ["Draft", "Pending", "Sent", "Partial", "Paid", "Cancelled"];

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

    #endregion

    #region Delete Confirmation

    private string _deleteInvoiceIdInternal = string.Empty;

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

    #endregion

    #region Create Modal

    public void OpenCreateModal()
    {
        LoadCustomerOptions(includeAllOption: false);
        ResetForm();
        IsEditMode = false;
        ModalTitle = "Create Invoice";
        SaveButtonText = "Preview";
        IsCreateEditModalOpen = true;
    }

    #endregion

    #region Edit Modal

    public void OpenEditModal(InvoiceDisplayItem? item)
    {
        if (item == null) return;

        LoadCustomerOptions(includeAllOption: false);

        var invoice = App.CompanyManager?.CompanyData?.Invoices?.FirstOrDefault(i => i.Id == item.Id);
        if (invoice == null) return;

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

    public void OpenDeleteConfirm(InvoiceDisplayItem? item)
    {
        if (item == null) return;

        _deleteInvoiceIdInternal = item.Id;
        DeleteInvoiceId = item.Id;
        DeleteInvoiceAmount = item.TotalFormatted;
        IsDeleteConfirmOpen = true;
    }

    [RelayCommand]
    private void CloseDeleteConfirm()
    {
        IsDeleteConfirmOpen = false;
        _deleteInvoiceIdInternal = string.Empty;
        DeleteInvoiceId = string.Empty;
        DeleteInvoiceAmount = string.Empty;
    }

    [RelayCommand]
    private void DeleteInvoice()
    {
        var companyData = App.CompanyManager?.CompanyData;
        if (companyData?.Invoices == null) return;

        var invoice = companyData.Invoices.FirstOrDefault(i => i.Id == _deleteInvoiceIdInternal);
        if (invoice == null) return;

        // Create undo action
        var deletedInvoice = invoice;
        var action = new DelegateAction(
            $"Delete invoice {invoice.Id}",
            () =>
            {
                companyData.Invoices.Add(deletedInvoice);
                InvoiceDeleted?.Invoke(this, EventArgs.Empty);
            },
            () =>
            {
                companyData.Invoices.Remove(deletedInvoice);
                InvoiceDeleted?.Invoke(this, EventArgs.Empty);
            });

        // Remove the invoice
        companyData.Invoices.Remove(invoice);

        // Record undo action and mark as changed
        App.UndoRedoManager?.RecordAction(action);
        App.CompanyManager?.MarkAsChanged();

        InvoiceDeleted?.Invoke(this, EventArgs.Empty);
        CloseDeleteConfirm();
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

        var invoice = App.CompanyManager?.CompanyData?.Invoices?.FirstOrDefault(i => i.Id == item.Id);
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
        var payments = App.CompanyManager?.CompanyData?.Payments?
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
        if (invoice.History?.Count > 0)
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

    [RelayCommand]
    private void OpenPreviewModal()
    {
        // Validation before showing preview
        if (SelectedCustomer == null || string.IsNullOrEmpty(SelectedCustomer.Id))
        {
            HasCustomerError = true;
            ValidationMessage = "Please select a customer";
            HasValidationMessage = true;
            return;
        }

        if (LineItems.Count == 0)
        {
            ValidationMessage = "Please add at least one line item";
            HasValidationMessage = true;
            return;
        }

        if (LineItems.Any(i => string.IsNullOrWhiteSpace(i.Description)))
        {
            ValidationMessage = "All line items must have a description";
            HasValidationMessage = true;
            return;
        }

        // Update preview properties
        OnPropertyChanged(nameof(PreviewCustomerName));
        OnPropertyChanged(nameof(PreviewIssueDate));
        OnPropertyChanged(nameof(PreviewDueDate));

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
    private void CreateAndSendInvoice()
    {
        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null) return;

        // Generate invoice ID
        var nextNumber = (companyData.Invoices?.Count ?? 0) + 1;
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
            Status = InvoiceStatus.Sent, // Mark as Sent since we're "sending" it
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
            $"Create and send invoice {invoiceId}",
            () =>
            {
                companyData.Invoices!.Remove(invoice);
                InvoiceSaved?.Invoke(this, EventArgs.Empty);
            },
            () =>
            {
                companyData.Invoices!.Add(invoice);
                InvoiceSaved?.Invoke(this, EventArgs.Empty);
            });

        // Add the invoice
        companyData.Invoices!.Add(invoice);

        // Record undo action and mark as changed
        App.UndoRedoManager?.RecordAction(action);
        App.CompanyManager?.MarkAsChanged();

        // TODO: Actually send email to customer here
        // For now, we just mark it as sent

        InvoiceSaved?.Invoke(this, EventArgs.Empty);
        IsPreviewModalOpen = false;
        ResetForm();
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
            ValidationMessage = "Please select a customer";
            HasValidationMessage = true;
            return;
        }

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null) return;

        // Generate invoice ID
        var nextNumber = (companyData.Invoices?.Count ?? 0) + 1;
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
                companyData.Invoices!.Remove(invoice);
                InvoiceSaved?.Invoke(this, EventArgs.Empty);
            },
            () =>
            {
                companyData.Invoices!.Add(invoice);
                InvoiceSaved?.Invoke(this, EventArgs.Empty);
            });

        // Add the invoice
        companyData.Invoices!.Add(invoice);

        // Record undo action and mark as changed
        App.UndoRedoManager?.RecordAction(action);
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
            ValidationMessage = "Please select a customer";
            HasValidationMessage = true;
            return;
        }

        if (LineItems.Count == 0)
        {
            ValidationMessage = "Please add at least one line item";
            HasValidationMessage = true;
            return;
        }

        if (LineItems.Any(i => string.IsNullOrWhiteSpace(i.Description)))
        {
            ValidationMessage = "All line items must have a description";
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
        var nextNumber = (companyData.Invoices?.Count ?? 0) + 1;
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
                companyData.Invoices!.Remove(invoice);
                InvoiceSaved?.Invoke(this, EventArgs.Empty);
            },
            () =>
            {
                companyData.Invoices!.Add(invoice);
                InvoiceSaved?.Invoke(this, EventArgs.Empty);
            });

        // Add the invoice
        companyData.Invoices!.Add(invoice);

        // Record undo action and mark as changed
        App.UndoRedoManager?.RecordAction(action);
        App.CompanyManager?.MarkAsChanged();

        InvoiceSaved?.Invoke(this, EventArgs.Empty);
    }

    private void SaveEditedInvoice(CompanyData companyData)
    {
        var invoice = companyData.Invoices?.FirstOrDefault(i => i.Id == _editingInvoiceId);
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
            });

        // Record undo action and mark as changed
        App.UndoRedoManager?.RecordAction(action);
        App.CompanyManager?.MarkAsChanged();

        InvoiceSaved?.Invoke(this, EventArgs.Empty);
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
    private string _description = string.Empty;

    [ObservableProperty]
    private decimal _quantity = 1;

    [ObservableProperty]
    private decimal _unitPrice;

    public decimal Amount => Quantity * UnitPrice;
    public string AmountFormatted => $"${Amount:N2}";

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

    public string DateTimeFormatted => DateTime.ToString("MMM d, yyyy 'at' h:mm tt");
}
