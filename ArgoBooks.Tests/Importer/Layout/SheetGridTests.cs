using ArgoBooks.Core.Services.Layout;
using ClosedXML.Excel;
using Xunit;

namespace ArgoBooks.Tests.Importer.Layout;

/// <summary>
/// Tests for <see cref="SheetGrid.FromWorksheet"/>.
///
/// Coordinate convention: 1-based throughout (matching ClosedXML), both in SheetGrid's
/// MergedRange records and in the test assertions that reference specific rows/columns.
/// Cells are accessed via 0-based indices into the Cells list: Cells[r-1][c-1].
///
/// Date-as-numeric: dates count toward NumericFraction (see SheetGrid XML docs).
/// </summary>
public class SheetGridTests
{
    /// <summary>
    /// Builds a representative in-memory workbook:
    ///   Row 1: "Annual Report" in A1, merged A1:C1 (title row — text)
    ///   Row 2: (blank)
    ///   Row 3: "Note: figures in USD" in A3 (preamble note — text)
    ///   Row 4: "Date", "Amount", "Desc"  (header row — all text labels)
    ///   Row 5: 2024-01-15 (date), 100.50 (number), "Coffee"  (data row 1)
    ///   Row 6: 2024-02-20 (date), 250.00 (number), "Office supplies"  (data row 2)
    /// </summary>
    private static IXLWorksheet BuildWorksheet()
    {
        var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Sheet1");

        // Row 1 — merged title
        ws.Cell(1, 1).Value = "Annual Report";
        ws.Cell(1, 2).Value = "";
        ws.Cell(1, 3).Value = "";
        ws.Range("A1:C1").Merge();

        // Row 2 — blank (no values set)

        // Row 3 — preamble note
        ws.Cell(3, 1).Value = "Note: figures in USD";

        // Row 4 — header labels
        ws.Cell(4, 1).Value = "Date";
        ws.Cell(4, 2).Value = "Amount";
        ws.Cell(4, 3).Value = "Desc";

        // Row 5 — first data row (date + number + text)
        ws.Cell(5, 1).Value = new DateTime(2024, 1, 15);
        ws.Cell(5, 2).Value = 100.50;
        ws.Cell(5, 3).Value = "Coffee";

        // Row 6 — second data row
        ws.Cell(6, 1).Value = new DateTime(2024, 2, 20);
        ws.Cell(6, 2).Value = 250.00;
        ws.Cell(6, 3).Value = "Office supplies";

        return ws;
    }

    // ─── Grid dimensions ────────────────────────────────────────────────────

    [Fact]
    public void FromWorksheet_EmptySheet_ReturnsEmptyGrid()
    {
        var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Empty");

        var grid = SheetGrid.FromWorksheet(ws);

        Assert.Equal(0, grid.RowCount);
        Assert.Equal(0, grid.ColCount);
        Assert.Empty(grid.Cells);
        Assert.Empty(grid.MergedRanges);
        Assert.Empty(grid.Shapes);
    }

    [Fact]
    public void FromWorksheet_UsedRange_HasCorrectDimensions()
    {
        var ws = BuildWorksheet();
        var grid = SheetGrid.FromWorksheet(ws);

        // 6 rows (rows 1-6), 3 columns (A-C)
        Assert.Equal(6, grid.RowCount);
        Assert.Equal(3, grid.ColCount);
        Assert.Equal(6, grid.Cells.Count);
        Assert.Equal(6, grid.Shapes.Count);
    }

    // ─── Cell values ────────────────────────────────────────────────────────

    [Fact]
    public void FromWorksheet_HeaderRow_CellValuesAreCorrect()
    {
        var ws = BuildWorksheet();
        var grid = SheetGrid.FromWorksheet(ws);

        // Row 4 (0-based index 3) is the header row
        var headerRow = grid.Cells[3];
        Assert.Equal("Date", headerRow[0]);
        Assert.Equal("Amount", headerRow[1]);
        Assert.Equal("Desc", headerRow[2]);
    }

    [Fact]
    public void FromWorksheet_DataRow_CellValuesAreCorrect()
    {
        var ws = BuildWorksheet();
        var grid = SheetGrid.FromWorksheet(ws);

        // Row 5 (0-based index 4): date, number, text
        var dataRow = grid.Cells[4];
        Assert.Equal("2024-01-15", dataRow[0]);      // date formatted as ISO date
        Assert.Equal("100.5", dataRow[1]);            // number in invariant culture
        Assert.Equal("Coffee", dataRow[2]);
    }

    [Fact]
    public void FromWorksheet_BlankRow_AllCellsAreEmptyString()
    {
        var ws = BuildWorksheet();
        var grid = SheetGrid.FromWorksheet(ws);

        // Row 2 (0-based index 1) is blank
        var blankRow = grid.Cells[1];
        Assert.All(blankRow, cell => Assert.Equal("", cell));
    }

