using ArgoBooks.Core.Services.Layout;
using ClosedXML.Excel;
using Xunit;

namespace ArgoBooks.Tests.Importer.Layout;

/// <summary>
/// Tests for <see cref="GridExtractor.Extract"/>.
///
/// Conventions pinned by these tests:
///   - <b>Multi-row header join</b>: header rows are concatenated top-to-bottom with " &gt; ",
///     skipping empty parts, then trimmed. A horizontally merged parent header (e.g. "2024"
///     spanning Q1..Q4) contributes its value to every spanned column.
///   - <b>Wide / cross-tab transpose</b>: output headers are
///     [&lt;key column headers...&gt;, "Column", "Value"]. One output row per (data row x spread
///     column). Empty spread cells emit no row.
///
/// Coordinate convention: TableRegion indices are 0-based into SheetGrid.Cells.
/// SheetGrid.MergedRanges are 1-based (ClosedXML native).
/// </summary>
public class GridExtractorTests
{
    // ─── Test 1: Long table with a 3-row preamble ────────────────────────────

    /// <summary>
    ///   Row 1: "Annual Report" (title — preamble)
    ///   Row 2: (blank — preamble)
    ///   Row 3: "Note: figures in USD" (preamble)
    ///   Row 4: "Date", "Amount", "Desc"      (header row)
    ///   Row 5: 2024-01-15, 100.50, "Coffee"  (data row 1)
    ///   Row 6: 2024-02-20, 250.00, "Office"  (data row 2)
    ///   Row 7: 2024-03-10, 12.00,  "Tea"     (data row 3)
    /// </summary>
    private static IXLWorksheet BuildLongSheet()
    {
        var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Sheet1");

        ws.Cell(1, 1).Value = "Annual Report";
        // Row 2 blank
        ws.Cell(3, 1).Value = "Note: figures in USD";

        ws.Cell(4, 1).Value = "Date";
        ws.Cell(4, 2).Value = "Amount";
        ws.Cell(4, 3).Value = "Desc";

        ws.Cell(5, 1).Value = new DateTime(2024, 1, 15);
        ws.Cell(5, 2).Value = 100.50;
        ws.Cell(5, 3).Value = "Coffee";

        ws.Cell(6, 1).Value = new DateTime(2024, 2, 20);
        ws.Cell(6, 2).Value = 250.00;
        ws.Cell(6, 3).Value = "Office";

        ws.Cell(7, 1).Value = new DateTime(2024, 3, 10);
        ws.Cell(7, 2).Value = 12.00;
        ws.Cell(7, 3).Value = "Tea";

        return ws;
    }

    [Fact]
    public void Extract_LongWithPreamble_HeadersAndRowsExcludePreamble()
    {
        var grid = SheetGrid.FromWorksheet(BuildLongSheet());

        var region = new TableRegion
        {
            HeaderRows = [3],      // 0-based: worksheet row 4
            FirstDataRow = 4,      // 0-based: worksheet row 5
            LastDataRow = 6,       // 0-based: worksheet row 7
            FirstCol = 0,
            LastCol = 2,
            Orientation = "long",
        };

        var (headers, rows) = GridExtractor.Extract(grid, region);

        Assert.Equal(new[] { "Date", "Amount", "Desc" }, headers);

        Assert.Equal(3, rows.Count);
        Assert.Equal(new[] { "2024-01-15", "100.5", "Coffee" }, rows[0]);
        Assert.Equal(new[] { "2024-02-20", "250", "Office" }, rows[1]);
        Assert.Equal(new[] { "2024-03-10", "12", "Tea" }, rows[2]);
    }

