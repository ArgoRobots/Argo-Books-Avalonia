using ArgoBooks.Core.Services.Layout;
using ClosedXML.Excel;
using Xunit;

namespace ArgoBooks.Tests.Importer.Layout;

/// <summary>
/// Tests for <see cref="LayoutGate.NeedsInterpretation"/>.
///
/// The gate returns FALSE for a clean, simple tabular sheet (single dense text header at
/// row 0, consistent data rows, no merged ranges) so it stays on the zero-cost fast path.
/// It returns TRUE for any of the messy-sheet signals: merged headers, long preamble,
/// numeric-looking first-dense-row, or inconsistent row widths.
///
/// SheetGrids are built via ClosedXML in-memory worksheets, matching the pattern used by
/// SheetGridTests and GridExtractorTests.
/// </summary>
public class LayoutGateTests
{
    // ─── Test 1: Clean tabular sheet ─────────────────────────────────────────

    /// <summary>
    /// A minimal clean sheet:
    ///   Row 1: "Date", "Amount", "Description"  (dense text header, first row)
    ///   Row 2: 2024-01-15, 100.50, "Coffee"
    ///   Row 3: 2024-02-20, 250.00, "Office supplies"
    ///   Row 4: 2024-03-10, 12.00,  "Tea"
    ///
    /// No merged cells, header at row 0, consistent widths -> fast path.
    /// </summary>
    private static SheetGrid BuildCleanTabularSheet()
    {
        var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Sheet1");

        ws.Cell(1, 1).Value = "Date";
        ws.Cell(1, 2).Value = "Amount";
        ws.Cell(1, 3).Value = "Description";

        ws.Cell(2, 1).Value = new DateTime(2024, 1, 15);
        ws.Cell(2, 2).Value = 100.50;
        ws.Cell(2, 3).Value = "Coffee";

        ws.Cell(3, 1).Value = new DateTime(2024, 2, 20);
        ws.Cell(3, 2).Value = 250.00;
        ws.Cell(3, 3).Value = "Office supplies";

        ws.Cell(4, 1).Value = new DateTime(2024, 3, 10);
        ws.Cell(4, 2).Value = 12.00;
        ws.Cell(4, 3).Value = "Tea";

        return SheetGrid.FromWorksheet(ws);
    }

    [Fact]
    public void NeedsInterpretation_CleanTabularSheet_ReturnsFalse()
    {
        var grid = BuildCleanTabularSheet();

        var result = LayoutGate.NeedsInterpretation(grid);

        Assert.False(result,
            "A clean single-header sheet with consistent data rows should be on the fast path (false).");
    }

    // ─── Test 2: Merged header ────────────────────────────────────────────────

    /// <summary>
    /// A sheet with a merged cell in the first row (A1:C1 merged).
    /// Merged headers in the top few rows signal a non-trivial layout -> AI interpretation.
    /// </summary>
    private static SheetGrid BuildMergedHeaderSheet()
    {
        var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Sheet1");

        // Row 1: merged title header
        ws.Cell(1, 1).Value = "Sales Summary";
        ws.Range("A1:C1").Merge();

        // Row 2: column headers
        ws.Cell(2, 1).Value = "Date";
        ws.Cell(2, 2).Value = "Amount";
        ws.Cell(2, 3).Value = "Notes";

        // Row 3+: data
        ws.Cell(3, 1).Value = new DateTime(2024, 1, 15);
        ws.Cell(3, 2).Value = 100.50;
        ws.Cell(3, 3).Value = "Coffee";

        ws.Cell(4, 1).Value = new DateTime(2024, 2, 20);
        ws.Cell(4, 2).Value = 250.00;
        ws.Cell(4, 3).Value = "Office supplies";

        return SheetGrid.FromWorksheet(ws);
    }

    [Fact]
    public void NeedsInterpretation_MergedHeaderInFirstFewRows_ReturnsTrue()
    {
        var grid = BuildMergedHeaderSheet();

        var result = LayoutGate.NeedsInterpretation(grid);

        Assert.True(result,
            "A sheet with merged cells in the top rows signals a non-standard layout.");
    }

    // ─── Test 3: Long preamble ────────────────────────────────────────────────

    /// <summary>
    /// A sheet with a 3-row preamble before the actual header:
    ///   Row 1: "Annual Report 2024"   (title)
    ///   Row 2: (blank)
    ///   Row 3: "Figures in USD"       (note)
    ///   Row 4: "Date", "Amount", "Description"  (actual header)
    ///   Row 5+: data rows
    ///
    /// The header does not appear at/near the top -> AI interpretation.
    /// </summary>
    private static SheetGrid BuildLongPreambleSheet()
    {
        var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Sheet1");

        ws.Cell(1, 1).Value = "Annual Report 2024";
        // Row 2 blank intentionally
        ws.Cell(3, 1).Value = "Figures in USD";

        ws.Cell(4, 1).Value = "Date";
        ws.Cell(4, 2).Value = "Amount";
        ws.Cell(4, 3).Value = "Description";

        ws.Cell(5, 1).Value = new DateTime(2024, 1, 15);
        ws.Cell(5, 2).Value = 100.50;
        ws.Cell(5, 3).Value = "Coffee";

        ws.Cell(6, 1).Value = new DateTime(2024, 2, 20);
        ws.Cell(6, 2).Value = 250.00;
        ws.Cell(6, 3).Value = "Office supplies";

        return SheetGrid.FromWorksheet(ws);
    }

