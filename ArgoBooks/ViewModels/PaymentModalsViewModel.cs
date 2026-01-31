using ArgoBooks.Localization;
using ArgoBooks.Services;
using System.Collections.ObjectModel;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Transactions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for payment modals, shared between PaymentsPage and AppShell.
/// </summary>
public partial class PaymentModalsViewModel : ObservableObject
{
    #region Modal State

    [ObservableProperty]
    private bool _isAddModalOpen;

    [ObservableProperty]
    private bool _isEditModalOpen;

    [ObservableProperty]
    private bool _isDeleteConfirmOpen;

    [ObservableProperty]
    private bool _isFilterModalOpen;

    #endregion

    #region Modal Form Fields

    [ObservableProperty]
    private string? _modalInvoiceId;

    [ObservableProperty]
    private string? _modalCustomerId;

    [ObservableProperty]
    private string _modalAmount = string.Empty;

    [ObservableProperty]
    private DateTimeOffset? _modalDate = DateTimeOffset.Now;

    [ObservableProperty]
    private string _modalPaymentMethod = "Cash";

    [ObservableProperty]
    private string _modalReferenceNumber = string.Empty;

    [ObservableProperty]
    private string _modalNotes = string.Empty;

    [ObservableProperty]
    private string? _modalInvoiceError;

    [ObservableProperty]
    private string? _modalAmountError;

    /// <summary>
    /// The payment being edited (null for add).
    /// </summary>
    private Payment? _editingPayment;

    // Original values for change detection in edit mode
    private string? _originalInvoiceId;
    private string _originalAmount = string.Empty;
    private string _originalPaymentMethod = "Cash";
    private string _originalReferenceNumber = string.Empty;
    private string _originalNotes = string.Empty;

    /// <summary>
    /// Returns true if any data has been entered in the Add modal.
    /// </summary>
    public bool HasAddModalEnteredData =>
        !string.IsNullOrEmpty(ModalInvoiceId) ||
        !string.IsNullOrWhiteSpace(ModalAmount) ||
        !string.IsNullOrWhiteSpace(ModalReferenceNumber) ||
        !string.IsNullOrWhiteSpace(ModalNotes);

    /// <summary>
    /// Returns true if any changes have been made in the Edit modal.
    /// </summary>
    public bool HasEditModalChanges =>
        ModalInvoiceId != _originalInvoiceId ||
        ModalAmount != _originalAmount ||
        ModalPaymentMethod != _originalPaymentMethod ||
        ModalReferenceNumber != _originalReferenceNumber ||
        ModalNotes != _originalNotes;

    #endregion

    #region Filter Fields

    [ObservableProperty]
    private string _filterPaymentMethod = "All";

    [ObservableProperty]
    private string _filterStatus = "All";

    [ObservableProperty]
    private string? _filterCustomerId;

    [ObservableProperty]
    private string? _filterAmountMin;

    [ObservableProperty]
    private string? _filterAmountMax;

    [ObservableProperty]
    private DateTimeOffset? _filterDateFrom;

    [ObservableProperty]
    private DateTimeOffset? _filterDateTo;

    #endregion

    #region Dropdown Options

    /// <summary>
    /// Payment method options for add/edit modal (Cash and Check only as per requirements).
    /// </summary>
    public ObservableCollection<string> ModalPaymentMethodOptions { get; } = ["Cash", "Check"];

    /// <summary>
    /// Payment method options for filter (includes All).
    /// </summary>
    public ObservableCollection<string> FilterPaymentMethodOptions { get; } = ["All", "Cash", "Check"];

    /// <summary>
    /// Status options for filter.
    /// </summary>
    public ObservableCollection<string> StatusOptions { get; } = ["All", "Completed", "Pending", "Partial", "Refunded"];

    /// <summary>
    /// Customer options for filter (populated from company data).
    /// </summary>
    public ObservableCollection<CustomerOption> CustomerOptions { get; } = [];

    /// <summary>
    /// Invoice options for add/edit modal (populated from company data).
    /// </summary>
    public ObservableCollection<InvoiceOption> InvoiceOptions { get; } = [];

    /// <summary>
    /// Selected customer option for filter.
    /// </summary>
    [ObservableProperty]
    private CustomerOption? _selectedCustomerFilter;

