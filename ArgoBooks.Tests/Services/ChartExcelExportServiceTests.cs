using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// Tests for the ChartExcelExportService static helpers.
/// Note: GetColumnLetter and TruncateSheetName are private methods;
/// they are tested indirectly through the public export methods.
/// </summary>
public class ChartExcelExportServiceTests
{
    #region ExportChartAsync Tests

    [Fact]
    public async Task ExportChartAsync_WithEmptyLabels_DoesNotThrow()
    {
        var tempFile = Path.GetTempFileName() + ".xlsx";
        try
        {
            await ChartExcelExportService.ExportChartAsync(
                tempFile, "Test Chart", [], [], "Date", "Value");

            // With empty data, the method should return without creating a file
            // or create an empty workbook depending on implementation
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ExportChartAsync_WithValidData_CreatesFile()
    {
        var tempFile = Path.GetTempFileName() + ".xlsx";
        try
        {
            var labels = new[] { "Jan", "Feb", "Mar" };
            var values = new[] { 100.0, 200.0, 300.0 };

            await ChartExcelExportService.ExportChartAsync(
                tempFile, "Revenue", labels, values, "Month", "Amount");

            Assert.True(File.Exists(tempFile));
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ExportChartAsync_WithNullFilePath_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            ChartExcelExportService.ExportChartAsync(
                null!, "Test", ["A"], [1.0]));
    }

    [Fact]
    public async Task ExportChartAsync_WithNullLabels_ThrowsArgumentNullException()
    {
        var tempFile = Path.GetTempFileName() + ".xlsx";
        try
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                ChartExcelExportService.ExportChartAsync(
                    tempFile, "Test", null!, [1.0]));
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    #endregion

    #region ExportMultiSeriesChartAsync Tests

    [Fact]
    public async Task ExportMultiSeriesChartAsync_WithValidData_CreatesFile()
    {
        var tempFile = Path.GetTempFileName() + ".xlsx";
        try
        {
            var labels = new[] { "Jan", "Feb", "Mar" };
            var seriesData = new Dictionary<string, double[]>
            {
                ["Revenue"] = [100.0, 200.0, 300.0],
                ["Expenses"] = [50.0, 75.0, 100.0]
            };

            await ChartExcelExportService.ExportMultiSeriesChartAsync(
                tempFile, "Revenue vs Expenses", labels, seriesData);

            Assert.True(File.Exists(tempFile));
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    #endregion
}
