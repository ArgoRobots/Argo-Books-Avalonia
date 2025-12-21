using System.Collections.ObjectModel;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Reports;
using ArgoBooks.Core.Services;
using ArgoBooks.Services;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SkiaSharp;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the Reports page with 3-step wizard navigation.
/// </summary>
public partial class ReportsPageViewModel : ViewModelBase
{
    #region Wizard Step Management

    [ObservableProperty]
    private int _currentStep = 1;

    [ObservableProperty]
    private bool _step1Completed;

    [ObservableProperty]
    private bool _step2Completed;

    [ObservableProperty]
    private string _currentStepTitle = "Template & Settings";

    partial void OnCurrentStepChanged(int value)
    {
        CurrentStepTitle = value switch
        {
            1 => "Template & Settings",
            2 => "Layout Designer",
            3 => "Preview & Export",
            _ => "Template & Settings"
        };
    }

    public bool CanGoBack => CurrentStep > 1;
    public bool CanGoNext => CurrentStep < 3;
    public bool IsOnFinalStep => CurrentStep == 3;

    [RelayCommand]
    private void GoToPreviousStep()
    {
        if (CurrentStep > 1)
        {
            CurrentStep--;
            OnPropertyChanged(nameof(CanGoBack));
            OnPropertyChanged(nameof(CanGoNext));
            OnPropertyChanged(nameof(IsOnFinalStep));
        }
    }

    [RelayCommand]
    private void GoToNextStep()
    {
        if (CurrentStep < 3)
        {
            if (CurrentStep == 1)
            {
                Step1Completed = true;
                ApplyFiltersToConfiguration();
            }
            else if (CurrentStep == 2)
            {
                Step2Completed = true;
                GeneratePreview();
            }

            CurrentStep++;
            OnPropertyChanged(nameof(CanGoBack));
            OnPropertyChanged(nameof(CanGoNext));
            OnPropertyChanged(nameof(IsOnFinalStep));
        }
    }

    #endregion

    #region Step 1 - Template & Settings

    [ObservableProperty]
    private string _selectedTemplateName = ReportTemplateFactory.TemplateNames.Custom;

    [ObservableProperty]
    private string _reportName = "Untitled Report";

    [ObservableProperty]
    private string _selectedDatePreset = DatePresetNames.ThisMonth;

    [ObservableProperty]
    private DateTimeOffset? _customStartDate;

    [ObservableProperty]
    private DateTimeOffset? _customEndDate;

    [ObservableProperty]
    private bool _isCustomDateRange;

    [ObservableProperty]
    private TransactionType _selectedTransactionType = TransactionType.Revenue;

    public ObservableCollection<string> TemplateNames { get; } = [];
    public ObservableCollection<string> CustomTemplateNames { get; } = [];
    public ObservableCollection<string> DatePresets { get; } = [];

    // Chart selection
    public ObservableCollection<ChartOption> AvailableCharts { get; } = [];
    public ObservableCollection<ChartOption> SelectedCharts { get; } = [];

    partial void OnSelectedTemplateNameChanged(string value)
    {
        LoadTemplate(value);
    }

    partial void OnSelectedDatePresetChanged(string value)
    {
        IsCustomDateRange = value == DatePresetNames.Custom;
    }

    [RelayCommand]
    private void AddChart(ChartOption chart)
    {
        if (chart != null && !SelectedCharts.Contains(chart))
        {
            SelectedCharts.Add(chart);
            AvailableCharts.Remove(chart);
        }
    }

    [RelayCommand]
    private void RemoveChart(ChartOption chart)
    {
        if (chart != null && SelectedCharts.Contains(chart))
        {
            SelectedCharts.Remove(chart);
            AvailableCharts.Add(chart);
        }
    }

    [RelayCommand]
    private void SelectTemplate(string? templateName)
    {
        if (!string.IsNullOrEmpty(templateName))
        {
            SelectedTemplateName = templateName;
        }
    }

    #endregion

    #region Step 2 - Layout Designer

    [ObservableProperty]
    private ReportConfiguration _configuration = new();

    [ObservableProperty]
    private ReportElementBase? _selectedElement;

    [ObservableProperty]
    private bool _showGrid = true;

    [ObservableProperty]
    private int _gridSize = 10;

    [ObservableProperty]
    private bool _snapToGrid = true;

    [ObservableProperty]
    private double _canvasWidth = 1123;

