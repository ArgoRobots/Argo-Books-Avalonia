using System.Collections.ObjectModel;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Transactions;
using ArgoBooks.Services;
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
    private DateTime _modalDate = DateTime.Today;

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

    /// <summary>
    /// The payment being deleted.
    /// </summary>
    private PaymentDisplayItem? _deletingPayment;

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
    private DateTime? _filterDateFrom;

    [ObservableProperty]
    private DateTime? _filterDateTo;

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
            Date = ModalDate,
            Amount = decimal.Parse(ModalAmount),
            PaymentMethod = paymentMethod,
            ReferenceNumber = string.IsNullOrWhiteSpace(ModalReferenceNumber) ? null : ModalReferenceNumber.Trim(),
            Notes = ModalNotes.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        companyData.Payments.Add(newPayment);
        companyData.MarkAsModified();

        var paymentToUndo = newPayment;
        App.UndoRedoManager?.RecordAction(new PaymentAddAction(
            $"Record payment '{newPayment.Id}'",
            paymentToUndo,
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
        ModalDate = payment.Date;
        ModalPaymentMethod = payment.PaymentMethod switch
        {
            PaymentMethod.Cash => "Cash",
            PaymentMethod.Check => "Check",
            _ => "Cash"
        };
        ModalReferenceNumber = payment.ReferenceNumber ?? string.Empty;
        ModalNotes = payment.Notes;

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
        var newDate = ModalDate;
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

        App.UndoRedoManager?.RecordAction(new PaymentEditAction(
            $"Edit payment '{paymentToEdit.Id}'",
            paymentToEdit,
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

    public void OpenDeleteConfirm(PaymentDisplayItem? item)
    {
        if (item == null)
            return;

        _deletingPayment = item;
        OnPropertyChanged(nameof(DeletingPaymentId));
        OnPropertyChanged(nameof(DeletingPaymentAmount));
        IsDeleteConfirmOpen = true;
    }

    [RelayCommand]
    public void CloseDeleteConfirm()
    {
        IsDeleteConfirmOpen = false;
        _deletingPayment = null;
    }

    [RelayCommand]
    public void ConfirmDelete()
    {
        if (_deletingPayment == null)
            return;

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null)
            return;

        var payment = companyData.Payments.FirstOrDefault(p => p.Id == _deletingPayment.Id);
        if (payment != null)
        {
            var deletedPayment = payment;
            companyData.Payments.Remove(payment);
            companyData.MarkAsModified();

            App.UndoRedoManager?.RecordAction(new PaymentDeleteAction(
                $"Delete payment '{deletedPayment.Id}'",
                deletedPayment,
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
        CloseDeleteConfirm();
    }

    public string DeletingPaymentId => _deletingPayment?.Id ?? string.Empty;
    public string DeletingPaymentAmount => _deletingPayment?.AmountFormatted ?? "$0.00";

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

    [RelayCommand]
    public void ApplyFilters()
    {
        FiltersApplied?.Invoke(this, EventArgs.Empty);
        CloseFilterModal();
    }

    [RelayCommand]
    public void ClearFilters()
    {
        FilterPaymentMethod = "All";
        FilterStatus = "All";
        FilterCustomerId = null;
        SelectedCustomerFilter = CustomerOptions.FirstOrDefault();
        FilterAmountMin = null;
        FilterAmountMax = null;
        FilterDateFrom = null;
        FilterDateTo = null;
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

    #region Modal Helpers

    private void ClearModalFields()
    {
        ModalInvoiceId = null;
        SelectedInvoice = null;
        ModalCustomerId = null;
        ModalAmount = string.Empty;
        ModalDate = DateTime.Today;
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

    private bool ValidateModal()
    {
        ClearModalErrors();
        var isValid = true;

        // Validate amount (required and must be a valid number)
        if (string.IsNullOrWhiteSpace(ModalAmount))
        {
            ModalAmountError = "Amount is required.";
            isValid = false;
        }
        else if (!decimal.TryParse(ModalAmount, out var amount) || amount == 0)
        {
            ModalAmountError = "Please enter a valid amount.";
            isValid = false;
        }

        return isValid;
    }

    #endregion
}
