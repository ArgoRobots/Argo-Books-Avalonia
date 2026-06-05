using System.Collections.ObjectModel;
using System.Globalization;
using ArgoBooks.Core;
using ArgoBooks.Localization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>One dated record feeding a month overview, with whether it is reconciled (matched or ignored).</summary>
public readonly record struct MonthMatchItem(DateTime Date, bool IsResolved);

/// <summary>
/// Reusable month-by-month match overview. Buckets dated items by month of <see cref="SelectedYear"/>
/// and colors each month green (all resolved), amber (some), red (none) or grey (no items). Used for
/// both the bank-lines side and the books (missing-from-statement) side of the bank matching page.
/// </summary>
public partial class MonthlyMatchCalendar : ObservableObject
{
    private IReadOnlyList<MonthMatchItem> _items = [];
    private int _minYear;
    private int _maxYear;

    /// <param name="emptyLabel">Status text for months with no items (e.g. "No bank lines" or "No records").</param>
    public MonthlyMatchCalendar(string emptyLabel) => EmptyLabel = emptyLabel;

    /// <summary>Label shown on (and in the legend for) months that have nothing to reconcile.</summary>
    public string EmptyLabel { get; }

    public ObservableCollection<MonthStatusCell> MonthCells { get; } = [];

    [ObservableProperty]
    private int _selectedYear;

    [ObservableProperty]
    private bool _canGoPreviousYear;

    [ObservableProperty]
    private bool _canGoNextYear;

    partial void OnSelectedYearChanged(int value)
    {
        BuildMonthCells();
        UpdateYearNav();
    }

    /// <summary>Replaces the data set and rebuilds, keeping the selected year within the data range.</summary>
    public void SetItems(IReadOnlyList<MonthMatchItem> items)
    {
        _items = items;

        // Ignore default/MinValue dates (year 1), which represent undated records.
        var years = items.Select(i => i.Date.Year).Where(y => y > 1).ToList();
        var currentYear = DateTime.Today.Year;
        int defaultYear;
        if (years.Count == 0)
        {
            _minYear = _maxYear = defaultYear = currentYear;
        }
        else
        {
            _minYear = years.Min();
            defaultYear = years.Max();
            // Always let the user reach the current year, even when no items fall in it yet.
            _maxYear = Math.Max(defaultYear, currentYear);
        }

        // Default to the most recent year with data on first load; otherwise keep the user's year in range.
        SelectedYear = SelectedYear == 0
            ? defaultYear
            : Math.Clamp(SelectedYear, _minYear, _maxYear);

        BuildMonthCells();
        UpdateYearNav();
    }

    private void BuildMonthCells()
    {
        MonthCells.Clear();
        for (var month = 1; month <= 12; month++)
        {
            var inMonth = _items
                .Where(i => i.Date.Year == SelectedYear && i.Date.Month == month)
                .ToList();
            MonthCells.Add(MonthStatusCell.For(month, inMonth.Count, inMonth.Count(i => i.IsResolved), EmptyLabel));
        }
    }

    private void UpdateYearNav()
    {
        CanGoPreviousYear = SelectedYear > _minYear;
        CanGoNextYear = SelectedYear < _maxYear;
    }

    [RelayCommand]
    private void PreviousYear()
    {
        if (SelectedYear > _minYear) SelectedYear--;
    }

    [RelayCommand]
    private void NextYear()
    {
        if (SelectedYear < _maxYear) SelectedYear++;
    }
}

/// <summary>
/// One month tile in a <see cref="MonthlyMatchCalendar"/>. A month is fully matched only when every
/// item in it is resolved; partially matched when some are; not matched when none are.
/// </summary>
public sealed class MonthStatusCell
{
    public required string MonthLabel { get; init; }
    public required string StatusLabel { get; init; }
    public required string CountDisplay { get; init; }

    /// <summary>Solid status color for the dot. Theme-independent semantic color.</summary>
    public required string AccentColor { get; init; }

    /// <summary>
    /// Semi-transparent tint of the accent for the tile background. Because it carries an alpha
    /// channel it composites over both the light and dark page background, so no per-theme color is needed.
    /// </summary>
    public required string FillColor { get; init; }

    public required bool HasData { get; init; }

    public static MonthStatusCell For(int month, int total, int resolved, string emptyLabel)
    {
        var label = CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(month);

        if (total == 0)
        {
            return new MonthStatusCell
            {
                MonthLabel = label,
                StatusLabel = emptyLabel,
                CountDisplay = string.Empty,
                AccentColor = AppColors.GrayMedium,
                FillColor = Tint(AppColors.GrayMedium, "14"), // ~8% opacity
                HasData = false
            };
        }

        // Three data states: all resolved (green), some but not all (amber), none resolved (red).
        var (statusLabel, accent) = resolved == total
            ? ("Fully matched", AppColors.Success)
            : resolved == 0
                ? ("Not matched", AppColors.Error)
                : ("Partially matched", AppColors.Warning);

        return new MonthStatusCell
        {
            MonthLabel = label,
            StatusLabel = statusLabel.Translate(),
            CountDisplay = $"{resolved}/{total}",
            AccentColor = accent,
            FillColor = Tint(accent, "33"), // ~20% opacity
            HasData = true
        };
    }

    /// <summary>Prefixes a "#RRGGBB" color with an alpha byte to make a "#AARRGGBB" tint.</summary>
    private static string Tint(string hex, string alpha) => $"#{alpha}{hex.TrimStart('#')}";
}
