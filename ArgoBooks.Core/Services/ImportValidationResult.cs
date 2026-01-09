namespace ArgoBooks.Core.Services;

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
    /// Critical errors that prevent import.
    /// </summary>
    public List<string> Errors { get; } = [];

    /// <summary>
    /// Warnings that don't prevent import but should be noted.
    /// </summary>
    public List<string> Warnings { get; } = [];

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
