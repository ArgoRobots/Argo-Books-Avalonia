using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Charts;
using ArgoBooks.Core.Models.Common;
using ArgoBooks.Core.Models.Reports;
using SkiaSharp;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Renders reports to images and PDF files using SkiaSharp.
/// </summary>
public class ReportRenderer : IDisposable
{
    private readonly CompanyData? _companyData;
    private readonly ReportConfiguration _config;
    private readonly float _renderScale;
    private readonly ReportChartDataService? _chartDataService;
    private readonly ITranslationProvider _translationProvider;

    // Cached paints
    private readonly SKPaint _backgroundPaint;
    private readonly SKPaint _textPaint;
    private readonly SKPaint _headerPaint;
    private readonly SKPaint _borderPaint;

    // Fonts
    private readonly SKTypeface _defaultTypeface;
    private readonly SKTypeface _boldTypeface;
    private readonly SKFont _defaultFont;
    private readonly SKFont _headerFont;

    // Chart colors (matching ChartLoaderService)
    private static readonly SKColor ChartBarColor = SKColor.Parse("#6495ED"); // Cornflower Blue
    private static readonly SKColor ChartProfitColor = SKColor.Parse("#22C55E"); // Green
    private static readonly SKColor ChartAxisColor = SKColor.Parse("#374151"); // Gray
    private static readonly SKColor ChartGridColor = SKColor.Parse("#E5E7EB"); // Light gray

    /// <summary>
    /// Determines if a chart type should display currency formatting on the Y-axis.
    /// </summary>
    private static bool ShouldShowCurrency(ChartDataType chartType)
    {
        return chartType switch
        {
            // Count-based charts (no currency)
            ChartDataType.TotalTransactions => false,
            ChartDataType.CustomerGrowth => false,
            ChartDataType.ActiveVsInactiveCustomers => false,
            ChartDataType.RentalsPerCustomer => false,
            ChartDataType.AccountantsTransactions => false,
            ChartDataType.CountriesOfOrigin => false,
            ChartDataType.CountriesOfDestination => false,
            ChartDataType.CompaniesOfOrigin => false,
            ChartDataType.CompaniesOfDestination => false,
            // All other charts show currency
            _ => true
        };
    }

    /// <summary>
    /// Formats a Y-axis value based on whether the chart shows currency.
    /// </summary>
    private static string FormatYAxisValue(double value, ChartDataType chartType)
    {
        return ShouldShowCurrency(chartType) ? $"${value:N0}" : $"{value:N0}";
    }

    public ReportRenderer(ReportConfiguration config, CompanyData? companyData, float renderScale = 1f, ITranslationProvider? translationProvider = null)
    {
        _config = config;
        _companyData = companyData;
        _renderScale = renderScale;
        _translationProvider = translationProvider ?? DefaultTranslationProvider.Instance;

        // Initialize chart data service for rendering actual charts
        if (companyData != null)
        {
            _chartDataService = new ReportChartDataService(companyData, config.Filters);
        }

        _defaultTypeface = SKTypeface.FromFamilyName("Segoe UI") ?? SKTypeface.Default;
        _boldTypeface = SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold) ?? SKTypeface.Default;

        _defaultFont = new SKFont(_defaultTypeface, 12 * renderScale);
        _headerFont = new SKFont(_boldTypeface, (float)config.TitleFontSize * renderScale);

        _backgroundPaint = new SKPaint
        {
            Color = ParseColor(_config.BackgroundColor),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        _textPaint = new SKPaint
        {
            Color = SKColors.Black,
            IsAntialias = true
        };

        _headerPaint = new SKPaint
        {
            Color = SKColors.Black,
            IsAntialias = true
        };

        _borderPaint = new SKPaint
        {
            Color = SKColors.Gray,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1 * renderScale,
            IsAntialias = true
        };
    }

    /// <summary>
    /// Renders the report to a bitmap.
    /// </summary>
    public SKBitmap RenderToBitmap()
    {
        var (width, height) = PageDimensions.GetDimensions(_config.PageSize, _config.PageOrientation);
        var scaledWidth = (int)(width * _renderScale);
        var scaledHeight = (int)(height * _renderScale);

        var bitmap = new SKBitmap(scaledWidth, scaledHeight);
        using var canvas = new SKCanvas(bitmap);

        RenderToCanvas(canvas, scaledWidth, scaledHeight);

        return bitmap;
    }

    /// <summary>
    /// Renders the report to a canvas.
    /// </summary>
    public void RenderToCanvas(SKCanvas canvas, int width, int height)
    {
        canvas.Clear(_backgroundPaint.Color);

        // Render header
        if (_config.ShowHeader)
        {
            RenderHeader(canvas, width);
        }

        // Render elements sorted by Z-order
        foreach (var element in _config.GetElementsByZOrder())
        {
            if (!element.IsVisible)
                continue;

            RenderElement(canvas, element);
        }

        // Render footer
        if (_config.ShowFooter)
        {
            RenderFooter(canvas, width, height);
        }
    }

    /// <summary>
    /// Renders only the elements to a canvas (for design mode).
    /// Does not clear the canvas or render header/footer.
    /// </summary>
    public void RenderElementsToCanvas(SKCanvas canvas)
    {
        // Render elements sorted by Z-order
        foreach (var element in _config.GetElementsByZOrder())
        {
            if (!element.IsVisible)
                continue;

            RenderElement(canvas, element);
        }
    }

    /// <summary>
    /// Renders only the elements for a specific page to a canvas (for design mode).
    /// Does not clear the canvas or render header/footer.
    /// </summary>
    public void RenderElementsToCanvas(SKCanvas canvas, int pageNumber)
    {
        foreach (var element in _config.GetElementsByZOrderForPage(pageNumber))
        {
            if (!element.IsVisible)
                continue;

            RenderElement(canvas, element);
        }
    }

    /// <summary>
    /// Renders a specific page to a canvas, including header, elements, and footer.
    /// </summary>
    public void RenderPageToCanvas(SKCanvas canvas, int pageNumber, int width, int height)
    {
        canvas.Clear(_backgroundPaint.Color);

        if (_config.ShowHeader)
        {
            RenderHeader(canvas, width);
        }

        foreach (var element in _config.GetElementsByZOrderForPage(pageNumber))
        {
            if (!element.IsVisible)
                continue;

            RenderElement(canvas, element);
        }

        if (_config.ShowFooter)
        {
            RenderFooter(canvas, width, height);
        }
    }

    /// <summary>
    /// Renders a specific page to a bitmap.
    /// </summary>
    public SKBitmap RenderPageToBitmap(int pageNumber)
    {
        var (width, height) = PageDimensions.GetDimensions(_config.PageSize, _config.PageOrientation);
        var scaledWidth = (int)(width * _renderScale);
        var scaledHeight = (int)(height * _renderScale);

        _config.CurrentPageNumber = pageNumber;
        _config.TotalPageCount = _config.PageCount;

        var bitmap = new SKBitmap(scaledWidth, scaledHeight);
        using var canvas = new SKCanvas(bitmap);

        RenderPageToCanvas(canvas, pageNumber, scaledWidth, scaledHeight);

        return bitmap;
    }

    /// <summary>
    /// Creates a preview bitmap for a specific page at the specified scale.
    /// </summary>
    public SKBitmap CreatePagePreview(int pageNumber, int maxWidth, int maxHeight)
    {
        var (width, height) = PageDimensions.GetDimensions(_config.PageSize, _config.PageOrientation);

        var scaleX = (float)maxWidth / width;
        var scaleY = (float)maxHeight / height;
        var scale = Math.Min(scaleX, scaleY);

        var previewWidth = (int)(width * scale);
        var previewHeight = (int)(height * scale);

        _config.CurrentPageNumber = pageNumber;
        _config.TotalPageCount = _config.PageCount;

        var bitmap = new SKBitmap(previewWidth, previewHeight);
        using var canvas = new SKCanvas(bitmap);

        canvas.Scale(scale, scale);
        RenderPageToCanvas(canvas, pageNumber, width, height);

        return bitmap;
    }

