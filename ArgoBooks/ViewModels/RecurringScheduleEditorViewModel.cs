using System.Collections.ObjectModel;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Transactions;
using ArgoBooks.Core.Services;
using ArgoBooks.Localization;
using ArgoBooks.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// Create/edit form for one recurring schedule. Lives on the shell so its overlay covers the
/// window, and is shared by the Expenses and Revenue tabs rather than existing twice.
/// </summary>
public partial class RecurringScheduleEditorViewModel : ViewModelBase
{
    /// <summary>Raised after a save so the list that opened it can reload.</summary>
    public event Action? Saved;

    [ObservableProperty]
    private bool _isOpen;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _amount = string.Empty;

    [ObservableProperty]
    private int _frequencyIndex = (int)Frequency.Monthly;

    [ObservableProperty]
    private DateTimeOffset? _startDate = DateTimeOffset.Now;

    [ObservableProperty]
    private DateTimeOffset? _endDate;

    /// <summary>Customers on the revenue side, suppliers on the expense side.</summary>
    public ObservableCollection<CounterpartyOption> CounterpartyOptions { get; } = [];

    [ObservableProperty]
    private CounterpartyOption? _selectedCounterparty;

    [ObservableProperty]
    private bool _hasCounterpartyError;

    [ObservableProperty]
    private string _counterpartyLabel = string.Empty;

    [ObservableProperty]
    private string _counterpartyPlaceholder = string.Empty;

    [ObservableProperty]
    private string _counterpartyAddNewText = string.Empty;

    /// <summary>Generated entries need a line item with a product, the same as one entered by hand.</summary>
    public ObservableCollection<ProductOption> ProductOptions { get; } = [];

    [ObservableProperty]
    private ProductOption? _selectedProduct;

