using System.Globalization;
using ClosedXML.Excel;

namespace ArgoBooks.Core.Services.Layout;

/// <summary>
/// A coordinate-grid snapshot of a worksheet: cell strings, merged ranges, and a
/// per-row shape profile. All coordinates are <b>1-based</b>, matching ClosedXML's
/// native row/column numbering. This class is pure and deterministic — it reads the
/// worksheet once at construction time and holds no references back to ClosedXML.
/// </summary>
public sealed class SheetGrid
{
    /// <summary>
    /// String value of every cell over the worksheet's used range, in row-major order.
    /// <c>Cells[rowIndex][colIndex]</c> is 0-based into this list; to convert a 1-based
    /// ClosedXML row/column pair <c>(r, c)</c> use <c>Cells[r - 1][c - 1]</c>.
    /// Blank cells are represented as <see cref="string.Empty"/>.
    /// </summary>
    public IReadOnlyList<IReadOnlyList<string>> Cells { get; }

    /// <summary>
    /// All merged ranges in the worksheet.
    /// Coordinates are <b>1-based</b> to match ClosedXML convention.
    /// </summary>
    public IReadOnlyList<MergedRange> MergedRanges { get; }

    /// <summary>
    /// One <see cref="RowShape"/> per row, in the same row order as <see cref="Cells"/>.
    /// </summary>
    public IReadOnlyList<RowShape> Shapes { get; }

    /// <summary>Number of rows in the used range (0 when the sheet is empty).</summary>
    public int RowCount => Cells.Count;

    /// <summary>Number of columns in the used range (0 when the sheet is empty).</summary>
    public int ColCount => Cells.Count > 0 ? Cells[0].Count : 0;

    private SheetGrid(
        IReadOnlyList<IReadOnlyList<string>> cells,
        IReadOnlyList<MergedRange> mergedRanges,
        IReadOnlyList<RowShape> shapes)
    {
        Cells = cells;
        MergedRanges = mergedRanges;
        Shapes = shapes;
    }

    /// <summary>
    /// Builds a <see cref="SheetGrid"/> from the given worksheet.
    /// Reads the worksheet's used range and captures cell values, merged ranges,
    /// and per-row shape profiles. Returns an empty grid if the sheet is blank.
    /// </summary>
    public static SheetGrid FromWorksheet(IXLWorksheet worksheet)
    {
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
        var lastCol = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;

        if (lastRow == 0 || lastCol == 0)
            return new SheetGrid([], [], []);

        // Build the cell matrix (0-based indexing into a 1-based coordinate grid).
        var cells = new List<IReadOnlyList<string>>(lastRow);
        var shapes = new List<RowShape>(lastRow);

        for (int r = 1; r <= lastRow; r++)
        {
            var xlRow = worksheet.Row(r);
            var rowCells = new string[lastCol];

            int nonEmpty = 0;
            int numericOrDate = 0;

            for (int c = 1; c <= lastCol; c++)
            {
                var cell = xlRow.Cell(c);
                var val = ReadCellString(cell);
                rowCells[c - 1] = val;

                if (val.Length > 0)
                {
                    nonEmpty++;
                    if (IsNumericOrDate(cell, val))
                        numericOrDate++;
                }
            }

            cells.Add(rowCells);

            // Fractions are over non-empty cells; an all-empty row yields 0/0/0.
            double numericFraction = nonEmpty > 0 ? (double)numericOrDate / nonEmpty : 0.0;
            double textFraction = nonEmpty > 0 ? (double)(nonEmpty - numericOrDate) / nonEmpty : 0.0;
            shapes.Add(new RowShape(nonEmpty, numericFraction, textFraction));
        }

        // Capture merged ranges (1-based coordinates from ClosedXML).
        var mergedRanges = new List<MergedRange>();
        foreach (var merged in worksheet.MergedRanges)
        {
            mergedRanges.Add(new MergedRange(
                merged.FirstRow().RowNumber(),
                merged.FirstColumn().ColumnNumber(),
                merged.LastRow().RowNumber(),
                merged.LastColumn().ColumnNumber()));
        }

        return new SheetGrid(cells, mergedRanges, shapes);
    }

    /// <summary>
    /// Returns the cell's string representation using the same convention as the
    /// spreadsheet importer: DateTimes are formatted as ISO 8601, numbers as
    /// invariant-culture strings, booleans as "True"/"False", all others via
    /// <see cref="IXLCell.GetString()"/>. Blank cells return <see cref="string.Empty"/>.
    /// </summary>
    private static string ReadCellString(IXLCell cell)
    {
        if (cell.IsEmpty()) return "";
        return cell.DataType switch
        {
            XLDataType.DateTime => cell.GetDateTime().TimeOfDay == TimeSpan.Zero
                ? cell.GetDateTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                : cell.GetDateTime().ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture),
            XLDataType.Number => cell.GetDouble().ToString(CultureInfo.InvariantCulture),
            XLDataType.Boolean => cell.GetBoolean().ToString(),
            _ => cell.GetString()
        };
    }

    /// <summary>
    /// Returns <c>true</c> when the cell holds numeric or date/time data.
    /// <para>
    /// Design decision: dates count as "numeric" for <see cref="RowShape.NumericFraction"/>.
    /// Both numbers and dates indicate a structured data column (as opposed to a free-text
    /// label), which is the signal the layout gate needs to distinguish header/preamble rows
    /// from data rows. Treating dates as text would incorrectly lower the numeric fraction
    /// of date-heavy data rows.
    /// </para>
    /// </summary>
    private static bool IsNumericOrDate(IXLCell cell, string cellString)
    {
        // Defer to the ClosedXML data type first (most reliable).
        if (cell.DataType is XLDataType.Number or XLDataType.DateTime)
            return true;

        // For text-typed cells that still look like numbers (e.g. numbers stored as text),
        // try parsing as a double with invariant culture.
        return double.TryParse(cellString, NumberStyles.Any, CultureInfo.InvariantCulture, out _);
    }
}

/// <summary>
/// Describes a merged cell region in a worksheet.
/// All coordinates are <b>1-based</b>, matching ClosedXML's native convention.
/// </summary>
/// <param name="FirstRow">1-based row number of the top-left corner.</param>
/// <param name="FirstCol">1-based column number of the top-left corner.</param>
/// <param name="LastRow">1-based row number of the bottom-right corner.</param>
/// <param name="LastCol">1-based column number of the bottom-right corner.</param>
public sealed record MergedRange(int FirstRow, int FirstCol, int LastRow, int LastCol);

/// <summary>
/// Shape profile for a single row in the worksheet, summarising how many cells are
/// non-blank and what fraction carry numeric/date vs. text data.
/// </summary>
/// <param name="NonEmpty">Count of non-blank cells in the row.</param>
/// <param name="NumericFraction">
/// Fraction of <paramref name="NonEmpty"/> cells whose value is numeric or a date.
/// Dates count as numeric because both signal a structured data column rather than a
/// free-text label. 0.0 when <paramref name="NonEmpty"/> is 0.
/// </param>
/// <param name="TextFraction">
/// Fraction of <paramref name="NonEmpty"/> cells whose value is non-numeric (text).
/// Equal to <c>1 - NumericFraction</c> when <paramref name="NonEmpty"/> &gt; 0,
/// otherwise 0.0. 0.0 when <paramref name="NonEmpty"/> is 0.
/// </param>
public sealed record RowShape(int NonEmpty, double NumericFraction, double TextFraction);
