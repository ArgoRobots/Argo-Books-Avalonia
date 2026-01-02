using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Charts;
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
    private static readonly SKColor ChartExpenseColor = SKColor.Parse("#EF4444"); // Red
    private static readonly SKColor ChartProfitColor = SKColor.Parse("#22C55E"); // Green
    private static readonly SKColor ChartAxisColor = SKColor.Parse("#374151"); // Gray
    private static readonly SKColor ChartGridColor = SKColor.Parse("#E5E7EB"); // Light gray

    public ReportRenderer(ReportConfiguration config, CompanyData? companyData, float renderScale = 1f)
    {
        _config = config;
        _companyData = companyData;
        _renderScale = renderScale;

        // Initialize chart data service for rendering actual charts
        if (companyData != null)
        {
            _chartDataService = new ReportChartDataService(companyData, config.Filters);
        }

        _defaultTypeface = SKTypeface.FromFamilyName("Segoe UI") ?? SKTypeface.Default;
        _boldTypeface = SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold) ?? SKTypeface.Default;

        _defaultFont = new SKFont(_defaultTypeface, 12 * renderScale);
        _headerFont = new SKFont(_boldTypeface, 18 * renderScale);

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
    /// Exports the report to an image file.
    /// </summary>
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

            await using var stream = File.Create(filePath);
            using var document = SKDocument.CreatePdf(stream);
            using var canvas = document.BeginPage(scaledWidth, scaledHeight);

            RenderToCanvas(canvas, scaledWidth, scaledHeight);

            document.EndPage();
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
        var titleHeight = 0f;
        if (chart.ShowTitle)
        {
            using var titleFont = new SKFont(_boldTypeface, (float)chart.TitleFontSize * _renderScale);
            using var titlePaint = new SKPaint { Color = SKColors.Black, IsAntialias = true };

            var title = GetChartTitle(chart.ChartType);
            titleHeight = 25 * _renderScale;
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
            RenderMultiSeriesBarChart(canvas, barChartArea, seriesData, chart);
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
                chart.ChartStyle == ReportChartStyle.Area)
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
        using var noDataPaint = new SKPaint { Color = SKColors.Gray, IsAntialias = true };
        canvas.DrawText("No data available", chartArea.MidX, chartArea.MidY, SKTextAlign.Center, noDataFont, noDataPaint);
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
        return chartType is ChartDataType.SalesVsExpenses
            or ChartDataType.PurchaseVsSaleReturns
            or ChartDataType.PurchaseVsSaleLosses;
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
            or ChartDataType.AccountantsTransactions;
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

        // Calculate value range
        var maxValue = dataPoints.Max(p => Math.Abs(p.Value));
        var minValue = dataPoints.Min(p => p.Value);

        // Ensure we have a sensible range
        if (maxValue == 0) maxValue = 1;

        // Add 20% padding at top so bars don't reach the very top
        // This makes single datapoint charts look better (like LiveChartsCore)
        var paddedMaxValue = maxValue * 1.2;

        // Determine if we have negative values
        var hasNegatives = minValue < 0;
        var baselineY = hasNegatives
            ? chartArea.Top + chartArea.Height * (float)(paddedMaxValue / (paddedMaxValue - minValue))
            : chartArea.Bottom;

        // Draw grid lines
        using var gridPaint = new SKPaint
        {
            Color = ChartGridColor,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1 * _renderScale,
            IsAntialias = true
        };

        var gridLineCount = 5;
        for (int i = 0; i <= gridLineCount; i++)
        {
            var y = chartArea.Top + (chartArea.Height * i / gridLineCount);
            canvas.DrawLine(chartArea.Left, y, chartArea.Right, y, gridPaint);

            // Draw Y-axis value labels using padded max for proper scaling
            var value = paddedMaxValue - (paddedMaxValue - (hasNegatives ? minValue : 0)) * i / gridLineCount;
            using var yLabelFont = new SKFont(_defaultTypeface, 9 * _renderScale);
            using var yLabelPaint = new SKPaint { Color = ChartAxisColor, IsAntialias = true };
            canvas.DrawText($"${value:N0}", chartArea.Left - 5 * _renderScale, y + 4 * _renderScale, SKTextAlign.Right, yLabelFont, yLabelPaint);
        }

        // Draw Y-axis
        using var axisPaint = new SKPaint
        {
            Color = ChartAxisColor,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1 * _renderScale,
            IsAntialias = true
        };
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

        using var barPaint = new SKPaint
        {
            Color = barColor,
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        using var xLabelFont = new SKFont(_defaultTypeface, 8 * _renderScale);
        using var xLabelPaint = new SKPaint { Color = ChartAxisColor, IsAntialias = true };

        // Draw bars
        for (int i = 0; i < dataPoints.Count; i++)
        {
            var point = dataPoints[i];
            var x = startX + (i * (barWidth + barSpacing));

            // Calculate bar height based on value (using padded max for proper scaling)
            var valueRatio = (float)(point.Value / paddedMaxValue);
            var barHeight = chartArea.Height * Math.Abs(valueRatio);

            // Handle positive vs negative values
            SKRect barRect;
            if (point.Value >= 0)
            {
                barRect = new SKRect(x, baselineY - barHeight, x + barWidth, baselineY);
            }
            else
            {
                barRect = new SKRect(x, baselineY, x + barWidth, baselineY + barHeight);
            }

            canvas.DrawRect(barRect, barPaint);

            // Draw X-axis label (rotate if many bars)
            var label = point.Label;
            if (label.Length > 8) label = label[..8] + "...";

            var labelX = x + barWidth / 2;
            var labelY = chartArea.Bottom + 15 * _renderScale;

            // Rotate labels if there are many bars
            if (barCount > 6)
            {
                canvas.Save();
                canvas.RotateDegrees(-45, labelX, labelY);
                canvas.DrawText(label, labelX, labelY, SKTextAlign.Right, xLabelFont, xLabelPaint);
                canvas.Restore();
            }
            else
            {
                canvas.DrawText(label, labelX, labelY, SKTextAlign.Center, xLabelFont, xLabelPaint);
            }
        }
    }

    /// <summary>
    /// Renders a line chart using SkiaSharp.
    /// Supports Line, StepLine, and Area styles.
    /// </summary>
    private void RenderLineChart(SKCanvas canvas, SKRect chartArea, List<ChartDataPoint> dataPoints, ChartReportElement chart)
    {
        if (dataPoints.Count == 0) return;

        // Calculate value range
        var maxValue = dataPoints.Max(p => Math.Abs(p.Value));
        var minValue = dataPoints.Min(p => p.Value);

        // Ensure we have a sensible range
        if (maxValue == 0) maxValue = 1;

        // Add 20% padding at top
        var paddedMaxValue = maxValue * 1.2;

        var hasNegatives = minValue < 0;
        var baselineY = hasNegatives
            ? chartArea.Top + chartArea.Height * (float)(paddedMaxValue / (paddedMaxValue - minValue))
            : chartArea.Bottom;

        // Draw grid lines
        using var gridPaint = new SKPaint
        {
            Color = ChartGridColor,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1 * _renderScale,
            IsAntialias = true
        };

        var gridLineCount = 5;
        for (int i = 0; i <= gridLineCount; i++)
        {
            var y = chartArea.Top + (chartArea.Height * i / gridLineCount);
            canvas.DrawLine(chartArea.Left, y, chartArea.Right, y, gridPaint);

            var value = paddedMaxValue - (paddedMaxValue - (hasNegatives ? minValue : 0)) * i / gridLineCount;
            using var yLabelFont = new SKFont(_defaultTypeface, 9 * _renderScale);
            using var yLabelPaint = new SKPaint { Color = ChartAxisColor, IsAntialias = true };
            canvas.DrawText($"${value:N0}", chartArea.Left - 5 * _renderScale, y + 4 * _renderScale, SKTextAlign.Right, yLabelFont, yLabelPaint);
        }

        // Draw axes
        using var axisPaint = new SKPaint
        {
            Color = ChartAxisColor,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1 * _renderScale,
            IsAntialias = true
        };
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
            var valueRatio = (float)(dataPoints[i].Value / paddedMaxValue);
            var y = baselineY - (chartArea.Height * valueRatio);
            points[i] = new SKPoint(x, y);
        }

        // Render based on chart style
        if (chart.ChartStyle == ReportChartStyle.Area)
        {
            // Draw filled area under line
            using var path = new SKPath();
            path.MoveTo(points[0].X, baselineY);
            foreach (var point in points)
            {
                path.LineTo(point.X, point.Y);
            }
            path.LineTo(points[^1].X, baselineY);
            path.Close();

            using var fillPaint = new SKPaint
            {
                Color = lineColor.WithAlpha(80),
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };
            canvas.DrawPath(path, fillPaint);
        }

        // Draw the line
        using var linePaint = new SKPaint
        {
            Color = lineColor,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2 * _renderScale,
            IsAntialias = true,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round
        };

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
            // Regular line
            for (int i = 1; i < points.Length; i++)
            {
                linePath.LineTo(points[i]);
            }
        }

        canvas.DrawPath(linePath, linePaint);

        // Draw points
        using var pointPaint = new SKPaint
        {
            Color = lineColor,
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };
        var pointRadius = 4 * _renderScale;

        foreach (var point in points)
        {
            canvas.DrawCircle(point, pointRadius, pointPaint);
        }

        // Draw X-axis labels
        using var xLabelFont = new SKFont(_defaultTypeface, 8 * _renderScale);
        using var xLabelPaint = new SKPaint { Color = ChartAxisColor, IsAntialias = true };

        for (int i = 0; i < pointCount; i++)
        {
            var label = dataPoints[i].Label;
            if (label.Length > 8) label = label[..8] + "...";

            var labelX = points[i].X;
            var labelY = chartArea.Bottom + 15 * _renderScale;

            if (pointCount > 6)
            {
                canvas.Save();
                canvas.RotateDegrees(-45, labelX, labelY);
                canvas.DrawText(label, labelX, labelY, SKTextAlign.Right, xLabelFont, xLabelPaint);
                canvas.Restore();
            }
            else
            {
                canvas.DrawText(label, labelX, labelY, SKTextAlign.Center, xLabelFont, xLabelPaint);
            }
        }
    }

    /// <summary>
    /// Renders a pie chart using SkiaSharp.
    /// </summary>
    private void RenderPieChart(SKCanvas canvas, SKRect chartArea, List<ChartDataPoint> dataPoints, ChartReportElement chart)
    {
        if (dataPoints.Count == 0) return;

        // Calculate total value for percentages
        var total = dataPoints.Sum(p => Math.Abs(p.Value));
        if (total == 0) return;

        // Reserve space for legend on the right
        var legendWidth = chart.ShowLegend ? 120 * _renderScale : 0;
        var pieAreaWidth = chartArea.Width - legendWidth;

        // Determine pie chart dimensions (smaller of width/height, with padding)
        var pieSize = Math.Min(pieAreaWidth, chartArea.Height) * 0.8f;
        var centerX = chartArea.Left + pieAreaWidth / 2;
        var centerY = chartArea.MidY;
        var radius = pieSize / 2;

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
        var legendY = chartArea.Top + 10 * _renderScale;
        var legendX = chartArea.Left + pieAreaWidth + 10 * _renderScale;

        using var labelFont = new SKFont(_defaultTypeface, 9 * _renderScale);
        using var labelPaint = new SKPaint { Color = SKColors.Black, IsAntialias = true };

        for (int i = 0; i < dataPoints.Count; i++)
        {
            var point = dataPoints[i];
            var percentage = (float)(Math.Abs(point.Value) / total);
            var sweepAngle = percentage * 360f;
            var color = colors[i % colors.Length];

            // Draw pie slice
            using var slicePaint = new SKPaint
            {
                Color = color,
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };

            var pieRect = new SKRect(
                centerX - radius,
                centerY - radius,
                centerX + radius,
                centerY + radius
            );

            using var path = new SKPath();
            path.MoveTo(centerX, centerY);
            path.ArcTo(pieRect, startAngle, sweepAngle, false);
            path.LineTo(centerX, centerY);
            path.Close();
            canvas.DrawPath(path, slicePaint);

            // Draw slice border
            using var borderPaint = new SKPaint
            {
                Color = SKColors.White,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2 * _renderScale,
                IsAntialias = true
            };
            canvas.DrawPath(path, borderPaint);

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
    /// Renders a multi-series bar chart (e.g., Sales vs Expenses).
    /// </summary>
    private void RenderMultiSeriesBarChart(SKCanvas canvas, SKRect chartArea, List<ChartSeriesData> seriesData, ChartReportElement chart)
    {
        if (seriesData.Count == 0) return;

        // Get all data points and find max value
        var allDataPoints = seriesData.SelectMany(s => s.DataPoints).ToList();
        if (allDataPoints.Count == 0) return;

        var maxValue = allDataPoints.Max(p => Math.Abs(p.Value));
        var minValue = allDataPoints.Min(p => p.Value);
        if (maxValue == 0) maxValue = 1;

        // Add 20% padding at top so bars don't reach the very top
        var paddedMaxValue = maxValue * 1.2;

        var hasNegatives = minValue < 0;
        var baselineY = hasNegatives
            ? chartArea.Top + chartArea.Height * (float)(paddedMaxValue / (paddedMaxValue - minValue))
            : chartArea.Bottom;

        // Draw grid lines
        using var gridPaint = new SKPaint
        {
            Color = ChartGridColor,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1 * _renderScale,
            IsAntialias = true
        };

        var gridLineCount = 5;
        for (int i = 0; i <= gridLineCount; i++)
        {
            var y = chartArea.Top + (chartArea.Height * i / gridLineCount);
            canvas.DrawLine(chartArea.Left, y, chartArea.Right, y, gridPaint);

            var value = paddedMaxValue - (paddedMaxValue - (hasNegatives ? minValue : 0)) * i / gridLineCount;
            using var yLabelFont = new SKFont(_defaultTypeface, 9 * _renderScale);
            using var yLabelPaint = new SKPaint { Color = ChartAxisColor, IsAntialias = true };
            canvas.DrawText($"${value:N0}", chartArea.Left - 5 * _renderScale, y + 4 * _renderScale, SKTextAlign.Right, yLabelFont, yLabelPaint);
        }

        // Draw axes
        using var axisPaint = new SKPaint
        {
            Color = ChartAxisColor,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1 * _renderScale,
            IsAntialias = true
        };
        canvas.DrawLine(chartArea.Left, chartArea.Top, chartArea.Left, chartArea.Bottom, axisPaint);
        canvas.DrawLine(chartArea.Left, baselineY, chartArea.Right, baselineY, axisPaint);

        // Get unique labels (X-axis categories)
        var labels = seriesData.First().DataPoints.Select(p => p.Label).ToList();
        var categoryCount = labels.Count;
        var seriesCount = seriesData.Count;

        if (categoryCount == 0) return;

        // Calculate bar dimensions - ensure bars are properly sized and centered
        var categoryWidth = chartArea.Width / categoryCount;
        var barSpacing = 4 * _renderScale;
        var maxBarWidth = 40 * _renderScale;
        var totalBarSpacePerCategory = categoryWidth * 0.8f; // Use 80% of category width for bars
        var barWidth = Math.Min(maxBarWidth, (totalBarSpacePerCategory - (barSpacing * (seriesCount - 1))) / seriesCount);

        using var xLabelFont = new SKFont(_defaultTypeface, 8 * _renderScale);
        using var xLabelPaint = new SKPaint { Color = ChartAxisColor, IsAntialias = true };

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

                var valueRatio = (float)(point.Value / paddedMaxValue);
                var barHeight = chartArea.Height * Math.Abs(valueRatio);

                var barColor = SKColor.Parse(series.Color);
                using var barPaint = new SKPaint
                {
                    Color = barColor,
                    Style = SKPaintStyle.Fill,
                    IsAntialias = true
                };

                SKRect barRect;
                if (point.Value >= 0)
                {
                    barRect = new SKRect(x, baselineY - barHeight, x + barWidth, baselineY);
                }
                else
                {
                    barRect = new SKRect(x, baselineY, x + barWidth, baselineY + barHeight);
                }

                canvas.DrawRect(barRect, barPaint);
            }

            // Draw X-axis label
            var label = labels[categoryIndex];
            if (label.Length > 10) label = label[..10] + "...";

            var labelX = categoryCenterX;
            var labelY = chartArea.Bottom + 15 * _renderScale;

            if (categoryCount > 6)
            {
                canvas.Save();
                canvas.RotateDegrees(-45, labelX, labelY);
                canvas.DrawText(label, labelX, labelY, SKTextAlign.Right, xLabelFont, xLabelPaint);
                canvas.Restore();
            }
            else
            {
                canvas.DrawText(label, labelX, labelY, SKTextAlign.Center, xLabelFont, xLabelPaint);
            }
        }

        // Draw legend at the top right
        if (chart.ShowLegend)
        {
            var legendY = chartArea.Top + 15 * _renderScale;
            var legendX = chartArea.Right - 100 * _renderScale;

            using var legendFont = new SKFont(_defaultTypeface, 9 * _renderScale);
            using var legendPaint = new SKPaint { Color = SKColors.Black, IsAntialias = true };

            foreach (var series in seriesData)
            {
                var seriesColor = SKColor.Parse(series.Color);
                using var boxPaint = new SKPaint { Color = seriesColor, Style = SKPaintStyle.Fill };

                var boxSize = 10 * _renderScale;
                canvas.DrawRect(legendX, legendY - boxSize + 3 * _renderScale, legendX + boxSize, legendY + 3 * _renderScale, boxPaint);
                canvas.DrawText(series.Name, legendX + boxSize + 5 * _renderScale, legendY, SKTextAlign.Left, legendFont, legendPaint);

                legendY += 15 * _renderScale;
            }
        }
    }

    /// <summary>
    /// Renders a simplified GeoMap chart showing country data.
    /// Since we can't use LiveChartsCore GeoMap in the Core project, we render a data summary.
    /// </summary>
    private void RenderGeoMap(SKCanvas canvas, SKRect chartArea, ChartReportElement chart)
    {
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
        var valueWidth = chartArea.Width * 0.15f;

        using var labelFont = new SKFont(_defaultTypeface, 10 * _renderScale);
        using var valueFont = new SKFont(_defaultTypeface, 9 * _renderScale);
        using var labelPaint = new SKPaint { Color = SKColors.Black, IsAntialias = true };
        using var valuePaint = new SKPaint { Color = ChartAxisColor, IsAntialias = true };

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

            using var barPaint = new SKPaint
            {
                Color = barColor,
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };

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
            using var morePaint = new SKPaint { Color = SKColors.Gray, IsAntialias = true };
            canvas.DrawText($"and {sortedData.Count - 10} more countries...", chartArea.Left, currentY + 10 * _renderScale, SKTextAlign.Left, moreFont, morePaint);
        }
    }

    private void RenderTable(SKCanvas canvas, TableReportElement table)
    {
        var rect = GetScaledRect(table);
        var columns = GetVisibleColumns(table);
        var columnCount = Math.Max(columns.Count, 1);
        var columnWidth = rect.Width / columnCount;

        // Get table data
        var tableData = GetTableData(table, columns);
        var maxRows = table.MaxRows > 0 ? table.MaxRows : 10;
        var dataRowCount = Math.Min(tableData.Count, maxRows);

        var headerRowHeight = table.HeaderRowHeight * _renderScale;
        var dataRowHeight = table.DataRowHeight * _renderScale;

        // Calculate total height needed
        var totalHeight = (table.ShowHeaders ? headerRowHeight : 0) + (dataRowCount * dataRowHeight);
        var tableRect = new SKRect(rect.Left, rect.Top, rect.Right, rect.Top + (float)totalHeight);

        // Draw table background for the entire element bounds (matching design canvas)
        canvas.DrawRect(rect, new SKPaint { Color = SKColors.White, Style = SKPaintStyle.Fill });

        var currentY = rect.Top;

        // Draw header row
        if (table.ShowHeaders)
        {
            var headerRect = new SKRect(rect.Left, currentY, rect.Right, currentY + (float)headerRowHeight);
            var headerFill = new SKPaint { Color = ParseColor(table.HeaderBackgroundColor), Style = SKPaintStyle.Fill };
            canvas.DrawRect(headerRect, headerFill);

            // Draw column headers
            using var headerFont = new SKFont(_boldTypeface, (float)table.FontSize * _renderScale);
            using var headerTextPaint = new SKPaint { Color = ParseColor(table.HeaderTextColor), IsAntialias = true };

            for (int i = 0; i < columns.Count; i++)
            {
                var x = rect.Left + (i * columnWidth) + (columnWidth / 2);
                var y = headerRect.MidY + (float)(table.FontSize * _renderScale) / 3;
                canvas.DrawText(columns[i], x, y, SKTextAlign.Center, headerFont, headerTextPaint);

                // Draw vertical grid line
                if (table.ShowGridLines && i > 0)
                {
                    var gridPaint = new SKPaint { Color = ParseColor(table.GridLineColor), Style = SKPaintStyle.Stroke, StrokeWidth = 1 * _renderScale };
                    canvas.DrawLine(rect.Left + (i * columnWidth), headerRect.Top, rect.Left + (i * columnWidth), headerRect.Bottom, gridPaint);
                }
            }

            // Draw header bottom border
            if (table.ShowGridLines)
            {
                var gridPaint = new SKPaint { Color = ParseColor(table.GridLineColor), Style = SKPaintStyle.Stroke, StrokeWidth = 1 * _renderScale };
                canvas.DrawLine(rect.Left, headerRect.Bottom, rect.Right, headerRect.Bottom, gridPaint);
            }

            currentY += (float)headerRowHeight;
        }

        // Draw data rows
        using var dataFont = new SKFont(_defaultTypeface, (float)table.FontSize * _renderScale);
        using var dataTextPaint = new SKPaint { Color = ParseColor(table.DataRowTextColor), IsAntialias = true };

        for (int rowIndex = 0; rowIndex < dataRowCount; rowIndex++)
        {
            var rowData = tableData[rowIndex];
            var rowRect = new SKRect(rect.Left, currentY, rect.Right, currentY + (float)dataRowHeight);

            // Alternate row colors
            var isAlternate = rowIndex % 2 == 1;
            var rowBgColor = table.AlternateRowColors && isAlternate
                ? ParseColor(table.AlternateRowColor)
                : ParseColor(table.BaseRowColor);
            canvas.DrawRect(rowRect, new SKPaint { Color = rowBgColor, Style = SKPaintStyle.Fill });

            // Draw cell data
            for (int colIndex = 0; colIndex < columns.Count; colIndex++)
            {
                var cellText = colIndex < rowData.Count ? rowData[colIndex] : "";
                var x = rect.Left + (colIndex * columnWidth) + (columnWidth / 2);
                var y = rowRect.MidY + (float)(table.FontSize * _renderScale) / 3;
                canvas.DrawText(cellText, x, y, SKTextAlign.Center, dataFont, dataTextPaint);

                // Draw vertical grid line
                if (table.ShowGridLines && colIndex > 0)
                {
                    var gridPaint = new SKPaint { Color = ParseColor(table.GridLineColor), Style = SKPaintStyle.Stroke, StrokeWidth = 1 * _renderScale };
                    canvas.DrawLine(rect.Left + (colIndex * columnWidth), rowRect.Top, rect.Left + (colIndex * columnWidth), rowRect.Bottom, gridPaint);
                }
            }

            // Draw row bottom border
            if (table.ShowGridLines)
            {
                var gridPaint = new SKPaint { Color = ParseColor(table.GridLineColor), Style = SKPaintStyle.Stroke, StrokeWidth = 1 * _renderScale };
                canvas.DrawLine(rect.Left, rowRect.Bottom, rect.Right, rowRect.Bottom, gridPaint);
            }

            currentY += (float)dataRowHeight;
        }

        // Draw outer border
        _borderPaint.Color = ParseColor(table.GridLineColor);
        canvas.DrawRect(tableRect, _borderPaint);
    }

    private List<List<string>> GetTableData(TableReportElement table, List<string> columns)
    {
        var result = new List<List<string>>();

        // Return empty list for design mode - no placeholder data
        if (_companyData == null)
            return result;

        var transactionType = table.TransactionType;

        // Build a list of transaction records
        var transactions = new List<(DateTime Date, string Id, string Company, string Product, decimal Qty, decimal UnitPrice, decimal Total, string Status, string Accountant, decimal Shipping)>();

        // Get sales (Revenue)
        if (transactionType == TransactionType.Revenue || transactionType == TransactionType.Both)
        {
            foreach (var sale in _companyData.Sales)
            {
                var customerName = _companyData.Customers.FirstOrDefault(c => c.Id == sale.CustomerId)?.Name ?? "N/A";
                var productName = sale.LineItems.FirstOrDefault()?.Description ?? sale.Description;
                var accountantName = _companyData.Accountants.FirstOrDefault(a => a.Id == sale.AccountantId)?.Name ?? "";
                transactions.Add((sale.Date, sale.Id, customerName, productName, sale.Quantity, sale.UnitPrice, sale.Total, sale.PaymentStatus, accountantName, sale.ShippingCost));
            }
        }

        // Get purchases (Expenses)
        if (transactionType == TransactionType.Expenses || transactionType == TransactionType.Both)
        {
            foreach (var purchase in _companyData.Purchases)
            {
                var supplierName = _companyData.Suppliers.FirstOrDefault(s => s.Id == purchase.SupplierId)?.Name ?? "N/A";
                var productName = purchase.LineItems.FirstOrDefault()?.Description ?? purchase.Description;
                var accountantName = _companyData.Accountants.FirstOrDefault(a => a.Id == purchase.AccountantId)?.Name ?? "";
                transactions.Add((purchase.Date, purchase.Id, supplierName, productName, purchase.Quantity, purchase.UnitPrice, purchase.Total, "Paid", accountantName, purchase.ShippingCost));
            }
        }

        // Sort transactions
        var sortOrder = table.SortOrder;
        transactions = sortOrder switch
        {
            TableSortOrder.DateAscending => transactions.OrderBy(t => t.Date).ToList(),
            TableSortOrder.DateDescending => transactions.OrderByDescending(t => t.Date).ToList(),
            TableSortOrder.AmountAscending => transactions.OrderBy(t => t.Total).ToList(),
            TableSortOrder.AmountDescending => transactions.OrderByDescending(t => t.Total).ToList(),
            _ => transactions.OrderByDescending(t => t.Date).ToList()
        };

        // Convert to row data based on visible columns
        foreach (var trans in transactions)
        {
            var row = new List<string>();
            foreach (var col in columns)
            {
                var value = col switch
                {
                    "Date" => trans.Date.ToString("MM/dd/yyyy"),
                    "ID" => trans.Id,
                    "Company" => trans.Company,
                    "Product" => trans.Product,
                    "Qty" => trans.Qty.ToString("N0"),
                    "Unit Price" => trans.UnitPrice.ToString("C2"),
                    "Total" => trans.Total.ToString("C2"),
                    "Status" => trans.Status,
                    "Accountant" => trans.Accountant,
                    "Shipping" => trans.Shipping.ToString("C2"),
                    _ => ""
                };
                row.Add(value);
            }
            result.Add(row);
        }

        return result;
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
        using var paint = new SKPaint { Color = ParseColor(label.TextColor), IsAntialias = true };

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

        canvas.DrawText(label.Text, x, (float)y, textAlign, font, paint);

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
            var underlineY = (float)y + (metrics.UnderlinePosition ?? 3 * _renderScale);
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
                    DrawPlaceholder(canvas, rect, "Image not found", !hasUserBackground);
                }
            }
            catch
            {
                DrawPlaceholder(canvas, rect, "Error loading image", !hasUserBackground);
            }
        }
        else
        {
            DrawPlaceholder(canvas, rect, "No image selected", !hasUserBackground);
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
            canvas.DrawRect(rect, borderPaint);
        }
    }

    private void RenderDateRange(SKCanvas canvas, DateRangeReportElement dateRange)
    {
        var rect = GetScaledRect(dateRange);

        var style = dateRange.IsItalic ? SKFontStyle.Italic : SKFontStyle.Normal;
        var typeface = SKTypeface.FromFamilyName(dateRange.FontFamily, style) ?? _defaultTypeface;
        using var font = new SKFont(typeface, (float)dateRange.FontSize * _renderScale);
        using var paint = new SKPaint { Color = ParseColor(dateRange.TextColor), IsAntialias = true };

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

        canvas.DrawText(text, x, (float)y, textAlign, font, paint);
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
        using var font = new SKFont(_defaultTypeface, (float)summary.FontSize * _renderScale);
        using var textPaint = new SKPaint { Color = SKColors.Black, IsAntialias = true };

        var textAlign = summary.HorizontalAlignment switch
        {
            HorizontalTextAlignment.Left => SKTextAlign.Left,
            HorizontalTextAlignment.Center => SKTextAlign.Center,
            HorizontalTextAlignment.Right => SKTextAlign.Right,
            _ => SKTextAlign.Left
        };

        var lines = new List<string>();

        if (summary.ShowTotalSales)
        {
            var total = CalculateTotalSales(summary);
            var label = summary.TransactionType == TransactionType.Expenses ? "Total Expenses" : "Total Revenue";
            lines.Add($"{label}: ${total:N2}");
        }

        if (summary.ShowTotalTransactions)
        {
            var count = CalculateTransactionCount(summary);
            lines.Add($"Transactions: {count:N0}");
        }

        if (summary.ShowAverageValue)
        {
            var avg = CalculateAverageValue(summary);
            lines.Add($"Average Value: ${avg:N2}");
        }

        if (summary.ShowGrowthRate)
        {
            var growth = CalculateGrowthRate(summary);
            var sign = growth >= 0 ? "+" : "";
            lines.Add($"Growth Rate: {sign}{growth:N1}%");
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
        using var footerPaint = new SKPaint { Color = SKColors.Gray, IsAntialias = true };

        var timestamp = DateTime.Now.ToString("MMM dd, yyyy HH:mm");
        canvas.DrawText($"Generated: {timestamp}", margin, footerY + footerHeight / 2 + 4 * _renderScale, SKTextAlign.Left, footerFont, footerPaint);

        // Draw page number if enabled
        if (_config.ShowPageNumbers)
        {
            canvas.DrawText($"Page {_config.CurrentPageNumber}", width - margin, footerY + footerHeight / 2 + 4 * _renderScale, SKTextAlign.Right, footerFont, footerPaint);
        }
    }

    #endregion

    #region Helpers

    private SKRect GetScaledRect(ReportElementBase element)
    {
        return new SKRect(
            (float)element.X * _renderScale,
            (float)element.Y * _renderScale,
            (float)(element.X + element.Width) * _renderScale,
            (float)(element.Y + element.Height) * _renderScale
        );
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

    private static string GetChartTitle(ChartDataType chartType)
    {
        return chartType switch
        {
            ChartDataType.TotalRevenue => "Total Revenue",
            ChartDataType.RevenueDistribution => "Revenue Distribution",
            ChartDataType.TotalExpenses => "Total Expenses",
            ChartDataType.ExpensesDistribution => "Expense Distribution",
            ChartDataType.TotalProfits => "Total Profits",
            ChartDataType.SalesVsExpenses => "Sales vs Expenses",
            ChartDataType.GrowthRates => "Growth Rates",
            ChartDataType.AverageTransactionValue => "Average Transaction Value",
            ChartDataType.TotalTransactions => "Total Transactions",
            ChartDataType.AverageShippingCosts => "Average Shipping Costs",
            ChartDataType.WorldMap => "Geographic Distribution",
            ChartDataType.CountriesOfOrigin => "Countries of Origin",
            ChartDataType.CountriesOfDestination => "Countries of Destination",
            ChartDataType.CompaniesOfOrigin => "Companies of Origin",
            ChartDataType.AccountantsTransactions => "Transactions by Accountant",
            ChartDataType.ReturnsOverTime => "Returns Over Time",
            ChartDataType.ReturnReasons => "Return Reasons",
            ChartDataType.ReturnFinancialImpact => "Return Financial Impact",
            ChartDataType.ReturnsByCategory => "Returns by Category",
            ChartDataType.ReturnsByProduct => "Returns by Product",
            ChartDataType.PurchaseVsSaleReturns => "Purchase vs Sale Returns",
            ChartDataType.LossesOverTime => "Losses Over Time",
            ChartDataType.LossReasons => "Loss Reasons",
            ChartDataType.LossFinancialImpact => "Loss Financial Impact",
            ChartDataType.LossesByCategory => "Losses by Category",
            ChartDataType.LossesByProduct => "Losses by Product",
            ChartDataType.PurchaseVsSaleLosses => "Purchase vs Sale Losses",
            _ => "Chart"
        };
    }

    private static List<string> GetVisibleColumns(TableReportElement table)
    {
        var columns = new List<string>();

        if (table.ShowDateColumn) columns.Add("Date");
        if (table.ShowTransactionIdColumn) columns.Add("ID");
        if (table.ShowCompanyColumn) columns.Add("Company");
        if (table.ShowProductColumn) columns.Add("Product");
        if (table.ShowQuantityColumn) columns.Add("Qty");
        if (table.ShowUnitPriceColumn) columns.Add("Unit Price");
        if (table.ShowTotalColumn) columns.Add("Total");
        if (table.ShowStatusColumn) columns.Add("Status");
        if (table.ShowAccountantColumn) columns.Add("Accountant");
        if (table.ShowShippingColumn) columns.Add("Shipping");

        return columns.Count > 0 ? columns : ["Date", "Description", "Amount"];
    }

    private string GetDateRangeText(string dateFormat)
    {
        var start = _config.Filters.StartDate;
        var end = _config.Filters.EndDate;

        if (start.HasValue && end.HasValue)
        {
            return $"Period: {start.Value.ToString(dateFormat)} to {end.Value.ToString(dateFormat)}";
        }

        return "Period: All Time";
    }

    private void DrawPlaceholder(SKCanvas canvas, SKRect rect, string message, bool drawBackground = true)
    {
        if (drawBackground)
        {
            var bgPaint = new SKPaint { Color = new SKColor(240, 240, 240), Style = SKPaintStyle.Fill };
            canvas.DrawRect(rect, bgPaint);
        }

        using var font = new SKFont(_defaultTypeface, 10 * _renderScale);
        using var textPaint = new SKPaint { Color = SKColors.Gray, IsAntialias = true };
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

    private decimal CalculateTotalSales(SummaryReportElement summary)
    {
        if (_companyData == null) return 0;

        var (startDate, endDate) = GetFilterDateRange();

        return summary.TransactionType switch
        {
            TransactionType.Revenue => _companyData.Sales?
                .Where(s => s.Date >= startDate && s.Date <= endDate)
                .Sum(s => s.Total) ?? 0,
            TransactionType.Expenses => _companyData.Purchases?
                .Where(p => p.Date >= startDate && p.Date <= endDate)
                .Sum(p => p.Total) ?? 0,
            _ => (_companyData.Sales?
                .Where(s => s.Date >= startDate && s.Date <= endDate)
                .Sum(s => s.Total) ?? 0) -
                 (_companyData.Purchases?
                .Where(p => p.Date >= startDate && p.Date <= endDate)
                .Sum(p => p.Total) ?? 0)
        };
    }

    private int CalculateTransactionCount(SummaryReportElement summary)
    {
        if (_companyData == null) return 0;

        var (startDate, endDate) = GetFilterDateRange();

        return summary.TransactionType switch
        {
            TransactionType.Revenue => _companyData.Sales?
                .Count(s => s.Date >= startDate && s.Date <= endDate) ?? 0,
            TransactionType.Expenses => _companyData.Purchases?
                .Count(p => p.Date >= startDate && p.Date <= endDate) ?? 0,
            _ => (_companyData.Sales?.Count(s => s.Date >= startDate && s.Date <= endDate) ?? 0) +
                 (_companyData.Purchases?.Count(p => p.Date >= startDate && p.Date <= endDate) ?? 0)
        };
    }

    private decimal CalculateAverageValue(SummaryReportElement summary)
    {
        if (_companyData == null) return 0;

        var (startDate, endDate) = GetFilterDateRange();

        var totals = new List<decimal>();

        if (summary.TransactionType is TransactionType.Revenue or TransactionType.Both)
        {
            var sales = _companyData.Sales?
                .Where(s => s.Date >= startDate && s.Date <= endDate)
                .Select(s => s.Total);
            if (sales != null) totals.AddRange(sales);
        }

        if (summary.TransactionType is TransactionType.Expenses or TransactionType.Both)
        {
            var purchases = _companyData.Purchases?
                .Where(p => p.Date >= startDate && p.Date <= endDate)
                .Select(p => p.Total);
            if (purchases != null) totals.AddRange(purchases);
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
            currentPeriod = _companyData.Sales?
                .Where(s => s.Date >= startDate && s.Date <= endDate)
                .Sum(s => s.Total) ?? 0;
            previousPeriod = _companyData.Sales?
                .Where(s => s.Date >= previousStart && s.Date <= previousEnd)
                .Sum(s => s.Total) ?? 0;
        }
        else if (summary.TransactionType == TransactionType.Expenses)
        {
            currentPeriod = _companyData.Purchases?
                .Where(p => p.Date >= startDate && p.Date <= endDate)
                .Sum(p => p.Total) ?? 0;
            previousPeriod = _companyData.Purchases?
                .Where(p => p.Date >= previousStart && p.Date <= previousEnd)
                .Sum(p => p.Total) ?? 0;
        }
        else
        {
            currentPeriod = ((_companyData.Sales?
                .Where(s => s.Date >= startDate && s.Date <= endDate)
                .Sum(s => s.Total) ?? 0) -
                (_companyData.Purchases?
                .Where(p => p.Date >= startDate && p.Date <= endDate)
                .Sum(p => p.Total) ?? 0));
            previousPeriod = ((_companyData.Sales?
                .Where(s => s.Date >= previousStart && s.Date <= previousEnd)
                .Sum(s => s.Total) ?? 0) -
                (_companyData.Purchases?
                .Where(p => p.Date >= previousStart && p.Date <= previousEnd)
                .Sum(p => p.Total) ?? 0));
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