    [ObservableProperty]
    private double _canvasHeight = 794;

    [ObservableProperty]
    private double _zoomLevel = 1.0;

    [ObservableProperty]
    private bool _isPageSettingsOpen;

    [ObservableProperty]
    private bool _isSaveTemplateOpen;

    public ReportUndoRedoManager UndoRedoManager { get; } = new();

    public ObservableCollection<ReportElementBase> SelectedElements { get; } = [];

    partial void OnConfigurationChanged(ReportConfiguration value)
    {
        UpdateCanvasDimensions();
    }

    private void UpdateCanvasDimensions()
    {
        var (width, height) = PageDimensions.GetDimensions(Configuration.PageSize, Configuration.PageOrientation);
        CanvasWidth = width;
        CanvasHeight = height;
    }

    [RelayCommand]
    private void AddElement(string elementType)
    {
        ReportElementBase element = elementType switch
        {
            "Chart" => new ChartReportElement { X = 100, Y = 150, Width = 300, Height = 200 },
            "Table" => new TableReportElement { X = 100, Y = 150, Width = 400, Height = 200 },
            "Label" => new LabelReportElement { X = 100, Y = 150, Width = 200, Height = 40 },
            "Image" => new ImageReportElement { X = 100, Y = 150, Width = 150, Height = 150 },
            "DateRange" => new DateRangeReportElement { X = 100, Y = 150, Width = 200, Height = 30 },
            "Summary" => new SummaryReportElement { X = 100, Y = 150, Width = 200, Height = 120 },
            _ => new LabelReportElement()
        };

        Configuration.AddElement(element);
        UndoRedoManager.RecordAction(new AddElementAction(Configuration, element));
        SelectedElement = element;
        SelectedElements.Clear();
        SelectedElements.Add(element);
        OnPropertyChanged(nameof(Configuration));
    }

    [RelayCommand]
    private void DeleteSelectedElements()
    {
        foreach (var element in SelectedElements.ToList())
        {
            UndoRedoManager.RecordAction(new RemoveElementAction(Configuration, element));
            Configuration.RemoveElement(element.Id);
        }
        SelectedElements.Clear();
        SelectedElement = null;
        OnPropertyChanged(nameof(Configuration));
    }

    [RelayCommand]
    private void Undo()
    {
        if (UndoRedoManager.CanUndo)
        {
            UndoRedoManager.Undo();
            OnPropertyChanged(nameof(Configuration));
        }
    }

    [RelayCommand]
    private void Redo()
    {
        if (UndoRedoManager.CanRedo)
        {
            UndoRedoManager.Redo();
            OnPropertyChanged(nameof(Configuration));
        }
    }

    [RelayCommand]
    private void BringToFront()
    {
        if (SelectedElement == null) return;

        var oldZOrders = Configuration.Elements.ToDictionary(e => e.Id, e => e.ZOrder);
        var maxZ = Configuration.Elements.Max(e => e.ZOrder);
        SelectedElement.ZOrder = maxZ + 1;
        var newZOrders = Configuration.Elements.ToDictionary(e => e.Id, e => e.ZOrder);

        UndoRedoManager.RecordAction(new ZOrderChangeAction(Configuration, oldZOrders, newZOrders, "Bring to front"));
        OnPropertyChanged(nameof(Configuration));
    }

    [RelayCommand]
    private void SendToBack()
    {
        if (SelectedElement == null) return;

        var oldZOrders = Configuration.Elements.ToDictionary(e => e.Id, e => e.ZOrder);
        var minZ = Configuration.Elements.Min(e => e.ZOrder);

        foreach (var element in Configuration.Elements)
        {
            if (element.Id != SelectedElement.Id)
                element.ZOrder++;
        }
        SelectedElement.ZOrder = minZ;

        var newZOrders = Configuration.Elements.ToDictionary(e => e.Id, e => e.ZOrder);
        UndoRedoManager.RecordAction(new ZOrderChangeAction(Configuration, oldZOrders, newZOrders, "Send to back"));
        OnPropertyChanged(nameof(Configuration));
    }

