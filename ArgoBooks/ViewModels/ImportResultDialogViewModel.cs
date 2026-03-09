using System.Collections.ObjectModel;
using ArgoBooks.Core.Services;
using ArgoBooks.Localization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// Represents a single sheet's import result for display.
/// </summary>
public class SheetResultItem
{
    public required string DisplayLabel { get; init; }
    public int Inserted { get; init; }
    public int Updated { get; init; }
    public int Skipped { get; init; }

    public string Summary
    {
        get
        {
            var parts = new List<string>();
            if (Inserted > 0) parts.Add($"{Inserted:N0} {"new".Translate()}");
            if (Updated > 0) parts.Add($"{Updated:N0} {"updated".Translate()}");
            if (Skipped > 0) parts.Add($"{Skipped:N0} {"skipped".Translate()}");
            return string.Join(", ", parts);
        }
    }
}

/// <summary>
/// ViewModel for the Import Result Dialog.
/// </summary>
public partial class ImportResultDialogViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isOpen;

    [ObservableProperty]
    private string _fileName = string.Empty;

    [ObservableProperty]
    private bool _isSuccess;

    [ObservableProperty]
    private int _totalNew;

    [ObservableProperty]
    private int _totalUpdated;

    [ObservableProperty]
    private int _totalSkipped;

    [ObservableProperty]
    private bool _hasUpdated;

    [ObservableProperty]
    private bool _hasSkipped;

    [ObservableProperty]
    private bool _hasMultipleSheets;

    [ObservableProperty]
    private bool _hasSkipReasons;

    [ObservableProperty]
    private bool _hasWarnings;

    [ObservableProperty]
    private bool _needsSave;

    public ObservableCollection<SheetResultItem> SheetResults { get; } = [];
    public ObservableCollection<string> SkipReasons { get; } = [];
    public ObservableCollection<string> Warnings { get; } = [];

    private TaskCompletionSource? _completionSource;

    public Task ShowAsync(
        string fileName,
        List<SheetImportResult> sheetResults,
        int totalNew,
        int totalUpdated,
        int totalSkipped,
        List<string> skipReasons,
        List<string> warnings,
        bool needsSave)
    {
        // Clear previous state
        SheetResults.Clear();
        SkipReasons.Clear();
        Warnings.Clear();

        FileName = fileName;
        TotalNew = totalNew;
        TotalUpdated = totalUpdated;
        TotalSkipped = totalSkipped;
        HasUpdated = totalUpdated > 0;
        HasSkipped = totalSkipped > 0;
        NeedsSave = needsSave;
        IsSuccess = totalSkipped == 0 && warnings.Count == 0;

        // Build per-sheet results
        foreach (var sr in sheetResults)
        {
            if (sr.Inserted == 0 && sr.Updated == 0 && sr.Skipped == 0) continue;

            var label = string.Equals(sr.SheetName, sr.EntityType, StringComparison.OrdinalIgnoreCase)
                ? sr.SheetName
                : $"{sr.SheetName} \u2192 {sr.EntityType}";

            SheetResults.Add(new SheetResultItem
            {
                DisplayLabel = label,
                Inserted = sr.Inserted,
                Updated = sr.Updated,
                Skipped = sr.Skipped
            });
        }
        HasMultipleSheets = SheetResults.Count > 1;

        // Skip reasons (capped at 10)
        foreach (var reason in skipReasons.Take(10))
            SkipReasons.Add(reason);
        if (skipReasons.Count > 10)
            SkipReasons.Add("and {0} more".TranslateFormat(skipReasons.Count - 10) + "...");
        HasSkipReasons = SkipReasons.Count > 0;

        // Warnings
        foreach (var warning in warnings)
            Warnings.Add(warning);
        HasWarnings = Warnings.Count > 0;

        IsOpen = true;
        _completionSource = new TaskCompletionSource();
        return _completionSource.Task;
    }

    [RelayCommand]
    private void Close()
    {
        IsOpen = false;
        _completionSource?.TrySetResult();
    }
}