    [ObservableProperty]
    private bool _hasProductError;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public bool HasError => ErrorMessage.Length > 0;

    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));

    private EventHandler? _counterpartySavedHandler;
    private CategoryType _side = CategoryType.Expense;
    private string? _editingId;
    private string _originalSnapshot = string.Empty;

    public void ShowNew(CategoryType side)
    {
        _side = side;
        _editingId = null;
        Title = (side == CategoryType.Revenue ? "New recurring revenue" : "New recurring expense").Translate();
        Description = string.Empty;
        Amount = string.Empty;
        FrequencyIndex = (int)Frequency.Monthly;
        StartDate = DateTimeOffset.Now;
        EndDate = null;
        ErrorMessage = string.Empty;
        LoadCounterparties(side);
        LoadProducts(side);
        SelectedCounterparty = null;
        SelectedProduct = null;
        HasCounterpartyError = false;
        HasProductError = false;
        _originalSnapshot = Snapshot();
        IsOpen = true;
    }

    public void ShowEdit(RecurringTransaction schedule)
    {
        if (schedule.Template == null) return;

        _side = schedule.Type;
        _editingId = schedule.Id;
        Title = "Edit recurring transaction".Translate();
        Description = schedule.Template.Description;
        Amount = schedule.Template.Total.ToString("0.##");
        FrequencyIndex = (int)schedule.Frequency;
        StartDate = new DateTimeOffset(schedule.StartDate);
        EndDate = schedule.EndDate == null ? null : new DateTimeOffset(schedule.EndDate.Value);
        ErrorMessage = string.Empty;
        LoadCounterparties(schedule.Type);
        var currentId = schedule.Type == CategoryType.Revenue
            ? (schedule.RevenueTemplate?.CustomerId)
            : (schedule.ExpenseTemplate?.SupplierId);
        SelectedCounterparty = CounterpartyOptions.FirstOrDefault(o => o.Id == currentId);
        LoadProducts(schedule.Type);
        var productId = schedule.Template?.LineItems.FirstOrDefault()?.ProductId;
        SelectedProduct = ProductOptions.FirstOrDefault(o => o.Id == productId);
        HasCounterpartyError = false;
        HasProductError = false;
        _originalSnapshot = Snapshot();
        IsOpen = true;
    }

    private void LoadCounterparties(CategoryType side)
    {
        CounterpartyOptions.Clear();
        var data = App.CompanyManager?.CompanyData;
        if (data == null) return;

        CounterpartyLabel = (side == CategoryType.Revenue ? "Customer" : "Supplier").Translate();
        CounterpartyPlaceholder = (side == CategoryType.Revenue
            ? "Search for a customer..."
            : "Search for a supplier...").Translate();
        CounterpartyAddNewText = (side == CategoryType.Revenue
            ? "Create new customer"
            : "Create new supplier").Translate();

        var options = side == CategoryType.Revenue
            ? data.Customers.Select(c => new CounterpartyOption { Id = c.Id, Name = c.Name })
            : data.Suppliers.Select(sup => new CounterpartyOption { Id = sup.Id, Name = sup.Name });

        foreach (var option in options.OrderBy(o => o.Name))
            CounterpartyOptions.Add(option);
    }

    /// <summary>
    /// Opens the create-customer or create-supplier modal on top of this one and selects whatever
    /// comes back, matching how the transaction modals offer it.
    /// </summary>
    [RelayCommand]
    private void CreateCounterparty()
    {
        if (_side == CategoryType.Revenue)
        {
            var customers = App.CustomerModalsViewModel;
            if (customers == null) return;

            CreateModalSubscription.RearmOnce(ref _counterpartySavedHandler,
                h => customers.CustomerSaved += h,
                h => customers.CustomerSaved -= h,
                () =>
                {
                    LoadCounterparties(_side);
                    SelectedCounterparty = CounterpartyOptions
                        .FirstOrDefault(o => o.Id == customers.LastSavedCustomerId);
                    HasCounterpartyError = false;
                });
            customers.OpenAddModal();
        }
        else
        {
            var suppliers = App.SupplierModalsViewModel;
            if (suppliers == null) return;

            CreateModalSubscription.RearmOnce(ref _counterpartySavedHandler,
                h => suppliers.SupplierSaved += h,
                h => suppliers.SupplierSaved -= h,
                () =>
                {
                    LoadCounterparties(_side);
                    SelectedCounterparty = CounterpartyOptions
                        .FirstOrDefault(o => o.Id == suppliers.LastSavedSupplierId);
                    HasCounterpartyError = false;
                });
            suppliers.OpenAddModal();
        }
    }

    /// <summary>
    /// Mirrors how the transaction modals filter: a product whose category belongs to the other
    /// side is not offered.
    /// </summary>
    private void LoadProducts(CategoryType side)
    {
        ProductOptions.Clear();
        var data = App.CompanyManager?.CompanyData;
        if (data == null) return;

        foreach (var product in data.Products.OrderBy(p => p.Name))
        {
            var category = data.Categories.FirstOrDefault(c => c.Id == product.CategoryId);
            if (category != null && category.Type != side) continue;

            ProductOptions.Add(new ProductOption
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                UnitPrice = side == CategoryType.Expense ? product.CostPrice : product.UnitPrice
            });
        }
    }

    private string Snapshot() =>
        $"{Description}|{Amount}|{FrequencyIndex}|{StartDate?.Date:d}|{EndDate?.Date:d}|{SelectedCounterparty?.Id}|{SelectedProduct?.Id}";

    private bool IsDirty => Snapshot() != _originalSnapshot;

    /// <summary>Backdrop click and the X both route here, so neither loses typed data silently.</summary>
    [RelayCommand]
    private async Task RequestClose()
    {
        if (IsDirty && !await ConfirmDiscardNewAsync()) return;
        IsOpen = false;
    }

    [RelayCommand]
    private async Task Save()
    {
        var data = App.CompanyManager?.CompanyData;
        if (data == null) return;

        if (string.IsNullOrWhiteSpace(Description))
        {
            ErrorMessage = "Enter a description.".Translate();
            return;
        }

        if (!decimal.TryParse(Amount, out var amount) || amount <= 0)
        {
            ErrorMessage = "Enter an amount greater than zero.".Translate();
            return;
        }

        if (StartDate == null)
        {
            ErrorMessage = "Choose a start date.".Translate();
            return;
        }

        if (SelectedCounterparty?.Id == null)
        {
            HasCounterpartyError = true;
            ErrorMessage = "Choose a {0}.".TranslateFormat(CounterpartyLabel.ToLowerInvariant());
            return;
        }

        HasCounterpartyError = false;

        var start = StartDate.Value.DateTime.Date;
        var end = EndDate?.DateTime.Date;

        if (end != null && end < start)
        {
            ErrorMessage = "The end date is before the start date.".Translate();
            return;
        }

        var existing = _editingId == null
            ? null
            : data.RecurringTransactions.FirstOrDefault(s => s.Id == _editingId);

        if (existing == null)
        {
            var created = new RecurringTransaction
            {
                Id = new Core.Data.IdGenerator(data).NextRecurringTransactionId(),
                Type = _side,
                Frequency = (Frequency)FrequencyIndex,
                StartDate = start,
                NextDate = start,
                EndDate = end
            };
            ApplyTemplate(created, amount);
            data.RecurringTransactions.Add(created);

            var dateBefore = created.NextDate;
            var generated = GenerateDueNow(data);
            var dateAfter = created.NextDate;

            App.UndoRedoManager.RecordAction(new DelegateAction(
                $"Add recurring schedule {created.Id}",
                () =>
                {
                    RemoveGenerated(data, generated);
                    created.NextDate = dateBefore;
                    data.RecurringTransactions.Remove(created);
                    Saved?.Invoke();
                },
                () =>
                {
                    data.RecurringTransactions.Add(created);
                    RestoreGenerated(data, generated);
                    created.NextDate = dateAfter;
                    Saved?.Invoke();
                }));

            IsOpen = false;
            App.CompanyManager?.MarkAsChanged();
            Saved?.Invoke();
            return;
        }
        else
        {
            var before = Capture(existing);
            var amountChanged = existing.Template != null && existing.Template.Total != amount;

            existing.Frequency = (Frequency)FrequencyIndex;
            existing.StartDate = start;
            existing.EndDate = end;
            ApplyTemplate(existing, amount);

            var after = Capture(existing);

            var dateBefore = existing.NextDate;
            var generated = GenerateDueNow(data);
            var dateAfter = existing.NextDate;

            App.UndoRedoManager.RecordAction(new DelegateAction(
                $"Edit recurring schedule {existing.Id}",
                () =>
                {
                    RemoveGenerated(data, generated);
                    Restore(existing, before);
                    existing.NextDate = dateBefore;
                    Saved?.Invoke();
                },
                () =>
                {
                    Restore(existing, after);
                    RestoreGenerated(data, generated);
                    existing.NextDate = dateAfter;
                    Saved?.Invoke();
                }));

            IsOpen = false;
            App.CompanyManager?.MarkAsChanged();
            Saved?.Invoke();

            if (amountChanged)
                await OfferRetroactiveCorrection(existing);
        }
    }

    /// <summary>
    /// Company open is the usual trigger, but a schedule starting today would otherwise show as
    /// due with nothing to show for it until the file was reopened.
    /// </summary>
    private static IReadOnlyList<Transaction> GenerateDueNow(Core.Data.CompanyData data)
    {
        var generated = RecurringTransactionService.GenerateDue(data, DateTime.UtcNow);
        if (generated.Count == 0) return generated;

        var expenses = generated.Count(t => t is Expense);
        RecurringTransactionService.RaiseGenerated(expenses, generated.Count - expenses);
        return generated;
    }

    private static void RemoveGenerated(Core.Data.CompanyData data, IReadOnlyList<Transaction> generated)
    {
        foreach (var entry in generated)
        {
            if (entry is Expense expense) data.Expenses.Remove(expense);
            else if (entry is Revenue revenue) data.Revenues.Remove(revenue);
        }
    }

    private static void RestoreGenerated(Core.Data.CompanyData data, IReadOnlyList<Transaction> generated)
    {
        foreach (var entry in generated)
        {
            if (entry is Expense expense && !data.Expenses.Contains(expense)) data.Expenses.Add(expense);
            else if (entry is Revenue revenue && !data.Revenues.Contains(revenue)) data.Revenues.Add(revenue);
        }
    }

    private void ApplyTemplate(RecurringTransaction schedule, decimal amount)
    {
        var lineItems = new List<Core.Models.Common.LineItem>
        {
            new()
            {
                ProductId = SelectedProduct?.Id,
                Description = Description.Trim(),
                Quantity = 1,
                UnitPrice = amount
            }
        };

        if (schedule.Type == CategoryType.Revenue)
        {
            schedule.ExpenseTemplate = null;
            schedule.RevenueTemplate = new Revenue
            {
                Description = Description.Trim(),
                Amount = amount,
                Subtotal = amount,
                Total = amount,
                Quantity = 1,
                UnitPrice = amount,
                CustomerId = SelectedCounterparty?.Id,
                LineItems = lineItems
            };
        }
        else
        {
            schedule.RevenueTemplate = null;
            schedule.ExpenseTemplate = new Expense
            {
                Description = Description.Trim(),
                Amount = amount,
                Total = amount,
                Quantity = 1,
                UnitPrice = amount,
                SupplierId = SelectedCounterparty?.Id,
                LineItems = lineItems
            };
        }
    }

    private static (Frequency Freq, DateTime Start, DateTime? End, Expense? Exp, Revenue? Rev) Capture(
        RecurringTransaction s) => (s.Frequency, s.StartDate, s.EndDate, s.ExpenseTemplate, s.RevenueTemplate);

    private static void Restore(
        RecurringTransaction s, (Frequency Freq, DateTime Start, DateTime? End, Expense? Exp, Revenue? Rev) state)
    {
        s.Frequency = state.Freq;
        s.StartDate = state.Start;
        s.EndDate = state.End;
        s.ExpenseTemplate = state.Exp;
        s.RevenueTemplate = state.Rev;
    }

    /// <summary>
    /// A schedule edit changes future occurrences. Ones already generated are only touched when
    /// the user says so, and never when they have been matched against a bank line.
    /// </summary>
    private async Task OfferRetroactiveCorrection(RecurringTransaction schedule)
    {
        var data = App.CompanyManager?.CompanyData;
        var dialog = App.ConfirmationDialog;
        if (data == null || dialog == null) return;

        var correctable = RecurringTransactionService.FindCorrectableOccurrences(data, schedule);
        if (correctable.Count == 0) return;

        var earliest = correctable.Min(t => t.Date);
        var latest = correctable.Max(t => t.Date);

        var result = await dialog.ShowAsync(new ConfirmationDialogOptions
        {
            Title = "Update past entries?".Translate(),
            Message = "{0} entries from {1} to {2} were generated at the old amount. Update them too? Entries matched to a bank line are left alone."
                .TranslateFormat(correctable.Count, earliest.ToString("MMM d, yyyy"), latest.ToString("MMM d, yyyy")),
            PrimaryButtonText = "Update them".Translate(),
            CancelButtonText = "Leave them".Translate()
        });

        if (result != ConfirmationResult.Primary) return;

        var before = correctable
            .Select(t => (Target: t, t.Amount, t.UnitPrice, t.TaxAmount, t.Total))
            .ToList();

        RecurringTransactionService.ApplyTemplateAmounts(schedule, correctable);

        App.UndoRedoManager.RecordAction(new DelegateAction(
            $"Update {correctable.Count} past entries for {schedule.Id}",
            () =>
            {
                foreach (var (target, amount, unitPrice, taxAmount, total) in before)
                {
                    target.Amount = amount;
                    target.UnitPrice = unitPrice;
                    target.TaxAmount = taxAmount;
                    target.Total = total;
                }
                Saved?.Invoke();
            },
            () =>
            {
                RecurringTransactionService.ApplyTemplateAmounts(schedule, correctable);
                Saved?.Invoke();
            }));

        App.CompanyManager?.MarkAsChanged();
        Saved?.Invoke();
    }
}