    [RelayCommand]
    private void AlignElements(string alignment)
    {
        if (SelectedElements.Count < 2) return;

        var oldBounds = SelectedElements.ToDictionary(e => e.Id, e => e.Bounds);

        var reference = SelectedElements[0];
        foreach (var element in SelectedElements.Skip(1))
        {
            switch (alignment)
            {
                case "Left":
                    element.X = reference.X;
                    break;
                case "Right":
                    element.X = reference.X + reference.Width - element.Width;
                    break;
                case "Top":
                    element.Y = reference.Y;
                    break;
                case "Bottom":
                    element.Y = reference.Y + reference.Height - element.Height;
                    break;
                case "CenterH":
                    element.X = reference.X + (reference.Width - element.Width) / 2;
                    break;
                case "CenterV":
                    element.Y = reference.Y + (reference.Height - element.Height) / 2;
                    break;
            }
        }

        var newBounds = SelectedElements.ToDictionary(e => e.Id, e => e.Bounds);
        UndoRedoManager.RecordAction(new BatchMoveResizeAction(Configuration, oldBounds, newBounds, $"Align {alignment}"));
        OnPropertyChanged(nameof(Configuration));
    }

    [RelayCommand]
    private void DistributeElements(string direction)
    {
        if (SelectedElements.Count < 3) return;

        var oldBounds = SelectedElements.ToDictionary(e => e.Id, e => e.Bounds);

        var sorted = direction == "Horizontal"
            ? SelectedElements.OrderBy(e => e.X).ToList()
            : SelectedElements.OrderBy(e => e.Y).ToList();

        if (direction == "Horizontal")
        {
            var totalWidth = sorted.Sum(e => e.Width);
            var minX = sorted.First().X;
            var maxX = sorted.Last().X + sorted.Last().Width;
            var spacing = (maxX - minX - totalWidth) / (sorted.Count - 1);

            var currentX = minX;
            foreach (var element in sorted)
            {
                element.X = currentX;
                currentX += element.Width + spacing;
            }
        }
        else
        {
            var totalHeight = sorted.Sum(e => e.Height);
            var minY = sorted.First().Y;
            var maxY = sorted.Last().Y + sorted.Last().Height;
            var spacing = (maxY - minY - totalHeight) / (sorted.Count - 1);

            var currentY = minY;
            foreach (var element in sorted)
            {
                element.Y = currentY;
                currentY += element.Height + spacing;
            }
        }

        var newBounds = SelectedElements.ToDictionary(e => e.Id, e => e.Bounds);
        UndoRedoManager.RecordAction(new BatchMoveResizeAction(Configuration, oldBounds, newBounds, $"Distribute {direction}"));
        OnPropertyChanged(nameof(Configuration));
    }

    [RelayCommand]
    private void MatchSize(string dimension)
    {
        if (SelectedElements.Count < 2) return;

        var oldBounds = SelectedElements.ToDictionary(e => e.Id, e => e.Bounds);
        var reference = SelectedElements[0];

        foreach (var element in SelectedElements.Skip(1))
        {
            switch (dimension)
            {
                case "Width":
                    element.Width = reference.Width;
                    break;
                case "Height":
                    element.Height = reference.Height;
                    break;
                case "Both":
                    element.Width = reference.Width;
                    element.Height = reference.Height;
                    break;
            }
        }

        var newBounds = SelectedElements.ToDictionary(e => e.Id, e => e.Bounds);
        UndoRedoManager.RecordAction(new BatchMoveResizeAction(Configuration, oldBounds, newBounds, $"Match {dimension}"));
        OnPropertyChanged(nameof(Configuration));
    }

    [RelayCommand]
    private void ZoomIn()
    {
        ZoomLevel = Math.Min(2.0, ZoomLevel + 0.1);
    }

    [RelayCommand]
    private void ZoomOut()
    {
        ZoomLevel = Math.Max(0.25, ZoomLevel - 0.1);
    }

    [RelayCommand]
    private void ZoomFit()
    {
        ZoomLevel = 1.0;
    }

    [RelayCommand]
    private void OpenPageSettings()
    {
        IsPageSettingsOpen = true;
    }

    [RelayCommand]
    private void ClosePageSettings()
    {
        IsPageSettingsOpen = false;
    }

    [RelayCommand]
    private void OpenSaveTemplate()
    {
        IsSaveTemplateOpen = true;
    }

    [RelayCommand]
    private void CloseSaveTemplate()
    {
        IsSaveTemplateOpen = false;
    }

    #endregion

    #region Step 3 - Preview & Export

    [ObservableProperty]
    private Bitmap? _previewImage;

