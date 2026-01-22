using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using System.Diagnostics;
using ArgoBooks.Core.Models.Telemetry;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Handles exporting chart data to Google Sheets.
/// </summary>
public class GoogleSheetsService
{
    private SheetsService? _sheetsService;
    private readonly IErrorLogger? _errorLogger;
    private readonly ITelemetryManager? _telemetryManager;

    /// <summary>
    /// Creates a new instance of the GoogleSheetsService.
    /// </summary>
    public GoogleSheetsService(IErrorLogger? errorLogger = null, ITelemetryManager? telemetryManager = null)
    {
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
        Pie
    }

    /// <summary>
    /// Initializes the Google Sheets service.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if initialization succeeded, false otherwise.</returns>
    private async Task<bool> InitializeServiceAsync(CancellationToken cancellationToken = default)
    {
        if (_sheetsService != null)
        {
            return true;
        }

        var credential = await GoogleCredentialsManager.GetUserCredentialAsync(cancellationToken);

        _sheetsService = new SheetsService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "Argo Books"
        });

        return true;
    }

    /// <summary>
    /// Exports chart data to a new Google Sheets spreadsheet.
    /// </summary>
    /// <param name="data">Dictionary of label to value pairs.</param>
    /// <param name="chartTitle">Title of the chart.</param>
    /// <param name="chartType">Type of chart to create.</param>
    /// <param name="column1Text">Header text for the first column (labels).</param>
    /// <param name="column2Text">Header text for the second column (values).</param>
    /// <param name="companyName">Name of the company for the spreadsheet title.</param>
    /// <param name="numberFormat">Number format pattern (default: "#,##0.00").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The URL of the created spreadsheet, or null if export failed.</returns>
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
            if (!await InitializeServiceAsync(cancellationToken))
            {
                return null;
            }

            const string sheetName = "Chart Data";

            var spreadsheet = await CreateSpreadsheetAsync(chartTitle, companyName, sheetName, cancellationToken);
            var spreadsheetId = spreadsheet.SpreadsheetId;

            // Prepare the data
            var values = new List<IList<object>> { new List<object> { column1Text, column2Text } };
            foreach (var item in data.OrderBy(x => x.Key))
            {
                values.Add(new List<object> { item.Key, item.Value });
            }

            // Write data to sheet
            var range = $"'{sheetName}'!A1:B{values.Count}";
            var valueRange = new ValueRange { Values = values };

            var updateRequest = _sheetsService!.Spreadsheets.Values.Update(valueRange, spreadsheetId, range);
            updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
            await updateRequest.ExecuteAsync(cancellationToken);

            // Format headers and numbers, add chart
            var requests = new List<Request>
            {
                CreateHeaderFormatRequest(0, 0, 0, 1),
                CreateNumberFormatRequest(1, values.Count - 1, 1, 1, numberFormat),
                CreateChartRequest(chartType, chartTitle, 0, values.Count - 1, [("A", "B")])
            };

            await _sheetsService.Spreadsheets
                .BatchUpdate(new BatchUpdateSpreadsheetRequest { Requests = requests }, spreadsheetId)
                .ExecuteAsync(cancellationToken);

            await AutoResizeColumnsAsync(spreadsheetId, 2, cancellationToken);

            success = true;
            return $"https://docs.google.com/spreadsheets/d/{spreadsheetId}";
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
                    0, // No file size for Google Sheets
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
        if (!await InitializeServiceAsync(cancellationToken))
        {
            return null;
        }

        const string sheetName = "Chart Data";

        var spreadsheet = await CreateSpreadsheetAsync(chartTitle, companyName, sheetName, cancellationToken);
        var spreadsheetId = spreadsheet.SpreadsheetId;

        // Set file permissions to be accessible by anyone with the link
        var driveService = new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = _sheetsService!.HttpClientInitializer,
            ApplicationName = "Argo Books"
        });
        await driveService.Permissions
            .Create(new Permission { Type = "anyone", Role = "writer", AllowFileDiscovery = false }, spreadsheetId)
            .ExecuteAsync(cancellationToken);

        // Get series names and prepare headers
        var seriesNames = data.First().Value.Keys.ToList();
        var orderedSeriesNames = seriesNames.OrderBy(x => x.Contains("Sales")).ToList();
        var headers = new List<object> { "Date" };
        headers.AddRange(orderedSeriesNames);

        // Prepare the data
        var values = new List<IList<object>> { headers };
        foreach (var dateEntry in data.OrderBy(x => x.Key))
        {
            var row = new List<object> { dateEntry.Key };
            foreach (var seriesName in orderedSeriesNames)
            {
                row.Add(dateEntry.Value[seriesName]);
            }
            values.Add(row);
        }

        // Write data to sheet
        var range = $"{sheetName}!A1:{(char)('A' + seriesNames.Count)}{values.Count}";
        var valueRange = new ValueRange { Values = values };
        var updateRequest = _sheetsService.Spreadsheets.Values.Update(valueRange, spreadsheetId, range);
        updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
        await updateRequest.ExecuteAsync(cancellationToken);

        // Format headers and numbers
        var requests = new List<Request> { CreateHeaderFormatRequest(0, 0, 0, seriesNames.Count) };
        for (var i = 1; i <= seriesNames.Count; i++)
        {
            requests.Add(CreateNumberFormatRequest(1, values.Count - 1, i, i, "#,##0.00"));
        }

        // Add chart with multiple series
        var seriesRanges = Enumerable.Range(0, seriesNames.Count)
            .Select(i => ("A", $"{(char)('B' + i)}"))
            .ToArray();
        requests.Add(CreateChartRequest(chartType, chartTitle, 0, values.Count - 1, seriesRanges));

        await _sheetsService.Spreadsheets
            .BatchUpdate(new BatchUpdateSpreadsheetRequest { Requests = requests }, spreadsheetId)
            .ExecuteAsync(cancellationToken);

        await AutoResizeColumnsAsync(spreadsheetId, seriesNames.Count + 1, cancellationToken);

        return $"https://docs.google.com/spreadsheets/d/{spreadsheetId}";
    }

    /// <summary>
    /// Exports pre-formatted chart data (from ChartLoaderService) to Google Sheets.
    /// </summary>
    /// <param name="exportData">Pre-formatted data as List of List of objects (first row is headers).</param>
    /// <param name="chartTitle">Title of the chart.</param>
    /// <param name="chartType">Type of chart to create.</param>
    /// <param name="companyName">Name of the company for the spreadsheet title.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The URL of the created spreadsheet, or null if export failed.</returns>
    public async Task<string?> ExportFormattedDataToGoogleSheetsAsync(
        List<List<object>> exportData,
        string chartTitle,
        ChartType chartType,
        string companyName,
        CancellationToken cancellationToken = default)
    {
        if (exportData.Count == 0 || !await InitializeServiceAsync(cancellationToken))
        {
            return null;
        }

        const string sheetName = "Chart Data";

        var spreadsheet = await CreateSpreadsheetAsync(chartTitle, companyName, sheetName, cancellationToken);
        var spreadsheetId = spreadsheet.SpreadsheetId;

        // Convert and write data
        var values = exportData.Select(row => (IList<object>)row.ToList()).ToList();
        var columnCount = values.Max(row => row.Count);
        var lastColumn = (char)('A' + columnCount - 1);

        var range = $"'{sheetName}'!A1:{lastColumn}{values.Count}";
        var valueRange = new ValueRange { Values = values };
        var updateRequest = _sheetsService!.Spreadsheets.Values.Update(valueRange, spreadsheetId, range);
        updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
        await updateRequest.ExecuteAsync(cancellationToken);

        // Format headers and numbers
        var requests = new List<Request> { CreateHeaderFormatRequest(0, 0, 0, columnCount - 1) };
        for (var i = 1; i < columnCount; i++)
        {
            requests.Add(CreateNumberFormatRequest(1, values.Count - 1, i, i, "#,##0.00"));
        }

        // Add chart
        var seriesRanges = Enumerable.Range(1, columnCount - 1)
            .Select(i => ("A", $"{(char)('A' + i)}"))
            .ToArray();
        requests.Add(CreateChartRequest(chartType, chartTitle, 0, values.Count - 1, seriesRanges));

        await _sheetsService.Spreadsheets
            .BatchUpdate(new BatchUpdateSpreadsheetRequest { Requests = requests }, spreadsheetId)
            .ExecuteAsync(cancellationToken);

        await AutoResizeColumnsAsync(spreadsheetId, columnCount, cancellationToken);

        return $"https://docs.google.com/spreadsheets/d/{spreadsheetId}";
    }

    /// <summary>
    /// Opens a Google Sheets URL in the default browser.
    /// </summary>
    /// <param name="url">The URL to open.</param>
    /// <returns>True if the browser was opened successfully, false otherwise.</returns>
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

    #region Private Helper Methods

    /// <summary>
    /// Creates a new spreadsheet with a single sheet.
    /// </summary>
    private async Task<Spreadsheet> CreateSpreadsheetAsync(
        string chartTitle,
        string companyName,
        string sheetName,
        CancellationToken cancellationToken)
    {
        var spreadsheet = new Spreadsheet
        {
            Properties = new SpreadsheetProperties
            {
                Title = $"{companyName} - {chartTitle} - {DateTime.Today:yyyy-MM-dd}"
            },
            Sheets =
            [
                new Sheet
                {
                    Properties = new SheetProperties
                    {
                        Title = sheetName,
                        SheetId = 0
                    }
                }
            ]
        };

        return await _sheetsService!.Spreadsheets
            .Create(spreadsheet)
            .ExecuteAsync(cancellationToken);
    }

    /// <summary>
    /// Auto-resizes columns in the spreadsheet.
    /// </summary>
    private async Task AutoResizeColumnsAsync(
        string spreadsheetId,
        int columnCount,
        CancellationToken cancellationToken)
    {
        var request = new Request
        {
            AutoResizeDimensions = new AutoResizeDimensionsRequest
            {
                Dimensions = new DimensionRange
                {
                    SheetId = 0,
                    Dimension = "COLUMNS",
                    StartIndex = 0,
                    EndIndex = columnCount
                }
            }
        };

        await _sheetsService!.Spreadsheets
            .BatchUpdate(new BatchUpdateSpreadsheetRequest { Requests = [request] }, spreadsheetId)
            .ExecuteAsync(cancellationToken);
    }

    private static Request CreateHeaderFormatRequest(
        int startRowIndex,
        int endRowIndex,
        int startColumnIndex,
        int endColumnIndex)
    {
        return new Request
        {
            RepeatCell = new RepeatCellRequest
            {
                Range = new GridRange
                {
                    SheetId = 0,
                    StartRowIndex = startRowIndex,
                    EndRowIndex = endRowIndex + 1,
                    StartColumnIndex = startColumnIndex,
                    EndColumnIndex = endColumnIndex + 1
                },
                Cell = new CellData
                {
                    UserEnteredFormat = new CellFormat
                    {
                        TextFormat = new TextFormat
                        {
                            Bold = true
                        },
                        BackgroundColor = new Color
                        {
                            Red = 0.678f,
                            Green = 0.847f,
                            Blue = 0.902f
                        }
                    }
                },
                Fields = "userEnteredFormat(textFormat,backgroundColor)"
            }
        };
    }

    private static Request CreateNumberFormatRequest(
        int startRowIndex,
        int endRowIndex,
        int startColumnIndex,
        int endColumnIndex,
        string numberFormat)
    {
        return new Request
        {
            RepeatCell = new RepeatCellRequest
            {
                Range = new GridRange
                {
                    SheetId = 0,
                    StartRowIndex = startRowIndex,
                    EndRowIndex = endRowIndex + 1,
                    StartColumnIndex = startColumnIndex,
                    EndColumnIndex = endColumnIndex + 1
                },
                Cell = new CellData
                {
                    UserEnteredFormat = new CellFormat
                    {
                        NumberFormat = new NumberFormat
                        {
                            Type = "NUMBER",
                            Pattern = numberFormat
                        }
                    }
                },
                Fields = "userEnteredFormat.numberFormat"
            }
        };
    }

    private static Request CreateChartRequest(
        ChartType chartType,
        string chartTitle,
        int startRowIndex,
        int endRowIndex,
        (string XColumn, string YColumn)[] seriesRanges)
    {
        var chartSpec = new ChartSpec
        {
            Title = chartTitle
        };

        switch (chartType)
        {
            case ChartType.Line:
            case ChartType.Spline:
                chartSpec.BasicChart = CreateBasicChartSpec(
                    seriesRanges,
                    startRowIndex,
                    endRowIndex,
                    "LINE",
                    chartType == ChartType.Spline
                );
                break;

            case ChartType.Column:
                chartSpec.BasicChart = CreateBasicChartSpec(
                    seriesRanges,
                    startRowIndex,
                    endRowIndex,
                    "COLUMN",
                    false
                );
                break;

            case ChartType.Pie:
                chartSpec.PieChart = CreatePieChartSpec(
                    seriesRanges[0],
                    startRowIndex,
                    endRowIndex
                );
                break;
        }

        return new Request
        {
            AddChart = new AddChartRequest
            {
                Chart = new EmbeddedChart
                {
                    Spec = chartSpec,
                    Position = new EmbeddedObjectPosition
                    {
                        OverlayPosition = new OverlayPosition
                        {
                            AnchorCell = new GridCoordinate
                            {
                                SheetId = 0,
                                RowIndex = 0,
                                ColumnIndex = seriesRanges.Length + 2
                            },
                            WidthPixels = 800,
                            HeightPixels = 420
                        }
                    }
                }
            }
        };
    }

    private static BasicChartSpec CreateBasicChartSpec(
        (string XColumn, string YColumn)[] seriesRanges,
        int startRowIndex,
        int endRowIndex,
        string chartType,
        bool isSpline = false)
    {
        var series = new List<BasicChartSeries>();

        foreach (var (_, yColumn) in seriesRanges)
        {
            series.Add(new BasicChartSeries
            {
                Series = new ChartData
                {
                    SourceRange = new ChartSourceRange
                    {
                        Sources =
                        [
                            new GridRange
                            {
                                SheetId = 0,
                                StartRowIndex = startRowIndex,
                                EndRowIndex = endRowIndex + 1,
                                StartColumnIndex = yColumn[0] - 'A',
                                EndColumnIndex = yColumn[0] - 'A' + 1
                            }
                        ]
                    }
                },
                TargetAxis = "LEFT_AXIS"
            });
        }

        return new BasicChartSpec
        {
            ChartType = chartType,
            LineSmoothing = isSpline,
            LegendPosition = "TOP_LEGEND",
            Domains =
            [
                new BasicChartDomain
                {
                    Domain = new ChartData
                    {
                        SourceRange = new ChartSourceRange
                        {
                            Sources =
                            [
                                new GridRange
                                {
                                    SheetId = 0,
                                    StartRowIndex = startRowIndex,
                                    EndRowIndex = endRowIndex + 1,
                                    StartColumnIndex = 0,
                                    EndColumnIndex = 1
                                }
                            ]
                        }
                    },
                    Reversed = false
                }
            ],
            Series = series,
            HeaderCount = 1,
            Axis =
            [
                new BasicChartAxis
                {
                    Position = "BOTTOM_AXIS",
                    Title = "",
                    Format = new TextFormat
                    {
                        FontSize = 10
                    }
                },
                new BasicChartAxis
                {
                    Position = "LEFT_AXIS",
                    Title = ""
                }
            ]
        };
    }

    private static PieChartSpec CreatePieChartSpec(
        (string XColumn, string YColumn) range,
        int startRowIndex,
        int endRowIndex)
    {
        return new PieChartSpec
        {
            LegendPosition = "RIGHT_LEGEND",
            Domain = new ChartData
            {
                SourceRange = new ChartSourceRange
                {
                    Sources =
                    [
                        new GridRange
                        {
                            SheetId = 0,
                            StartRowIndex = startRowIndex,
                            EndRowIndex = endRowIndex + 1,
                            StartColumnIndex = 0,
                            EndColumnIndex = 1
                        }
                    ]
                }
            },
            Series = new ChartData
            {
                SourceRange = new ChartSourceRange
                {
                    Sources =
                    [
                        new GridRange
                        {
                            SheetId = 0,
                            StartRowIndex = startRowIndex,
                            EndRowIndex = endRowIndex + 1,
                            StartColumnIndex = range.YColumn[0] - 'A',
                            EndColumnIndex = range.YColumn[0] - 'A' + 1
                        }
                    ]
                }
            },
            PieHole = 0,
            ThreeDimensional = false
        };
    }

    #endregion
}
