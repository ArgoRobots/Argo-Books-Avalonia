using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;
using OfficeOpenXml.Style;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Service for exporting chart data to Microsoft Excel files.
/// Creates formatted Excel workbooks with data tables and embedded charts using EPPlus.
/// </summary>
public class ChartExcelExportService
{
    // Constants for formatting
    private const string CurrencyFormat = "\"$\"#,##0.00";
    private const string NumberFormat = "#,##0";
    private const string PercentFormat = "0.00%";

    // Chart dimensions
    private const int ChartWidth = 800;
    private const int ChartHeight = 400;

    static ChartExcelExportService()
    {
        // Set EPPlus license for non-commercial use
        ExcelPackage.License.SetNonCommercialPersonal("ArgoBooks");
    }

    /// <summary>
    /// Exports single-series chart data to an Excel file with an embedded chart.
    /// </summary>
    /// <param name="filePath">The path to save the Excel file.</param>
    /// <param name="chartTitle">The title of the chart.</param>
    /// <param name="labels">The category labels (e.g., dates).</param>
    /// <param name="values">The data values.</param>
    /// <param name="column1Header">Header for the first column (labels).</param>
    /// <param name="column2Header">Header for the second column (values).</param>
    /// <param name="isCurrency">Whether to format values as currency.</param>
    /// <param name="useLineChart">Whether to use a line chart (default) or column chart.</param>
    public static async Task ExportChartAsync(
        string filePath,
        string chartTitle,
        string[] labels,
        double[] values,
        string column1Header = "Date",
        string column2Header = "Value",
        bool isCurrency = true,
        bool useLineChart = true)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        ArgumentNullException.ThrowIfNull(labels);
        ArgumentNullException.ThrowIfNull(values);

        if (labels.Length == 0 || values.Length == 0)
            return;

