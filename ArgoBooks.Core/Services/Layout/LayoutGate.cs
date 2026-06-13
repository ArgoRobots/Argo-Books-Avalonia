namespace ArgoBooks.Core.Services.Layout;

/// <summary>
/// A cheap, local heuristic that decides whether a worksheet's layout is complex enough
/// to require AI interpretation. This is a pure static computation over <see cref="SheetGrid"/>
/// data — it never calls an LLM, performs I/O, or has any side effects.
///
/// <para>
/// Contract: when <see cref="NeedsInterpretation"/> returns <see langword="false"/>, the
/// caller can safely use the existing fast heuristic import path at zero added cost or
/// latency. When it returns <see langword="true"/>, the sheet should be forwarded to the
/// AI layout interpreter before extraction.
/// </para>
/// </summary>
public static class LayoutGate
{
    // ─── Thresholds (tunable) ────────────────────────────────────────────────

    /// <summary>
    /// Merged ranges whose first row is at or before this 1-based row number signal
    /// a non-standard multi-row or titled header structure.
    /// </summary>
    private const int MergeCheckRows = 3;

    /// <summary>
    /// A row is considered "dense" (a real header or data row) when it has at least
    /// this many non-empty cells. Sparse rows (titles, notes, blank rows) have fewer.
    /// </summary>
    private const int DenseMinNonEmpty = 2;

    /// <summary>
    /// If the first dense row does not appear within this many rows from the top
    /// (0-based), the sheet is considered to have a long preamble.
    /// A value of 2 means rows 0, 1, 2 are all checked; if none is dense, it's a preamble.
    /// </summary>
    private const int MaxPreambleRows = 2;

    /// <summary>
    /// If the first dense row has a <see cref="RowShape.NumericFraction"/> at or above
    /// this threshold, it is treated as a data/cross-tab row rather than a text header.
    /// At 0.6 a five-column row with 3+ numeric cells (e.g. year columns) triggers AI.
    /// </summary>
    private const double NumericHeaderThreshold = 0.6;

    /// <summary>
    /// When the spread (max minus min) of the NonEmpty counts across all populated rows
    /// exceeds this fraction of the median NonEmpty, the row widths are considered
    /// too inconsistent to be a single clean table (ragged/stacked-table signal).
    /// A value of 0.5 means the spread must be no more than 50% of the median.
    /// </summary>
    private const double RaggedSpreadFraction = 0.5;

    /// <summary>
    /// Minimum number of populated rows required before the ragged-row check fires.
    /// With only 1-2 rows there is not enough data to assess consistency.
    /// </summary>
    private const int RaggedMinRows = 3;

    // ─── Public API ──────────────────────────────────────────────────────────

    /// <summary>
    /// Returns <see langword="true"/> when the sheet has characteristics that suggest a
    /// non-standard layout (merged headers, long preamble, numeric-looking header row,
    /// or ragged/inconsistent row widths) and therefore needs AI interpretation.
    ///
    /// Returns <see langword="false"/> for a clean, single-header tabular sheet where
    /// the existing fast-path heuristic is sufficient.
    /// </summary>
    /// <param name="grid">The pre-built grid snapshot of the worksheet.</param>
    public static bool NeedsInterpretation(SheetGrid grid)
    {
        if (grid.RowCount == 0)
            return false;

        // Rule 1: any merged range starting in the top few rows signals a non-trivial header.
        if (HasTopMergedRanges(grid))
            return true;

        // Rule 2: the first dense row (real header or data) does not appear near the top.
        int firstDenseRow = FindFirstDenseRow(grid);
        if (firstDenseRow < 0 || firstDenseRow > MaxPreambleRows)
            return true;

        // Rule 3: the first dense row is mostly numeric (cross-tab / numbers-as-headers).
        if (grid.Shapes[firstDenseRow].NumericFraction >= NumericHeaderThreshold)
            return true;

        // Rule 4: populated rows vary widely in width (ragged / stacked tables).
        if (HasRaggedRows(grid))
            return true;

        return false;
    }

    // ─── Rule implementations ────────────────────────────────────────────────

    /// <summary>
    /// Returns <see langword="true"/> if any merged range has its first row within the
    /// top <see cref="MergeCheckRows"/> rows (1-based).
    /// </summary>
    private static bool HasTopMergedRanges(SheetGrid grid)
    {
        foreach (var merge in grid.MergedRanges)
        {
            if (merge.FirstRow <= MergeCheckRows)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Returns the 0-based index of the first row that qualifies as "dense"
    /// (has at least <see cref="DenseMinNonEmpty"/> non-empty cells).
    /// Returns -1 when no such row exists.
    /// </summary>
    private static int FindFirstDenseRow(SheetGrid grid)
    {
        for (int i = 0; i < grid.RowCount; i++)
        {
            if (grid.Shapes[i].NonEmpty >= DenseMinNonEmpty)
                return i;
        }
        return -1;
    }

    /// <summary>
    /// Returns <see langword="true"/> when the spread (max - min) of <see cref="RowShape.NonEmpty"/>
    /// counts across all populated rows exceeds <see cref="RaggedSpreadFraction"/> times
    /// the median, indicating an inconsistent/ragged table structure.
    /// Requires at least <see cref="RaggedMinRows"/> populated rows to fire.
    /// </summary>
    private static bool HasRaggedRows(SheetGrid grid)
    {
        // Collect NonEmpty counts for all populated (non-blank) rows.
        var counts = new List<int>(grid.RowCount);
        foreach (var shape in grid.Shapes)
        {
            if (shape.NonEmpty > 0)
                counts.Add(shape.NonEmpty);
        }

        if (counts.Count < RaggedMinRows)
            return false;

        counts.Sort();
        int min = counts[0];
        int max = counts[counts.Count - 1];
        double spread = max - min;

        // Median of a sorted list.
        int mid = counts.Count / 2;
        double median = counts.Count % 2 == 0
            ? (counts[mid - 1] + counts[mid]) / 2.0
            : counts[mid];

        if (median <= 0)
            return false;

        return spread / median > RaggedSpreadFraction;
    }
}