    partial void OnSelectedCustomerFilterChanged(CustomerOption? value)
    {
        FilterCustomerId = value?.Id;
    }

    /// <summary>
    /// Selected invoice option for modal.
    /// </summary>
    [ObservableProperty]
    private InvoiceOption? _selectedInvoice;

    partial void OnSelectedInvoiceChanged(InvoiceOption? value)
    {
        ModalInvoiceId = value?.Id;
        // Clear error when user selects an invoice
        ModalInvoiceError = null;
        // Auto-fill customer when invoice is selected
        if (value?.Id != null)
        {
            var companyData = App.CompanyManager?.CompanyData;
            var invoice = companyData?.GetInvoice(value.Id);
            if (invoice != null)
            {
                ModalCustomerId = invoice.CustomerId;
                // Auto-fill amount with remaining due if empty
                if (string.IsNullOrEmpty(ModalAmount) || ModalAmount == "0")
                {
                    var totalPaid = companyData?.Payments
                        .Where(p => p.InvoiceId == invoice.Id && p.Amount > 0)
                        .Sum(p => p.Amount) ?? 0;
                    var remaining = invoice.Total - totalPaid;
                    if (remaining > 0)
                    {
                        ModalAmount = remaining.ToString("F2");
                    }
                }
            }
        }
    }

    #endregion

    #region Events

    /// <summary>
    /// Fired when a payment is saved (added or edited).
    /// </summary>
    public event EventHandler? PaymentSaved;

    /// <summary>
    /// Fired when a payment is deleted.
    /// </summary>
    public event EventHandler? PaymentDeleted;

    /// <summary>
    /// Fired when filters are applied.
    /// </summary>
    public event EventHandler? FiltersApplied;

    /// <summary>
    /// Fired when filters are cleared.
    /// </summary>
    public event EventHandler? FiltersCleared;

    #endregion

    #region Add Payment

    [RelayCommand]
    public void OpenAddModal()
    {
        _editingPayment = null;
        ClearModalFields();
        LoadInvoiceOptions();
        LoadCustomerOptionsForFilter();
        IsAddModalOpen = true;
    }

    [RelayCommand]
    public void CloseAddModal()
    {
        IsAddModalOpen = false;
        ClearModalFields();
    }

    /// <summary>
    /// Requests to close the Add modal, showing confirmation if data was entered.
    /// </summary>
    [RelayCommand]
    public async Task RequestCloseAddModalAsync()
    {
        if (HasAddModalEnteredData)
        {
            var dialog = App.ConfirmationDialog;
            if (dialog != null)
            {
                var result = await dialog.ShowAsync(new ConfirmationDialogOptions
                {
                    Title = "Discard Changes?".Translate(),
                    Message = "You have entered data that will be lost. Are you sure you want to close?".Translate(),
                    PrimaryButtonText = "Discard".Translate(),
                    CancelButtonText = "Cancel".Translate(),
                    IsPrimaryDestructive = true
                });

                if (result != ConfirmationResult.Primary)
                    return;
            }
        }

        CloseAddModal();
    }

    /// <summary>
    /// Navigates to Invoices page and opens the create invoice modal.
    /// </summary>
    [RelayCommand]
    private void NavigateToCreateInvoice()
    {
        // Close the current modal
        IsAddModalOpen = false;
        IsEditModalOpen = false;

        // Navigate to Invoices page with openAddModal parameter
        App.NavigationService?.NavigateTo("Invoices", new Dictionary<string, object?> { { "openAddModal", true } });
    }

    [RelayCommand]
    public void SaveNewPayment()
    {
        if (!ValidateModal())
            return;

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null)
            return;

        companyData.IdCounters.Payment++;
        var newId = $"PAY-{companyData.IdCounters.Payment:D3}";

        var paymentMethod = ModalPaymentMethod switch
        {
            "Cash" => PaymentMethod.Cash,
            "Check" => PaymentMethod.Check,
            _ => PaymentMethod.Cash
        };

