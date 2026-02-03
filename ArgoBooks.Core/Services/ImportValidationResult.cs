namespace ArgoBooks.Core.Services;

/// <summary>
/// Severity level for validation issues.
/// </summary>
public enum ValidationIssueSeverity
{
    Warning,
    Error
}

/// <summary>
/// Represents a single validation issue with location information.
/// </summary>
public class ValidationIssue
{
    /// <summary>
    /// The sheet name where the issue was found.
    /// </summary>
    public string SheetName { get; set; } = string.Empty;

    /// <summary>
    /// The row number in the spreadsheet (1-based, includes header row).
    /// </summary>
    public int RowNumber { get; set; }

    /// <summary>
    /// The column name (header) where the issue was found.
    /// </summary>
    public string ColumnName { get; set; } = string.Empty;

    /// <summary>
    /// The invalid value that caused the issue.
    /// </summary>
    public string InvalidValue { get; set; } = string.Empty;

    /// <summary>
    /// The type of reference that is missing or invalid.
    /// </summary>
    public string ReferenceType { get; set; } = string.Empty;

    /// <summary>
    /// A description of the issue.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// The severity of the issue.
    /// </summary>
    public ValidationIssueSeverity Severity { get; set; } = ValidationIssueSeverity.Warning;

    /// <summary>
    /// The ID of the row (if available) for context.
    /// </summary>
    public string RowId { get; set; } = string.Empty;

    /// <summary>
    /// Whether this issue can be automatically fixed by creating the missing reference.
    /// True for missing Suppliers, Customers, Categories, Tax Rates, etc.
    /// False for typos, invalid transaction IDs, or other issues requiring manual fix.
    /// </summary>
    public bool IsAutoFixable { get; set; } = false;
}

/// <summary>
/// Result of validating an import file before importing.
/// </summary>
public class ImportValidationResult
{
    /// <summary>
    /// Whether the import file is valid and can be imported.
    /// </summary>
    public bool IsValid => MissingReferences.Count == 0 && Errors.Count == 0;

    /// <summary>
    /// Whether there are missing references that could be auto-created.
    /// </summary>
    public bool HasMissingReferences => MissingReferences.Count > 0;

    /// <summary>
    /// Whether there are any issues (errors, warnings, or missing references).
    /// </summary>
    public bool HasIssues => Issues.Count > 0 || MissingReferences.Count > 0 || Errors.Count > 0 || Warnings.Count > 0;

    /// <summary>
    /// Critical errors that prevent import.
    /// </summary>
    public List<string> Errors { get; } = [];

    /// <summary>
    /// Warnings that don't prevent import but should be noted.
    /// </summary>
    public List<string> Warnings { get; } = [];

    /// <summary>
    /// Detailed validation issues with location information.
    /// </summary>
    public List<ValidationIssue> Issues { get; } = [];

    /// <summary>
    /// Missing references grouped by type.
    /// </summary>
    public Dictionary<string, HashSet<string>> MissingReferences { get; } = new();

    /// <summary>
    /// Summary of what will be imported (counts by entity type).
    /// </summary>
    public Dictionary<string, ImportSummary> ImportSummaries { get; } = new();

    /// <summary>
    /// Add a missing reference.
    /// </summary>
    public void AddMissingReference(string referenceType, string referenceId)
    {
        if (string.IsNullOrEmpty(referenceId)) return;

        if (!MissingReferences.ContainsKey(referenceType))
            MissingReferences[referenceType] = [];

        MissingReferences[referenceType].Add(referenceId);
    }

    /// <summary>
    /// Add a validation issue with full location information.
    /// </summary>
    public void AddIssue(
        string sheetName,
        int rowNumber,
        string columnName,
        string invalidValue,
        string referenceType,
        string description,
        ValidationIssueSeverity severity = ValidationIssueSeverity.Warning,
        bool isAutoFixable = false,
        string? rowId = null)
    {
        Issues.Add(new ValidationIssue
        {
            SheetName = sheetName,
            RowNumber = rowNumber,
            ColumnName = columnName,
            InvalidValue = invalidValue,
            ReferenceType = referenceType,
            Description = description,
            Severity = severity,
            IsAutoFixable = isAutoFixable,
            RowId = rowId ?? string.Empty
        });

        // Also add to MissingReferences for backward compatibility (only for auto-fixable issues)
        if (isAutoFixable && severity == ValidationIssueSeverity.Warning && !string.IsNullOrEmpty(invalidValue))
        {
            AddMissingReference(referenceType, invalidValue);
        }
    }

    /// <summary>
    /// Get a formatted summary of missing references for display.
    /// </summary>
    public string GetMissingReferencesSummary()
    {
        if (!HasMissingReferences) return string.Empty;

        var lines = new List<string>();
        foreach (var (type, ids) in MissingReferences)
        {
            var count = ids.Count;
            var examples = string.Join(", ", ids.Take(3));
            if (count > 3)
                examples += $", ... (+{count - 3} more)";
            lines.Add($"• {type}: {examples}");
        }
        return string.Join("\n", lines);
    }

    /// <summary>
    /// Get total count of missing references.
    /// </summary>
    public int TotalMissingReferences => MissingReferences.Values.Sum(s => s.Count);

    /// <summary>
    /// Count of issues that can be automatically fixed (missing references that can be created).
    /// </summary>
    public int AutoFixableIssueCount => Issues.Count(i => i.IsAutoFixable);

    /// <summary>
    /// Count of issues that require manual intervention (typos, invalid IDs, etc).
    /// </summary>
    public int NonAutoFixableIssueCount => Issues.Count(i => !i.IsAutoFixable);

    /// <summary>
    /// Whether there are issues that cannot be automatically fixed.
    /// </summary>
    public bool HasNonAutoFixableIssues => Issues.Any(i => !i.IsAutoFixable);

    /// <summary>
    /// Whether all issues can be automatically fixed.
    /// </summary>
    public bool AllIssuesAutoFixable => Issues.All(i => i.IsAutoFixable);

    /// <summary>
    /// Get issues grouped by sheet name for display.
    /// </summary>
    public Dictionary<string, List<ValidationIssue>> GetIssuesBySheet()
    {
        return Issues
            .GroupBy(i => i.SheetName)
            .ToDictionary(g => g.Key, g => g.ToList());
    }
}

/// <summary>
/// Summary of import counts for an entity type.
/// </summary>
public class ImportSummary
{
    public int NewRecords { get; set; }
    public int UpdatedRecords { get; set; }
    public int TotalInFile { get; set; }
}
