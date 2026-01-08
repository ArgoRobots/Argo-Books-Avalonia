using ClosedXML.Excel;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Service for exporting chart data to Microsoft Excel files.
/// Creates formatted Excel workbooks with data tables and embedded charts.
/// </summary>
public class ChartExcelExportService
{
    // Constants for formatting
    private const string CurrencyFormatPattern = "\"$\"#,##0.00";
    private const string NumberFormatPattern = "#,##0";
    private const string PercentFormatPattern = "0.00%";

    /// <summary>
    /// Exports single-series chart data to an Excel file with currency formatting.
    /// </summary>
    /// <param name="filePath">The path to save the Excel file.</param>
    /// <param name="chartTitle">The title of the chart.</param>
    /// <param name="labels">The category labels (e.g., dates).</param>
    /// <param name="values">The data values.</param>
    /// <param name="column1Header">Header for the first column (labels).</param>
    /// <param name="column2Header">Header for the second column (values).</param>
    /// <param name="isCurrency">Whether to format values as currency.</param>
    public static async Task ExportChartAsync(
        string filePath,
        string chartTitle,
        string[] labels,
        double[] values,
        string column1Header = "Date",
        string column2Header = "Value",
        bool isCurrency = true)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        ArgumentNullException.ThrowIfNull(labels);
        ArgumentNullException.ThrowIfNull(values);

        if (labels.Length == 0 || values.Length == 0)
            return;