    [Fact]
    public void NeedsInterpretation_LongPreamble_ReturnsTrue()
    {
        var grid = BuildLongPreambleSheet();

        var result = LayoutGate.NeedsInterpretation(grid);

        Assert.True(result,
            "A sheet where the first dense row does not appear in the top few rows signals a preamble.");
    }

    // ─── Test 4: Cross-tab / numeric-looking header row ───────────────────────

    /// <summary>
    /// A cross-tab style sheet where the first row (the apparent header) is mostly numeric
    /// (e.g. years or quarter numbers as column headers):
    ///   Row 1: "Category", 2021, 2022, 2023, 2024   (first dense row; 4/5 cells = 80% numeric)
    ///   Row 2: "Revenue",  100,  150,  200,  250
    ///   Row 3: "Expenses", 80,   90,   120,  130
    ///
    /// High NumericFraction on the first dense row -> AI interpretation.
    /// </summary>
    private static SheetGrid BuildCrossTabSheet()
    {
        var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Sheet1");

        // Row 1: mostly-numeric header (years)
        ws.Cell(1, 1).Value = "Category";
        ws.Cell(1, 2).Value = 2021;
        ws.Cell(1, 3).Value = 2022;
        ws.Cell(1, 4).Value = 2023;
        ws.Cell(1, 5).Value = 2024;

        // Row 2: data
        ws.Cell(2, 1).Value = "Revenue";
        ws.Cell(2, 2).Value = 100;
        ws.Cell(2, 3).Value = 150;
        ws.Cell(2, 4).Value = 200;
        ws.Cell(2, 5).Value = 250;

        // Row 3: data
        ws.Cell(3, 1).Value = "Expenses";
        ws.Cell(3, 2).Value = 80;
        ws.Cell(3, 3).Value = 90;
        ws.Cell(3, 4).Value = 120;
        ws.Cell(3, 5).Value = 130;

        return SheetGrid.FromWorksheet(ws);
    }

    [Fact]
    public void NeedsInterpretation_CrossTabNumericHeader_ReturnsTrue()
    {
        var grid = BuildCrossTabSheet();

        var result = LayoutGate.NeedsInterpretation(grid);

        Assert.True(result,
            "A sheet where the first dense row is mostly numeric signals a cross-tab or pivot layout.");
    }

    // ─── Test 5 (optional): Ragged / inconsistent row widths ─────────────────

    /// <summary>
    /// A sheet with inconsistent row widths (stacked or ragged tables):
    ///   Row 1: "Date", "Amount", "Description"  (3 filled)
    ///   Row 2: 2024-01-15, 100.50, "Coffee"     (3 filled)
    ///   Row 3: "Subtotal"                        (1 filled — breaks consistency)
    ///   Row 4: 2024-02-20, 250.00, "Office"     (3 filled)
    ///   Row 5: "Grand Total", 362.50             (2 filled — another inconsistency)
    ///   Row 6: 2024-03-10, 12.00, "Tea"         (3 filled)
    ///
    /// Large spread in NonEmpty counts -> AI interpretation.
    /// </summary>
    private static SheetGrid BuildRaggedRowsSheet()
    {
        var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Sheet1");

        ws.Cell(1, 1).Value = "Date";
        ws.Cell(1, 2).Value = "Amount";
        ws.Cell(1, 3).Value = "Description";

        ws.Cell(2, 1).Value = new DateTime(2024, 1, 15);
        ws.Cell(2, 2).Value = 100.50;
        ws.Cell(2, 3).Value = "Coffee";

        // Row 3: single-cell subtotal label (disrupts the pattern)
        ws.Cell(3, 1).Value = "Subtotal";

        ws.Cell(4, 1).Value = new DateTime(2024, 2, 20);
        ws.Cell(4, 2).Value = 250.00;
        ws.Cell(4, 3).Value = "Office";

        // Row 5: two-cell grand total row
        ws.Cell(5, 1).Value = "Grand Total";
        ws.Cell(5, 2).Value = 362.50;

        ws.Cell(6, 1).Value = new DateTime(2024, 3, 10);
        ws.Cell(6, 2).Value = 12.00;
        ws.Cell(6, 3).Value = "Tea";

        return SheetGrid.FromWorksheet(ws);
    }

