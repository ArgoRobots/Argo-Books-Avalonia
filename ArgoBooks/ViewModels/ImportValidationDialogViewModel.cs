using System.Collections.ObjectModel;
using ArgoBooks.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// Result of the import validation dialog.
/// </summary>
public enum ImportValidationDialogResult
{
    Cancel,
    ImportAnyway,
    CreateMissingAndImport
}

/// <summary>
/// Group of validation issues for a single sheet.
/// </summary>
public partial class SheetIssueGroup : ObservableObject
{
    [ObservableProperty]
    private string _sheetName = string.Empty;

    [ObservableProperty]
    private bool _isExpanded = true;

    public ObservableCollection<ValidationIssueViewModel> Issues { get; } = [];

    public int IssueCount => Issues.Count;
}

/// <summary>
/// ViewModel wrapper for a ValidationIssue.
/// </summary>
public partial class ValidationIssueViewModel : ObservableObject
{
    private readonly ValidationIssue _issue;

    public ValidationIssueViewModel(ValidationIssue issue)
    {
        _issue = issue;
    }

    public int RowNumber => _issue.RowNumber;
    public string ColumnName => _issue.ColumnName;
    public string InvalidValue => _issue.InvalidValue;
    public string Description => _issue.Description;
    public string ReferenceType => _issue.ReferenceType;
    public string RowId => _issue.RowId;
    public bool IsError => _issue.Severity == ValidationIssueSeverity.Error;
    public bool IsWarning => _issue.Severity == ValidationIssueSeverity.Warning;
    public bool IsAutoFixable => _issue.IsAutoFixable;

    public string CellReference => $"Row {RowNumber}";
    public string DisplayValue => string.IsNullOrEmpty(InvalidValue) ? "(empty)" : InvalidValue;

    /// <summary>
    /// Gets a description with an indicator if the issue can be auto-fixed.
    /// </summary>
    public string FullDescription => IsAutoFixable
        ? $"{Description} (will be auto-created)"
        : $"{Description} (requires manual fix)";
}

/// <summary>
/// ViewModel for the Import Validation Dialog.
/// </summary>
public partial class ImportValidationDialogViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isOpen;

    [ObservableProperty]
    private string _title = "Import Validation";

    [ObservableProperty]
    private string _summary = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanImport))]
    private bool _hasErrors;

    [ObservableProperty]
    private bool _hasWarnings;

    [ObservableProperty]
    private bool _hasMissingReferences;

    [ObservableProperty]
    private bool _hasIssues;

    [ObservableProperty]
    private int _totalIssues;

    [ObservableProperty]
    private int _totalMissingReferences;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanAutoFix))]
    [NotifyPropertyChangedFor(nameof(CanImport))]
    private bool _hasNonAutoFixableIssues;

    [ObservableProperty]
    private int _autoFixableCount;

    [ObservableProperty]
    private int _nonAutoFixableCount;

    public ObservableCollection<SheetIssueGroup> SheetGroups { get; } = [];

    public ObservableCollection<string> GeneralErrors { get; } = [];

    public ObservableCollection<string> GeneralWarnings { get; } = [];

    private TaskCompletionSource<ImportValidationDialogResult>? _completionSource;

    /// <summary>
    /// Shows the dialog with the validation result.
    /// </summary>
    public Task<ImportValidationDialogResult> ShowAsync(ImportValidationResult validationResult)
    {
        ArgumentNullException.ThrowIfNull(validationResult);

        // Clear previous state
        SheetGroups.Clear();
        GeneralErrors.Clear();
        GeneralWarnings.Clear();

        // Populate general errors
        foreach (var error in validationResult.Errors)
        {
            GeneralErrors.Add(error);
        }

        // Populate general warnings (legacy)
        foreach (var warning in validationResult.Warnings)
        {
            GeneralWarnings.Add(warning);
        }

        // Group issues by sheet
        var issuesBySheet = validationResult.GetIssuesBySheet();
        foreach (var (sheetName, issues) in issuesBySheet.OrderBy(g => g.Key))
        {
            var group = new SheetIssueGroup { SheetName = sheetName };
            foreach (var issue in issues.OrderBy(i => i.RowNumber))
            {
                group.Issues.Add(new ValidationIssueViewModel(issue));
            }
            SheetGroups.Add(group);
        }

        // Update summary
        HasErrors = validationResult.Errors.Count > 0;
        HasWarnings = validationResult.Warnings.Count > 0 || validationResult.Issues.Any(i => i.Severity == ValidationIssueSeverity.Warning);
        HasMissingReferences = validationResult.HasMissingReferences;
        TotalIssues = validationResult.Issues.Count + validationResult.Errors.Count + validationResult.Warnings.Count;
        TotalMissingReferences = validationResult.TotalMissingReferences;
        HasIssues = TotalIssues > 0;
        HasNonAutoFixableIssues = validationResult.HasNonAutoFixableIssues;
        AutoFixableCount = validationResult.AutoFixableIssueCount;
        NonAutoFixableCount = validationResult.NonAutoFixableIssueCount;

        // Build summary message
        var summaryParts = new List<string>();
        if (HasErrors)
        {
            summaryParts.Add($"{validationResult.Errors.Count} error(s) that prevent import");
        }
        if (NonAutoFixableCount > 0)
        {
            summaryParts.Add($"{NonAutoFixableCount} issue(s) that require fixing the spreadsheet");
        }
        if (AutoFixableCount > 0)
        {
            summaryParts.Add($"{AutoFixableCount} missing reference(s) that can be auto-created");
        }
        Summary = summaryParts.Count > 0
            ? string.Join(", ", summaryParts) + "."
            : "No issues found.";

        Title = HasErrors
            ? "Import Cannot Continue"
            : HasNonAutoFixableIssues
                ? "Spreadsheet Needs Fixing"
                : TotalIssues > 0
                    ? "Import Issues Found"
                    : "Ready to Import";

        IsOpen = true;
        _completionSource = new TaskCompletionSource<ImportValidationDialogResult>();
        return _completionSource.Task;
    }

    [RelayCommand]
    private void Cancel()
    {
        IsOpen = false;
        _completionSource?.TrySetResult(ImportValidationDialogResult.Cancel);
    }

    [RelayCommand]
    private void ImportAnyway()
    {
        IsOpen = false;
        _completionSource?.TrySetResult(ImportValidationDialogResult.ImportAnyway);
    }

    [RelayCommand]
    private void CreateMissingAndImport()
    {
        IsOpen = false;
        _completionSource?.TrySetResult(ImportValidationDialogResult.CreateMissingAndImport);
    }

    /// <summary>
    /// Gets whether the import can proceed with auto-fix (no critical errors and all issues are auto-fixable).
    /// </summary>
    public bool CanAutoFix => !HasErrors && !HasNonAutoFixableIssues;

    /// <summary>
    /// Gets whether the import can proceed (no critical errors).
    /// When there are non-auto-fixable issues, the user will need to fix the spreadsheet first.
    /// </summary>
    public bool CanImport => !HasErrors && !HasNonAutoFixableIssues;
}
