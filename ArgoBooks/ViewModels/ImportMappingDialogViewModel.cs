using System.Collections.ObjectModel;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.AI;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// Result of the import mapping review dialog.
/// </summary>
public enum ImportMappingDialogResult
{
    Cancel,
    Accept
}

/// <summary>
/// ViewModel wrapper for a SheetAnalysis, making it observable for the UI.
/// </summary>
public partial class SheetAnalysisViewModel : ObservableObject
{
    private readonly SheetAnalysis _analysis;

    public SheetAnalysisViewModel(SheetAnalysis analysis)
    {
        _analysis = analysis;
        DetectedType = analysis.DetectedType;
        IsIncluded = analysis.IsIncluded;

        foreach (var mapping in analysis.ColumnMappings)
            ColumnMappings.Add(new ColumnMappingViewModel(mapping));

        foreach (var col in analysis.UnmappedSourceColumns)
            UnmappedSourceColumns.Add(col);

        foreach (var col in analysis.UnmappedTargetColumns)
            UnmappedTargetColumns.Add(col);
    }

    public string SourceSheetName => _analysis.SourceSheetName;
    public double Confidence => _analysis.Confidence;
    public ProcessingTier Tier => _analysis.Tier;
    public string TierReason => _analysis.TierReason;
    public int RowCount => _analysis.RowCount;
    public int MappedColumnCount => ColumnMappings.Count;
    public bool IsTier2 => Tier == ProcessingTier.Tier2_LlmProcessing;

    public string ConfidenceDisplay => $"Match: {Confidence:P0}";
    public string RowCountDisplay => $"{RowCount:N0} rows";

    public string TierDisplay => Tier == ProcessingTier.Tier1_Mapping
        ? "Direct Mapping"
        : "AI Processing";

    public string SheetSummary => $"{DetectedType} - {RowCountDisplay}";

    /// <summary>
    /// Confidence color category: high (>0.9), medium (0.7-0.9), low (<0.7).
    /// </summary>
    public string ConfidenceLevel => Confidence switch
    {
        > 0.9 => "High",
        > 0.7 => "Medium",
        _ => "Low"
    };

    [ObservableProperty]
    private SpreadsheetSheetType _detectedType;

    [ObservableProperty]
    private bool _isIncluded;

    [ObservableProperty]
    private bool _isExpanded = true;

    public ObservableCollection<ColumnMappingViewModel> ColumnMappings { get; } = [];
    public ObservableCollection<string> UnmappedSourceColumns { get; } = [];
    public ObservableCollection<string> UnmappedTargetColumns { get; } = [];

    public bool HasUnmappedSource => UnmappedSourceColumns.Count > 0;
    public bool HasUnmappedTarget => UnmappedTargetColumns.Count > 0;

    /// <summary>
    /// Updates the underlying analysis object with any user changes.
    /// </summary>
    public void ApplyChanges()
    {
        _analysis.DetectedType = DetectedType;
        _analysis.IsIncluded = IsIncluded;
    }

    /// <summary>
    /// Available entity types for the dropdown.
    /// </summary>
    public static SpreadsheetSheetType[] AvailableTypes { get; } = Enum.GetValues<SpreadsheetSheetType>();
}

/// <summary>
/// ViewModel wrapper for a ColumnMapping.
/// </summary>
public partial class ColumnMappingViewModel : ObservableObject
{
    public ColumnMappingViewModel(ColumnMapping mapping)
    {
        SourceColumn = mapping.SourceColumn;
        TargetColumn = mapping.TargetColumn;
        Confidence = mapping.Confidence;
        TransformHint = mapping.TransformHint;
    }

    public string SourceColumn { get; }

    [ObservableProperty]
    private string _targetColumn = string.Empty;

    public double Confidence { get; }
    public string? TransformHint { get; }

    public string ConfidenceDisplay => $"Match: {Confidence:P0}";