        var newPayment = new Payment
        {
            Id = newId,
            InvoiceId = ModalInvoiceId ?? string.Empty,
            CustomerId = ModalCustomerId ?? string.Empty,
            Date = ModalDate?.DateTime ?? DateTime.Today,
            Amount = decimal.Parse(ModalAmount),
            PaymentMethod = paymentMethod,
            ReferenceNumber = string.IsNullOrWhiteSpace(ModalReferenceNumber) ? null : ModalReferenceNumber.Trim(),
            Notes = ModalNotes.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        companyData.Payments.Add(newPayment);
        companyData.MarkAsModified();

        var paymentToUndo = newPayment;
        App.UndoRedoManager.RecordAction(new DelegateAction(
            $"Record payment '{newPayment.Id}'",
            () =>
            {
                companyData.Payments.Remove(paymentToUndo);
                companyData.MarkAsModified();
                PaymentSaved?.Invoke(this, EventArgs.Empty);
            },
            () =>
            {
                companyData.Payments.Add(paymentToUndo);
                companyData.MarkAsModified();
                PaymentSaved?.Invoke(this, EventArgs.Empty);
            }));

        PaymentSaved?.Invoke(this, EventArgs.Empty);
        CloseAddModal();
    }

    #endregion

    #region Edit Payment

    public void OpenEditModal(PaymentDisplayItem? item)
    {
        if (item == null)
            return;

        var companyData = App.CompanyManager?.CompanyData;
        var payment = companyData?.Payments.FirstOrDefault(p => p.Id == item.Id);
        if (payment == null)
            return;

        _editingPayment = payment;
        LoadInvoiceOptions();
        LoadCustomerOptionsForFilter();

        ModalInvoiceId = payment.InvoiceId;
        SelectedInvoice = InvoiceOptions.FirstOrDefault(i => i.Id == payment.InvoiceId);
        ModalCustomerId = payment.CustomerId;
        ModalAmount = payment.Amount.ToString("F2");
        ModalDate = new DateTimeOffset(payment.Date);
        ModalPaymentMethod = payment.PaymentMethod switch
        {
            PaymentMethod.Cash => "Cash",
            PaymentMethod.Check => "Check",
            _ => "Cash"
        };
        ModalReferenceNumber = payment.ReferenceNumber ?? string.Empty;
        ModalNotes = payment.Notes;

        // Store original values for change detection
        _originalInvoiceId = ModalInvoiceId;
        _originalAmount = ModalAmount;
        _originalPaymentMethod = ModalPaymentMethod;
        _originalReferenceNumber = ModalReferenceNumber;
        _originalNotes = ModalNotes;

        ClearModalErrors();
        IsEditModalOpen = true;
    }

    [RelayCommand]
    public void CloseEditModal()
    {
        IsEditModalOpen = false;
        _editingPayment = null;
        ClearModalFields();
    }

    /// <summary>
    /// Requests to close the Edit modal, showing confirmation if changes were made.
    /// </summary>
    [RelayCommand]
    public async Task RequestCloseEditModalAsync()
    {
        if (HasEditModalChanges)
        {
            var dialog = App.ConfirmationDialog;
            if (dialog != null)
            {
                var result = await dialog.ShowAsync(new ConfirmationDialogOptions
                {
                    Title = "Discard Changes?".Translate(),
                    Message = "You have unsaved changes that will be lost. Are you sure you want to close?".Translate(),
                    PrimaryButtonText = "Discard".Translate(),
                    CancelButtonText = "Cancel".Translate(),
                    IsPrimaryDestructive = true
                });

                if (result != ConfirmationResult.Primary)
                    return;
            }
        }

        CloseEditModal();
    }

    [RelayCommand]
    public void SaveEditedPayment()
    {
        if (!ValidateModal() || _editingPayment == null)
            return;

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null)
            return;

        var oldInvoiceId = _editingPayment.InvoiceId;
        var oldCustomerId = _editingPayment.CustomerId;
        var oldDate = _editingPayment.Date;
        var oldAmount = _editingPayment.Amount;
        var oldPaymentMethod = _editingPayment.PaymentMethod;
        var oldReferenceNumber = _editingPayment.ReferenceNumber;
        var oldNotes = _editingPayment.Notes;

        var newInvoiceId = ModalInvoiceId ?? string.Empty;
        var newCustomerId = ModalCustomerId ?? string.Empty;
        var newDate = ModalDate?.DateTime ?? DateTime.Today;
        var newAmount = decimal.Parse(ModalAmount);
        var newPaymentMethod = ModalPaymentMethod switch
        {
            "Cash" => PaymentMethod.Cash,
            "Check" => PaymentMethod.Check,
            _ => PaymentMethod.Cash
        };
        var newReferenceNumber = string.IsNullOrWhiteSpace(ModalReferenceNumber) ? null : ModalReferenceNumber.Trim();
        var newNotes = ModalNotes.Trim();

