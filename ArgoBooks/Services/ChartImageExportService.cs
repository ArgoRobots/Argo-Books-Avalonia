using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using ArgoBooks.Localization;
using ArgoBooks.ViewModels;
using LiveChartsCore.SkiaSharpView.Avalonia;
using LiveChartsCore.SkiaSharpView.SKCharts;
using SkiaSharp;

namespace ArgoBooks.Services;

/// <summary>
/// Shared service for exporting charts as images.
/// </summary>
public static class ChartImageExportService
{
    /// <summary>
    /// Default width for exported charts when bounds are invalid.
    /// </summary>
    private const int DefaultExportWidth = 800;

    /// <summary>
    /// Default height for exported charts when bounds are invalid.
    /// </summary>
    private const int DefaultExportHeight = 400;

    /// <summary>
    /// Saves a CartesianChart as an image file with a file picker dialog.
    /// </summary>
    /// <param name="topLevel">The top-level control for the file picker.</param>
    /// <param name="chart">The chart to save.</param>
    /// <param name="suggestedFileName">Suggested file name (without extension).</param>
    /// <returns>True if saved successfully, false if cancelled or failed.</returns>
    public static async Task<bool> SaveChartAsImageAsync(TopLevel topLevel, CartesianChart chart, string suggestedFileName)
    {
        var filePath = await ShowSaveFilePickerAsync(topLevel, suggestedFileName);
        if (filePath == null) return false;

        try
        {
            var format = GetImageFormat(filePath);
            var (width, height) = GetValidDimensions(chart.Bounds);
            var skChart = new SKCartesianChart(chart)
            {
                Width = width,
                Height = height,
                Background = SKColors.Transparent
            };
            skChart.SaveImage(filePath, format, 100);
            System.Diagnostics.Debug.WriteLine($"Chart saved to: {filePath}");
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save chart: {ex.Message}");
            await ShowSaveErrorDialog(ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Saves a PieChart as an image file with a file picker dialog.
    /// </summary>
    public static async Task<bool> SaveChartAsImageAsync(TopLevel topLevel, PieChart chart, string suggestedFileName)
    {
        var filePath = await ShowSaveFilePickerAsync(topLevel, suggestedFileName);
        if (filePath == null) return false;

        try
        {
            var format = GetImageFormat(filePath);
            var (width, height) = GetValidDimensions(chart.Bounds);
            var skChart = new SKPieChart(chart)
            {
                Width = width,
                Height = height,
                Background = SKColors.Transparent
            };
            skChart.SaveImage(filePath, format, 100);
            System.Diagnostics.Debug.WriteLine($"Chart saved to: {filePath}");
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save chart: {ex.Message}");
            await ShowSaveErrorDialog(ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Saves a GeoMap as an image file with a file picker dialog.
    /// </summary>
    public static async Task<bool> SaveChartAsImageAsync(TopLevel topLevel, GeoMap chart, string suggestedFileName)
    {
        var filePath = await ShowSaveFilePickerAsync(topLevel, suggestedFileName);
        if (filePath == null) return false;

        try
        {
            var format = GetImageFormat(filePath);
            var (width, height) = GetValidDimensions(chart.Bounds);
            var skChart = new SKGeoMap(chart)
            {
                Width = width,
                Height = height,
                Background = SKColors.Transparent
            };
            skChart.SaveImage(filePath, format, 100);
            System.Diagnostics.Debug.WriteLine($"Chart saved to: {filePath}");
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save chart: {ex.Message}");
            await ShowSaveErrorDialog(ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Saves any supported chart type as an image file.
    /// </summary>
    public static async Task<bool> SaveChartAsImageAsync(TopLevel topLevel, Control chart, string suggestedFileName)
    {
        return chart switch
        {
            CartesianChart cartesianChart => await SaveChartAsImageAsync(topLevel, cartesianChart, suggestedFileName),
            PieChart pieChart => await SaveChartAsImageAsync(topLevel, pieChart, suggestedFileName),
            GeoMap geoMap => await SaveChartAsImageAsync(topLevel, geoMap, suggestedFileName),
            _ => false
        };
    }

    /// <summary>
    /// Creates a file-safe name from a chart title.
    /// </summary>
    public static string CreateSafeFileName(string chartName)
    {
        var safeName = string.Join("_", chartName.Split(Path.GetInvalidFileNameChars()));
        safeName = safeName.Replace(" ", "_");
        return $"{safeName}_{DateTime.Now:yyyy-MM-dd}";
    }

    /// <summary>
    /// Gets valid dimensions for chart export, using defaults if bounds are invalid.
    /// </summary>
    /// <param name="bounds">The chart's bounds.</param>
    /// <returns>A tuple of (width, height) with valid dimensions.</returns>
    private static (int Width, int Height) GetValidDimensions(Rect bounds)
    {
        var width = (int)bounds.Width;
        var height = (int)bounds.Height;

        // Use defaults if dimensions are invalid (zero, negative, or unreasonably small)
        if (width < 100 || double.IsNaN(bounds.Width) || double.IsInfinity(bounds.Width))
            width = DefaultExportWidth;

        if (height < 50 || double.IsNaN(bounds.Height) || double.IsInfinity(bounds.Height))
            height = DefaultExportHeight;

        return (width, height);
    }

    /// <summary>
    /// Shows the save file picker dialog for image export.
    /// </summary>
    private static async Task<string?> ShowSaveFilePickerAsync(TopLevel topLevel, string suggestedFileName)
    {
        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Chart as Image",
            SuggestedFileName = suggestedFileName,
            DefaultExtension = "png",
            FileTypeChoices =
            [
                new FilePickerFileType("PNG Image") { Patterns = ["*.png"] },
                new FilePickerFileType("JPEG Image") { Patterns = ["*.jpg", "*.jpeg"] }
            ]
        });

        return file?.Path.LocalPath;
    }

    /// <summary>
    /// Gets the image format based on file extension.
    /// </summary>
    private static SKEncodedImageFormat GetImageFormat(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => SKEncodedImageFormat.Jpeg,
            _ => SKEncodedImageFormat.Png
        };
    }

    /// <summary>
    /// Shows an error dialog when saving fails.
    /// </summary>
    private static async Task ShowSaveErrorDialog(string errorMessage)
    {
        var dialog = App.ConfirmationDialog;
        if (dialog != null)
        {
            await dialog.ShowAsync(new ConfirmationDialogOptions
            {
                Title = "Save Failed".Translate(),
                Message = "Failed to save the chart image: {0}".TranslateFormat(errorMessage),
                PrimaryButtonText = "OK".Translate(),
                SecondaryButtonText = null,
                CancelButtonText = null
            });
        }
    }
}