    [Fact]
    public void Extract_LongWithIgnoreRows_SkipsSubtotalRow()
    {
        var grid = SheetGrid.FromWorksheet(BuildLongSheet());

        var region = new TableRegion
        {
            HeaderRows = [3],
            FirstDataRow = 4,
            LastDataRow = 6,
            FirstCol = 0,
            LastCol = 2,
            Orientation = "long",
            IgnoreRows = [5], // skip worksheet row 6 (0-based 5)
        };

        var (_, rows) = GridExtractor.Extract(grid, region);

        Assert.Equal(2, rows.Count);
        Assert.Equal("Coffee", rows[0][2]);
        Assert.Equal("Tea", rows[1][2]);
    }

    // ─── Test 2: Two-row merged header ───────────────────────────────────────

    /// <summary>
    ///   Row 1: "Region" | "2024"(merged B1:C1) |
    ///   Row 2: ""       | "Q1"  | "Q2"
    ///   Row 3: "North"  | 10    | 20   (data)
    ///   Row 4: "South"  | 30    | 40   (data)
    ///
    /// Pinned convention: a merged parent ("2024" over B..C) applies to both spanned columns,
    /// producing "2024 &gt; Q1" and "2024 &gt; Q2". The non-merged "Region" column has only a
    /// value in row 1; row 2 is empty there, so the empty part is skipped -&gt; "Region".
    /// </summary>
    private static IXLWorksheet BuildMergedHeaderSheet()
    {
        var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Sheet1");

        ws.Cell(1, 1).Value = "Region";
        ws.Cell(1, 2).Value = "2024";
        ws.Range("B1:C1").Merge();

        ws.Cell(2, 2).Value = "Q1";
        ws.Cell(2, 3).Value = "Q2";

        ws.Cell(3, 1).Value = "North";
        ws.Cell(3, 2).Value = 10;
        ws.Cell(3, 3).Value = 20;

        ws.Cell(4, 1).Value = "South";
        ws.Cell(4, 2).Value = 30;
        ws.Cell(4, 3).Value = 40;

        return ws;
    }

    [Fact]
    public void Extract_TwoRowMergedHeader_ConcatenatesParentChild()
    {
        var grid = SheetGrid.FromWorksheet(BuildMergedHeaderSheet());

        var region = new TableRegion
        {
            HeaderRows = [0, 1],   // 0-based rows 1 and 2
            FirstDataRow = 2,      // 0-based row 3
            LastDataRow = 3,       // 0-based row 4
            FirstCol = 0,
            LastCol = 2,
            Orientation = "long",
        };

        var (headers, rows) = GridExtractor.Extract(grid, region);

        Assert.Equal(new[] { "Region", "2024 > Q1", "2024 > Q2" }, headers);

        Assert.Equal(2, rows.Count);
        Assert.Equal(new[] { "North", "10", "20" }, rows[0]);
        Assert.Equal(new[] { "South", "30", "40" }, rows[1]);
    }

    // ─── Test 3: Cross-tab transpose ─────────────────────────────────────────

    /// <summary>
    ///   Row 1: "Product" | "Jan" | "Feb"   (header)
    ///   Row 2: "Widget"  | 10    | 20      (data)
    ///   Row 3: "Gadget"  | 5     | 8       (data)
    ///
    /// Wide orientation with KeyColumns=[0] (Product). Spread columns are Jan and Feb.
    /// </summary>
    private static IXLWorksheet BuildCrossTabSheet()
    {
        var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Sheet1");

        ws.Cell(1, 1).Value = "Product";
        ws.Cell(1, 2).Value = "Jan";
        ws.Cell(1, 3).Value = "Feb";

        ws.Cell(2, 1).Value = "Widget";
        ws.Cell(2, 2).Value = 10;
        ws.Cell(2, 3).Value = 20;

        ws.Cell(3, 1).Value = "Gadget";
        ws.Cell(3, 2).Value = 5;
        ws.Cell(3, 3).Value = 8;

        return ws;
    }