        await Task.Run(() =>
        {
            using var package = new ExcelPackage();
            var worksheetName = TruncateSheetName(chartTitle);
            var worksheet = package.Workbook.Worksheets.Add(worksheetName);

            // Add headers
            worksheet.Cells[1, 1].Value = column1Header;
            worksheet.Cells[1, 2].Value = column2Header;
            FormatHeaderRow(worksheet, 1, 2);

            // Add data
            var rowCount = Math.Min(labels.Length, values.Length);
            for (int i = 0; i < rowCount; i++)
            {
                worksheet.Cells[i + 2, 1].Value = labels[i];
                worksheet.Cells[i + 2, 2].Value = values[i];
            }

            // Format value column
            var valueFormat = isCurrency ? CurrencyFormat : NumberFormat;
            worksheet.Cells[2, 2, rowCount + 1, 2].Style.Numberformat.Format = valueFormat;

            // Add total row
            var totalRow = rowCount + 2;
            worksheet.Cells[totalRow, 1].Value = "Total";
            worksheet.Cells[totalRow, 1].Style.Font.Bold = true;
            worksheet.Cells[totalRow, 2].Formula = $"SUM(B2:B{rowCount + 1})";
            worksheet.Cells[totalRow, 2].Style.Font.Bold = true;
            worksheet.Cells[totalRow, 2].Style.Numberformat.Format = valueFormat;

            // Auto-fit columns
            worksheet.Cells.AutoFitColumns();

            // Create embedded chart
            var chartType = useLineChart ? eChartType.Line : eChartType.ColumnClustered;
            var chart = worksheet.Drawings.AddChart(chartTitle, chartType);
            chart.SetPosition(0, 0, 3, 0); // Position to the right of data
            chart.SetSize(ChartWidth, ChartHeight);
            chart.Title.Text = chartTitle;

            // Add series - exclude total row
            var series = chart.Series.Add(
                worksheet.Cells[2, 2, rowCount + 1, 2],  // Y values (data)
                worksheet.Cells[2, 1, rowCount + 1, 1]); // X values (labels)
            series.Header = column2Header;

            // Enable smooth lines for line charts
            if (useLineChart && chart is ExcelLineChart lineChart)
            {
                lineChart.Smooth = true;
            }

            // Save the file
            package.SaveAs(new FileInfo(filePath));
        });
    }

    /// <summary>
    /// Exports multi-series chart data to an Excel file with an embedded chart (e.g., Revenue vs Expenses).
    /// </summary>
    /// <param name="filePath">The path to save the Excel file.</param>
    /// <param name="chartTitle">The title of the chart.</param>
    /// <param name="labels">The category labels (e.g., dates).</param>
    /// <param name="seriesData">Dictionary of series name to values.</param>
    /// <param name="labelHeader">Header for the labels column.</param>
    /// <param name="isCurrency">Whether to format values as currency.</param>
    /// <param name="useLineChart">Whether to use a line chart (default) or column chart.</param>
    public static async Task ExportMultiSeriesChartAsync(
        string filePath,
        string chartTitle,
        string[] labels,
        Dictionary<string, double[]> seriesData,
        string labelHeader = "Date",
        bool isCurrency = true,
        bool useLineChart = true)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        ArgumentNullException.ThrowIfNull(labels);
        ArgumentNullException.ThrowIfNull(seriesData);

        if (labels.Length == 0 || seriesData.Count == 0)
            return;

        await Task.Run(() =>
        {
            using var package = new ExcelPackage();
            var worksheetName = TruncateSheetName(chartTitle);
            var worksheet = package.Workbook.Worksheets.Add(worksheetName);

            var seriesNames = seriesData.Keys.ToList();
            var columnCount = seriesNames.Count + 1;

            // Add headers
            worksheet.Cells[1, 1].Value = labelHeader;
            for (int i = 0; i < seriesNames.Count; i++)
            {
                worksheet.Cells[1, i + 2].Value = seriesNames[i];
            }
            FormatHeaderRow(worksheet, 1, columnCount);

            // Add data
            var valueFormat = isCurrency ? CurrencyFormat : NumberFormat;
            for (int row = 0; row < labels.Length; row++)
            {
                worksheet.Cells[row + 2, 1].Value = labels[row];

                for (int col = 0; col < seriesNames.Count; col++)
                {
                    var values = seriesData[seriesNames[col]];
                    if (row < values.Length)
                    {
                        worksheet.Cells[row + 2, col + 2].Value = values[row];
                    }
                }
            }

            // Format value columns
            for (int col = 0; col < seriesNames.Count; col++)
            {
                worksheet.Cells[2, col + 2, labels.Length + 1, col + 2].Style.Numberformat.Format = valueFormat;
            }

            // Add total row
            var totalRow = labels.Length + 2;
            worksheet.Cells[totalRow, 1].Value = "Total";
            worksheet.Cells[totalRow, 1].Style.Font.Bold = true;

            for (int col = 0; col < seriesNames.Count; col++)
            {
                var colLetter = GetColumnLetter(col + 2);
                worksheet.Cells[totalRow, col + 2].Formula = $"SUM({colLetter}2:{colLetter}{labels.Length + 1})";
                worksheet.Cells[totalRow, col + 2].Style.Font.Bold = true;
                worksheet.Cells[totalRow, col + 2].Style.Numberformat.Format = valueFormat;
            }

            // Auto-fit columns
            worksheet.Cells.AutoFitColumns();

            // Create embedded chart
            var chartType = useLineChart ? eChartType.Line : eChartType.ColumnClustered;
            var chart = worksheet.Drawings.AddChart(chartTitle, chartType);
            chart.SetPosition(0, 0, columnCount + 1, 0); // Position to the right of data
            chart.SetSize(ChartWidth, ChartHeight);
            chart.Title.Text = chartTitle;

            // Add each series
            for (int col = 0; col < seriesNames.Count; col++)
            {
                var series = chart.Series.Add(
                    worksheet.Cells[2, col + 2, labels.Length + 1, col + 2],  // Y values
                    worksheet.Cells[2, 1, labels.Length + 1, 1]);              // X values
                series.Header = seriesNames[col];
            }

            // Enable smooth lines for line charts
            if (useLineChart && chart is ExcelLineChart lineChart)
            {
                lineChart.Smooth = true;
            }

            // Save the file
            package.SaveAs(new FileInfo(filePath));
        });
    }

    /// <summary>
    /// Exports distribution/pie chart data to an Excel file with an embedded pie chart.
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
            using var package = new ExcelPackage();
            var worksheetName = TruncateSheetName(chartTitle);
            var worksheet = package.Workbook.Worksheets.Add(worksheetName);

            // Add headers
            worksheet.Cells[1, 1].Value = categoryHeader;
            worksheet.Cells[1, 2].Value = valueHeader;
            worksheet.Cells[1, 3].Value = "Percentage";
            FormatHeaderRow(worksheet, 1, 3);

            // Calculate total for percentages
            var total = values.Sum();

            // Sort data by value descending
            var sortedData = labels.Zip(values, (l, v) => new { Label = l, Value = v })
                .OrderByDescending(x => x.Value)
                .ToList();

            var valueFormat = isCurrency ? CurrencyFormat : NumberFormat;

            // Add data
            for (int i = 0; i < sortedData.Count; i++)
            {
                worksheet.Cells[i + 2, 1].Value = sortedData[i].Label;
                worksheet.Cells[i + 2, 2].Value = sortedData[i].Value;
                worksheet.Cells[i + 2, 2].Style.Numberformat.Format = valueFormat;
                worksheet.Cells[i + 2, 3].Value = total > 0 ? sortedData[i].Value / total : 0;
                worksheet.Cells[i + 2, 3].Style.Numberformat.Format = PercentFormat;
            }

            // Add total row
            var totalRow = sortedData.Count + 2;
            worksheet.Cells[totalRow, 1].Value = "Total";
            worksheet.Cells[totalRow, 1].Style.Font.Bold = true;
            worksheet.Cells[totalRow, 2].Formula = $"SUM(B2:B{sortedData.Count + 1})";
            worksheet.Cells[totalRow, 2].Style.Font.Bold = true;
            worksheet.Cells[totalRow, 2].Style.Numberformat.Format = valueFormat;
            worksheet.Cells[totalRow, 3].Value = 1.0;
            worksheet.Cells[totalRow, 3].Style.Font.Bold = true;
            worksheet.Cells[totalRow, 3].Style.Numberformat.Format = PercentFormat;

            // Auto-fit columns
            worksheet.Cells.AutoFitColumns();

            // Create embedded pie chart
            var chart = worksheet.Drawings.AddChart(chartTitle, eChartType.Pie) as ExcelPieChart;
            chart!.SetPosition(0, 0, 4, 0); // Position to the right of data
            chart.SetSize(ChartWidth, ChartHeight);
            chart.Title.Text = chartTitle;

            // Add series - exclude total row, use original order (not sorted) for cleaner pie
            var series = chart.Series.Add(
                worksheet.Cells[2, 2, sortedData.Count + 1, 2],  // Values
                worksheet.Cells[2, 1, sortedData.Count + 1, 1]); // Labels
            series.Header = valueHeader;

            // Show data labels with percentages
            chart.DataLabel.ShowPercent = true;
            chart.DataLabel.ShowCategory = true;
            chart.DataLabel.ShowValue = false;

            // Save the file
            package.SaveAs(new FileInfo(filePath));
        });
    }

    /// <summary>
    /// Formats the header row with bold text and background color.
    /// </summary>
    private static void FormatHeaderRow(ExcelWorksheet worksheet, int row, int columnCount)
    {
        var headerRange = worksheet.Cells[row, 1, row, columnCount];
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
        headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightSkyBlue);
        headerRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
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
    /// Truncates worksheet name to Excel's 31 character limit and removes invalid characters.
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
