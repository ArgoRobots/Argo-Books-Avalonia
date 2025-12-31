using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using System.Diagnostics;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Handles exporting chart data to Google Sheets.
/// </summary>
public class GoogleSheetsService
{
    private SheetsService? _sheetsService;

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
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The URL of the created spreadsheet, or null if export failed.</returns>
    public async Task<string?> ExportChartToGoogleSheetsAsync(
        IReadOnlyDictionary<string, double> data,
        string chartTitle,
        ChartType chartType,
        string column1Text,
        string column2Text,
        string companyName,
        CancellationToken cancellationToken = default)
    {
        if (!await InitializeServiceAsync(cancellationToken))
        {
            return null;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var sheetName = "Chart Data";

        // Create a new spreadsheet
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

        cancellationToken.ThrowIfCancellationRequested();

        spreadsheet = await _sheetsService!.Spreadsheets
            .Create(spreadsheet)
            .ExecuteAsync(cancellationToken);

        var spreadsheetId = spreadsheet.SpreadsheetId;

        cancellationToken.ThrowIfCancellationRequested();

        // Prepare the data
        var values = new List<IList<object>>
        {
            new List<object> { column1Text, column2Text }
        };

        foreach (var item in data.OrderBy(x => x.Key))
        {
            cancellationToken.ThrowIfCancellationRequested();
            values.Add(new List<object> { item.Key, item.Value });
        }

        cancellationToken.ThrowIfCancellationRequested();

        // Write data to sheet
        var range = $"'{sheetName}'!A1:B{values.Count}";
        var valueRange = new ValueRange { Values = values };

        var updateRequest = _sheetsService.Spreadsheets.Values.Update(valueRange, spreadsheetId, range);
        updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;

        await updateRequest.ExecuteAsync(cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        // Format headers and numbers
        var requests = new List<Request>
        {
            CreateHeaderFormatRequest(0, 0, 0, 1),
            CreateNumberFormatRequest(1, values.Count - 1, 1, 1, "#,##0.00")
        };

        // Add chart
        var chartRequest = CreateChartRequest(
            chartType,
            chartTitle,
            0, values.Count - 1,
            [("A", "B")]
        );
        requests.Add(chartRequest);

        cancellationToken.ThrowIfCancellationRequested();

        // Execute all formatting requests
        var batchUpdateRequest = new BatchUpdateSpreadsheetRequest
        {
            Requests = requests
        };

        await _sheetsService.Spreadsheets
            .BatchUpdate(batchUpdateRequest, spreadsheetId)
            .ExecuteAsync(cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        // Auto-resize columns
        var dimensionRange = new DimensionRange
        {
            SheetId = 0,
            Dimension = "COLUMNS",
            StartIndex = 0,
            EndIndex = 2
        };

        var autoResizeRequest = new Request
        {
            AutoResizeDimensions = new AutoResizeDimensionsRequest
            {
                Dimensions = dimensionRange
            }
        };

        await _sheetsService.Spreadsheets
            .BatchUpdate(new BatchUpdateSpreadsheetRequest
            {
                Requests = [autoResizeRequest]
            }, spreadsheetId)
            .ExecuteAsync(cancellationToken);

        return $"https://docs.google.com/spreadsheets/d/{spreadsheetId}";
    }

    /// <summary>
    /// Exports chart data with integer values (counts) to Google Sheets.
    /// </summary>
    public async Task<string?> ExportCountChartToGoogleSheetsAsync(
        IReadOnlyDictionary<string, int> data,
        string chartTitle,
        ChartType chartType,
        string column1Text,
        string column2Text,
        string companyName,
        CancellationToken cancellationToken = default)
    {
        // Convert to double dictionary
        var doubleData = data.ToDictionary(kvp => kvp.Key, kvp => (double)kvp.Value);

        if (!await InitializeServiceAsync(cancellationToken))
        {
            return null;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var sheetName = "Chart Data";

        // Create a new spreadsheet
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

        spreadsheet = await _sheetsService!.Spreadsheets
            .Create(spreadsheet)
            .ExecuteAsync(cancellationToken);

        var spreadsheetId = spreadsheet.SpreadsheetId;

        // Prepare the data
        var values = new List<IList<object>>
        {
            new List<object> { column1Text, column2Text }
        };

        foreach (var item in data.OrderBy(x => x.Key))
        {
            values.Add(new List<object> { item.Key, item.Value });
        }

        // Write data to sheet
        var range = $"'{sheetName}'!A1:B{values.Count}";
        var valueRange = new ValueRange { Values = values };

        var updateRequest = _sheetsService.Spreadsheets.Values.Update(valueRange, spreadsheetId, range);
        updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;

        await updateRequest.ExecuteAsync(cancellationToken);

        // Format headers and numbers (no decimals for counts)
        var requests = new List<Request>
        {
            CreateHeaderFormatRequest(0, 0, 0, 1),
            CreateNumberFormatRequest(1, values.Count - 1, 1, 1, "#,##0")
        };

        // Add chart
        var chartRequest = CreateChartRequest(
            chartType,
            chartTitle,
            0, values.Count - 1,
            [("A", "B")]
        );
        requests.Add(chartRequest);

        await _sheetsService.Spreadsheets
            .BatchUpdate(new BatchUpdateSpreadsheetRequest { Requests = requests }, spreadsheetId)
            .ExecuteAsync(cancellationToken);

        // Auto-resize columns
        await _sheetsService.Spreadsheets
            .BatchUpdate(new BatchUpdateSpreadsheetRequest
            {
                Requests =
                [
                    new Request
                    {
                        AutoResizeDimensions = new AutoResizeDimensionsRequest
                        {
                            Dimensions = new DimensionRange
                            {
                                SheetId = 0,
                                Dimension = "COLUMNS",
                                StartIndex = 0,
                                EndIndex = 2
                            }
                        }
                    }
                ]
            }, spreadsheetId)
            .ExecuteAsync(cancellationToken);

        return $"https://docs.google.com/spreadsheets/d/{spreadsheetId}";
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

        var sheetName = "Chart Data";

        // Create a new spreadsheet
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

        spreadsheet = await _sheetsService!.Spreadsheets
            .Create(spreadsheet)
            .ExecuteAsync(cancellationToken);

        var spreadsheetId = spreadsheet.SpreadsheetId;

        // Create drive service for permissions
        var driveService = new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = _sheetsService.HttpClientInitializer,
            ApplicationName = "Argo Books"
        });

        // Set file permissions to be accessible by anyone with the link
        var permission = new Permission
        {
            Type = "anyone",
            Role = "writer",
            AllowFileDiscovery = false
        };

        await driveService.Permissions
            .Create(permission, spreadsheetId)
            .ExecuteAsync(cancellationToken);

        // Get series names and prepare headers
        var seriesNames = data.First().Value.Keys.ToList();
        var orderedSeriesNames = seriesNames
            .OrderBy(x => x.Contains("Sales"))
            .ToList();
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
        var requests = new List<Request>
        {
            CreateHeaderFormatRequest(0, 0, 0, seriesNames.Count)
        };

        // Format number columns
        for (var i = 1; i <= seriesNames.Count; i++)
        {
            requests.Add(CreateNumberFormatRequest(1, values.Count - 1, i, i, "#,##0.00"));
        }

        // Add chart with multiple series
        var seriesRanges = new List<(string, string)>();
        for (var i = 0; i < seriesNames.Count; i++)
        {
            seriesRanges.Add(("A", $"{(char)('B' + i)}"));
        }

        var chartRequest = CreateChartRequest(
            chartType,
            chartTitle,
            0,
            values.Count - 1,
            seriesRanges.ToArray()
        );
        requests.Add(chartRequest);

        await _sheetsService.Spreadsheets
            .BatchUpdate(new BatchUpdateSpreadsheetRequest { Requests = requests }, spreadsheetId)
            .ExecuteAsync(cancellationToken);

        // Auto-resize columns
        await _sheetsService.Spreadsheets
            .BatchUpdate(new BatchUpdateSpreadsheetRequest
            {
                Requests =
                [
                    new Request
                    {
                        AutoResizeDimensions = new AutoResizeDimensionsRequest
                        {
                            Dimensions = new DimensionRange
                            {
                                SheetId = 0,
                                Dimension = "COLUMNS",
                                StartIndex = 0,
                                EndIndex = seriesNames.Count + 1
                            }
                        }
                    }
                ]
            }, spreadsheetId)
            .ExecuteAsync(cancellationToken);

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
        if (exportData.Count == 0)
        {
            return null;
        }

        if (!await InitializeServiceAsync(cancellationToken))
        {
            return null;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var sheetName = "Chart Data";

        // Create a new spreadsheet
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

        spreadsheet = await _sheetsService!.Spreadsheets
            .Create(spreadsheet)
            .ExecuteAsync(cancellationToken);

        var spreadsheetId = spreadsheet.SpreadsheetId;

        cancellationToken.ThrowIfCancellationRequested();

        // Convert List<List<object>> to IList<IList<object>>
        var values = exportData.Select(row => (IList<object>)row.ToList()).ToList();

        // Determine the number of columns
        var columnCount = values.Max(row => row.Count);
        var lastColumn = (char)('A' + columnCount - 1);

        // Write data to sheet
        var range = $"'{sheetName}'!A1:{lastColumn}{values.Count}";
        var valueRange = new ValueRange { Values = values };

        var updateRequest = _sheetsService.Spreadsheets.Values.Update(valueRange, spreadsheetId, range);
        updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;

        await updateRequest.ExecuteAsync(cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        // Format headers and numbers
        var requests = new List<Request>
        {
            CreateHeaderFormatRequest(0, 0, 0, columnCount - 1)
        };

        // Format number columns (all columns except the first one which is typically labels)
        for (var i = 1; i < columnCount; i++)
        {
            requests.Add(CreateNumberFormatRequest(1, values.Count - 1, i, i, "#,##0.00"));
        }

        // Add chart
        var seriesRanges = new List<(string, string)>();
        for (var i = 1; i < columnCount; i++)
        {
            seriesRanges.Add(("A", $"{(char)('A' + i)}"));
        }

        var chartRequest = CreateChartRequest(
            chartType,
            chartTitle,
            0, values.Count - 1,
            seriesRanges.ToArray()
        );
        requests.Add(chartRequest);

        cancellationToken.ThrowIfCancellationRequested();

        await _sheetsService.Spreadsheets
            .BatchUpdate(new BatchUpdateSpreadsheetRequest { Requests = requests }, spreadsheetId)
            .ExecuteAsync(cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        // Auto-resize columns
        await _sheetsService.Spreadsheets
            .BatchUpdate(new BatchUpdateSpreadsheetRequest
            {
                Requests =
                [
                    new Request
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
                    }
                ]
            }, spreadsheetId)
            .ExecuteAsync(cancellationToken);

        return $"https://docs.google.com/spreadsheets/d/{spreadsheetId}";
    }

    /// <summary>
    /// Opens a Google Sheets URL in the default browser.
    /// </summary>
    /// <param name="url">The URL to open.</param>
    public static void OpenInBrowser(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch
        {
            // Ignore errors opening browser
        }
    }

    #region Private Helper Methods

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
                        BackgroundColor = new Google.Apis.Sheets.v4.Data.Color
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
                    },
                    ViewWindowOptions = new ChartAxisViewWindowOptions
                    {
                        ViewWindowMode = "EXPLICIT",
                        ViewWindowMin = 0,
                        ViewWindowMax = endRowIndex + 0.5
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
