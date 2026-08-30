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
/// One row on the Recurring page. Both sides live in one list because a user setting up their
/// monthly bills should not have to do it in two places.
/// </summary>
public partial class RecurringDisplayItem : ObservableObject
{
    public string Id { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TypeLabel { get; set; } = string.Empty;
    public string FrequencyLabel { get; set; } = string.Empty;
    public string AmountFormatted { get; set; } = string.Empty;
    public string NextDateFormatted { get; set; } = string.Empty;
    public string StatusLabel { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsExpense { get; set; }
}

public partial class RecurringSchedulesViewModel : ViewModelBase
{
    /// <summary>Which side this list belongs to. The hosting tab already says which, so the
    /// editor has no type picker.</summary>
    private readonly CategoryType _side;

    public ObservableCollection<RecurringDisplayItem> Schedules { get; } = [];

    public bool HasSchedules => Schedules.Count > 0;

    [ObservableProperty]
    private bool _isEditorOpen;

    [ObservableProperty]
    private string _editorTitle = string.Empty;

    [ObservableProperty]
    private string _editorDescription = string.Empty;

    [ObservableProperty]
    private string _editorAmount = string.Empty;

    [ObservableProperty]
    private int _editorFrequencyIndex = 2;

    [ObservableProperty]
    private DateTimeOffset? _editorStartDate = DateTimeOffset.Now;

    [ObservableProperty]
    private DateTimeOffset? _editorEndDate;

    [ObservableProperty]
    private string _editorError = string.Empty;

    public bool HasEditorError => EditorError.Length > 0;

    private string? _editingId;

    public RecurringSchedulesViewModel() : this(CategoryType.Expense)
    {
    }

    public RecurringSchedulesViewModel(CategoryType side)
    {
        _side = side;
        Load();
    }

    partial void OnEditorErrorChanged(string value) => OnPropertyChanged(nameof(HasEditorError));

    private void Load()
    {
        Schedules.Clear();

        var data = App.CompanyManager?.CompanyData;
        if (data == null)
        {
            OnPropertyChanged(nameof(HasSchedules));
            return;
        }

        foreach (var schedule in data.RecurringTransactions.Where(s => s.Type == _side).OrderBy(s => s.NextDate))
        {
            var template = schedule.Template;
            Schedules.Add(new RecurringDisplayItem
            {
                Id = schedule.Id,
                Description = template?.Description ?? string.Empty,
                TypeLabel = (schedule.Type == CategoryType.Revenue ? "Revenue" : "Expense").Translate(),
                FrequencyLabel = schedule.Frequency.ToString().Translate(),
                AmountFormatted = CurrencyService.Format(template?.Total ?? 0m),
                NextDateFormatted = schedule.NextDate.ToString("MMM d, yyyy"),
                StatusLabel = schedule.Status.ToString().Translate(),
                IsActive = schedule.Status == RecurringTransactionStatus.Active,
                IsExpense = schedule.Type != CategoryType.Revenue
            });
        }

        OnPropertyChanged(nameof(HasSchedules));
    }

    [RelayCommand]
    private void AddSchedule()
    {
        _editingId = null;
        EditorTitle = "New recurring transaction".Translate();
        EditorDescription = string.Empty;
        EditorAmount = string.Empty;
        EditorFrequencyIndex = (int)Frequency.Monthly;
        EditorStartDate = DateTimeOffset.Now;
        EditorEndDate = null;
        EditorError = string.Empty;
        IsEditorOpen = true;
    }

    [RelayCommand]
    private void EditSchedule(RecurringDisplayItem? item)
    {
        var schedule = Find(item);
        if (schedule?.Template == null) return;

        _editingId = schedule.Id;
        EditorTitle = "Edit recurring transaction".Translate();
        EditorDescription = schedule.Template.Description;
        EditorAmount = schedule.Template.Total.ToString("0.##");
        EditorFrequencyIndex = (int)schedule.Frequency;
        EditorStartDate = new DateTimeOffset(schedule.StartDate);
        EditorEndDate = schedule.EndDate == null ? null : new DateTimeOffset(schedule.EndDate.Value);
        EditorError = string.Empty;
        IsEditorOpen = true;
    }

    [RelayCommand]
    private void CloseEditor() => IsEditorOpen = false;

    [RelayCommand]
    private async Task SaveSchedule()
    {
        var data = App.CompanyManager?.CompanyData;
        if (data == null) return;

        if (string.IsNullOrWhiteSpace(EditorDescription))
        {
            EditorError = "Enter a description.".Translate();
            return;
        }

        if (!decimal.TryParse(EditorAmount, out var amount) || amount <= 0)
        {
            EditorError = "Enter an amount greater than zero.".Translate();
            return;
        }

        if (EditorStartDate == null)
        {
            EditorError = "Choose a start date.".Translate();
            return;
        }

        var type = _side;
        var start = EditorStartDate.Value.DateTime.Date;
        var frequency = (Frequency)EditorFrequencyIndex;

        var schedule = _editingId == null ? null : data.RecurringTransactions.FirstOrDefault(s => s.Id == _editingId);
        var isNew = schedule == null;

        if (schedule == null)
        {
            schedule = new RecurringTransaction
            {
                Id = new Core.Data.IdGenerator(data).NextRecurringTransactionId(),
                StartDate = start,
                NextDate = start
            };
            data.RecurringTransactions.Add(schedule);
        }

        var amountChanged = !isNew && schedule.Template != null && schedule.Template.Total != amount;

        schedule.Type = type;
        schedule.Frequency = frequency;
        schedule.StartDate = start;
        schedule.EndDate = EditorEndDate?.DateTime.Date;

        if (isNew)
            schedule.NextDate = start;

        if (type == CategoryType.Revenue)
        {
            schedule.ExpenseTemplate = null;
            schedule.RevenueTemplate = new Revenue
            {
                Description = EditorDescription.Trim(),
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
                Description = EditorDescription.Trim(),
                Amount = amount,
                Total = amount,
                Quantity = 1,
                UnitPrice = amount
            };
        }

        IsEditorOpen = false;
        App.CompanyManager?.MarkAsChanged();
        Load();

        if (amountChanged)
            await OfferRetroactiveCorrection(schedule);
    }

    /// <summary>
    /// A schedule edit changes future occurrences. Occurrences already generated are only touched
    /// when the user says so, and never when they have been matched against a bank line.
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

        var before = correctable.Select(t => (Target: t, t.Amount, t.UnitPrice, t.TaxAmount, t.Total)).ToList();
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
            },
            () => RecurringTransactionService.ApplyTemplateAmounts(schedule, correctable)));

        App.CompanyManager?.MarkAsChanged();
    }

    [RelayCommand]
    private void TogglePause(RecurringDisplayItem? item)
    {
        var schedule = Find(item);
        if (schedule == null || schedule.Status == RecurringTransactionStatus.Completed) return;

        schedule.Status = schedule.Status == RecurringTransactionStatus.Active
            ? RecurringTransactionStatus.Paused
            : RecurringTransactionStatus.Active;

        App.CompanyManager?.MarkAsChanged();
        Load();
    }

    [RelayCommand]
    private void SkipNext(RecurringDisplayItem? item)
    {
        var schedule = Find(item);
        if (schedule == null) return;

        RecurringTransactionService.SkipOccurrence(schedule, schedule.NextDate);
        schedule.NextDate = RecurrenceSchedule.AdvanceDate(
            schedule.NextDate, schedule.Frequency, schedule.StartDate.Day);

        App.CompanyManager?.MarkAsChanged();
        Load();
    }

    [RelayCommand]
    private async Task DeleteSchedule(RecurringDisplayItem? item)
    {
        var data = App.CompanyManager?.CompanyData;
        var dialog = App.ConfirmationDialog;
        var schedule = Find(item);
        if (data == null || dialog == null || schedule == null) return;

        var result = await dialog.ShowAsync(new ConfirmationDialogOptions
        {
            Title = "Delete schedule".Translate(),
            Message = "This stops future entries being generated. Entries it already created stay in your books.".Translate(),
            PrimaryButtonText = "Delete".Translate(),
            CancelButtonText = "Cancel".Translate(),
            IsPrimaryDestructive = true
        });

        if (result != ConfirmationResult.Primary) return;

        data.RecurringTransactions.Remove(schedule);
        App.UndoRedoManager.RecordAction(new DelegateAction(
            $"Delete schedule {schedule.Id}",
            () => { data.RecurringTransactions.Add(schedule); Load(); },
            () => { data.RecurringTransactions.Remove(schedule); Load(); }));

        App.CompanyManager?.MarkAsChanged();
        Load();
    }

    private RecurringTransaction? Find(RecurringDisplayItem? item) =>
        item == null
            ? null
            : App.CompanyManager?.CompanyData?.RecurringTransactions.FirstOrDefault(s => s.Id == item.Id);
}