        // Check if anything actually changed
        var hasChanges = oldInvoiceId != newInvoiceId ||
                         oldCustomerId != newCustomerId ||
                         oldDate != newDate ||
                         oldAmount != newAmount ||
                         oldPaymentMethod != newPaymentMethod ||
                         oldReferenceNumber != newReferenceNumber ||
                         oldNotes != newNotes;

        // If nothing changed, just close the modal without recording an action
        if (!hasChanges)
        {
            CloseEditModal();
            return;
        }

        var paymentToEdit = _editingPayment;
        paymentToEdit.InvoiceId = newInvoiceId;
        paymentToEdit.CustomerId = newCustomerId;
        paymentToEdit.Date = newDate;
        paymentToEdit.Amount = newAmount;
        paymentToEdit.PaymentMethod = newPaymentMethod;
        paymentToEdit.ReferenceNumber = newReferenceNumber;
        paymentToEdit.Notes = newNotes;

        companyData.MarkAsModified();

        App.UndoRedoManager.RecordAction(new DelegateAction(
            $"Edit payment '{paymentToEdit.Id}'",
            () =>
            {
                paymentToEdit.InvoiceId = oldInvoiceId;
                paymentToEdit.CustomerId = oldCustomerId;
                paymentToEdit.Date = oldDate;
                paymentToEdit.Amount = oldAmount;
                paymentToEdit.PaymentMethod = oldPaymentMethod;
                paymentToEdit.ReferenceNumber = oldReferenceNumber;
                paymentToEdit.Notes = oldNotes;
                companyData.MarkAsModified();
                PaymentSaved?.Invoke(this, EventArgs.Empty);
            },
            () =>
            {
                paymentToEdit.InvoiceId = newInvoiceId;
                paymentToEdit.CustomerId = newCustomerId;
                paymentToEdit.Date = newDate;
                paymentToEdit.Amount = newAmount;
                paymentToEdit.PaymentMethod = newPaymentMethod;
                paymentToEdit.ReferenceNumber = newReferenceNumber;
                paymentToEdit.Notes = newNotes;
                companyData.MarkAsModified();
                PaymentSaved?.Invoke(this, EventArgs.Empty);
            }));