    [Fact]
    public void FromWorksheet_TitleCell_ValueIsCorrect()
    {
        var ws = BuildWorksheet();
        var grid = SheetGrid.FromWorksheet(ws);

        // Row 1 (0-based index 0), column A (0-based index 0)
        Assert.Equal("Annual Report", grid.Cells[0][0]);
        // Columns B and C of the merged title row are empty (only A1 holds the value)
        Assert.Equal("", grid.Cells[0][1]);
        Assert.Equal("", grid.Cells[0][2]);
    }

    // ─── Merged ranges ───────────────────────────────────────────────────────

    [Fact]
    public void FromWorksheet_MergedRange_IsCaptured()
    {
        var ws = BuildWorksheet();
        var grid = SheetGrid.FromWorksheet(ws);

        Assert.Single(grid.MergedRanges);
        var m = grid.MergedRanges[0];

        // A1:C1 — 1-based coordinates
        Assert.Equal(1, m.FirstRow);
        Assert.Equal(1, m.FirstCol);
        Assert.Equal(1, m.LastRow);
        Assert.Equal(3, m.LastCol);
    }

    // ─── Row shape profiles ──────────────────────────────────────────────────

    [Fact]
    public void FromWorksheet_BlankRow_ShapeIsAllZero()
    {
        var ws = BuildWorksheet();
        var grid = SheetGrid.FromWorksheet(ws);

        var shape = grid.Shapes[1]; // row 2 (0-based)
        Assert.Equal(0, shape.NonEmpty);
        Assert.Equal(0.0, shape.NumericFraction);
        Assert.Equal(0.0, shape.TextFraction);
    }

    [Fact]
    public void FromWorksheet_HeaderRow_IsAllText()
    {
        var ws = BuildWorksheet();
        var grid = SheetGrid.FromWorksheet(ws);

        // Row 4 header: "Date", "Amount", "Desc" — all text labels, no numeric values
        var shape = grid.Shapes[3]; // 0-based index 3
        Assert.Equal(3, shape.NonEmpty);
        Assert.Equal(0.0, shape.NumericFraction);
        Assert.Equal(1.0, shape.TextFraction);
    }

    [Fact]
    public void FromWorksheet_DataRow_NumericFractionReflectsDateAndNumber()
    {
        var ws = BuildWorksheet();
        var grid = SheetGrid.FromWorksheet(ws);

        // Row 5 data: date + number + text => 2 of 3 cells are numeric/date
        var shape = grid.Shapes[4]; // 0-based index 4
        Assert.Equal(3, shape.NonEmpty);
        Assert.True(shape.NumericFraction > 0.0,
            "Expected NumericFraction > 0 because the row contains a date and a number.");
        // Exactly 2 of 3 cells are numeric/date => ~0.667
        Assert.Equal(2.0 / 3.0, shape.NumericFraction, precision: 10);
        Assert.Equal(1.0 / 3.0, shape.TextFraction, precision: 10);
    }

    [Fact]
    public void FromWorksheet_TitleRow_IsMostlyText()
    {
        var ws = BuildWorksheet();
        var grid = SheetGrid.FromWorksheet(ws);

        // Row 1: "Annual Report" in A1; B1 and C1 are empty (merged).
        // NonEmpty = 1, that 1 cell is text => TextFraction = 1.0
        var shape = grid.Shapes[0];
        Assert.Equal(1, shape.NonEmpty);
        Assert.Equal(0.0, shape.NumericFraction);
        Assert.Equal(1.0, shape.TextFraction);
    }

    [Fact]
    public void FromWorksheet_PreambleRow_IsText()
    {
        var ws = BuildWorksheet();
        var grid = SheetGrid.FromWorksheet(ws);

        // Row 3: "Note: figures in USD" in A3 only
        var shape = grid.Shapes[2];
        Assert.Equal(1, shape.NonEmpty);
        Assert.Equal(0.0, shape.NumericFraction);
        Assert.Equal(1.0, shape.TextFraction);
    }

    [Fact]
    public void FromWorksheet_NumericFractionPlusTextFraction_EqualsOne_WhenNonEmpty()
    {
        var ws = BuildWorksheet();
        var grid = SheetGrid.FromWorksheet(ws);

        foreach (var shape in grid.Shapes.Where(s => s.NonEmpty > 0))
        {
            Assert.Equal(1.0, shape.NumericFraction + shape.TextFraction, precision: 10);
        }
    }

    // ─── Second data row (additional coverage) ───────────────────────────────

    [Fact]
    public void FromWorksheet_SecondDataRow_MatchesFirstPattern()
    {
        var ws = BuildWorksheet();
        var grid = SheetGrid.FromWorksheet(ws);

        // Row 6 (0-based 5): date + number + text — same shape pattern as row 5
        var shape = grid.Shapes[5];
        Assert.Equal(3, shape.NonEmpty);
        Assert.Equal(2.0 / 3.0, shape.NumericFraction, precision: 10);

        var cells = grid.Cells[5];
        Assert.Equal("2024-02-20", cells[0]);
        Assert.Equal("250", cells[1]);
        Assert.Equal("Office supplies", cells[2]);
    }
}