    [ObservableProperty]
    private ExportFormat _selectedExportFormat = ExportFormat.PDF;

    [ObservableProperty]
    private int _exportQuality = 95;

    [ObservableProperty]
    private string _exportFilePath = string.Empty;

    [ObservableProperty]
    private bool _openAfterExport = true;

    [ObservableProperty]
    private bool _includeMetadata = true;

    [ObservableProperty]
    private double _previewZoom = 1.0;

    [ObservableProperty]
    private bool _isExporting;

    [ObservableProperty]
    private string? _exportMessage;

    public ObservableCollection<ExportFormatOption> ExportFormats { get; } =
    [
        new("PDF Document", ExportFormat.PDF, "For printing & sharing"),
        new("PNG Image", ExportFormat.PNG, "High quality, lossless"),
        new("JPEG Image", ExportFormat.JPEG, "Smaller file size")
    ];

    private void GeneratePreview()
    {
        try
        {
            var companyData = App.CompanyManager?.CompanyData;
            using var renderer = new ReportRenderer(Configuration, companyData, 1f);
            using var skBitmap = renderer.CreatePreview(800, 600);
            PreviewImage = ConvertToBitmap(skBitmap);
        }
        catch
        {
            PreviewImage = null;
        }
    }

    [RelayCommand]
    private void PreviewZoomIn()
    {
        PreviewZoom = Math.Min(3.0, PreviewZoom + 0.25);
    }

    [RelayCommand]
    private void PreviewZoomOut()
    {
        PreviewZoom = Math.Max(0.25, PreviewZoom - 0.25);
    }

    [RelayCommand]
    private void PreviewZoomFit()
    {
        PreviewZoom = 1.0;
    }