    [Fact]
    public void NeedsInterpretation_RaggedRows_ReturnsTrue()
    {
        var grid = BuildRaggedRowsSheet();

        var result = LayoutGate.NeedsInterpretation(grid);

        Assert.True(result,
            "A sheet with highly inconsistent row widths (stacked/ragged tables) needs AI interpretation.");
    }

    // ─── Test 6: Period-header cross-tab (text month names) ───────────────────

    /// <summary>
    /// The deceptively-clean cross-tab: structurally a tidy rectangle, but its column
    /// headers are month names (text, so the numeric-header rule never fires):
    ///   Row 1: "Product", "Jan", "Feb", "Mar"   (3 of 4 cells are months)
    ///   Row 2: "Widget",  100,   200,   150
    ///   Row 3: "Gadget",  300,   400,   350
    ///   Row 4: "Gizmo",    50,    75,    60
    ///
    /// Period labels dominating the header -> AI interpretation (so it can be pivoted).
    /// This mirrors TestData/MainImporter/corpus/cross-tab-sales.
    /// </summary>
    private static SheetGrid BuildMonthCrossTabSheet()
    {
        var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Sheet1");

        ws.Cell(1, 1).Value = "Product";
        ws.Cell(1, 2).Value = "Jan";
        ws.Cell(1, 3).Value = "Feb";
        ws.Cell(1, 4).Value = "Mar";

        ws.Cell(2, 1).Value = "Widget";
        ws.Cell(2, 2).Value = 100;
        ws.Cell(2, 3).Value = 200;
        ws.Cell(2, 4).Value = 150;

        ws.Cell(3, 1).Value = "Gadget";
        ws.Cell(3, 2).Value = 300;
        ws.Cell(3, 3).Value = 400;
        ws.Cell(3, 4).Value = 350;

        ws.Cell(4, 1).Value = "Gizmo";
        ws.Cell(4, 2).Value = 50;
        ws.Cell(4, 3).Value = 75;
        ws.Cell(4, 4).Value = 60;

        return SheetGrid.FromWorksheet(ws);
    }

    [Fact]
    public void NeedsInterpretation_MonthNameCrossTab_ReturnsTrue()
    {
        var grid = BuildMonthCrossTabSheet();

        var result = LayoutGate.NeedsInterpretation(grid);

        Assert.True(result,
            "A text cross-tab whose headers are month names should be sent for interpretation so it can be pivoted.");
    }

    [Theory]
    [InlineData("January", "February", "March", "April")]   // full month names
    [InlineData("Jan 2024", "Feb 2024", "Mar 2024", "Apr 2024")] // month + year
    [InlineData("Q1", "Q2", "Q3", "Q4")]                    // quarters
    [InlineData("W1", "W2", "W3", "W4")]                    // weeks
    public void NeedsInterpretation_PeriodHeaderVariants_ReturnTrue(string h1, string h2, string h3, string h4)
    {
        var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Sheet1");

        ws.Cell(1, 1).Value = "Category";
        ws.Cell(1, 2).Value = h1;
        ws.Cell(1, 3).Value = h2;
        ws.Cell(1, 4).Value = h3;
        ws.Cell(1, 5).Value = h4;

        ws.Cell(2, 1).Value = "Revenue";
        ws.Cell(2, 2).Value = 100;
        ws.Cell(2, 3).Value = 150;
        ws.Cell(2, 4).Value = 200;
        ws.Cell(2, 5).Value = 250;

        var grid = SheetGrid.FromWorksheet(ws);

        Assert.True(LayoutGate.NeedsInterpretation(grid),
            $"A header row of period labels ({h1}, {h2}, ...) should trip the period cross-tab rule.");
    }

    /// <summary>
    /// Guard against false positives: a normal table with a single legitimately-named
    /// "Month" column (and no other period labels) must NOT be treated as a cross-tab.
    /// </summary>
    [Fact]
    public void NeedsInterpretation_SingleMonthColumn_ReturnsFalse()
    {
        var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Sheet1");

        ws.Cell(1, 1).Value = "Month";
        ws.Cell(1, 2).Value = "Revenue";
        ws.Cell(1, 3).Value = "Expenses";

        ws.Cell(2, 1).Value = "January";
        ws.Cell(2, 2).Value = 1000;
        ws.Cell(2, 3).Value = 800;

        ws.Cell(3, 1).Value = "February";
        ws.Cell(3, 2).Value = 1200;
        ws.Cell(3, 3).Value = 900;

        ws.Cell(4, 1).Value = "March";
        ws.Cell(4, 2).Value = 1100;
        ws.Cell(4, 3).Value = 850;

        var grid = SheetGrid.FromWorksheet(ws);

        Assert.False(LayoutGate.NeedsInterpretation(grid),
            "A long-form table with month VALUES under a 'Month' column is already importable and must stay on the fast path.");
    }
}
