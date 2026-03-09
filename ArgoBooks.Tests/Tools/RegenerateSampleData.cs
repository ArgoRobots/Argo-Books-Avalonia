using ClosedXML.Excel;
using Xunit;

namespace ArgoBooks.Tests.Tools;

/// <summary>
/// One-time tool to regenerate SampleCompanyData.xlsx with less volatile financial data.
/// Run manually when sample data needs to be re-smoothed. Skipped in CI.
/// </summary>
public class RegenerateSampleData
{
    // Path to the XLSX embedded resource (relative to repo root)
    private static readonly string XlsxPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "ArgoBooks", "Resources", "SampleCompanyData.xlsx"));

    [Fact(Skip = "Manual tool — run explicitly to regenerate sample data XLSX")]
    public void RegenerateXlsx()
    {
        Assert.True(File.Exists(XlsxPath), $"XLSX not found at {XlsxPath}");

        using var workbook = new XLWorkbook(XlsxPath);

        foreach (var worksheet in workbook.Worksheets)
        {
            var name = worksheet.Name.Trim().ToLowerInvariant();
            if (name is "sales" or "revenue" or "expenses" or "purchases")
            {
                SmoothSheet(worksheet);
            }
        }

        workbook.SaveAs(XlsxPath);
    }

    /// <summary>
    /// Smooths a Sales/Revenue or Expenses/Purchases sheet by:
    /// 1. Capping outlier transactions per product to 3x the product median
    /// 2. Applying a 5-month centered moving average to monthly totals
    /// 3. Scaling each row proportionally so monthly totals match the smoothed target
    /// </summary>
    private static void SmoothSheet(IXLWorksheet ws)
    {
        // Find header row and column indices
        var headerRow = FindHeaderRow(ws);
        if (headerRow < 1) return;

        int dateCol = FindColumn(ws, headerRow, "Date");
        int unitPriceCol = FindColumn(ws, headerRow, "Unit Price");
        int taxCol = FindColumn(ws, headerRow, "Tax");
        int totalCol = FindColumn(ws, headerRow, "Total");
        int productCol = FindColumn(ws, headerRow, "Product");
        int shippingCol = FindColumn(ws, headerRow, "Shipping");

        if (dateCol < 1 || totalCol < 1) return;

        var lastRow = ws.LastRowUsed()?.RowNumber() ?? headerRow;

        // Collect all data rows
        var rows = new List<RowData>();
        for (int r = headerRow + 1; r <= lastRow; r++)
        {
            var dateCell = ws.Cell(r, dateCol);
            if (dateCell.IsEmpty()) continue;

            DateTime date;
            if (dateCell.DataType == XLDataType.DateTime)
                date = dateCell.GetDateTime();
            else if (DateTime.TryParse(dateCell.GetString(), out var parsed))
                date = parsed;
            else
                continue;

            var total = GetDecimal(ws, r, totalCol);
            var unitPrice = unitPriceCol > 0 ? GetDecimal(ws, r, unitPriceCol) : 0m;
            var tax = taxCol > 0 ? GetDecimal(ws, r, taxCol) : 0m;
            var shipping = shippingCol > 0 ? GetDecimal(ws, r, shippingCol) : 0m;
            var product = productCol > 0 ? ws.Cell(r, productCol).GetString().Trim() : "";

            rows.Add(new RowData
            {
                Row = r,
                Date = date,
                Month = new DateTime(date.Year, date.Month, 1),
                UnitPrice = unitPrice,
                Tax = tax,
                Total = total,
                Shipping = shipping,
                Product = product
            });
        }

        if (rows.Count < 3) return;

        // Step 1: Cap per-product outliers to 3x the product median
        CapOutliers(rows);

        // Write back capped values before monthly smoothing
        foreach (var row in rows)
        {
            if (unitPriceCol > 0) ws.Cell(row.Row, unitPriceCol).Value = (double)row.UnitPrice;
            if (taxCol > 0) ws.Cell(row.Row, taxCol).Value = (double)row.Tax;
            ws.Cell(row.Row, totalCol).Value = (double)row.Total;
        }

        // Step 2: Compute monthly totals and apply 5-month centered moving average
        var monthGroups = rows
            .GroupBy(r => r.Month)
            .OrderBy(g => g.Key)
            .ToList();

        if (monthGroups.Count < 3) return;

        var monthlyTotals = monthGroups.Select(g => g.Sum(r => r.Total)).ToList();
        var smoothed = new List<decimal>(monthlyTotals.Count);

        for (int i = 0; i < monthlyTotals.Count; i++)
        {
            int start = Math.Max(0, i - 2);
            int end = Math.Min(monthlyTotals.Count - 1, i + 2);
            decimal sum = 0;
            int count = 0;
            for (int j = start; j <= end; j++)
            {
                sum += monthlyTotals[j];
                count++;
            }
            smoothed.Add(sum / count);
        }

        // Step 3: Scale each row proportionally
        for (int i = 0; i < monthGroups.Count; i++)
        {
            var originalTotal = monthlyTotals[i];
            if (originalTotal == 0) continue;

            var scaleFactor = smoothed[i] / originalTotal;
            if (scaleFactor == 1m) continue;

            foreach (var row in monthGroups[i])
            {
                row.UnitPrice = Math.Round(row.UnitPrice * scaleFactor, 2);
                row.Tax = Math.Round(row.Tax * scaleFactor, 2);
                row.Total = Math.Round(row.Total * scaleFactor, 2);
                // Don't scale shipping — it's a flat fee

                if (unitPriceCol > 0) ws.Cell(row.Row, unitPriceCol).Value = (double)row.UnitPrice;
                if (taxCol > 0) ws.Cell(row.Row, taxCol).Value = (double)row.Tax;
                ws.Cell(row.Row, totalCol).Value = (double)row.Total;
            }
        }
    }

    /// <summary>
    /// Caps each transaction's total to 3x the median total for that product.
    /// Scales UnitPrice and Tax proportionally when capping.
    /// </summary>
    private static void CapOutliers(List<RowData> rows)
    {
        var byProduct = rows.GroupBy(r => r.Product).ToList();

        foreach (var group in byProduct)
        {
            var totals = group.Select(r => r.Total).OrderBy(x => x).ToList();
            var median = totals[totals.Count / 2];
            var cap = median * 3m;

            if (cap <= 0) continue;

            foreach (var row in group)
            {
                if (row.Total > cap && row.Total > 0)
                {
                    var scaleFactor = cap / row.Total;
                    row.UnitPrice = Math.Round(row.UnitPrice * scaleFactor, 2);
                    row.Tax = Math.Round(row.Tax * scaleFactor, 2);
                    row.Total = Math.Round(row.Total * scaleFactor, 2);
                }
            }
        }
    }

    private static int FindHeaderRow(IXLWorksheet ws)
    {
        for (int r = 1; r <= 10; r++)
        {
            int nonEmpty = 0;
            for (int c = 1; c <= 15; c++)
            {
                if (!ws.Cell(r, c).IsEmpty()) nonEmpty++;
            }
            if (nonEmpty >= 2) return r;
        }
        return -1;
    }

    private static int FindColumn(IXLWorksheet ws, int headerRow, string name)
    {
        var lastCol = ws.ColumnsUsed().Count();
        for (int c = 1; c <= lastCol + 5; c++)
        {
            var cell = ws.Cell(headerRow, c);
            if (!cell.IsEmpty() && cell.GetString().Trim().Equals(name, StringComparison.OrdinalIgnoreCase))
                return c;
        }
        return -1;
    }

    private static decimal GetDecimal(IXLWorksheet ws, int row, int col)
    {
        var cell = ws.Cell(row, col);
        if (cell.IsEmpty()) return 0;
        if (cell.DataType == XLDataType.Number) return (decimal)cell.GetDouble();
        if (decimal.TryParse(cell.GetString(), out var v)) return v;
        return 0;
    }

    private class RowData
    {
        public int Row { get; set; }
        public DateTime Date { get; set; }
        public DateTime Month { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }
        public decimal Shipping { get; set; }
        public string Product { get; set; } = "";
    }
}