        await Task.Run(() =>
        {
            using var workbook = new XLWorkbook();
            var worksheetName = TruncateSheetName(chartTitle);
            var worksheet = workbook.Worksheets.Add(worksheetName);

            // Add headers
            worksheet.Cell(1, 1).Value = column1Header;
            worksheet.Cell(1, 2).Value = column2Header;

            // Format headers
            var headerRange = worksheet.Range("A1:B1");
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightSkyBlue;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Add data
            var rowCount = Math.Min(labels.Length, values.Length);
            for (int i = 0; i < rowCount; i++)
            {
                worksheet.Cell(i + 2, 1).Value = labels[i];
                var valueCell = worksheet.Cell(i + 2, 2);
                valueCell.Value = values[i];
                valueCell.Style.NumberFormat.Format = isCurrency ? CurrencyFormatPattern : NumberFormatPattern;
            }

            // Add total row if applicable
            if (isCurrency && rowCount > 0)
            {
                var totalRow = rowCount + 2;
                worksheet.Cell(totalRow, 1).Value = "Total";
                worksheet.Cell(totalRow, 1).Style.Font.Bold = true;
                var totalCell = worksheet.Cell(totalRow, 2);
                totalCell.FormulaA1 = $"=SUM(B2:B{rowCount + 1})";
                totalCell.Style.Font.Bold = true;
                totalCell.Style.NumberFormat.Format = CurrencyFormatPattern;
            }

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            // Save the file
            workbook.SaveAs(filePath);
        });
    }

    /// <summary>
    /// Exports multi-series chart data to an Excel file (e.g., Sales vs Expenses).
    /// </summary>
    /// <param name="filePath">The path to save the Excel file.</param>
    /// <param name="chartTitle">The title of the chart.</param>
    /// <param name="labels">The category labels (e.g., dates).</param>
    /// <param name="seriesData">Dictionary of series name to values.</param>
    /// <param name="labelHeader">Header for the labels column.</param>
    /// <param name="isCurrency">Whether to format values as currency.</param>
    public static async Task ExportMultiSeriesChartAsync(
        string filePath,
        string chartTitle,
        string[] labels,
        Dictionary<string, double[]> seriesData,
        string labelHeader = "Date",
        bool isCurrency = true)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        ArgumentNullException.ThrowIfNull(labels);
        ArgumentNullException.ThrowIfNull(seriesData);

        if (labels.Length == 0 || seriesData.Count == 0)
            return;

        await Task.Run(() =>
        {
            using var workbook = new XLWorkbook();
            var worksheetName = TruncateSheetName(chartTitle);
            var worksheet = workbook.Worksheets.Add(worksheetName);

            var seriesNames = seriesData.Keys.ToList();

            // Add headers
            worksheet.Cell(1, 1).Value = labelHeader;
            for (int i = 0; i < seriesNames.Count; i++)
            {
                worksheet.Cell(1, i + 2).Value = seriesNames[i];
            }

            // Format headers
            var headerRange = worksheet.Range(1, 1, 1, seriesNames.Count + 1);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightSkyBlue;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Add data
            for (int row = 0; row < labels.Length; row++)
            {
                worksheet.Cell(row + 2, 1).Value = labels[row];

                for (int col = 0; col < seriesNames.Count; col++)
                {
                    var values = seriesData[seriesNames[col]];
                    if (row < values.Length)
                    {
                        var cell = worksheet.Cell(row + 2, col + 2);
                        cell.Value = values[row];
                        cell.Style.NumberFormat.Format = isCurrency ? CurrencyFormatPattern : NumberFormatPattern;
                    }
                }
            }

            // Add total row
            if (isCurrency && labels.Length > 0)
            {
                var totalRow = labels.Length + 2;
                worksheet.Cell(totalRow, 1).Value = "Total";
                worksheet.Cell(totalRow, 1).Style.Font.Bold = true;

                for (int col = 0; col < seriesNames.Count; col++)
                {
                    var totalCell = worksheet.Cell(totalRow, col + 2);
                    var columnLetter = GetColumnLetter(col + 2);
                    totalCell.FormulaA1 = $"=SUM({columnLetter}2:{columnLetter}{labels.Length + 1})";
                    totalCell.Style.Font.Bold = true;
                    totalCell.Style.NumberFormat.Format = CurrencyFormatPattern;
                }
            }

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            // Save the file
            workbook.SaveAs(filePath);
        });
    }

    /// <summary>
    /// Exports distribution/pie chart data to an Excel file.
    /// </summary>
    /// <param name="filePath">The path to save the Excel file.</param>
    /// <param name="chartTitle">The title of the chart.</param>
    /// <param name="labels">The category labels.</param>
    /// <param name="values">The data values.</param>
    /// <param name="categoryHeader">Header for the category column.</param>
    /// <param name="valueHeader">Header for the value column.</param>
    /// <param name="isCurrency">Whether to format values as currency.</param>
    public static async Task ExportDistributionChartAsync(
        string filePath,
        string chartTitle,
        string[] labels,
        double[] values,
        string categoryHeader = "Category",
        string valueHeader = "Amount",
        bool isCurrency = true)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        ArgumentNullException.ThrowIfNull(labels);
        ArgumentNullException.ThrowIfNull(values);

        if (labels.Length == 0 || values.Length == 0)
            return;

        await Task.Run(() =>
        {
            using var workbook = new XLWorkbook();
            var worksheetName = TruncateSheetName(chartTitle);
            var worksheet = workbook.Worksheets.Add(worksheetName);

            // Add headers
            worksheet.Cell(1, 1).Value = categoryHeader;
            worksheet.Cell(1, 2).Value = valueHeader;
            worksheet.Cell(1, 3).Value = "Percentage";

            // Format headers
            var headerRange = worksheet.Range("A1:C1");
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightSkyBlue;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Calculate total for percentages
            var total = values.Sum();

            // Add data (sorted by value descending)
            var sortedData = labels.Zip(values, (l, v) => new { Label = l, Value = v })
                .OrderByDescending(x => x.Value)
                .ToList();

            for (int i = 0; i < sortedData.Count; i++)
            {
                worksheet.Cell(i + 2, 1).Value = sortedData[i].Label;

                var valueCell = worksheet.Cell(i + 2, 2);
                valueCell.Value = sortedData[i].Value;
                valueCell.Style.NumberFormat.Format = isCurrency ? CurrencyFormatPattern : NumberFormatPattern;

                var percentCell = worksheet.Cell(i + 2, 3);
                percentCell.Value = total > 0 ? sortedData[i].Value / total : 0;
                percentCell.Style.NumberFormat.Format = PercentFormatPattern;
            }

            // Add total row
            var totalRow = sortedData.Count + 2;
            worksheet.Cell(totalRow, 1).Value = "Total";
            worksheet.Cell(totalRow, 1).Style.Font.Bold = true;

            var totalValueCell = worksheet.Cell(totalRow, 2);
            totalValueCell.FormulaA1 = $"=SUM(B2:B{sortedData.Count + 1})";
            totalValueCell.Style.Font.Bold = true;
            totalValueCell.Style.NumberFormat.Format = isCurrency ? CurrencyFormatPattern : NumberFormatPattern;

            var totalPercentCell = worksheet.Cell(totalRow, 3);
            totalPercentCell.Value = 1.0;
            totalPercentCell.Style.Font.Bold = true;
            totalPercentCell.Style.NumberFormat.Format = PercentFormatPattern;

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            // Save the file
            workbook.SaveAs(filePath);
        });
    }

    /// <summary>
    /// Gets the Excel column letter for a given column number (1-based).
    /// </summary>
    private static string GetColumnLetter(int columnNumber)
    {
        var result = "";
        while (columnNumber > 0)
        {
            columnNumber--;
            result = (char)('A' + columnNumber % 26) + result;
            columnNumber /= 26;
        }
        return result;
    }

    /// <summary>
    /// Truncates worksheet name to Excel's 31 character limit.
    /// </summary>
    private static string TruncateSheetName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return "Chart Data";

        // Remove invalid characters for sheet names
        var invalidChars = new[] { ':', '\\', '/', '?', '*', '[', ']' };
        foreach (var c in invalidChars)
        {
            name = name.Replace(c, '_');
        }

        return name.Length > 31 ? name[..31] : name;
    }
}