    [Fact]
    public void Extract_CrossTab_TransposesToLong()
    {
        var grid = SheetGrid.FromWorksheet(BuildCrossTabSheet());

        var region = new TableRegion
        {
            HeaderRows = [0],
            FirstDataRow = 1,
            LastDataRow = 2,
            FirstCol = 0,
            LastCol = 2,
            Orientation = "wide",
            KeyColumns = [0],
        };

        var (headers, rows) = GridExtractor.Extract(grid, region);

        Assert.Equal(new[] { "Product", "Column", "Value" }, headers);

        Assert.Equal(4, rows.Count);
        Assert.Equal(new[] { "Widget", "Jan", "10" }, rows[0]);
        Assert.Equal(new[] { "Widget", "Feb", "20" }, rows[1]);
        Assert.Equal(new[] { "Gadget", "Jan", "5" }, rows[2]);
        Assert.Equal(new[] { "Gadget", "Feb", "8" }, rows[3]);
    }

    [Fact]
    public void Extract_CrossTab_SkipsEmptySpreadCells()
    {
        var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Sheet1");
        ws.Cell(1, 1).Value = "Product";
        ws.Cell(1, 2).Value = "Jan";
        ws.Cell(1, 3).Value = "Feb";
        ws.Cell(2, 1).Value = "Widget";
        ws.Cell(2, 2).Value = 10;
        // Feb for Widget left blank
        ws.Cell(3, 1).Value = "Gadget";
        ws.Cell(3, 2).Value = 5;
        ws.Cell(3, 3).Value = 8;

        var grid = SheetGrid.FromWorksheet(ws);

        var region = new TableRegion
        {
            HeaderRows = [0],
            FirstDataRow = 1,
            LastDataRow = 2,
            FirstCol = 0,
            LastCol = 2,
            Orientation = "wide",
            KeyColumns = [0],
        };

        var (_, rows) = GridExtractor.Extract(grid, region);

        // Widget/Feb is empty => skipped. 3 rows remain.
        Assert.Equal(3, rows.Count);
        Assert.Equal(new[] { "Widget", "Jan", "10" }, rows[0]);
        Assert.Equal(new[] { "Gadget", "Jan", "5" }, rows[1]);
        Assert.Equal(new[] { "Gadget", "Feb", "8" }, rows[2]);
    }

    // ─── Robustness ──────────────────────────────────────────────────────────

    [Fact]
    public void Extract_LongSkipsFullyEmptyDataRows()
    {
        var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Sheet1");
        ws.Cell(1, 1).Value = "A";
        ws.Cell(1, 2).Value = "B";
        ws.Cell(2, 1).Value = "x";
        ws.Cell(2, 2).Value = "y";
        // Row 3 left blank
        ws.Cell(4, 1).Value = "p";
        ws.Cell(4, 2).Value = "q";

        var grid = SheetGrid.FromWorksheet(ws);

        var region = new TableRegion
        {
            HeaderRows = [0],
            FirstDataRow = 1,
            LastDataRow = 3,
            FirstCol = 0,
            LastCol = 1,
            Orientation = "long",
        };

        var (_, rows) = GridExtractor.Extract(grid, region);

        Assert.Equal(2, rows.Count);
        Assert.Equal(new[] { "x", "y" }, rows[0]);
        Assert.Equal(new[] { "p", "q" }, rows[1]);
    }

    [Fact]
    public void Extract_ClampsOutOfRangeIndices()
    {
        var grid = SheetGrid.FromWorksheet(BuildLongSheet());

        var region = new TableRegion
        {
            HeaderRows = [3],
            FirstDataRow = 4,
            LastDataRow = 999,     // beyond the sheet
            FirstCol = 0,
            LastCol = 50,          // beyond the sheet
            Orientation = "long",
        };

        var (headers, rows) = GridExtractor.Extract(grid, region);

        // Columns clamped to the grid width (3 columns).
        Assert.Equal(3, headers.Count);
        // Rows clamped to the last real data row.
        Assert.Equal(3, rows.Count);
    }
}