    public string ConfidenceLevel => Confidence switch
    {
        > 0.9 => "High",
        > 0.7 => "Medium",
        _ => "Low"
    };

    public bool HasTransformHint => !string.IsNullOrEmpty(TransformHint);
}

/// <summary>
/// ViewModel for the Import Mapping Review Dialog.
/// Shows the AI-detected sheet types and column mappings for user review before import.
/// </summary>
public partial class ImportMappingDialogViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isOpen;

    [ObservableProperty]
    private string _fileName = string.Empty;

    [ObservableProperty]
    private int _totalSheets;

    [ObservableProperty]
    private int _totalRows;

    [ObservableProperty]
    private int _totalMappedColumns;

    [ObservableProperty]
    private int _tier1SheetCount;

    [ObservableProperty]
    private int _tier2SheetCount;

    [ObservableProperty]
    private string _rateLimitDisplay = string.Empty;

    [ObservableProperty]
    private bool _showRateLimit;

    public ObservableCollection<SheetAnalysisViewModel> Sheets { get; } = [];

    public ObservableCollection<string> Warnings { get; } = [];

    public bool HasWarnings => Warnings.Count > 0;

    private TaskCompletionSource<ImportMappingDialogResult>? _completionSource;
    private SpreadsheetAnalysisResult? _analysisResult;

    /// <summary>
    /// Shows the dialog with the analysis result for user review.
    /// </summary>
    public Task<ImportMappingDialogResult> ShowAsync(
        SpreadsheetAnalysisResult analysis,
        int remainingImports = -1,
        int maxImports = -1)
    {
        ArgumentNullException.ThrowIfNull(analysis);
        _analysisResult = analysis;

        // Clear previous state
        Sheets.Clear();
        Warnings.Clear();

        FileName = analysis.FileName;

        // Populate sheets
        foreach (var sheet in analysis.Sheets)
        {
            Sheets.Add(new SheetAnalysisViewModel(sheet));
        }

        // Populate warnings
        foreach (var warning in analysis.Warnings)
        {
            Warnings.Add(warning);
        }

        // Calculate summary stats
        TotalSheets = analysis.Sheets.Count;
        TotalRows = analysis.Sheets.Sum(s => s.RowCount);
        TotalMappedColumns = analysis.Sheets.Sum(s => s.ColumnMappings.Count);
        Tier1SheetCount = analysis.Sheets.Count(s => s.Tier == ProcessingTier.Tier1_Mapping);
        Tier2SheetCount = analysis.Sheets.Count(s => s.Tier == ProcessingTier.Tier2_LlmProcessing);

        // Rate limit display
        if (remainingImports >= 0 && maxImports > 0)
        {
            RateLimitDisplay = $"{remainingImports}/{maxImports} remaining this month";
            ShowRateLimit = true;
        }
        else
        {
            ShowRateLimit = false;
        }

        OnPropertyChanged(nameof(HasWarnings));

        IsOpen = true;
        _completionSource = new TaskCompletionSource<ImportMappingDialogResult>();
        return _completionSource.Task;
    }

    /// <summary>
    /// Gets the updated analysis result after user may have made changes.
    /// </summary>
    public SpreadsheetAnalysisResult? GetUpdatedAnalysis()
    {
        if (_analysisResult == null) return null;

        foreach (var sheetVm in Sheets)
        {
            sheetVm.ApplyChanges();
        }

        return _analysisResult;
    }

    [RelayCommand]
    private void Cancel()
    {
        IsOpen = false;
        _completionSource?.TrySetResult(ImportMappingDialogResult.Cancel);
    }

    [RelayCommand]
    private void Accept()
    {
        // Apply any user edits back to the analysis
        foreach (var sheetVm in Sheets)
        {
            sheetVm.ApplyChanges();
        }

        IsOpen = false;
        _completionSource?.TrySetResult(ImportMappingDialogResult.Accept);
    }
}
