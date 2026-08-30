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

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public bool HasError => ErrorMessage.Length > 0;

    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));

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
        _originalSnapshot = Snapshot();
        IsOpen = true;
    }

    private string Snapshot() =>
        $"{Description}|{Amount}|{FrequencyIndex}|{StartDate?.Date:d}|{EndDate?.Date:d}";

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

            App.UndoRedoManager.RecordAction(new DelegateAction(
                $"Add recurring schedule {created.Id}",
                () => { data.RecurringTransactions.Remove(created); Saved?.Invoke(); },
                () => { data.RecurringTransactions.Add(created); Saved?.Invoke(); }));
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
            App.UndoRedoManager.RecordAction(new DelegateAction(
                $"Edit recurring schedule {existing.Id}",
                () => { Restore(existing, before); Saved?.Invoke(); },
                () => { Restore(existing, after); Saved?.Invoke(); }));

            if (amountChanged)
            {
                IsOpen = false;
                App.CompanyManager?.MarkAsChanged();
                Saved?.Invoke();
                await OfferRetroactiveCorrection(existing);
                return;
            }
        }

        IsOpen = false;
        App.CompanyManager?.MarkAsChanged();
        Saved?.Invoke();
    }

    private void ApplyTemplate(RecurringTransaction schedule, decimal amount)
    {
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
                UnitPrice = amount
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
                UnitPrice = amount
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