    [RelayCommand]
    private async Task ExportReportAsync()
    {
        if (string.IsNullOrWhiteSpace(ExportFilePath))
        {
            ExportMessage = "Please specify a file path.";
            return;
        }

        IsExporting = true;
        ExportMessage = null;

        try
        {
            var companyData = App.CompanyManager?.CompanyData;
            using var renderer = new ReportRenderer(Configuration, companyData, PageDimensions.RenderScale);

            bool success;
            if (SelectedExportFormat == ExportFormat.PDF)
            {
                success = await renderer.ExportToPdfAsync(ExportFilePath);
            }
            else
            {
                success = await renderer.ExportToImageAsync(ExportFilePath, SelectedExportFormat, ExportQuality);
            }

            if (success)
            {
                ExportMessage = "Export completed successfully!";

                if (OpenAfterExport && File.Exists(ExportFilePath))
                {
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = ExportFilePath,
                            UseShellExecute = true
                        });
                    }
                    catch
                    {
                        // Ignore if we can't open the file
                    }
                }
            }
            else
            {
                ExportMessage = "Export failed. Please check the file path and try again.";
            }
        }
        catch (Exception ex)
        {
            ExportMessage = $"Export error: {ex.Message}";
        }
        finally
        {
            IsExporting = false;
        }
    }

    [RelayCommand]
    private async Task BrowseExportPathAsync()
    {
        // In a real implementation, this would use a file picker dialog
        // For now, we'll set a default path
        var defaultName = string.IsNullOrWhiteSpace(ReportName) ? "Report" : ReportName;
        var extension = SelectedExportFormat switch
        {
            ExportFormat.PDF => ".pdf",
            ExportFormat.PNG => ".png",
            ExportFormat.JPEG => ".jpg",
            _ => ".pdf"
        };

        var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        ExportFilePath = Path.Combine(documentsPath, $"{defaultName}{extension}");
        await Task.CompletedTask;
    }

    #endregion

    #region Page Settings Properties

    [ObservableProperty]
    private PageSize _pageSize = PageSize.A4;

    [ObservableProperty]
    private PageOrientation _pageOrientation = PageOrientation.Landscape;

    [ObservableProperty]
    private double _marginTop = 40;

    [ObservableProperty]
    private double _marginRight = 40;

    [ObservableProperty]
    private double _marginBottom = 40;

    [ObservableProperty]
    private double _marginLeft = 40;

    [ObservableProperty]
    private bool _showHeader = true;

    [ObservableProperty]
    private bool _showFooter = true;

    [ObservableProperty]
    private bool _showPageNumbers = true;

    [ObservableProperty]
    private string _backgroundColor = "#FFFFFF";

    public ObservableCollection<PageSize> PageSizes { get; } =
        new(Enum.GetValues<PageSize>());

    public ObservableCollection<PageOrientation> Orientations { get; } =
        new(Enum.GetValues<PageOrientation>());

    [RelayCommand]
    private void ApplyPageSettings()
    {
        Configuration.PageSize = PageSize;
        Configuration.PageOrientation = PageOrientation;
        Configuration.PageMargins = new ReportMargins(MarginLeft, MarginTop, MarginRight, MarginBottom);
        Configuration.ShowHeader = ShowHeader;
        Configuration.ShowFooter = ShowFooter;
        Configuration.ShowPageNumbers = ShowPageNumbers;
        Configuration.BackgroundColor = BackgroundColor;

        UpdateCanvasDimensions();
        IsPageSettingsOpen = false;
        OnPropertyChanged(nameof(Configuration));
    }

    #endregion

    #region Save Template Properties

    [ObservableProperty]
    private string _saveTemplateName = string.Empty;

    [ObservableProperty]
    private string? _saveTemplateMessage;

    [RelayCommand]
    private async Task SaveTemplateAsync()
    {
        if (string.IsNullOrWhiteSpace(SaveTemplateName))
        {
            SaveTemplateMessage = "Please enter a template name.";
            return;
        }

        var storage = new ReportTemplateStorage();

        // Check if template exists
        if (storage.TemplateExists(SaveTemplateName))
        {
            SaveTemplateMessage = "A template with this name already exists.";
            return;
        }

        var success = await storage.SaveTemplateAsync(Configuration, SaveTemplateName);

        if (success)
        {
            SaveTemplateMessage = "Template saved successfully!";
            await Task.Delay(1500);
            IsSaveTemplateOpen = false;
            SaveTemplateMessage = null;

            // Refresh custom templates list
            LoadCustomTemplates();
        }
        else
        {
            SaveTemplateMessage = "Failed to save template. Please try again.";
        }
    }

    #endregion

    #region Constructor & Initialization

    private readonly ReportTemplateStorage _templateStorage = new();

    public ReportsPageViewModel()
    {
        InitializeCollections();
        LoadTemplate(SelectedTemplateName);
    }

    private void InitializeCollections()
    {
        // Load built-in templates
        foreach (var name in ReportTemplateFactory.GetBuiltInTemplateNames())
        {
            TemplateNames.Add(name);
        }

        // Load custom templates
        LoadCustomTemplates();

        // Load date presets
        foreach (var preset in DatePresetNames.GetAllPresets())
        {
            DatePresets.Add(preset);
        }

        // Load available charts
        InitializeChartOptions();
    }

    private void LoadCustomTemplates()
    {
        CustomTemplateNames.Clear();
        var customNames = _templateStorage.GetSavedTemplateNames();
        foreach (var name in customNames)
        {
            CustomTemplateNames.Add(name);
        }
    }

    private void InitializeChartOptions()
    {
        AvailableCharts.Clear();

        // Revenue charts
        AvailableCharts.Add(new ChartOption(ChartDataType.TotalRevenue, "Total Revenue", "Revenue over time"));
        AvailableCharts.Add(new ChartOption(ChartDataType.RevenueDistribution, "Revenue Distribution", "Revenue by category"));

        // Expense charts
        AvailableCharts.Add(new ChartOption(ChartDataType.TotalExpenses, "Total Expenses", "Expenses over time"));
        AvailableCharts.Add(new ChartOption(ChartDataType.ExpensesDistribution, "Expense Distribution", "Expenses by category"));

        // Financial charts
        AvailableCharts.Add(new ChartOption(ChartDataType.SalesVsExpenses, "Sales vs Expenses", "Compare revenue and costs"));
        AvailableCharts.Add(new ChartOption(ChartDataType.TotalProfits, "Total Profits", "Profit over time"));
        AvailableCharts.Add(new ChartOption(ChartDataType.GrowthRates, "Growth Rates", "Period-over-period growth"));

        // Transaction charts
        AvailableCharts.Add(new ChartOption(ChartDataType.AverageTransactionValue, "Avg. Transaction Value", "Average transaction amounts"));
        AvailableCharts.Add(new ChartOption(ChartDataType.TotalTransactions, "Total Transactions", "Transaction volume"));

        // Geographic charts
        AvailableCharts.Add(new ChartOption(ChartDataType.WorldMap, "World Map", "Geographic distribution"));
        AvailableCharts.Add(new ChartOption(ChartDataType.CountriesOfOrigin, "Countries of Origin", "Sales by origin country"));

        // Return/Loss charts
        AvailableCharts.Add(new ChartOption(ChartDataType.ReturnsOverTime, "Returns Over Time", "Return trends"));
        AvailableCharts.Add(new ChartOption(ChartDataType.ReturnReasons, "Return Reasons", "Why items are returned"));
        AvailableCharts.Add(new ChartOption(ChartDataType.LossesOverTime, "Losses Over Time", "Loss trends"));
        AvailableCharts.Add(new ChartOption(ChartDataType.LossReasons, "Loss Reasons", "Why items are lost"));
    }

    private void LoadTemplate(string templateName)
    {
        if (ReportTemplateFactory.IsBuiltInTemplate(templateName))
        {
            Configuration = ReportTemplateFactory.CreateFromTemplate(templateName);
        }
        else
        {
            // Try to load custom template
            Task.Run(async () =>
            {
                var config = await _templateStorage.LoadTemplateAsync(templateName);
                if (config != null)
                {
                    Configuration = config;
                }
                else
                {
                    Configuration = new ReportConfiguration();
                }
            });
        }

        // Update page settings from configuration
        PageSize = Configuration.PageSize;
        PageOrientation = Configuration.PageOrientation;
        MarginTop = Configuration.PageMargins.Top;
        MarginRight = Configuration.PageMargins.Right;
        MarginBottom = Configuration.PageMargins.Bottom;
        MarginLeft = Configuration.PageMargins.Left;
        ShowHeader = Configuration.ShowHeader;
        ShowFooter = Configuration.ShowFooter;
        ShowPageNumbers = Configuration.ShowPageNumbers;
        BackgroundColor = Configuration.BackgroundColor;

        // Update date preset
        if (!string.IsNullOrEmpty(Configuration.Filters.DatePresetName))
        {
            SelectedDatePreset = Configuration.Filters.DatePresetName;
        }

        // Update transaction type
        SelectedTransactionType = Configuration.Filters.TransactionType;

        // Update selected charts
        SelectedCharts.Clear();
        foreach (var chartType in Configuration.Filters.SelectedChartTypes)
        {
            var option = AvailableCharts.FirstOrDefault(c => c.ChartType == chartType);
            if (option != null)
            {
                SelectedCharts.Add(option);
                AvailableCharts.Remove(option);
            }
        }

        UpdateCanvasDimensions();
        UndoRedoManager.Clear();
        OnPropertyChanged(nameof(Configuration));
    }

    private void ApplyFiltersToConfiguration()
    {
        Configuration.Title = ReportName;
        Configuration.Filters.TransactionType = SelectedTransactionType;
        Configuration.Filters.DatePresetName = SelectedDatePreset;

        if (IsCustomDateRange)
        {
            Configuration.Filters.StartDate = CustomStartDate?.DateTime;
            Configuration.Filters.EndDate = CustomEndDate?.DateTime;
        }
        else if (!string.IsNullOrEmpty(SelectedDatePreset))
        {
            var (start, end) = DatePresetNames.GetDateRange(SelectedDatePreset);
            Configuration.Filters.StartDate = start;
            Configuration.Filters.EndDate = end;
        }

        Configuration.Filters.SelectedChartTypes.Clear();
        foreach (var chart in SelectedCharts)
        {
            Configuration.Filters.SelectedChartTypes.Add(chart.ChartType);
        }
    }

    private static Bitmap? ConvertToBitmap(SKBitmap skBitmap)
    {
        try
        {
            using var image = SKImage.FromBitmap(skBitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = new MemoryStream();
            data.SaveTo(stream);
            stream.Position = 0;
            return new Bitmap(stream);
        }
        catch
        {
            return null;
        }
    }

    #endregion
}

/// <summary>
/// Represents a chart option for selection.
/// </summary>
public class ChartOption(ChartDataType chartType, string name, string description)
{
    public ChartDataType ChartType { get; } = chartType;
    public string Name { get; } = name;
    public string Description { get; } = description;
}

/// <summary>
/// Represents an export format option.
/// </summary>
public class ExportFormatOption(string name, ExportFormat format, string description)
{
    public string Name { get; } = name;
    public ExportFormat Format { get; } = format;
    public string Description { get; } = description;
}
