using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using ArgoBooks.Core.Models.Portal;
using ArgoBooks.Core.Models.Telemetry;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Handles exporting chart data to Google Sheets via the argorobots.com server proxy.
/// </summary>
public class GoogleSheetsService
{
    private const string ExportEndpoint = "https://argorobots.com/api/google/sheets/export";

    private readonly HttpClient _httpClient;
    private readonly IErrorLogger? _errorLogger;
    private readonly ITelemetryManager? _telemetryManager;

    /// <summary>
    /// Creates a new instance of the GoogleSheetsService.
    /// </summary>
    public GoogleSheetsService(IErrorLogger? errorLogger = null, ITelemetryManager? telemetryManager = null)
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        _errorLogger = errorLogger;
        _telemetryManager = telemetryManager;
    }

    /// <summary>
    /// Chart type for Google Sheets visualization.
    /// </summary>
    public enum ChartType
    {
        Line,
        Spline,
        Column,
        Pie,
        Area,
        StepLine,
        Scatter
    }

    /// <summary>
    /// Exports chart data to a new Google Sheets spreadsheet.
    /// </summary>
    public async Task<string?> ExportChartToGoogleSheetsAsync(
        IReadOnlyDictionary<string, double> data,
        string chartTitle,
        ChartType chartType,
        string column1Text,
        string column2Text,
        string companyName,
        string numberFormat = "#,##0.00",
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var success = false;

        try
        {
            var rows = data.OrderBy(x => x.Key)
                .Select(item => new List<object> { item.Key, item.Value })
                .ToList();

            var sheet = new
            {
                name = "Chart Data",
                headers = new[] { column1Text, column2Text },
                rows,
                numberFormat
            };

            var result = await SendExportRequestAsync(
                $"{companyName} - {chartTitle} - {DateTime.Today:yyyy-MM-dd}",
                new[] { sheet },
                new { type = MapChartType(chartType), title = chartTitle },
                true,
                cancellationToken);

            success = result != null;
            return result;
        }
        catch (Exception ex)
        {
            _errorLogger?.LogError(ex, ErrorCategory.Api, "Google Sheets export failed");
            return null;
        }
        finally
        {
            stopwatch.Stop();
            _ = _telemetryManager?.TrackApiCallAsync(
                ApiName.GoogleSheets,
                stopwatch.ElapsedMilliseconds,
                success,
                cancellationToken: cancellationToken);
            if (success)
            {
                _ = _telemetryManager?.TrackExportAsync(
                    ExportType.GoogleSheets,
                    stopwatch.ElapsedMilliseconds,
                    0,
                    cancellationToken);
            }
        }
    }

    /// <summary>
    /// Exports chart data with integer values (counts) to Google Sheets.
    /// </summary>
    public Task<string?> ExportCountChartToGoogleSheetsAsync(
        IReadOnlyDictionary<string, int> data,
        string chartTitle,
        ChartType chartType,
        string column1Text,
        string column2Text,
        string companyName,
        CancellationToken cancellationToken = default)
    {
        var doubleData = data.ToDictionary(kvp => kvp.Key, kvp => (double)kvp.Value);
        return ExportChartToGoogleSheetsAsync(
            doubleData, chartTitle, chartType, column1Text, column2Text, companyName,
            numberFormat: "#,##0", cancellationToken);
    }

    /// <summary>
    /// Exports multi-dataset chart data to Google Sheets.
    /// </summary>
    public async Task<string?> ExportMultiDataSetChartToGoogleSheetsAsync(
        Dictionary<string, Dictionary<string, double>> data,
        string chartTitle,
        ChartType chartType,
        string companyName,
        CancellationToken cancellationToken = default)
    {
        var seriesNames = data.First().Value.Keys.ToList();
        var orderedSeriesNames = seriesNames.OrderBy(x => x.Contains("Sales")).ToList();

        var headers = new List<string> { "Date" };
        headers.AddRange(orderedSeriesNames);

        var rows = data.OrderBy(x => x.Key)
            .Select(dateEntry =>
            {
                var row = new List<object> { dateEntry.Key };
                foreach (var seriesName in orderedSeriesNames)
                {
                    row.Add(dateEntry.Value[seriesName]);
                }
                return row;
            })
            .ToList();

        var sheet = new
        {
            name = "Chart Data",
            headers = headers.ToArray(),
            rows,
            numberFormat = "#,##0.00"
        };

        return await SendExportRequestAsync(
            $"{companyName} - {chartTitle} - {DateTime.Today:yyyy-MM-dd}",
            new[] { sheet },
            new { type = MapChartType(chartType), title = chartTitle },
            true,
            cancellationToken);
    }

    /// <summary>
    /// Exports pre-formatted chart data (from ChartLoaderService) to Google Sheets.
    /// </summary>
    public async Task<string?> ExportFormattedDataToGoogleSheetsAsync(
        List<List<object>> exportData,
        string chartTitle,
        ChartType chartType,
        string companyName,
        CancellationToken cancellationToken = default)
    {
        if (exportData.Count == 0)
            return null;

        // First row is headers
        var headers = exportData[0].Select(h => h.ToString() ?? "").ToArray();
        var rows = exportData.Skip(1).ToList();

        var sheet = new
        {
            name = "Chart Data",
            headers,
            rows,
            numberFormat = "#,##0.00"
        };

        return await SendExportRequestAsync(
            $"{companyName} - {chartTitle} - {DateTime.Today:yyyy-MM-dd}",
            new[] { sheet },
            new { type = MapChartType(chartType), title = chartTitle },
            true,
            cancellationToken);
    }

    /// <summary>
    /// Opens a Google Sheets URL in the default browser.
    /// </summary>
    public static bool OpenInBrowser(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<string?> SendExportRequestAsync(
        string title,
        object sheets,
        object chartConfig,
        bool shareAsReader,
        CancellationToken cancellationToken)
    {
        if (!PortalSettings.IsConfigured)
            return null;

        var requestBody = new
        {
            title,
            sheets,
            chartConfig,
            shareAsReader
        };

        var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, ExportEndpoint);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", PortalSettings.ApiKey);
        request.Headers.Add("X-Api-Key", PortalSettings.ApiKey);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _errorLogger?.LogError($"Google Sheets export proxy error {response.StatusCode}", ErrorCategory.Api, "Google Sheets export");
            return null;
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        if (root.TryGetProperty("success", out var success) && success.GetBoolean()
            && root.TryGetProperty("spreadsheetUrl", out var url))
        {
            return url.GetString();
        }

        return null;
    }

    private static string MapChartType(ChartType chartType)
    {
        return chartType switch
        {
            ChartType.Line => "line",
            ChartType.Spline => "line",
            ChartType.Column => "column",
            ChartType.Pie => "pie",
            ChartType.Area => "area",
            ChartType.StepLine => "stepped_area",
            ChartType.Scatter => "scatter",
            _ => "column"
        };
    }
}
