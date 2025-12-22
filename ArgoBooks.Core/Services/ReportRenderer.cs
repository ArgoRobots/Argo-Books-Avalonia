using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
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

    public ReportRenderer(ReportConfiguration config, CompanyData? companyData, float renderScale = 1f)
    {
        _config = config;
        _companyData = companyData;
        _renderScale = renderScale;

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
            using var titlePaint = new SKPaint { Color = SKColors.Black, IsAntialias = true };

            var title = GetChartTitle(chart.ChartType);
            canvas.DrawText(title, rect.MidX, rect.Top + 20 * _renderScale, SKTextAlign.Center, titleFont, titlePaint);
        }

        // Draw placeholder for chart
        var placeholderPaint = new SKPaint
        {
            Color = SKColors.LightGray.WithAlpha(100),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        var chartArea = new SKRect(
            rect.Left + 10 * _renderScale,
            rect.Top + (chart.ShowTitle ? 35 : 10) * _renderScale,
            rect.Right - 10 * _renderScale,
            rect.Bottom - 10 * _renderScale
        );

        canvas.DrawRect(chartArea, placeholderPaint);

        // Draw chart type indicator text
        using var typeFont = new SKFont(_defaultTypeface, 12 * _renderScale);
        using var typePaint = new SKPaint { Color = SKColors.Gray, IsAntialias = true };
        canvas.DrawText($"[{chart.ChartType}]", chartArea.MidX, chartArea.MidY, SKTextAlign.Center, typeFont, typePaint);
    }

    private void RenderTable(SKCanvas canvas, TableReportElement table)
    {
        var rect = GetScaledRect(table);

        // Draw table background
        canvas.DrawRect(rect, new SKPaint { Color = SKColors.White, Style = SKPaintStyle.Fill });

        // Draw header row
        if (table.ShowHeaders)
        {
            var headerRect = new SKRect(rect.Left, rect.Top, rect.Right, rect.Top + table.HeaderRowHeight * _renderScale);
            var headerFill = new SKPaint { Color = ParseColor(table.HeaderBackgroundColor), Style = SKPaintStyle.Fill };
            canvas.DrawRect(headerRect, headerFill);

            // Draw column headers
            var columns = GetVisibleColumns(table);
            var columnWidth = rect.Width / columns.Count;
            using var headerFont = new SKFont(_boldTypeface, (float)table.FontSize * _renderScale);
            using var headerTextPaint = new SKPaint { Color = ParseColor(table.HeaderTextColor), IsAntialias = true };

            for (int i = 0; i < columns.Count; i++)
            {
                var x = rect.Left + (i * columnWidth) + (columnWidth / 2);
                var y = headerRect.MidY + (float)(table.FontSize * _renderScale) / 3;
                canvas.DrawText(columns[i], x, y, SKTextAlign.Center, headerFont, headerTextPaint);
            }
        }

        // Draw grid lines
        if (table.ShowGridLines)
        {
            var gridPaint = new SKPaint
            {
                Color = ParseColor(table.GridLineColor),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1 * _renderScale,
                IsAntialias = true
            };
            canvas.DrawRect(rect, gridPaint);
        }

        // Draw border
        _borderPaint.Color = ParseColor(table.GridLineColor);
        canvas.DrawRect(rect, _borderPaint);
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

        // Draw background
        if (!string.IsNullOrEmpty(image.BackgroundColor) && image.BackgroundColor != "#00FFFFFF")
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

                        // Apply opacity
                        if (image.Opacity < 255)
                        {
                            var paint = new SKPaint { Color = SKColors.White.WithAlpha(image.Opacity) };
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
                    DrawPlaceholder(canvas, rect, "Image not found");
                }
            }
            catch
            {
                DrawPlaceholder(canvas, rect, "Error loading image");
            }
        }
        else
        {
            DrawPlaceholder(canvas, rect, "No image selected");
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

    private void DrawPlaceholder(SKCanvas canvas, SKRect rect, string message)
    {
        var bgPaint = new SKPaint { Color = new SKColor(240, 240, 240), Style = SKPaintStyle.Fill };
        canvas.DrawRect(rect, bgPaint);

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