        PaymentSaved?.Invoke(this, EventArgs.Empty);
        CloseEditModal();
    }

    #endregion

    #region Delete Payment

    public async void OpenDeleteConfirm(PaymentDisplayItem? item)
    {
        if (item == null)
            return;

        var dialog = App.ConfirmationDialog;
        if (dialog == null)
            return;

        var result = await dialog.ShowAsync(new ConfirmationDialogOptions
        {
            Title = "Delete Payment".Translate(),
            Message = "Are you sure you want to delete this payment?\n\nPayment ID: {0}\nAmount: {1}".TranslateFormat(item.Id, item.AmountFormatted),
            PrimaryButtonText = "Delete".Translate(),
            CancelButtonText = "Cancel".Translate(),
            IsPrimaryDestructive = true
        });

        if (result != ConfirmationResult.Primary)
            return;

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null)
            return;

        var payment = companyData.Payments.FirstOrDefault(p => p.Id == item.Id);
        if (payment != null)
        {
            var deletedPayment = payment;
            companyData.Payments.Remove(payment);
            companyData.MarkAsModified();

            App.UndoRedoManager.RecordAction(new DelegateAction(
                $"Delete payment '{deletedPayment.Id}'",
                () =>
                {
                    companyData.Payments.Add(deletedPayment);
                    companyData.MarkAsModified();
                    PaymentDeleted?.Invoke(this, EventArgs.Empty);
                },
                () =>
                {
                    companyData.Payments.Remove(deletedPayment);
                    companyData.MarkAsModified();
                    PaymentDeleted?.Invoke(this, EventArgs.Empty);
                }));
        }

        PaymentDeleted?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Filter Modal

    [RelayCommand]
    public void OpenFilterModal()
    {
        LoadCustomerOptionsForFilter();
        SelectedCustomerFilter = CustomerOptions.FirstOrDefault(c => c.Id == FilterCustomerId);
        IsFilterModalOpen = true;
    }

    [RelayCommand]
    public void CloseFilterModal()
    {
        IsFilterModalOpen = false;
    }

    /// <summary>
    /// Returns true if any filter differs from its default value.
    /// </summary>
    public bool HasFilterChanges =>
        FilterDateFrom != null ||
        FilterDateTo != null ||
        FilterPaymentMethod != "All" ||
        FilterStatus != "All" ||
        !string.IsNullOrWhiteSpace(FilterAmountMin) ||
        !string.IsNullOrWhiteSpace(FilterAmountMax) ||
        SelectedCustomerFilter != null;

    /// <summary>
    /// Requests to close the Filter modal, showing confirmation if filter changes exist.
    /// </summary>
    [RelayCommand]
    public async Task RequestCloseFilterModalAsync()
    {
        if (HasFilterChanges)
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

            ResetFilterDefaults();
        }

        CloseFilterModal();
    }

    [RelayCommand]
    public void ApplyFilters()
    {
        FiltersApplied?.Invoke(this, EventArgs.Empty);
        CloseFilterModal();
    }

    [RelayCommand]
    public void ClearFilters()
    {
        ResetFilterDefaults();
        FiltersCleared?.Invoke(this, EventArgs.Empty);
        CloseFilterModal();
    }

    #endregion

    #region Data Loading

    private void LoadInvoiceOptions()
    {
        InvoiceOptions.Clear();
        InvoiceOptions.Add(new InvoiceOption { Id = null, Display = "No Invoice" });

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData?.Invoices == null)
            return;

        foreach (var invoice in companyData.Invoices.OrderByDescending(i => i.IssueDate))
        {
            var customer = companyData.GetCustomer(invoice.CustomerId);
            var customerName = customer?.Name ?? "Unknown";
            var totalPaid = companyData.Payments
                .Where(p => p.InvoiceId == invoice.Id && p.Amount > 0)
                .Sum(p => p.Amount);
            var amountDue = invoice.Total - totalPaid;

            InvoiceOptions.Add(new InvoiceOption
            {
                Id = invoice.Id,
                Display = $"{invoice.Id} - {customerName} (${amountDue:N2} due)",
                AmountDue = amountDue
            });
        }
    }

    private void LoadCustomerOptionsForFilter()
    {
        CustomerOptions.Clear();
        CustomerOptions.Add(new CustomerOption { Id = string.Empty, Name = "All Customers" });

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData?.Customers == null)
            return;

        foreach (var customer in companyData.Customers.OrderBy(c => c.Name))
        {
            CustomerOptions.Add(new CustomerOption { Id = customer.Id, Name = customer.Name });
        }
    }

    #endregion

    #region Property Changed Handlers

    partial void OnModalAmountChanged(string value)
    {
        if (decimal.TryParse(value, out var amount) && amount != 0)
        {
            ModalAmountError = null;
        }
    }

    #endregion

    #region Modal Helpers

    private void ClearModalFields()
    {
        ModalInvoiceId = null;
        SelectedInvoice = null;
        ModalCustomerId = null;
        ModalAmount = string.Empty;
        ModalDate = DateTimeOffset.Now;
        ModalPaymentMethod = "Cash";
        ModalReferenceNumber = string.Empty;
        ModalNotes = string.Empty;
        ClearModalErrors();
    }

    private void ClearModalErrors()
    {
        ModalInvoiceError = null;
        ModalAmountError = null;
    }

    private void ResetFilterDefaults()
    {
        FilterDateFrom = null;
        FilterDateTo = null;
        FilterPaymentMethod = "All";
        FilterStatus = "All";
        FilterAmountMin = null;
        FilterAmountMax = null;
        SelectedCustomerFilter = null;
    }

    private bool ValidateModal()
    {
        ClearModalErrors();
        var isValid = true;

        // Validate amount (required and must be a valid number)
        if (string.IsNullOrWhiteSpace(ModalAmount))
        {
            ModalAmountError = "Amount is required.".Translate();
            isValid = false;
        }
        else if (!decimal.TryParse(ModalAmount, out var amount) || amount == 0)
        {
            ModalAmountError = "Please enter a valid amount.".Translate();
            isValid = false;
        }

        return isValid;
    }

    #endregion
}