    /// <summary>
    /// Exports the report to an image file.
    /// </summary>
    public async Task<bool> ExportPageToImageAsync(string filePath, int pageNumber, ExportFormat format, int quality = 95)
    {
        try
        {
            using var bitmap = RenderPageToBitmap(pageNumber);
            using var image = SKImage.FromBitmap(bitmap);

            var formatType = format switch
            {
                ExportFormat.PNG => SKEncodedImageFormat.Png,
                ExportFormat.JPEG => SKEncodedImageFormat.Jpeg,
                _ => SKEncodedImageFormat.Png
            };

            using var data = image.Encode(formatType, quality);

            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await using var stream = File.Create(filePath);
            data.SaveTo(stream);

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> ExportToImageAsync(string filePath, ExportFormat format, int quality = 95)
    {
        try
        {
            using var bitmap = RenderToBitmap();
            using var image = SKImage.FromBitmap(bitmap);

            var formatType = format switch
            {
                ExportFormat.PNG => SKEncodedImageFormat.Png,
                ExportFormat.JPEG => SKEncodedImageFormat.Jpeg,
                _ => SKEncodedImageFormat.Png
            };

            using var data = image.Encode(formatType, quality);

            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await using var stream = File.Create(filePath);
            data.SaveTo(stream);

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Exports the report to a PDF file.
    /// </summary>
    public async Task<bool> ExportToPdfAsync(string filePath)
    {
        try
        {
            var (width, height) = PageDimensions.GetDimensions(_config.PageSize, _config.PageOrientation);
            var scaledWidth = (int)(width * _renderScale);
            var scaledHeight = (int)(height * _renderScale);

            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            _config.TotalPageCount = _config.PageCount;

            await using var stream = File.Create(filePath);
            using var document = SKDocument.CreatePdf(stream);

            for (int page = 1; page <= _config.PageCount; page++)
            {
                _config.CurrentPageNumber = page;
                using var canvas = document.BeginPage(scaledWidth, scaledHeight);
                RenderPageToCanvas(canvas, page, scaledWidth, scaledHeight);
                document.EndPage();
            }

            document.Close();

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Creates a preview bitmap at the specified scale.
    /// </summary>
    public SKBitmap CreatePreview(int maxWidth, int maxHeight)
    {
        var (width, height) = PageDimensions.GetDimensions(_config.PageSize, _config.PageOrientation);

        // Calculate scale to fit within bounds
        var scaleX = (float)maxWidth / width;
        var scaleY = (float)maxHeight / height;
        var scale = Math.Min(scaleX, scaleY);

        var previewWidth = (int)(width * scale);
        var previewHeight = (int)(height * scale);

        var bitmap = new SKBitmap(previewWidth, previewHeight);
        using var canvas = new SKCanvas(bitmap);

        canvas.Scale(scale, scale);
        RenderToCanvas(canvas, width, height);

        return bitmap;
    }

    #region Element Rendering

    private void RenderElement(SKCanvas canvas, ReportElementBase element)
    {
        switch (element)
        {
            case ChartReportElement chart:
                RenderChart(canvas, chart);
                break;
            case TableReportElement table:
                RenderTable(canvas, table);
                break;
            case LabelReportElement label:
                RenderLabel(canvas, label);
                break;
            case ImageReportElement image:
                RenderImage(canvas, image);
                break;
            case DateRangeReportElement dateRange:
                RenderDateRange(canvas, dateRange);
                break;
            case SummaryReportElement summary:
                RenderSummary(canvas, summary);
                break;
        }
    }

    private void RenderChart(SKCanvas canvas, ChartReportElement chart)
    {
        var rect = GetScaledRect(chart);

        // Draw background for chart using the element's background color
        var bgPaint = new SKPaint
        {
            Color = ParseColor(chart.BackgroundColor),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };
        canvas.DrawRect(rect, bgPaint);

        // Draw border if configured
        if (chart.BorderThickness > 0)
        {
            var borderPaint = new SKPaint
            {
                Color = ParseColor(chart.BorderColor),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = chart.BorderThickness * _renderScale,
                IsAntialias = true
            };
            canvas.DrawRect(rect, borderPaint);
        }

        // Draw chart title
        if (chart.ShowTitle)
        {
            using var titleFont = new SKFont(_boldTypeface, (float)chart.TitleFontSize * _renderScale);
            using var titlePaint = new SKPaint();
            titlePaint.Color = SKColors.Black;
            titlePaint.IsAntialias = true;

            var title = GetChartTitle(chart.ChartType);
            canvas.DrawText(title, rect.MidX, rect.Top + 20 * _renderScale, SKTextAlign.Center, titleFont, titlePaint);
        }

        // Define chart area with title offset
        var topPadding = (chart.ShowTitle ? 35 : 15) * _renderScale;

        // For bar/line charts, add padding for Y-axis and X-axis labels
        var barChartArea = new SKRect(
            rect.Left + 60 * _renderScale,   // Space for Y-axis labels
            rect.Top + topPadding,
            rect.Right - 15 * _renderScale,  // Right padding
            rect.Bottom - 40 * _renderScale  // Space for X-axis labels
        );

        // For pie charts and no-data, use minimal padding
        var pieChartArea = new SKRect(
            rect.Left + 15 * _renderScale,
            rect.Top + topPadding,
            rect.Right - 15 * _renderScale,
            rect.Bottom - 15 * _renderScale
        );

        // Handle different chart types
        if (IsGeoMapChart(chart.ChartType))
        {
            RenderGeoMap(canvas, pieChartArea, chart);
            return;
        }

        if (IsMultiSeriesChart(chart.ChartType))
        {
            var seriesData = GetMultiSeriesData(chart.ChartType);
            if (seriesData == null || seriesData.Count == 0)
            {
                DrawNoDataPlaceholder(canvas, pieChartArea);
                return;
            }

            // Choose rendering based on chart style
            if (chart.ChartStyle == ReportChartStyle.Line ||
                chart.ChartStyle == ReportChartStyle.StepLine ||
                chart.ChartStyle == ReportChartStyle.Area ||
                chart.ChartStyle == ReportChartStyle.Scatter)
            {
                RenderMultiSeriesLineChart(canvas, barChartArea, seriesData, chart);
            }
            else
            {
                RenderMultiSeriesBarChart(canvas, barChartArea, seriesData, chart);
            }
            return;
        }

        // Get single-series chart data
        var chartData = GetChartDataPoints(chart.ChartType);

        if (chartData == null || chartData.Count == 0)
        {
            DrawNoDataPlaceholder(canvas, pieChartArea);
            return;
        }

        // Render the appropriate chart type
        if (IsDistributionChart(chart.ChartType))
        {
            RenderPieChart(canvas, pieChartArea, chartData, chart);
        }
        else
        {
            // Choose rendering based on chart style
            if (chart.ChartStyle == ReportChartStyle.Line ||
                chart.ChartStyle == ReportChartStyle.StepLine ||
                chart.ChartStyle == ReportChartStyle.Area ||
                chart.ChartStyle == ReportChartStyle.Scatter)
            {
                RenderLineChart(canvas, barChartArea, chartData, chart);
            }
            else
            {
                RenderBarChart(canvas, barChartArea, chartData, chart);
            }
        }
    }

    /// <summary>
    /// Draws a placeholder when no data is available.
    /// </summary>
    private void DrawNoDataPlaceholder(SKCanvas canvas, SKRect chartArea)
    {
        // Just show text, no background rectangle
        using var noDataFont = new SKFont(_defaultTypeface, 12 * _renderScale);
        using var noDataPaint = new SKPaint();
        noDataPaint.Color = SKColors.Gray;
        noDataPaint.IsAntialias = true;
        canvas.DrawText(Tr("No data available"), chartArea.MidX, chartArea.MidY, SKTextAlign.Center, noDataFont, noDataPaint);
    }

    /// <summary>
    /// Checks if the chart type is a GeoMap.
    /// </summary>
    private static bool IsGeoMapChart(ChartDataType chartType)
    {
        return chartType == ChartDataType.WorldMap;
    }

    /// <summary>
    /// Checks if the chart type requires multi-series rendering.
    /// </summary>
    private static bool IsMultiSeriesChart(ChartDataType chartType)
    {
        return chartType is ChartDataType.RevenueVsExpenses
            or ChartDataType.ExpenseVsRevenueReturns
            or ChartDataType.ExpenseVsRevenueLosses
            or ChartDataType.TotalTransactions
            or ChartDataType.AverageTransactionValue;
    }

    /// <summary>
    /// Checks if the chart type should be rendered as a pie chart.
    /// </summary>
    private static bool IsDistributionChart(ChartDataType chartType)
    {
        return chartType is ChartDataType.RevenueDistribution
            or ChartDataType.ExpensesDistribution
            or ChartDataType.ReturnReasons
            or ChartDataType.LossReasons
            or ChartDataType.ReturnsByCategory
            or ChartDataType.LossesByCategory
            or ChartDataType.ReturnsByProduct
            or ChartDataType.LossesByProduct
            or ChartDataType.CountriesOfOrigin
            or ChartDataType.CountriesOfDestination
            or ChartDataType.CompaniesOfOrigin
            or ChartDataType.CompaniesOfDestination
            or ChartDataType.AccountantsTransactions
            or ChartDataType.TopCustomersByRevenue
            or ChartDataType.CustomerPaymentStatus
            or ChartDataType.ActiveVsInactiveCustomers;
    }

    /// <summary>
    /// Gets chart data points for a specific chart type.
    /// </summary>
    private List<ChartDataPoint>? GetChartDataPoints(ChartDataType chartType)
    {
        if (_chartDataService == null)
            return null;

        var data = _chartDataService.GetChartData(chartType);

        if (data is List<ChartDataPoint> dataPoints)
            return dataPoints;

        return null;
    }

    /// <summary>
    /// Gets multi-series chart data.
    /// </summary>
    private List<ChartSeriesData>? GetMultiSeriesData(ChartDataType chartType)
    {
        if (_chartDataService == null)
            return null;

        var data = _chartDataService.GetChartData(chartType);

        if (data is List<ChartSeriesData> seriesData)
            return seriesData;

        return null;
    }

    /// <summary>
    /// Gets world map data for GeoMap chart.
    /// </summary>
    private Dictionary<string, double>? GetWorldMapData()
    {
        if (_chartDataService == null)
            return null;

        var data = _chartDataService.GetChartData(ChartDataType.WorldMap);

        if (data is Dictionary<string, double> mapData)
            return mapData;

        return null;
    }

    /// <summary>
    /// Renders a bar chart using SkiaSharp.
    /// </summary>
    private void RenderBarChart(SKCanvas canvas, SKRect chartArea, List<ChartDataPoint> dataPoints, ChartReportElement chart)
    {
        if (dataPoints.Count == 0) return;

        // Get typeface for chart labels
        var chartTypeface = SKTypeface.FromFamilyName(chart.FontFamily) ?? _defaultTypeface;

        // Calculate value range
        var maxValue = dataPoints.Max(p => p.Value);
        var minValue = dataPoints.Min(p => p.Value);

        // Ensure we have a sensible range
        if (maxValue == 0 && minValue == 0) maxValue = 1;

        // Determine if we have negative values
        var hasNegatives = minValue < 0;

        // Add 20% padding to both ends for better visual appearance
        var paddedMaxValue = maxValue > 0 ? maxValue * 1.2 : maxValue;
        var paddedMinValue = hasNegatives ? minValue * 1.2 : 0;

        // Calculate the total range the chart needs to display
        var totalRange = paddedMaxValue - paddedMinValue;
        if (totalRange == 0) totalRange = 1;

        // Calculate baseline position (where value = 0)
        var baselineY = hasNegatives
            ? chartArea.Top + chartArea.Height * (float)(paddedMaxValue / totalRange)
            : chartArea.Bottom;

        // Draw grid lines
        using var gridPaint = new SKPaint();
        gridPaint.Color = ChartGridColor;
        gridPaint.Style = SKPaintStyle.Stroke;
        gridPaint.StrokeWidth = 1 * _renderScale;
        gridPaint.IsAntialias = true;

        var gridLineCount = 5;
        for (int i = 0; i <= gridLineCount; i++)
        {
            var y = chartArea.Top + (chartArea.Height * i / gridLineCount);
            canvas.DrawLine(chartArea.Left, y, chartArea.Right, y, gridPaint);

            // Draw Y-axis value labels using the full padded range
            var value = paddedMaxValue - totalRange * i / gridLineCount;
            using var yLabelFont = new SKFont(chartTypeface, 9 * _renderScale);
            using var yLabelPaint = new SKPaint();
            yLabelPaint.Color = ChartAxisColor;
            yLabelPaint.IsAntialias = true;

            canvas.DrawText(FormatYAxisValue(value, chart.ChartType), chartArea.Left - 5 * _renderScale, y + 4 * _renderScale, SKTextAlign.Right, yLabelFont, yLabelPaint);
        }

        // Draw Y-axis
        using var axisPaint = new SKPaint();
        axisPaint.Color = ChartAxisColor;
        axisPaint.Style = SKPaintStyle.Stroke;
        axisPaint.StrokeWidth = 1 * _renderScale;
        axisPaint.IsAntialias = true;
        canvas.DrawLine(chartArea.Left, chartArea.Top, chartArea.Left, chartArea.Bottom, axisPaint);

        // Draw X-axis (baseline)
        canvas.DrawLine(chartArea.Left, baselineY, chartArea.Right, baselineY, axisPaint);

        // Calculate bar dimensions
        var barCount = dataPoints.Count;
        var totalBarSpace = chartArea.Width;
        var barSpacing = 4 * _renderScale;
        var maxBarWidth = 50 * _renderScale;
        var barWidth = Math.Min(maxBarWidth, (totalBarSpace - (barSpacing * (barCount + 1))) / barCount);

        // Center the bars in the chart area
        var totalBarsWidth = (barWidth * barCount) + (barSpacing * (barCount - 1));
        var startX = chartArea.Left + (chartArea.Width - totalBarsWidth) / 2;

        // Choose bar color based on chart type
        var barColor = chart.ChartType switch
        {
            ChartDataType.TotalExpenses or ChartDataType.ExpensesDistribution => ChartBarColor, // Blue for expenses (matching WinForms)
            ChartDataType.TotalRevenue or ChartDataType.RevenueDistribution => ChartProfitColor, // Green for revenue
            ChartDataType.TotalProfits => ChartProfitColor,
            _ => ChartBarColor
        };

        using var barPaint = new SKPaint();
        barPaint.Color = barColor;
        barPaint.Style = SKPaintStyle.Fill;
        barPaint.IsAntialias = true;

        // Draw bars
        for (int i = 0; i < dataPoints.Count; i++)
        {
            var point = dataPoints[i];
            var x = startX + (i * (barWidth + barSpacing));

            // Calculate bar height based on value relative to total range
            var barHeight = chartArea.Height * (float)(Math.Abs(point.Value) / totalRange);

            // Handle positive vs negative values
            var barRect = point.Value >= 0
                ? new SKRect(x, baselineY - barHeight, x + barWidth, baselineY)
                : new SKRect(x, baselineY, x + barWidth, baselineY + barHeight);

            canvas.DrawRect(barRect, barPaint);
        }

        // Draw X-axis labels - dynamically based on date range and available width
        using var xLabelFont = new SKFont(chartTypeface, 10 * _renderScale);
        using var xLabelPaint = new SKPaint();
        xLabelPaint.Color = ChartAxisColor;
        xLabelPaint.IsAntialias = true;

        var labelIndices = GetXAxisLabelIndices(dataPoints, chartArea.Width, _renderScale);
        var labelY = chartArea.Bottom + 18 * _renderScale;

        for (int idx = 0; idx < labelIndices.Length; idx++)
        {
            var i = labelIndices[idx];
            var point = dataPoints[i];
            var x = startX + (i * (barWidth + barSpacing));
            var label = FormatXAxisLabel(point);
            var labelX = x + barWidth / 2;

            // Left-align first label, right-align last label, center others
            var align = idx == 0 ? SKTextAlign.Left
                      : idx == labelIndices.Length - 1 ? SKTextAlign.Right
                      : SKTextAlign.Center;

            canvas.DrawText(label, labelX, labelY, align, xLabelFont, xLabelPaint);
        }
    }

    /// <summary>
    /// Renders a line chart using SkiaSharp.
    /// Supports Line, StepLine, and Area styles.
    /// </summary>
    private void RenderLineChart(SKCanvas canvas, SKRect chartArea, List<ChartDataPoint> dataPoints, ChartReportElement chart)
    {
        if (dataPoints.Count == 0) return;

        // Get typeface for chart labels
        var chartTypeface = SKTypeface.FromFamilyName(chart.FontFamily) ?? _defaultTypeface;

        // Calculate value range
        var maxValue = dataPoints.Max(p => p.Value);
        var minValue = dataPoints.Min(p => p.Value);

        // Ensure we have a sensible range
        if (maxValue == 0 && minValue == 0) maxValue = 1;

        // Determine if we have negative values
        var hasNegatives = minValue < 0;

        // Add 20% padding to both ends for better visual appearance
        var paddedMaxValue = maxValue > 0 ? maxValue * 1.2 : maxValue;
        var paddedMinValue = hasNegatives ? minValue * 1.2 : 0;

        // Calculate the total range the chart needs to display
        var totalRange = paddedMaxValue - paddedMinValue;
        if (totalRange == 0) totalRange = 1;

        // Calculate baseline position (where value = 0)
        var baselineY = hasNegatives
            ? chartArea.Top + chartArea.Height * (float)(paddedMaxValue / totalRange)
            : chartArea.Bottom;

        // Draw grid lines
        using var gridPaint = new SKPaint();
        gridPaint.Color = ChartGridColor;
        gridPaint.Style = SKPaintStyle.Stroke;
        gridPaint.StrokeWidth = 1 * _renderScale;
        gridPaint.IsAntialias = true;

        var gridLineCount = 5;
        for (int i = 0; i <= gridLineCount; i++)
        {
            var y = chartArea.Top + (chartArea.Height * i / gridLineCount);
            canvas.DrawLine(chartArea.Left, y, chartArea.Right, y, gridPaint);

            // Draw Y-axis value labels using the full padded range
            var value = paddedMaxValue - totalRange * i / gridLineCount;
            using var yLabelFont = new SKFont(chartTypeface, 9 * _renderScale);
            using var yLabelPaint = new SKPaint();
            yLabelPaint.Color = ChartAxisColor;
            yLabelPaint.IsAntialias = true;

            canvas.DrawText(FormatYAxisValue(value, chart.ChartType), chartArea.Left - 5 * _renderScale, y + 4 * _renderScale, SKTextAlign.Right, yLabelFont, yLabelPaint);
        }

        // Draw axes
        using var axisPaint = new SKPaint();
        axisPaint.Color = ChartAxisColor;
        axisPaint.Style = SKPaintStyle.Stroke;
        axisPaint.StrokeWidth = 1 * _renderScale;
        axisPaint.IsAntialias = true;
        canvas.DrawLine(chartArea.Left, chartArea.Top, chartArea.Left, chartArea.Bottom, axisPaint);
        canvas.DrawLine(chartArea.Left, baselineY, chartArea.Right, baselineY, axisPaint);

        // Choose line color based on chart type
        var lineColor = chart.ChartType switch
        {
            ChartDataType.TotalExpenses or ChartDataType.ExpensesDistribution => ChartBarColor,
            ChartDataType.TotalRevenue or ChartDataType.RevenueDistribution => ChartProfitColor,
            ChartDataType.TotalProfits => ChartProfitColor,
            _ => ChartBarColor
        };

        // Calculate point positions
        var pointCount = dataPoints.Count;
        var points = new SKPoint[pointCount];
        var xSpacing = chartArea.Width / Math.Max(1, pointCount - 1);

        for (int i = 0; i < pointCount; i++)
        {
            var x = chartArea.Left + (i * xSpacing);
            if (pointCount == 1) x = chartArea.MidX; // Center single point
            // Map value to Y position: paddedMaxValue at top, paddedMinValue at bottom
            var y = chartArea.Top + chartArea.Height * (float)((paddedMaxValue - dataPoints[i].Value) / totalRange);
            points[i] = new SKPoint(x, y);
        }

        // Render based on chart style
        if (chart.ChartStyle == ReportChartStyle.Area)
        {
            // Draw filled area under line using spline curve
            using var path = new SKPath();
            path.MoveTo(points[0].X, baselineY);
            path.LineTo(points[0].X, points[0].Y);

            // Use cubic Bezier curves for smooth spline
            for (int i = 0; i < points.Length - 1; i++)
            {
                var p0 = i > 0 ? points[i - 1] : points[i];
                var p1 = points[i];
                var p2 = points[i + 1];
                var p3 = i < points.Length - 2 ? points[i + 2] : points[i + 1];

                // Calculate control points for cubic Bezier (Catmull-Rom to Bezier conversion)
                var tension = 0.5f;
                var cp1 = new SKPoint(
                    p1.X + (p2.X - p0.X) * tension / 3,
                    p1.Y + (p2.Y - p0.Y) * tension / 3);
                var cp2 = new SKPoint(
                    p2.X - (p3.X - p1.X) * tension / 3,
                    p2.Y - (p3.Y - p1.Y) * tension / 3);

                path.CubicTo(cp1, cp2, p2);
            }

            path.LineTo(points[^1].X, baselineY);
            path.Close();

            using var fillPaint = new SKPaint();
            fillPaint.Color = lineColor.WithAlpha(80);
            fillPaint.Style = SKPaintStyle.Fill;
            fillPaint.IsAntialias = true;
            canvas.DrawPath(path, fillPaint);
        }

        // Draw the line (skip for Scatter charts - only points)
        if (chart.ChartStyle != ReportChartStyle.Scatter)
        {
            using var linePaint = new SKPaint();
            linePaint.Color = lineColor;
            linePaint.Style = SKPaintStyle.Stroke;
            linePaint.StrokeWidth = 2 * _renderScale;
            linePaint.IsAntialias = true;
            linePaint.StrokeCap = SKStrokeCap.Round;
            linePaint.StrokeJoin = SKStrokeJoin.Round;

            using var linePath = new SKPath();
            linePath.MoveTo(points[0]);

            if (chart.ChartStyle == ReportChartStyle.StepLine)
            {
                // Step line - horizontal then vertical
                for (int i = 1; i < points.Length; i++)
                {
                    linePath.LineTo(points[i].X, points[i - 1].Y); // Horizontal
                    linePath.LineTo(points[i].X, points[i].Y);     // Vertical
                }
            }
            else
            {
                // Smooth spline using cubic Bezier curves (Catmull-Rom style)
                for (int i = 0; i < points.Length - 1; i++)
                {
                    var p0 = i > 0 ? points[i - 1] : points[i];
                    var p1 = points[i];
                    var p2 = points[i + 1];
                    var p3 = i < points.Length - 2 ? points[i + 2] : points[i + 1];

                    // Calculate control points for cubic Bezier
                    var tension = 0.5f;
                    var cp1 = new SKPoint(
                        p1.X + (p2.X - p0.X) * tension / 3,
                        p1.Y + (p2.Y - p0.Y) * tension / 3);
                    var cp2 = new SKPoint(
                        p2.X - (p3.X - p1.X) * tension / 3,
                        p2.Y - (p3.Y - p1.Y) * tension / 3);

                    linePath.CubicTo(cp1, cp2, p2);
                }
            }

            canvas.DrawPath(linePath, linePaint);
        }

        // Draw points
        using var pointPaint = new SKPaint();
        pointPaint.Color = lineColor;
        pointPaint.Style = SKPaintStyle.Fill;
        pointPaint.IsAntialias = true;
        var pointRadius = 4 * _renderScale;

        foreach (var point in points)
        {
            canvas.DrawCircle(point, pointRadius, pointPaint);
        }

        // Draw X-axis labels - dynamically based on date range and available width
        using var xLabelFont = new SKFont(chartTypeface, 10 * _renderScale);
        using var xLabelPaint = new SKPaint();
        xLabelPaint.Color = ChartAxisColor;
        xLabelPaint.IsAntialias = true;

        var labelIndices = GetXAxisLabelIndices(dataPoints, chartArea.Width, _renderScale);
        var labelY = chartArea.Bottom + 18 * _renderScale;

        for (int idx = 0; idx < labelIndices.Length; idx++)
        {
            var i = labelIndices[idx];
            var label = FormatXAxisLabel(dataPoints[i]);
            var labelX = points[i].X;

            // Left-align first label, right-align last label, center others
            var align = idx == 0 ? SKTextAlign.Left
                      : idx == labelIndices.Length - 1 ? SKTextAlign.Right
                      : SKTextAlign.Center;

            canvas.DrawText(label, labelX, labelY, align, xLabelFont, xLabelPaint);
        }
    }

    /// <summary>
    /// Renders a pie chart using SkiaSharp.
    /// </summary>
    private void RenderPieChart(SKCanvas canvas, SKRect chartArea, List<ChartDataPoint> dataPoints, ChartReportElement chart)
    {
        if (dataPoints.Count == 0) return;

        // Get typeface for chart labels
        var chartTypeface = SKTypeface.FromFamilyName(chart.FontFamily) ?? _defaultTypeface;

        // Calculate total value for percentages
        var total = dataPoints.Sum(p => Math.Abs(p.Value));
        if (total == 0) return;

        // Determine pie chart dimensions - use smaller of width/height with padding
        // Center the pie in the chart area, legend will be positioned to the right of the pie
        var pieSize = Math.Min(chartArea.Width, chartArea.Height) * 0.7f;
        var centerX = chartArea.MidX;
        var centerY = chartArea.MidY;
        var radius = pieSize / 2;

        // Ensure we have a valid radius
        if (radius <= 0) return;

        // Color palette for pie slices
        var colors = new[]
        {
            SKColor.Parse("#6495ED"), // Cornflower Blue
            SKColor.Parse("#EF4444"), // Red
            SKColor.Parse("#22C55E"), // Green
            SKColor.Parse("#F59E0B"), // Amber
            SKColor.Parse("#8B5CF6"), // Purple
            SKColor.Parse("#EC4899"), // Pink
            SKColor.Parse("#14B8A6"), // Teal
            SKColor.Parse("#F97316"), // Orange
        };

        var startAngle = -90f; // Start at top

        // Position legend to the right of the pie
        var legendX = centerX + radius + 20 * _renderScale;
        var legendY = chartArea.Top + 10 * _renderScale;

        using var labelFont = new SKFont(chartTypeface, 9 * _renderScale);
        using var labelPaint = new SKPaint();
        labelPaint.Color = SKColors.Black;
        labelPaint.IsAntialias = true;

        var pieRect = new SKRect(
            centerX - radius,
            centerY - radius,
            centerX + radius,
            centerY + radius
        );

        for (int i = 0; i < dataPoints.Count; i++)
        {
            var point = dataPoints[i];
            var percentage = (float)(Math.Abs(point.Value) / total);
            var sweepAngle = percentage * 360f;
            var color = colors[i % colors.Length];

            // Draw pie slice
            using var slicePaint = new SKPaint();
            slicePaint.Color = color;
            slicePaint.Style = SKPaintStyle.Fill;
            slicePaint.IsAntialias = true;

            // Handle full circle (360 degrees) - SkiaSharp ArcTo doesn't handle this well
            if (sweepAngle >= 359.9f)
            {
                // Draw a full circle instead
                canvas.DrawOval(pieRect, slicePaint);
            }
            else if (sweepAngle > 0)
            {
                // Draw pie slice using arc
                using var path = new SKPath();
                path.MoveTo(centerX, centerY);
                path.ArcTo(pieRect, startAngle, sweepAngle, false);
                path.Close();
                canvas.DrawPath(path, slicePaint);

                // Draw slice border
                using var borderPaint = new SKPaint();
                borderPaint.Color = SKColors.White;
                borderPaint.Style = SKPaintStyle.Stroke;
                borderPaint.StrokeWidth = 2 * _renderScale;
                borderPaint.IsAntialias = true;
                canvas.DrawPath(path, borderPaint);
            }

            // Draw legend item (if chart has show legend enabled and there's space)
            if (chart.ShowLegend && legendX < chartArea.Right - 50 * _renderScale)
            {
                // Legend color box
                var legendBoxSize = 10 * _renderScale;
                var legendBoxRect = new SKRect(legendX, legendY, legendX + legendBoxSize, legendY + legendBoxSize);
                canvas.DrawRect(legendBoxRect, slicePaint);

                // Legend text
                var legendText = point.Label;
                if (legendText.Length > 12) legendText = legendText[..12] + "...";
                legendText = $"{legendText} ({percentage:P0})";

                canvas.DrawText(legendText, legendX + legendBoxSize + 5 * _renderScale, legendY + legendBoxSize - 2 * _renderScale, SKTextAlign.Left, labelFont, labelPaint);

                legendY += 18 * _renderScale;
            }

            startAngle += sweepAngle;
        }
    }

    /// <summary>
    /// Renders a multi-series bar chart (e.g., Revenue vs Expenses).
    /// </summary>
    private void RenderMultiSeriesBarChart(SKCanvas canvas, SKRect chartArea, List<ChartSeriesData> seriesData, ChartReportElement chart)
    {
        if (seriesData.Count == 0) return;

        // Get typeface for chart labels
        var chartTypeface = SKTypeface.FromFamilyName(chart.FontFamily) ?? _defaultTypeface;

        // Get all data points and find max value
        var allDataPoints = seriesData.SelectMany(s => s.DataPoints).ToList();
        if (allDataPoints.Count == 0) return;

        var maxValue = allDataPoints.Max(p => p.Value);
        var minValue = allDataPoints.Min(p => p.Value);
        if (maxValue == 0 && minValue == 0) maxValue = 1;

        // Determine if we have negative values
        var hasNegatives = minValue < 0;

        // Add 20% padding to both ends for better visual appearance
        var paddedMaxValue = maxValue > 0 ? maxValue * 1.2 : maxValue;
        var paddedMinValue = hasNegatives ? minValue * 1.2 : 0;

        // Calculate the total range the chart needs to display
        var totalRange = paddedMaxValue - paddedMinValue;
        if (totalRange == 0) totalRange = 1;

        // Calculate baseline position (where value = 0)
        var baselineY = hasNegatives
            ? chartArea.Top + chartArea.Height * (float)(paddedMaxValue / totalRange)
            : chartArea.Bottom;

        // Draw grid lines
        using var gridPaint = new SKPaint();
        gridPaint.Color = ChartGridColor;
        gridPaint.Style = SKPaintStyle.Stroke;
        gridPaint.StrokeWidth = 1 * _renderScale;
        gridPaint.IsAntialias = true;

        var gridLineCount = 5;
        for (int i = 0; i <= gridLineCount; i++)
        {
            var y = chartArea.Top + (chartArea.Height * i / gridLineCount);
            canvas.DrawLine(chartArea.Left, y, chartArea.Right, y, gridPaint);

            // Draw Y-axis value labels using the full padded range
            var value = paddedMaxValue - totalRange * i / gridLineCount;
            using var yLabelFont = new SKFont(chartTypeface, 9 * _renderScale);
            using var yLabelPaint = new SKPaint();
            yLabelPaint.Color = ChartAxisColor;
            yLabelPaint.IsAntialias = true;

            canvas.DrawText(FormatYAxisValue(value, chart.ChartType), chartArea.Left - 5 * _renderScale, y + 4 * _renderScale, SKTextAlign.Right, yLabelFont, yLabelPaint);
        }

        // Draw axes
        using var axisPaint = new SKPaint();
        axisPaint.Color = ChartAxisColor;
        axisPaint.Style = SKPaintStyle.Stroke;
        axisPaint.StrokeWidth = 1 * _renderScale;
        axisPaint.IsAntialias = true;
        canvas.DrawLine(chartArea.Left, chartArea.Top, chartArea.Left, chartArea.Bottom, axisPaint);
        canvas.DrawLine(chartArea.Left, baselineY, chartArea.Right, baselineY, axisPaint);

        // Get unique labels (X-axis categories) - use formatted dates if available
        var firstSeriesPoints = seriesData.First().DataPoints;
        var labels = firstSeriesPoints.Select(FormatXAxisLabel).ToList();
        var categoryCount = labels.Count;
        var seriesCount = seriesData.Count;

        if (categoryCount == 0) return;

        // Calculate bar dimensions - ensure bars are properly sized and centered
        var categoryWidth = chartArea.Width / categoryCount;
        var barSpacing = 4 * _renderScale;
        var maxBarWidth = 40 * _renderScale;
        var totalBarSpacePerCategory = categoryWidth * 0.8f; // Use 80% of category width for bars
        var barWidth = Math.Min(maxBarWidth, (totalBarSpacePerCategory - (barSpacing * (seriesCount - 1))) / seriesCount);

        // Draw bars for each category
        for (int categoryIndex = 0; categoryIndex < categoryCount; categoryIndex++)
        {
            var categoryCenterX = chartArea.Left + (categoryIndex * categoryWidth) + (categoryWidth / 2);
            var totalBarsWidth = (barWidth * seriesCount) + (barSpacing * (seriesCount - 1));
            var barStartX = categoryCenterX - (totalBarsWidth / 2);

            // Draw bars for each series
            for (int seriesIndex = 0; seriesIndex < seriesCount; seriesIndex++)
            {
                var series = seriesData[seriesIndex];
                if (categoryIndex >= series.DataPoints.Count) continue;

                var point = series.DataPoints[categoryIndex];
                var x = barStartX + (seriesIndex * (barWidth + barSpacing));

                // Calculate bar height based on value relative to total range
                var barHeight = chartArea.Height * (float)(Math.Abs(point.Value) / totalRange);

                var barColor = SKColor.Parse(series.Color);
                using var barPaint = new SKPaint();
                barPaint.Color = barColor;
                barPaint.Style = SKPaintStyle.Fill;
                barPaint.IsAntialias = true;

                SKRect barRect;
                barRect = point.Value >= 0
                    ? new SKRect(x, baselineY - barHeight, x + barWidth, baselineY)
                    : new SKRect(x, baselineY, x + barWidth, baselineY + barHeight);

                canvas.DrawRect(barRect, barPaint);
            }
        }

        // Draw X-axis labels - dynamically based on date range and available width
        using var xLabelFont = new SKFont(chartTypeface, 10 * _renderScale);
        using var xLabelPaint = new SKPaint();
        xLabelPaint.Color = ChartAxisColor;
        xLabelPaint.IsAntialias = true;

        var labelIndices = GetXAxisLabelIndices(firstSeriesPoints, chartArea.Width, _renderScale);
        var labelY = chartArea.Bottom + 18 * _renderScale;

        for (int idx = 0; idx < labelIndices.Length; idx++)
        {
            var categoryIndex = labelIndices[idx];
            var categoryCenterX = chartArea.Left + (categoryIndex * categoryWidth) + (categoryWidth / 2);
            var label = labels[categoryIndex];

            // Left-align first label, right-align last label, center others
            var align = idx == 0 ? SKTextAlign.Left
                      : idx == labelIndices.Length - 1 ? SKTextAlign.Right
                      : SKTextAlign.Center;

            canvas.DrawText(label, categoryCenterX, labelY, align, xLabelFont, xLabelPaint);
        }
    }

    /// <summary>
    /// Renders a multi-series line/area chart (e.g., Revenue vs Expenses as lines).
    /// </summary>
    private void RenderMultiSeriesLineChart(SKCanvas canvas, SKRect chartArea, List<ChartSeriesData> seriesData, ChartReportElement chart)
    {
        if (seriesData.Count == 0) return;

        // Get typeface for chart labels
        var chartTypeface = SKTypeface.FromFamilyName(chart.FontFamily) ?? _defaultTypeface;

        // Get all data points and find max/min values
        var allDataPoints = seriesData.SelectMany(s => s.DataPoints).ToList();
        if (allDataPoints.Count == 0) return;

        var maxValue = allDataPoints.Max(p => p.Value);
        var minValue = allDataPoints.Min(p => p.Value);
        if (maxValue == 0 && minValue == 0) maxValue = 1;

        // Determine if we have negative values
        var hasNegatives = minValue < 0;

        // Add 20% padding to both ends for better visual appearance
        var paddedMaxValue = maxValue > 0 ? maxValue * 1.2 : maxValue;
        var paddedMinValue = hasNegatives ? minValue * 1.2 : 0;

        // Calculate the total range the chart needs to display
        var totalRange = paddedMaxValue - paddedMinValue;
        if (totalRange == 0) totalRange = 1;

        // Calculate baseline position (where value = 0)
        var baselineY = hasNegatives
            ? chartArea.Top + chartArea.Height * (float)(paddedMaxValue / totalRange)
            : chartArea.Bottom;

        // Draw grid lines
        using var gridPaint = new SKPaint();
        gridPaint.Color = ChartGridColor;
        gridPaint.Style = SKPaintStyle.Stroke;
        gridPaint.StrokeWidth = 1 * _renderScale;
        gridPaint.IsAntialias = true;

        var gridLineCount = 5;
        for (int i = 0; i <= gridLineCount; i++)
        {
            var y = chartArea.Top + (chartArea.Height * i / gridLineCount);
            canvas.DrawLine(chartArea.Left, y, chartArea.Right, y, gridPaint);

            // Draw Y-axis value labels using the full padded range
            var value = paddedMaxValue - totalRange * i / gridLineCount;
            using var yLabelFont = new SKFont(chartTypeface, 9 * _renderScale);
            using var yLabelPaint = new SKPaint();
            yLabelPaint.Color = ChartAxisColor;
            yLabelPaint.IsAntialias = true;

            canvas.DrawText(FormatYAxisValue(value, chart.ChartType), chartArea.Left - 5 * _renderScale, y + 4 * _renderScale, SKTextAlign.Right, yLabelFont, yLabelPaint);
        }

        // Draw axes
        using var axisPaint = new SKPaint();
        axisPaint.Color = ChartAxisColor;
        axisPaint.Style = SKPaintStyle.Stroke;
        axisPaint.StrokeWidth = 1 * _renderScale;
        axisPaint.IsAntialias = true;
        canvas.DrawLine(chartArea.Left, chartArea.Top, chartArea.Left, chartArea.Bottom, axisPaint);
        canvas.DrawLine(chartArea.Left, baselineY, chartArea.Right, baselineY, axisPaint);

        // Get unique labels (X-axis categories)
        var firstSeriesPoints = seriesData.First().DataPoints;
        var pointCount = firstSeriesPoints.Count;
        if (pointCount == 0) return;

        var xSpacing = chartArea.Width / Math.Max(1, pointCount - 1);

        // Render each series
        foreach (var series in seriesData)
        {
            if (series.DataPoints.Count == 0) continue;

            var lineColor = SKColor.Parse(series.Color);

            // Calculate points for this series
            var points = new SKPoint[series.DataPoints.Count];
            for (int i = 0; i < series.DataPoints.Count; i++)
            {
                var x = chartArea.Left + (i * xSpacing);
                if (pointCount == 1) x = chartArea.MidX;
                // Map value to Y position: paddedMaxValue at top, paddedMinValue at bottom
                var y = chartArea.Top + chartArea.Height * (float)((paddedMaxValue - series.DataPoints[i].Value) / totalRange);
                points[i] = new SKPoint(x, y);
            }

            // Render based on chart style
            if (chart.ChartStyle == ReportChartStyle.Area)
            {
                // Draw filled area under line
                using var path = new SKPath();
                path.MoveTo(points[0].X, baselineY);
                path.LineTo(points[0].X, points[0].Y);

                // Use cubic Bezier curves for smooth spline
                for (int i = 0; i < points.Length - 1; i++)
                {
                    var p0 = i > 0 ? points[i - 1] : points[i];
                    var p1 = points[i];
                    var p2 = points[i + 1];
                    var p3 = i < points.Length - 2 ? points[i + 2] : points[i + 1];

                    var tension = 0.5f;
                    var cp1 = new SKPoint(
                        p1.X + (p2.X - p0.X) * tension / 3,
                        p1.Y + (p2.Y - p0.Y) * tension / 3);
                    var cp2 = new SKPoint(
                        p2.X - (p3.X - p1.X) * tension / 3,
                        p2.Y - (p3.Y - p1.Y) * tension / 3);

                    path.CubicTo(cp1, cp2, p2);
                }

                path.LineTo(points[^1].X, baselineY);
                path.Close();

                using var fillPaint = new SKPaint();
                fillPaint.Color = lineColor.WithAlpha(80);
                fillPaint.Style = SKPaintStyle.Fill;
                fillPaint.IsAntialias = true;
                canvas.DrawPath(path, fillPaint);
            }

            // Draw the line (skip for Scatter charts - only points)
            if (chart.ChartStyle != ReportChartStyle.Scatter)
            {
                using var linePaint = new SKPaint();
                linePaint.Color = lineColor;
                linePaint.Style = SKPaintStyle.Stroke;
                linePaint.StrokeWidth = 2 * _renderScale;
                linePaint.IsAntialias = true;
                linePaint.StrokeCap = SKStrokeCap.Round;
                linePaint.StrokeJoin = SKStrokeJoin.Round;

                using var linePath = new SKPath();
                linePath.MoveTo(points[0]);

                if (chart.ChartStyle == ReportChartStyle.StepLine)
                {
                    // Step line - horizontal then vertical
                    for (int i = 1; i < points.Length; i++)
                    {
                        linePath.LineTo(points[i].X, points[i - 1].Y);
                        linePath.LineTo(points[i].X, points[i].Y);
                    }
                }
                else
                {
                    // Smooth spline using cubic Bezier curves
                    for (int i = 0; i < points.Length - 1; i++)
                    {
                        var p0 = i > 0 ? points[i - 1] : points[i];
                        var p1 = points[i];
                        var p2 = points[i + 1];
                        var p3 = i < points.Length - 2 ? points[i + 2] : points[i + 1];

                        var tension = 0.5f;
                        var cp1 = new SKPoint(
                            p1.X + (p2.X - p0.X) * tension / 3,
                            p1.Y + (p2.Y - p0.Y) * tension / 3);
                        var cp2 = new SKPoint(
                            p2.X - (p3.X - p1.X) * tension / 3,
                            p2.Y - (p3.Y - p1.Y) * tension / 3);

                        linePath.CubicTo(cp1, cp2, p2);
                    }
                }

                canvas.DrawPath(linePath, linePaint);
            }

            // Draw points
            using var pointPaint = new SKPaint();
            pointPaint.Color = lineColor;
            pointPaint.Style = SKPaintStyle.Fill;
            pointPaint.IsAntialias = true;
            var pointRadius = 4 * _renderScale;

            foreach (var point in points)
            {
                canvas.DrawCircle(point, pointRadius, pointPaint);
            }
        }

        // Draw X-axis labels
        using var xLabelFont = new SKFont(chartTypeface, 10 * _renderScale);
        using var xLabelPaint = new SKPaint();
        xLabelPaint.Color = ChartAxisColor;
        xLabelPaint.IsAntialias = true;

        var labelIndices = GetXAxisLabelIndices(firstSeriesPoints, chartArea.Width, _renderScale);
        var labelY = chartArea.Bottom + 18 * _renderScale;

        for (int idx = 0; idx < labelIndices.Length; idx++)
        {
            var i = labelIndices[idx];
            var label = FormatXAxisLabel(firstSeriesPoints[i]);
            var labelX = chartArea.Left + (i * xSpacing);

            var align = idx == 0 ? SKTextAlign.Left
                      : idx == labelIndices.Length - 1 ? SKTextAlign.Right
                      : SKTextAlign.Center;

            canvas.DrawText(label, labelX, labelY, align, xLabelFont, xLabelPaint);
        }
    }

    /// <summary>
    /// Renders a simplified GeoMap chart showing country data.
    /// Since we can't use LiveChartsCore GeoMap in the Core project, we render a data summary.
    /// </summary>
    private void RenderGeoMap(SKCanvas canvas, SKRect chartArea, ChartReportElement chart)
    {
        // Get typeface for chart labels
        var chartTypeface = SKTypeface.FromFamilyName(chart.FontFamily) ?? _defaultTypeface;

        var mapData = GetWorldMapData();

        if (mapData == null || mapData.Count == 0)
        {
            DrawNoDataPlaceholder(canvas, chartArea);
            return;
        }

        // Sort by value descending
        var sortedData = mapData.OrderByDescending(kvp => kvp.Value).ToList();
        var maxValue = sortedData.Max(kvp => kvp.Value);
        if (maxValue == 0) maxValue = 1;

        // Draw a horizontal bar chart representation of the world map data
        var barHeight = Math.Min(25 * _renderScale, (chartArea.Height - 20 * _renderScale) / Math.Min(sortedData.Count, 10));
        var maxBarWidth = chartArea.Width * 0.6f;
        var labelWidth = chartArea.Width * 0.25f;

        using var labelFont = new SKFont(chartTypeface, 10 * _renderScale);
        using var valueFont = new SKFont(chartTypeface, 9 * _renderScale);
        using var labelPaint = new SKPaint();
        labelPaint.Color = SKColors.Black;
        labelPaint.IsAntialias = true;
        using var valuePaint = new SKPaint();
        valuePaint.Color = ChartAxisColor;
        valuePaint.IsAntialias = true;

        // Color gradient from light to dark blue
        var startColor = SKColor.Parse("#93C5FD"); // Light blue
        var endColor = SKColor.Parse("#1D4ED8");   // Dark blue

        var currentY = chartArea.Top + 10 * _renderScale;
        var displayCount = Math.Min(sortedData.Count, 10);

        for (int i = 0; i < displayCount; i++)
        {
            var kvp = sortedData[i];
            var ratio = (float)(kvp.Value / maxValue);

            // Interpolate color based on value
            var colorRatio = (float)i / Math.Max(displayCount - 1, 1);
            var barColor = new SKColor(
                (byte)(startColor.Red + (endColor.Red - startColor.Red) * colorRatio),
                (byte)(startColor.Green + (endColor.Green - startColor.Green) * colorRatio),
                (byte)(startColor.Blue + (endColor.Blue - startColor.Blue) * colorRatio)
            );

            using var barPaint = new SKPaint();
            barPaint.Color = barColor;
            barPaint.Style = SKPaintStyle.Fill;
            barPaint.IsAntialias = true;

            // Draw country name
            var countryName = kvp.Key;
            if (countryName.Length > 15) countryName = countryName[..15] + "...";
            canvas.DrawText(countryName, chartArea.Left, currentY + barHeight * 0.7f, SKTextAlign.Left, labelFont, labelPaint);

            // Draw bar
            var barStartX = chartArea.Left + labelWidth;
            var barEndX = barStartX + (maxBarWidth * ratio);
            var barRect = new SKRect(barStartX, currentY + 2 * _renderScale, barEndX, currentY + barHeight - 2 * _renderScale);
            canvas.DrawRect(barRect, barPaint);

            // Draw value
            canvas.DrawText($"${kvp.Value:N0}", chartArea.Right - 5 * _renderScale, currentY + barHeight * 0.7f, SKTextAlign.Right, valueFont, valuePaint);

            currentY += barHeight + 3 * _renderScale;
        }

        // Show "and X more..." if there are more countries
        if (sortedData.Count > 10)
        {
            using var moreFont = new SKFont(_defaultTypeface, 9 * _renderScale);
            using var morePaint = new SKPaint();
            morePaint.Color = SKColors.Gray;
            morePaint.IsAntialias = true;
            var moreCountriesText = string.Format(Tr("and {0} more countries..."), sortedData.Count - 10);
            canvas.DrawText(moreCountriesText, chartArea.Left, currentY + 10 * _renderScale, SKTextAlign.Left, moreFont, morePaint);
        }
    }

    private void RenderTable(SKCanvas canvas, TableReportElement table)
    {
        var rect = GetScaledRect(table);
        var columns = GetVisibleColumns(table);
        var columnCount = Math.Max(columns.Count, 1);

        // Get table data
        var tableData = GetTableData(table, columns);

        var headerRowHeight = table.HeaderRowHeight * _renderScale;
        var dataRowHeight = table.DataRowHeight * _renderScale;
        var titleRowHeight = headerRowHeight; // Title uses same height as header

        // Create fonts using table's font settings
        var titleTypeface = SKTypeface.FromFamilyName(table.TitleFontFamily, SKFontStyle.Bold) ?? _boldTypeface;
        var headerTypeface = SKTypeface.FromFamilyName(table.HeaderFontFamily, SKFontStyle.Bold) ?? _boldTypeface;
        var dataTypeface = SKTypeface.FromFamilyName(table.FontFamily) ?? _defaultTypeface;
        using var titleFont = new SKFont(titleTypeface, (float)table.TitleFontSize * _renderScale);
        using var headerFont = new SKFont(headerTypeface, (float)table.HeaderFontSize * _renderScale);
        using var dataFont = new SKFont(dataTypeface, (float)table.FontSize * _renderScale);

        // Get text alignment
        var textAlign = table.TextAlignment switch
        {
            HorizontalTextAlignment.Left => SKTextAlign.Left,
            HorizontalTextAlignment.Right => SKTextAlign.Right,
            _ => SKTextAlign.Center
        };
        var cellPadding = table.CellPadding * _renderScale;

        // Calculate available height for data rows
        var availableHeight = rect.Height;
        if (table.ShowTitle)
            availableHeight -= titleRowHeight;
        if (table.ShowHeaders)
            availableHeight -= headerRowHeight;
        if (table.ShowTotalsRow)
            availableHeight -= headerRowHeight; // Totals row uses header row height

        // Calculate max rows that fit in available space
        var maxRowsByHeight = (int)Math.Floor(availableHeight / dataRowHeight);
        var maxRowsSetting = table.MaxRows > 0 ? table.MaxRows : int.MaxValue;
        var dataRowCount = Math.Min(Math.Min(tableData.Count, maxRowsSetting), Math.Max(0, maxRowsByHeight));

        // Calculate column widths
        var columnWidths = CalculateColumnWidths(table, columns, tableData, rect.Width, headerFont, dataFont);

        // Calculate actual table height (constrained to element bounds)
        var actualHeight = (table.ShowTitle ? titleRowHeight : 0) +
                          (table.ShowHeaders ? headerRowHeight : 0) +
                          (dataRowCount * dataRowHeight) +
                          (table.ShowTotalsRow ? headerRowHeight : 0);
        actualHeight = Math.Min(actualHeight, rect.Height);
        var tableRect = new SKRect(rect.Left, rect.Top, rect.Right, rect.Top + actualHeight);

        // Draw table background only behind actual table content (not the full element bounds)
        canvas.DrawRect(tableRect, new SKPaint { Color = SKColors.White, Style = SKPaintStyle.Fill });

        var currentY = rect.Top;

        // Draw title row
        if (table.ShowTitle)
        {
            var titleRect = new SKRect(rect.Left, currentY, rect.Right, currentY + titleRowHeight);
            var titleFill = new SKPaint { Color = ParseColor(table.TitleBackgroundColor), Style = SKPaintStyle.Fill };
            canvas.DrawRect(titleRect, titleFill);

            // Build title text
            var titleText = BuildTableTitle(table);

            using var titleTextPaint = new SKPaint();
            titleTextPaint.Color = ParseColor(table.TitleTextColor);
            titleTextPaint.IsAntialias = true;

            var x = titleRect.MidX;
            var y = titleRect.MidY + (float)(table.TitleFontSize * _renderScale) / 3;
            canvas.DrawText(titleText, x, y, SKTextAlign.Center, titleFont, titleTextPaint);

            // Draw title bottom border
            if (table.ShowGridLines)
            {
                var gridPaint = new SKPaint { Color = ParseColor(table.GridLineColor), Style = SKPaintStyle.Stroke, StrokeWidth = 1 * _renderScale };
                canvas.DrawLine(rect.Left, titleRect.Bottom, rect.Right, titleRect.Bottom, gridPaint);
            }

            currentY += titleRowHeight;
        }

        // Draw header row
        if (table.ShowHeaders)
        {
            var headerRect = new SKRect(rect.Left, currentY, rect.Right, currentY + headerRowHeight);
            var headerFill = new SKPaint { Color = ParseColor(table.HeaderBackgroundColor), Style = SKPaintStyle.Fill };
            canvas.DrawRect(headerRect, headerFill);

            // Draw column headers
            using var headerTextPaint = new SKPaint();
            headerTextPaint.Color = ParseColor(table.HeaderTextColor);
            headerTextPaint.IsAntialias = true;

            float colX = rect.Left;
            for (int i = 0; i < columns.Count; i++)
            {
                var colWidth = columnWidths[i];
                var y = headerRect.MidY + (float)(table.HeaderFontSize * _renderScale) / 3;
                var truncatedText = TruncateText(columns[i], colWidth - cellPadding * 2, headerFont);
                DrawAlignedText(canvas, truncatedText, colX, colWidth, y, cellPadding, textAlign, headerFont, headerTextPaint);

                // Draw vertical grid line
                if (table.ShowGridLines && i > 0)
                {
                    var gridPaint = new SKPaint { Color = ParseColor(table.GridLineColor), Style = SKPaintStyle.Stroke, StrokeWidth = 1 * _renderScale };
                    canvas.DrawLine(colX, headerRect.Top, colX, headerRect.Bottom, gridPaint);
                }

                colX += colWidth;
            }

            // Draw header bottom border
            if (table.ShowGridLines)
            {
                var gridPaint = new SKPaint { Color = ParseColor(table.GridLineColor), Style = SKPaintStyle.Stroke, StrokeWidth = 1 * _renderScale };
                canvas.DrawLine(rect.Left, headerRect.Bottom, rect.Right, headerRect.Bottom, gridPaint);
            }

            currentY += headerRowHeight;
        }

        // Draw data rows
        using var dataTextPaint = new SKPaint();
        dataTextPaint.Color = ParseColor(table.DataRowTextColor);
        dataTextPaint.IsAntialias = true;

        for (int rowIndex = 0; rowIndex < dataRowCount; rowIndex++)
        {
            var rowData = tableData[rowIndex];
            var rowRect = new SKRect(rect.Left, currentY, rect.Right, currentY + dataRowHeight);

            // Alternate row colors
            var isAlternate = rowIndex % 2 == 1;
            var rowBgColor = table.AlternateRowColors && isAlternate
                ? ParseColor(table.AlternateRowColor)
                : ParseColor(table.BaseRowColor);
            canvas.DrawRect(rowRect, new SKPaint { Color = rowBgColor, Style = SKPaintStyle.Fill });

            // Draw cell data
            float colX = rect.Left;
            for (int colIndex = 0; colIndex < columns.Count; colIndex++)
            {
                var colWidth = columnWidths[colIndex];
                var cellText = colIndex < rowData.Count ? rowData[colIndex] : "";
                var y = rowRect.MidY + (float)(table.FontSize * _renderScale) / 3;
                var truncatedText = TruncateText(cellText, colWidth - cellPadding * 2, dataFont);
                DrawAlignedText(canvas, truncatedText, colX, colWidth, y, cellPadding, textAlign, dataFont, dataTextPaint);

                // Draw vertical grid line
                if (table.ShowGridLines && colIndex > 0)
                {
                    var gridPaint = new SKPaint { Color = ParseColor(table.GridLineColor), Style = SKPaintStyle.Stroke, StrokeWidth = 1 * _renderScale };
                    canvas.DrawLine(colX, rowRect.Top, colX, rowRect.Bottom, gridPaint);
                }

                colX += colWidth;
            }

            // Draw row bottom border
            if (table.ShowGridLines)
            {
                var gridPaint = new SKPaint { Color = ParseColor(table.GridLineColor), Style = SKPaintStyle.Stroke, StrokeWidth = 1 * _renderScale };
                canvas.DrawLine(rect.Left, rowRect.Bottom, rect.Right, rowRect.Bottom, gridPaint);
            }

            currentY += dataRowHeight;
        }

        // Draw totals row
        if (table.ShowTotalsRow)
        {
            var totalsRect = new SKRect(rect.Left, currentY, rect.Right, currentY + headerRowHeight);
            var totalsFill = new SKPaint { Color = ParseColor(table.HeaderBackgroundColor), Style = SKPaintStyle.Fill };
            canvas.DrawRect(totalsRect, totalsFill);

            // Calculate totals from displayed data
            var totals = CalculateTableTotals(tableData, columns, dataRowCount);

            using var totalsTextPaint = new SKPaint();
            totalsTextPaint.Color = ParseColor(table.HeaderTextColor);
            totalsTextPaint.IsAntialias = true;

            float colX = rect.Left;
            for (int i = 0; i < columns.Count; i++)
            {
                var colWidth = columnWidths[i];
                var y = totalsRect.MidY + (float)(table.HeaderFontSize * _renderScale) / 3;

                string cellText;
                if (i == 0)
                    cellText = Tr("Total");
                else if (totals.TryGetValue(columns[i], out var total))
                    cellText = total;
                else
                    cellText = "";

                var truncatedText = TruncateText(cellText, colWidth - cellPadding * 2, headerFont);
                DrawAlignedText(canvas, truncatedText, colX, colWidth, y, cellPadding, textAlign, headerFont, totalsTextPaint);

                // Draw vertical grid line
                if (table.ShowGridLines && i > 0)
                {
                    var gridPaint = new SKPaint { Color = ParseColor(table.GridLineColor), Style = SKPaintStyle.Stroke, StrokeWidth = 1 * _renderScale };
                    canvas.DrawLine(colX, totalsRect.Top, colX, totalsRect.Bottom, gridPaint);
                }

                colX += colWidth;
            }

            currentY += headerRowHeight;
        }

        // Draw outer border
        _borderPaint.Color = ParseColor(table.GridLineColor);
        canvas.DrawRect(tableRect, _borderPaint);
    }

    private string BuildTableTitle(TableReportElement table)
    {
        var parts = new List<string>();

        var typeText = table.TransactionType switch
        {
            TransactionType.Revenue => Tr("Revenue"),
            TransactionType.Expenses => Tr("Expenses"),
            TransactionType.Invoices => Tr("Invoices"),
            TransactionType.Payments => Tr("Payments"),
            TransactionType.RentalRecords => Tr("Rental Records"),
            TransactionType.RentalItems => Tr("Rental Items"),
            TransactionType.Inventory => Tr("Inventory"),
            TransactionType.PurchaseOrders => Tr("Purchase Orders"),
            TransactionType.StockAdjustments => Tr("Stock Adjustments"),
            TransactionType.StockTransfers => Tr("Stock Transfers"),
            TransactionType.Returns => Tr("Returns"),
            TransactionType.LostDamaged => Tr("Lost & Damaged"),
            TransactionType.Receipts => Tr("Receipts"),
            TransactionType.Customers => Tr("Customers"),
            TransactionType.Suppliers => Tr("Suppliers"),
            TransactionType.Products => Tr("Products"),
            TransactionType.Employees => Tr("Employees"),
            TransactionType.Departments => Tr("Departments"),
            TransactionType.Categories => Tr("Categories"),
            TransactionType.Locations => Tr("Locations"),
            TransactionType.Accountants => Tr("Accountants"),
            _ => Tr("Data")
        };
        parts.Add(typeText);

        // Include returns/losses (only relevant for Revenue/Expenses)
        if (table.TransactionType is TransactionType.Revenue or TransactionType.Expenses)
        {
            var inclusions = new List<string>();
            if (table.IncludeReturns)
                inclusions.Add(Tr("Returns"));
            if (table.IncludeLosses)
                inclusions.Add(Tr("Losses"));

            if (inclusions.Count > 0)
                parts.Add("(" + Tr("incl.") + " " + string.Join(", ", inclusions) + ")");
        }

        return string.Join(" ", parts);
    }

    private void DrawAlignedText(SKCanvas canvas, string text, float colX, float colWidth, float y, float padding, SKTextAlign align, SKFont font, SKPaint paint)
    {
        float x = align switch
        {
            SKTextAlign.Left => colX + padding,
            SKTextAlign.Right => colX + colWidth - padding,
            _ => colX + colWidth / 2
        };
        canvas.DrawText(text, x, y, align, font, paint);
    }

    private string TruncateText(string text, float maxWidth, SKFont font)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var textWidth = font.MeasureText(text);
        if (textWidth <= maxWidth)
            return text;

        const string ellipsis = "...";
        var ellipsisWidth = font.MeasureText(ellipsis);

        if (maxWidth <= ellipsisWidth)
            return ellipsis;

        // Binary search for the right length
        var availableWidth = maxWidth - ellipsisWidth;
        var length = text.Length;

        while (length > 0)
        {
            var truncated = text[..length];
            var width = font.MeasureText(truncated);
            if (width <= availableWidth)
                return truncated + ellipsis;
            length--;
        }

        return ellipsis;
    }

    private Dictionary<string, string> CalculateTableTotals(List<List<string>> tableData, List<string> columns, int rowCount)
    {
        var totals = new Dictionary<string, string>();
        var numericColumns = new HashSet<string>
        {
            "Qty", "Unit Price", "Total", "Shipping", "Paid", "Balance",
            "Rate", "Daily Rate", "Weekly Rate", "Monthly Rate",
            "Unit Cost", "In Stock", "Reserved", "Available",
            "Total Qty", "Rented", "Previous", "New",
            "Capacity", "In Use", "Budget", "Salary", "Price", "Cost",
            "Employees", "Transactions"
        };

        for (int colIndex = 0; colIndex < columns.Count; colIndex++)
        {
            var colName = columns[colIndex];
            if (!numericColumns.Contains(colName))
                continue;

            decimal sum = 0;
            for (int rowIndex = 0; rowIndex < rowCount && rowIndex < tableData.Count; rowIndex++)
            {
                var row = tableData[rowIndex];
                if (colIndex < row.Count)
                {
                    var text = row[colIndex];
                    // Parse currency or number - remove currency symbols and commas
                    var cleanText = text.Replace("$", "").Replace(",", "").Replace("€", "").Replace("£", "").Trim();
                    if (decimal.TryParse(cleanText, out var value))
                        sum += value;
                }
            }

            // Format based on column type
            if (colName == "Qty")
                totals[colName] = sum.ToString("N0");
            else
                totals[colName] = FormatCurrency(sum);
        }

        return totals;
    }

    private float[] CalculateColumnWidths(TableReportElement table, List<string> columns, List<List<string>> tableData, float totalWidth, SKFont headerFont, SKFont dataFont)
    {
        var columnCount = columns.Count;
        var columnWidths = new float[columnCount];

        if (!table.AutoSizeColumns || columnCount == 0)
        {
            // Equal width for all columns
            var equalWidth = totalWidth / Math.Max(columnCount, 1);
            for (int i = 0; i < columnCount; i++)
                columnWidths[i] = equalWidth;
            return columnWidths;
        }

        // Calculate max content width for each column
        var maxWidths = new float[columnCount];
        var cellPadding = table.CellPadding * _renderScale * 2; // Padding on both sides

        for (int i = 0; i < columnCount; i++)
        {
            // Measure header text width
            var headerWidth = headerFont.MeasureText(columns[i]) + cellPadding;
            maxWidths[i] = headerWidth;

            // Measure data cells
            foreach (var row in tableData)
            {
                if (i < row.Count)
                {
                    var cellWidth = dataFont.MeasureText(row[i]) + cellPadding;
                    maxWidths[i] = Math.Max(maxWidths[i], cellWidth);
                }
            }
        }

        // Calculate total measured width
        var totalMeasured = maxWidths.Sum();

        if (totalMeasured <= 0)
        {
            // Fallback to equal widths
            var equalWidth = totalWidth / columnCount;
            for (int i = 0; i < columnCount; i++)
                columnWidths[i] = equalWidth;
            return columnWidths;
        }

        // Scale widths proportionally to fit available width
        var scale = totalWidth / totalMeasured;
        for (int i = 0; i < columnCount; i++)
        {
            columnWidths[i] = maxWidths[i] * scale;
        }

        return columnWidths;
    }

    private List<List<string>> GetTableData(TableReportElement table, List<string> columns)
    {
        var result = new List<List<string>>();

        if (_companyData == null)
            return result;

        var tableDataService = new ReportTableDataService(_companyData, _config.Filters);

        switch (table.TransactionType)
        {
            case TransactionType.Revenue:
                foreach (var r in tableDataService.GetRevenueTableData(table))
                    result.Add(MapTransactionRow(r, columns));
                break;
            case TransactionType.Expenses:
                foreach (var r in tableDataService.GetExpensesTableData(table))
                    result.Add(MapTransactionRow(r, columns));
                break;
            case TransactionType.Invoices:
                foreach (var r in tableDataService.GetInvoicesTableData(table))
                    result.Add(columns.Select(col => col switch
                    {
                        "Date" => r.IssueDate.ToString("MM/dd/yyyy"),
                        "ID" => r.InvoiceNumber,
                        "Company" => r.CustomerName,
                        "Due Date" => r.DueDate.ToString("MM/dd/yyyy"),
                        "Total" => FormatCurrency(r.Total),
                        "Paid" => FormatCurrency(r.AmountPaid),
                        "Balance" => FormatCurrency(r.Balance),
                        "Status" => r.Status,
                        _ => ""
                    }).ToList());
                break;
            case TransactionType.Payments:
                foreach (var r in tableDataService.GetPaymentsTableData(table))
                    result.Add(columns.Select(col => col switch
                    {
                        "Date" => r.Date.ToString("MM/dd/yyyy"),
                        "Company" => r.CustomerName,
                        "Total" => FormatCurrency(r.Amount),
                        "Method" => r.PaymentMethod,
                        "ID" => r.ReferenceNumber,
                        "Invoice" => r.InvoiceId,
                        _ => ""
                    }).ToList());
                break;
            case TransactionType.RentalRecords:
                foreach (var r in tableDataService.GetRentalRecordsTableData(table))
                    result.Add(columns.Select(col => col switch
                    {
                        "Item" => r.ItemName,
                        "Company" => r.CustomerName,
                        "Date" => r.StartDate.ToString("MM/dd/yyyy"),
                        "Due Date" => r.DueDate.ToString("MM/dd/yyyy"),
                        "Return Date" => r.ReturnDate?.ToString("MM/dd/yyyy") ?? "",
                        "Rate" => FormatCurrency(r.RateAmount),
                        "Total" => FormatCurrency(r.TotalCost),
                        "Status" => r.Status,
                        _ => ""
                    }).ToList());
                break;
            case TransactionType.RentalItems:
                foreach (var r in tableDataService.GetRentalItemsTableData(table))
                    result.Add(columns.Select(col => col switch
                    {
                        "Name" => r.Name,
                        "Total Qty" => r.TotalQuantity.ToString("N0"),
                        "Available" => r.AvailableQuantity.ToString("N0"),
                        "Rented" => r.RentedQuantity.ToString("N0"),
                        "Daily Rate" => FormatCurrency(r.DailyRate),
                        "Weekly Rate" => FormatCurrency(r.WeeklyRate),
                        "Monthly Rate" => FormatCurrency(r.MonthlyRate),
                        "Status" => r.Status,
                        _ => ""
                    }).ToList());
                break;
            case TransactionType.Inventory:
                foreach (var r in tableDataService.GetInventoryTableData(table))
                    result.Add(columns.Select(col => col switch
                    {
                        "Product" => r.ProductName,
                        "SKU" => r.Sku,
                        "Location" => r.LocationName,
                        "In Stock" => r.InStock.ToString("N0"),
                        "Reserved" => r.Reserved.ToString("N0"),
                        "Available" => r.Available.ToString("N0"),
                        "Unit Cost" => FormatCurrency(r.UnitCost),
                        "Total" => FormatCurrency(r.TotalValue),
                        "Status" => r.Status,
                        _ => ""
                    }).ToList());
                break;
            case TransactionType.PurchaseOrders:
                foreach (var r in tableDataService.GetPurchaseOrdersTableData(table))
                    result.Add(columns.Select(col => col switch
                    {
                        "ID" => r.PoNumber,
                        "Company" => r.SupplierName,
                        "Date" => r.OrderDate.ToString("MM/dd/yyyy"),
                        "Due Date" => r.ExpectedDeliveryDate.ToString("MM/dd/yyyy"),
                        "Total" => FormatCurrency(r.Total),
                        "Status" => r.Status,
                        _ => ""
                    }).ToList());
                break;
            case TransactionType.StockAdjustments:
                foreach (var r in tableDataService.GetStockAdjustmentsTableData(table))
                    result.Add(columns.Select(col => col switch
                    {
                        "Date" => r.Timestamp.ToString("MM/dd/yyyy"),
                        "Product" => r.ProductName,
                        "Type" => r.AdjustmentType,
                        "Qty" => r.Quantity.ToString("N0"),
                        "Previous" => r.PreviousStock.ToString("N0"),
                        "New" => r.NewStock.ToString("N0"),
                        "Reason" => r.Reason,
                        _ => ""
                    }).ToList());
                break;
            case TransactionType.StockTransfers:
                foreach (var r in tableDataService.GetStockTransfersTableData(table))
                    result.Add(columns.Select(col => col switch
                    {
                        "Date" => r.TransferDate.ToString("MM/dd/yyyy"),
                        "Product" => r.ProductName,
                        "From" => r.SourceLocation,
                        "To" => r.DestinationLocation,
                        "Qty" => r.Quantity.ToString("N0"),
                        "Status" => r.Status,
                        _ => ""
                    }).ToList());
                break;
            case TransactionType.Returns:
                foreach (var r in tableDataService.GetReturnsTableData(table))
                    result.Add(columns.Select(col => col switch
                    {
                        "Date" => r.ReturnDate.ToString("MM/dd/yyyy"),
                        "ID" => r.OriginalTransactionId,
                        "Product" => r.ProductName,
                        "Category" => r.CategoryName,
                        "Qty" => r.Quantity.ToString("N0"),
                        "Total" => FormatCurrency(r.RefundAmount),
                        "Reason" => r.Reason,
                        "Status" => r.Status,
                        _ => ""
                    }).ToList());
                break;
            case TransactionType.LostDamaged:
                foreach (var r in tableDataService.GetLossesTableData(table))
                    result.Add(columns.Select(col => col switch
                    {
                        "Date" => r.ReportedDate.ToString("MM/dd/yyyy"),
                        "Product" => r.ProductName,
                        "Category" => r.CategoryName,
                        "Qty" => r.Quantity.ToString("N0"),
                        "Total" => FormatCurrency(r.EstimatedValue),
                        "Reason" => r.Reason,
                        _ => ""
                    }).ToList());
                break;
            case TransactionType.Receipts:
                foreach (var r in tableDataService.GetReceiptsTableData(table))
                    result.Add(columns.Select(col => col switch
                    {
                        "Date" => r.Date.ToString("MM/dd/yyyy"),
                        "File" => r.FileName,
                        "Company" => r.Supplier,
                        "Total" => FormatCurrency(r.Amount),
                        "Source" => r.Source,
                        _ => ""
                    }).ToList());
                break;
            case TransactionType.Customers:
                foreach (var r in tableDataService.GetCustomersTableData(table))
                    result.Add(columns.Select(col => col switch
                    {
                        "Name" => r.Name,
                        "Company" => r.CompanyName,
                        "Country" => r.Country,
                        "Total" => FormatCurrency(r.TotalPurchases),
                        "Status" => r.Status,
                        _ => ""
                    }).ToList());
                break;
            case TransactionType.Suppliers:
                foreach (var r in tableDataService.GetSuppliersTableData(table))
                    result.Add(columns.Select(col => col switch
                    {
                        "Name" => r.Name,
                        "Contact" => r.ContactPerson,
                        "Country" => r.Country,
                        "Terms" => r.PaymentTerms,
                        _ => ""
                    }).ToList());
                break;
            case TransactionType.Products:
                foreach (var r in tableDataService.GetProductsTableData(table))
                    result.Add(columns.Select(col => col switch
                    {
                        "Name" => r.Name,
                        "SKU" => r.Sku,
                        "Category" => r.CategoryName,
                        "Price" => FormatCurrency(r.UnitPrice),
                        "Cost" => FormatCurrency(r.CostPrice),
                        "Status" => r.Status,
                        _ => ""
                    }).ToList());
                break;
            case TransactionType.Employees:
                foreach (var r in tableDataService.GetEmployeesTableData(table))
                    result.Add(columns.Select(col => col switch
                    {
                        "Name" => r.FullName,
                        "Position" => r.Position,
                        "Department" => r.DepartmentName,
                        "Date" => r.HireDate.ToString("MM/dd/yyyy"),
                        "Type" => r.EmploymentType,
                        "Salary" => FormatCurrency(r.SalaryAmount),
                        "Status" => r.Status,
                        _ => ""
                    }).ToList());
                break;
            case TransactionType.Departments:
                foreach (var r in tableDataService.GetDepartmentsTableData(table))
                    result.Add(columns.Select(col => col switch
                    {
                        "Name" => r.Name,
                        "Head" => r.HeadName,
                        "Employees" => r.EmployeeCount.ToString("N0"),
                        "Budget" => FormatCurrency(r.Budget),
                        _ => ""
                    }).ToList());
                break;
            case TransactionType.Categories:
                foreach (var r in tableDataService.GetCategoriesTableData(table))
                    result.Add(columns.Select(col => col switch
                    {
                        "Name" => r.Name,
                        "Type" => r.Type,
                        "Item Type" => r.ItemType,
                        _ => ""
                    }).ToList());
                break;
            case TransactionType.Locations:
                foreach (var r in tableDataService.GetLocationsTableData(table))
                    result.Add(columns.Select(col => col switch
                    {
                        "Name" => r.Name,
                        "Contact" => r.ContactPerson,
                        "Capacity" => r.Capacity.ToString("N0"),
                        "In Use" => r.CurrentUtilization.ToString("N0"),
                        "Utilization" => r.UtilizationPercentage.ToString("F1") + "%",
                        _ => ""
                    }).ToList());
                break;
            case TransactionType.Accountants:
                foreach (var r in tableDataService.GetAccountantsTableData(table))
                    result.Add(columns.Select(col => col switch
                    {
                        "Name" => r.Name,
                        "Email" => r.Email,
                        "Phone" => r.Phone,
                        "Transactions" => r.AssignedTransactions.ToString("N0"),
                        _ => ""
                    }).ToList());
                break;
        }

        return result;
    }

    private List<string> MapTransactionRow(TransactionTableRow r, List<string> columns)
    {
        return columns.Select(col => col switch
        {
            "Date" => r.Date.ToString("MM/dd/yyyy"),
            "ID" => r.TransactionId,
            "Company" => r.CompanyName,
            "Product" => r.ProductName,
            "Qty" => r.Quantity.ToString("N0"),
            "Unit Price" => FormatCurrency(r.UnitPrice),
            "Total" => FormatCurrency(r.Total),
            "Status" => r.Status,
            "Accountant" => r.AccountantName,
            "Shipping" => FormatCurrency(r.ShippingCost),
            _ => ""
        }).ToList();
    }

    private void RenderLabel(SKCanvas canvas, LabelReportElement label)
    {
        var rect = GetScaledRect(label);

        var style = SKFontStyle.Normal;
        if (label.IsBold && label.IsItalic)
            style = SKFontStyle.BoldItalic;
        else if (label.IsBold)
            style = SKFontStyle.Bold;
        else if (label.IsItalic)
            style = SKFontStyle.Italic;

        var typeface = SKTypeface.FromFamilyName(label.FontFamily, style) ?? _defaultTypeface;
        using var font = new SKFont(typeface, (float)label.FontSize * _renderScale);
        using var paint = new SKPaint();
        paint.Color = ParseColor(label.TextColor);
        paint.IsAntialias = true;

        var textAlign = label.HorizontalAlignment switch
        {
            HorizontalTextAlignment.Left => SKTextAlign.Left,
            HorizontalTextAlignment.Center => SKTextAlign.Center,
            HorizontalTextAlignment.Right => SKTextAlign.Right,
            _ => SKTextAlign.Left
        };

        var x = label.HorizontalAlignment switch
        {
            HorizontalTextAlignment.Left => rect.Left,
            HorizontalTextAlignment.Center => rect.MidX,
            HorizontalTextAlignment.Right => rect.Right,
            _ => rect.Left
        };

        var y = label.VerticalAlignment switch
        {
            VerticalTextAlignment.Top => rect.Top + (float)label.FontSize * _renderScale,
            VerticalTextAlignment.Center => rect.MidY + (float)(label.FontSize * _renderScale) / 3,
            VerticalTextAlignment.Bottom => rect.Bottom,
            _ => rect.MidY
        };

        canvas.DrawText(label.Text, x, y, textAlign, font, paint);

        // Draw underline if specified
        if (label.IsUnderline)
        {
            font.GetFontMetrics(out var metrics);
            var underlinePaint = new SKPaint
            {
                Color = paint.Color,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1 * _renderScale,
                IsAntialias = true
            };

            var textWidth = font.MeasureText(label.Text);
            var underlineY = y + (metrics.UnderlinePosition ?? 3 * _renderScale);
            var startX = label.HorizontalAlignment switch
            {
                HorizontalTextAlignment.Left => x,
                HorizontalTextAlignment.Center => x - textWidth / 2,
                HorizontalTextAlignment.Right => x - textWidth,
                _ => x
            };

            canvas.DrawLine(startX, underlineY, startX + textWidth, underlineY, underlinePaint);
        }
    }

    private void RenderImage(SKCanvas canvas, ImageReportElement image)
    {
        var rect = GetScaledRect(image);

        // Calculate corner radius from percentage (based on shorter side)
        var cornerRadius = 0f;
        if (image.CornerRadiusPercent > 0)
        {
            var shortSide = Math.Min(rect.Width, rect.Height);
            cornerRadius = shortSide * image.CornerRadiusPercent / 200f; // 100% = fully round
        }

        // Apply rounded clip if needed
        var hasRoundedCorners = cornerRadius > 0;
        if (hasRoundedCorners)
        {
            canvas.Save();
            var clipPath = new SKPath();
            clipPath.AddRoundRect(rect, cornerRadius, cornerRadius);
            canvas.ClipPath(clipPath, SKClipOperation.Intersect, true);
        }

        // Check if user has set a background color (not transparent)
        var hasUserBackground = !string.IsNullOrEmpty(image.BackgroundColor) && image.BackgroundColor != "#00FFFFFF";

        // Draw background color if set
        if (hasUserBackground)
        {
            var bgPaint = new SKPaint { Color = ParseColor(image.BackgroundColor), Style = SKPaintStyle.Fill };
            canvas.DrawRect(rect, bgPaint);
        }

        // Load and draw image if path specified
        if (!string.IsNullOrEmpty(image.ImagePath))
        {
            // Try to load the image
            try
            {
                var templateStorage = new ReportTemplateStorage();
                var resolvedPath = templateStorage.ResolveImagePath(image.ImagePath);

                if (File.Exists(resolvedPath))
                {
                    using var stream = File.OpenRead(resolvedPath);
                    using var bitmap = SKBitmap.Decode(stream);

                    if (bitmap != null)
                    {
                        var destRect = CalculateImageRect(bitmap, rect, image.ScaleMode);

                        // Apply opacity (convert percentage 0-100 to alpha 0-255)
                        if (image.Opacity < 100)
                        {
                            var alpha = (byte)(image.Opacity * 255 / 100);
                            var paint = new SKPaint { Color = SKColors.White.WithAlpha(alpha) };
                            canvas.DrawBitmap(bitmap, destRect, paint);
                        }
                        else
                        {
                            canvas.DrawBitmap(bitmap, destRect);
                        }
                    }
                }
                else
                {
                    DrawPlaceholder(canvas, rect, Tr("Image not found"), !hasUserBackground);
                }
            }
            catch
            {
                DrawPlaceholder(canvas, rect, Tr("Error loading image"), !hasUserBackground);
            }
        }
        else
        {
            DrawPlaceholder(canvas, rect, Tr("No image selected"), !hasUserBackground);
        }

        // Restore canvas before drawing border (so border draws on top of clip)
        if (hasRoundedCorners)
        {
            canvas.Restore();
        }

        // Draw border
        if (image.BorderThickness > 0 && image.BorderColor != "#00FFFFFF")
        {
            var borderPaint = new SKPaint
            {
                Color = ParseColor(image.BorderColor),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = image.BorderThickness * _renderScale,
                IsAntialias = true
            };
            if (hasRoundedCorners)
            {
                var borderPath = new SKPath();
                borderPath.AddRoundRect(rect, cornerRadius, cornerRadius);
                canvas.DrawPath(borderPath, borderPaint);
            }
            else
            {
                canvas.DrawRect(rect, borderPaint);
            }
        }
    }

    private void RenderDateRange(SKCanvas canvas, DateRangeReportElement dateRange)
    {
        var rect = GetScaledRect(dateRange);

        // Handle bold and italic font styles
        var style = SKFontStyle.Normal;
        if (dateRange.IsBold && dateRange.IsItalic)
            style = SKFontStyle.BoldItalic;
        else if (dateRange.IsBold)
            style = SKFontStyle.Bold;
        else if (dateRange.IsItalic)
            style = SKFontStyle.Italic;

        var typeface = SKTypeface.FromFamilyName(dateRange.FontFamily, style) ?? _defaultTypeface;
        using var font = new SKFont(typeface, (float)dateRange.FontSize * _renderScale);
        using var paint = new SKPaint();
        paint.Color = ParseColor(dateRange.TextColor);
        paint.IsAntialias = true;

        var textAlign = dateRange.HorizontalAlignment switch
        {
            HorizontalTextAlignment.Left => SKTextAlign.Left,
            HorizontalTextAlignment.Center => SKTextAlign.Center,
            HorizontalTextAlignment.Right => SKTextAlign.Right,
            _ => SKTextAlign.Center
        };

        var text = GetDateRangeText(dateRange.DateFormat);

        var x = dateRange.HorizontalAlignment switch
        {
            HorizontalTextAlignment.Left => rect.Left,
            HorizontalTextAlignment.Center => rect.MidX,
            HorizontalTextAlignment.Right => rect.Right,
            _ => rect.MidX
        };

        var y = dateRange.VerticalAlignment switch
        {
            VerticalTextAlignment.Top => rect.Top + (float)dateRange.FontSize * _renderScale,
            VerticalTextAlignment.Center => rect.MidY + (float)(dateRange.FontSize * _renderScale) / 3,
            VerticalTextAlignment.Bottom => rect.Bottom,
            _ => rect.MidY
        };

        canvas.DrawText(text, x, y, textAlign, font, paint);

        // Draw underline if specified
        if (dateRange.IsUnderline)
        {
            font.GetFontMetrics(out var metrics);
            var underlinePaint = new SKPaint
            {
                Color = paint.Color,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1 * _renderScale,
                IsAntialias = true
            };

            var textWidth = font.MeasureText(text);
            var underlineY = y + (metrics.UnderlinePosition ?? 3 * _renderScale);
            var startX = dateRange.HorizontalAlignment switch
            {
                HorizontalTextAlignment.Left => x,
                HorizontalTextAlignment.Center => x - textWidth / 2,
                HorizontalTextAlignment.Right => x - textWidth,
                _ => x - textWidth / 2
            };

            canvas.DrawLine(startX, underlineY, startX + textWidth, underlineY, underlinePaint);
        }
    }

    private void RenderSummary(SKCanvas canvas, SummaryReportElement summary)
    {
        var rect = GetScaledRect(summary);

        // Draw background
        var bgPaint = new SKPaint { Color = ParseColor(summary.BackgroundColor), Style = SKPaintStyle.Fill };
        canvas.DrawRect(rect, bgPaint);

        // Draw border
        if (summary.BorderThickness > 0)
        {
            var borderPaint = new SKPaint
            {
                Color = ParseColor(summary.BorderColor),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = summary.BorderThickness * _renderScale,
                IsAntialias = true
            };
            canvas.DrawRect(rect, borderPaint);
        }

        // Draw summary statistics
        var summaryTypeface = SKTypeface.FromFamilyName(summary.FontFamily) ?? _defaultTypeface;
        using var font = new SKFont(summaryTypeface, (float)summary.FontSize * _renderScale);
        using var textPaint = new SKPaint();
        textPaint.Color = SKColors.Black;
        textPaint.IsAntialias = true;

        var textAlign = summary.HorizontalAlignment switch
        {
            HorizontalTextAlignment.Left => SKTextAlign.Left,
            HorizontalTextAlignment.Center => SKTextAlign.Center,
            HorizontalTextAlignment.Right => SKTextAlign.Right,
            _ => SKTextAlign.Left
        };

        var lines = new List<string>();

        if (summary.ShowTotalRevenue)
        {
            var total = CalculateTotalRevenue(summary);
            var label = summary.TransactionType == TransactionType.Expenses ? Tr("Total Expenses") : Tr("Total Revenue");
            lines.Add($"{label}: ${total:N2}");
        }

        if (summary.ShowTotalTransactions)
        {
            var count = CalculateTransactionCount(summary);
            lines.Add($"{Tr("Transactions")}: {count:N0}");
        }

        if (summary.ShowAverageValue)
        {
            var avg = CalculateAverageValue(summary);
            lines.Add($"{Tr("Average Value")}: ${avg:N2}");
        }

        if (summary.ShowGrowthRate)
        {
            var growth = CalculateGrowthRate(summary);
            var sign = growth >= 0 ? "+" : "";
            lines.Add($"{Tr("Growth Rate")}: {sign}{growth:N1}%");
        }

        var lineHeight = (float)summary.FontSize * _renderScale * 1.5f;
        var startY = summary.VerticalAlignment switch
        {
            VerticalTextAlignment.Top => rect.Top + 10 * _renderScale + (float)summary.FontSize * _renderScale,
            VerticalTextAlignment.Center => rect.MidY - ((lines.Count - 1) * lineHeight / 2),
            VerticalTextAlignment.Bottom => rect.Bottom - (lines.Count * lineHeight) - 10 * _renderScale,
            _ => rect.Top + 10 * _renderScale + (float)summary.FontSize * _renderScale
        };

        var x = summary.HorizontalAlignment switch
        {
            HorizontalTextAlignment.Left => rect.Left + 10 * _renderScale,
            HorizontalTextAlignment.Center => rect.MidX,
            HorizontalTextAlignment.Right => rect.Right - 10 * _renderScale,
            _ => rect.Left + 10 * _renderScale
        };

        for (int i = 0; i < lines.Count; i++)
        {
            canvas.DrawText(lines[i], x, startY + (i * lineHeight), textAlign, font, textPaint);
        }
    }

    #endregion

    #region Header and Footer

    private void RenderHeader(SKCanvas canvas, int width)
    {
        var headerHeight = PageDimensions.HeaderHeight * _renderScale;
        var margin = (float)_config.PageMargins.Left * _renderScale;

        // Draw header background
        var headerRect = new SKRect(0, 0, width, headerHeight);
        canvas.DrawRect(headerRect, new SKPaint { Color = SKColors.White, Style = SKPaintStyle.Fill });

        // Draw title
        canvas.DrawText(_config.Title, width / 2f, headerHeight / 2 + 6 * _renderScale, SKTextAlign.Center, _headerFont, _headerPaint);

        // Draw separator line
        var separatorPaint = new SKPaint
        {
            Color = SKColors.LightGray,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1 * _renderScale
        };
        canvas.DrawLine(margin, headerHeight - 5 * _renderScale, width - margin, headerHeight - 5 * _renderScale, separatorPaint);
    }

    private void RenderFooter(SKCanvas canvas, int width, int height)
    {
        var footerHeight = PageDimensions.FooterHeight * _renderScale;
        var footerY = height - footerHeight;
        var margin = (float)_config.PageMargins.Left * _renderScale;

        // Draw separator line
        var separatorPaint = new SKPaint
        {
            Color = SKColors.LightGray,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1 * _renderScale
        };
        canvas.DrawLine(margin, footerY + 5 * _renderScale, width - margin, footerY + 5 * _renderScale, separatorPaint);

        // Draw timestamp
        using var footerFont = new SKFont(_defaultTypeface, 10 * _renderScale);
        using var footerPaint = new SKPaint();
        footerPaint.Color = SKColors.Gray;
        footerPaint.IsAntialias = true;

        var timeFormat = _config.Use24HourFormat ? "HH:mm" : "h:mm tt";
        var timestamp = DateTime.Now.ToString($"MMM dd, yyyy {timeFormat}");
        canvas.DrawText($"{Tr("Generated")}: {timestamp}", margin, footerY + footerHeight / 2 + 4 * _renderScale, SKTextAlign.Left, footerFont, footerPaint);

        // Draw page number if enabled
        if (_config.ShowPageNumbers)
        {
            var pageText = _config.TotalPageCount > 1
                ? $"{Tr("Page")} {_config.CurrentPageNumber} {Tr("of")} {_config.TotalPageCount}"
                : $"{Tr("Page")} {_config.CurrentPageNumber}";
            canvas.DrawText(pageText, width - margin, footerY + footerHeight / 2 + 4 * _renderScale, SKTextAlign.Right, footerFont, footerPaint);
        }
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Helper method to translate strings using the translation provider.
    /// </summary>
    private string Tr(string text) => _translationProvider.Translate(text);

    private SKRect GetScaledRect(ReportElementBase element)
    {
        return new SKRect(
            (float)element.X * _renderScale,
            (float)element.Y * _renderScale,
            (float)(element.X + element.Width) * _renderScale,
            (float)(element.Y + element.Height) * _renderScale
        );
    }

    /// <summary>
    /// Formats a chart data point's label for X-axis display.
    /// Uses the configured date format if a Date is available, otherwise falls back to Label.
    /// </summary>
    private string FormatXAxisLabel(ChartDataPoint point)
    {
        if (point.Date.HasValue)
        {
            var dateFormat = _config.Filters.DateFormat;
            return point.Date.Value.ToString(dateFormat);
        }
        return point.Label;
    }

    /// <summary>
    /// Calculates which indices should have X-axis labels displayed.
    /// Calculates how many labels fit based on available width and distributes them evenly,
    /// matching how LiveCharts handles axis labels.
    /// </summary>
    private static int[] GetXAxisLabelIndices(List<ChartDataPoint> dataPoints, float chartWidth, float renderScale)
    {
        var totalCount = dataPoints.Count;
        if (totalCount == 0) return [];
        if (totalCount == 1) return [0];
        if (totalCount == 2) return [0, 1];

        // Estimate label width (date format like "MM-dd-yyyy" ~85px at base size)
        var estimatedLabelWidth = 85 * renderScale;

        // Calculate how many labels can fit with spacing
        var maxLabels = Math.Max(2, (int)(chartWidth / estimatedLabelWidth));

        // Don't show more labels than data points
        if (maxLabels >= totalCount)
        {
            return Enumerable.Range(0, totalCount).ToArray();
        }

        // Distribute maxLabels evenly across the data points
        var indices = new List<int>();
        var step = (double)(totalCount - 1) / (maxLabels - 1);

        for (int i = 0; i < maxLabels; i++)
        {
            var index = (int)Math.Round(i * step);
            if (!indices.Contains(index))
            {
                indices.Add(index);
            }
        }

        return indices.OrderBy(x => x).ToArray();
    }

    private static SKColor ParseColor(string colorString)
    {
        if (string.IsNullOrEmpty(colorString))
            return SKColors.Black;

        try
        {
            if (colorString.StartsWith("#"))
            {
                return SKColor.Parse(colorString);
            }
        }
        catch
        {
            // Fall through to default
        }

        return SKColors.Black;
    }

    private static string GetChartTitle(ChartDataType chartType) => chartType.GetDisplayName();

    private static List<string> GetVisibleColumns(TableReportElement table)
    {
        // For Revenue/Expenses, use the existing column visibility flags
        if (table.TransactionType is TransactionType.Revenue or TransactionType.Expenses)
        {
            var columns = new List<string>();
            if (table.ShowDateColumn) columns.Add("Date");
            if (table.ShowTransactionIdColumn) columns.Add("ID");
            if (table.ShowCompanyColumn) columns.Add("Company");
            if (table.ShowProductColumn) columns.Add("Product");
            if (table.ShowQuantityColumn) columns.Add("Qty");
            if (table.ShowTotalColumn) columns.Add("Total");
            if (table.ShowStatusColumn) columns.Add("Status");
            if (table.ShowAccountantColumn) columns.Add("Accountant");
            if (table.ShowShippingColumn) columns.Add("Shipping");
            return columns.Count > 0 ? columns : ["Date", "Description", "Amount"];
        }

        // For other types, return their natural column set
        return table.TransactionType switch
        {
            TransactionType.Invoices => ["Date", "ID", "Company", "Due Date", "Total", "Paid", "Balance", "Status"],
            TransactionType.Payments => ["Date", "Company", "Total", "Method", "ID"],
            TransactionType.RentalRecords => ["Item", "Company", "Date", "Due Date", "Rate", "Total", "Status"],
            TransactionType.RentalItems => ["Name", "Total Qty", "Available", "Rented", "Daily Rate", "Status"],
            TransactionType.Inventory => ["Product", "SKU", "Location", "In Stock", "Available", "Unit Cost", "Total", "Status"],
            TransactionType.PurchaseOrders => ["ID", "Company", "Date", "Due Date", "Total", "Status"],
            TransactionType.StockAdjustments => ["Date", "Product", "Type", "Qty", "Previous", "New", "Reason"],
            TransactionType.StockTransfers => ["Date", "Product", "From", "To", "Qty", "Status"],
            TransactionType.Returns => ["Date", "Product", "Qty", "Total", "Reason", "Status"],
            TransactionType.LostDamaged => ["Date", "Product", "Qty", "Total", "Reason"],
            TransactionType.Receipts => ["Date", "File", "Company", "Total", "Source"],
            TransactionType.Customers => ["Name", "Company", "Country", "Total", "Status"],
            TransactionType.Suppliers => ["Name", "Contact", "Country", "Terms"],
            TransactionType.Products => ["Name", "SKU", "Category", "Price", "Cost", "Status"],
            TransactionType.Employees => ["Name", "Position", "Department", "Date", "Salary", "Status"],
            TransactionType.Departments => ["Name", "Head", "Employees", "Budget"],
            TransactionType.Categories => ["Name", "Type", "Item Type"],
            TransactionType.Locations => ["Name", "Contact", "Capacity", "In Use", "Utilization"],
            TransactionType.Accountants => ["Name", "Email", "Phone", "Transactions"],
            _ => ["Date", "Description", "Amount"]
        };
    }

    private string GetDateRangeText(string dateFormat)
    {
        var start = _config.Filters.StartDate;
        var end = _config.Filters.EndDate;

        if (start.HasValue && end.HasValue)
        {
            return $"{Tr("Period")}: {start.Value.ToString(dateFormat)} {Tr("to")} {end.Value.ToString(dateFormat)}";
        }

        return $"{Tr("Period")}: {Tr("All Time")}";
    }

    private void DrawPlaceholder(SKCanvas canvas, SKRect rect, string message, bool drawBackground = true)
    {
        if (drawBackground)
        {
            var bgPaint = new SKPaint { Color = new SKColor(240, 240, 240), Style = SKPaintStyle.Fill };
            canvas.DrawRect(rect, bgPaint);
        }

        using var font = new SKFont(_defaultTypeface, 10 * _renderScale);
        using var textPaint = new SKPaint();
        textPaint.Color = SKColors.Gray;
        textPaint.IsAntialias = true;
        canvas.DrawText(message, rect.MidX, rect.MidY, SKTextAlign.Center, font, textPaint);

        var borderPaint = new SKPaint { Color = SKColors.Black, Style = SKPaintStyle.Stroke, StrokeWidth = 1 };
        canvas.DrawRect(rect, borderPaint);
    }

    private static SKRect CalculateImageRect(SKBitmap bitmap, SKRect bounds, ImageScaleMode scaleMode)
    {
        if (scaleMode == ImageScaleMode.Stretch)
            return bounds;

        var imageAspect = (float)bitmap.Width / bitmap.Height;
        var boundsAspect = bounds.Width / bounds.Height;

        float width, height;

        switch (scaleMode)
        {
            case ImageScaleMode.Fit:
                if (imageAspect > boundsAspect)
                {
                    width = bounds.Width;
                    height = bounds.Width / imageAspect;
                }
                else
                {
                    width = bounds.Height * imageAspect;
                    height = bounds.Height;
                }
                break;

            case ImageScaleMode.Fill:
                if (imageAspect > boundsAspect)
                {
                    width = bounds.Height * imageAspect;
                    height = bounds.Height;
                }
                else
                {
                    width = bounds.Width;
                    height = bounds.Width / imageAspect;
                }
                break;

            case ImageScaleMode.Center:
                width = Math.Min(bitmap.Width, bounds.Width);
                height = Math.Min(bitmap.Height, bounds.Height);
                break;

            default:
                return bounds;
        }

        var x = bounds.Left + (bounds.Width - width) / 2;
        var y = bounds.Top + (bounds.Height - height) / 2;

        return new SKRect(x, y, x + width, y + height);
    }

    #endregion

    #region Statistics Calculation

    private decimal CalculateTotalRevenue(SummaryReportElement summary)
    {
        if (_companyData == null) return 0;

        var (startDate, endDate) = GetFilterDateRange();

        return summary.TransactionType switch
        {
            TransactionType.Revenue => _companyData.Revenues
                .Where(s => s.Date >= startDate && s.Date <= endDate)
                .Sum(s => s.Total),
            TransactionType.Expenses => _companyData.Expenses
                .Where(p => p.Date >= startDate && p.Date <= endDate)
                .Sum(p => p.Total) ,
            _ => (_companyData.Revenues
                .Where(s => s.Date >= startDate && s.Date <= endDate)
                .Sum(s => s.Total) ) -
                 (_companyData.Expenses
                .Where(p => p.Date >= startDate && p.Date <= endDate)
                .Sum(p => p.Total) )
        };
    }

    private int CalculateTransactionCount(SummaryReportElement summary)
    {
        if (_companyData == null) return 0;

        var (startDate, endDate) = GetFilterDateRange();

        return summary.TransactionType switch
        {
            TransactionType.Revenue => _companyData.Revenues
                .Count(s => s.Date >= startDate && s.Date <= endDate),
            TransactionType.Expenses => _companyData.Expenses
                .Count(p => p.Date >= startDate && p.Date <= endDate),
            _ => _companyData.Revenues.Count(s => s.Date >= startDate && s.Date <= endDate) +
                 _companyData.Expenses.Count(p => p.Date >= startDate && p.Date <= endDate)
        };
    }

    private decimal CalculateAverageValue(SummaryReportElement summary)
    {
        if (_companyData == null) return 0;

        var (startDate, endDate) = GetFilterDateRange();

        var totals = new List<decimal>();

        if (summary.TransactionType is TransactionType.Revenue)
        {
            var sales = _companyData.Revenues
                .Where(s => s.Date >= startDate && s.Date <= endDate)
                .Select(s => s.Total);
            totals.AddRange(sales);
        }

        if (summary.TransactionType is TransactionType.Expenses)
        {
            var purchases = _companyData.Expenses
                .Where(p => p.Date >= startDate && p.Date <= endDate)
                .Select(p => p.Total);
            totals.AddRange(purchases);
        }

        return totals.Count > 0 ? totals.Average() : 0;
    }

    private double CalculateGrowthRate(SummaryReportElement summary)
    {
        if (_companyData == null) return 0;

        var (startDate, endDate) = GetFilterDateRange();

        if (!startDate.HasValue || !endDate.HasValue)
            return 0;

        var periodLength = (endDate.Value - startDate.Value).Days;
        if (periodLength <= 0) return 0;

        var previousStart = startDate.Value.AddDays(-periodLength);
        var previousEnd = startDate.Value.AddSeconds(-1);

        decimal currentPeriod, previousPeriod;

        if (summary.TransactionType == TransactionType.Revenue)
        {
            currentPeriod = _companyData.Revenues
                .Where(s => s.Date >= startDate && s.Date <= endDate)
                .Sum(s => s.Total);
            previousPeriod = _companyData.Revenues
                .Where(s => s.Date >= previousStart && s.Date <= previousEnd)
                .Sum(s => s.Total);
        }
        else if (summary.TransactionType == TransactionType.Expenses)
        {
            currentPeriod = _companyData.Expenses
                .Where(p => p.Date >= startDate && p.Date <= endDate)
                .Sum(p => p.Total);
            previousPeriod = _companyData.Expenses
                .Where(p => p.Date >= previousStart && p.Date <= previousEnd)
                .Sum(p => p.Total);
        }
        else
        {
            currentPeriod = _companyData.Revenues
                    .Where(s => s.Date >= startDate && s.Date <= endDate)
                    .Sum(s => s.Total) -
                _companyData.Expenses
                    .Where(p => p.Date >= startDate && p.Date <= endDate)
                    .Sum(p => p.Total);
            previousPeriod = _companyData.Revenues
                     .Where(s => s.Date >= previousStart && s.Date <= previousEnd)
                     .Sum(s => s.Total) -
                 _companyData.Expenses
                     .Where(p => p.Date >= previousStart && p.Date <= previousEnd)
                     .Sum(p => p.Total);
        }

        if (previousPeriod == 0)
            return currentPeriod > 0 ? 100 : 0;

        return (double)((currentPeriod - previousPeriod) / Math.Abs(previousPeriod) * 100);
    }

    private (DateTime? Start, DateTime? End) GetFilterDateRange()
    {
        if (!string.IsNullOrEmpty(_config.Filters.DatePresetName) &&
            _config.Filters.DatePresetName != DatePresetNames.Custom)
        {
            var (start, end) = DatePresetNames.GetDateRange(_config.Filters.DatePresetName);
            return (start, end);
        }

        return (_config.Filters.StartDate, _config.Filters.EndDate);
    }

    /// <summary>
    /// Formats a currency amount using the company's currency setting.
    /// </summary>
    private string FormatCurrency(decimal amount)
    {
        var currencyCode = _companyData?.Settings.Localization.Currency ?? "USD";
        return CurrencyInfo.FormatAmount(amount, currencyCode);
    }

    #endregion

    public void Dispose()
    {
        _backgroundPaint.Dispose();
        _textPaint.Dispose();
        _headerPaint.Dispose();
        _borderPaint.Dispose();
        _defaultTypeface.Dispose();
        _boldTypeface.Dispose();
        _defaultFont.Dispose();
        _headerFont.Dispose();
        GC.SuppressFinalize(this);
    }
}
