using System.Collections.ObjectModel;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Reports;
using ArgoBooks.Core.Services;
using ArgoBooks.Localization;
using ArgoBooks.Services;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SkiaSharp;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the Reports page with 3-step wizard navigation.
/// </summary>
public partial class ReportsPageViewModel : ViewModelBase
{
    #region Report Template Options for Step 1

    /// <summary>
    /// Template options for the Step 1 template grid.
    /// </summary>
    public ObservableCollection<ReportTemplateOption> ReportTemplateOptions { get; } =
    [
        new(ReportTemplateFactory.TemplateNames.MonthlySales, "Monthly Sales", "Summarize monthly sales data",
            "M3 13.125C3 12.504 3.504 12 4.125 12H6.375C6.996 12 7.5 12.504 7.5 13.125V19.875C7.5 20.496 6.996 21 6.375 21H4.125C3.504 21 3 20.496 3 19.875V13.125ZM9.75 8.625C9.75 8.004 10.254 7.5 10.875 7.5H13.125C13.746 7.5 14.25 8.004 14.25 8.625V19.875C14.25 20.496 13.746 21 13.125 21H10.875C10.254 21 9.75 20.496 9.75 19.875V8.625ZM16.5 4.125C16.5 3.504 17.004 3 17.625 3H19.875C20.496 3 21 3.504 21 4.125V19.875C21 20.496 20.496 21 19.875 21H17.625C17.004 21 16.5 20.496 16.5 19.875V4.125Z",
            "#2196F3", "#E3F2FD"),
        new(ReportTemplateFactory.TemplateNames.FinancialOverview, "Financial Overview", "Full financial breakdown",
            "M3.5 18.49l6-6.01 4 4L22 6.92l-1.41-1.41-7.09 7.97-4-4L2 16.99z",
            "#4CAF50", "#E8F5E9"),
        new(ReportTemplateFactory.TemplateNames.PerformanceAnalysis, "Performance Analysis", "Business performance metrics",
            "M19 3H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zM9 17H7v-7h2v7zm4 0h-2V7h2v10zm4 0h-2v-4h2v4z",
            "#FF9800", "#FFF3E0"),
        new(ReportTemplateFactory.TemplateNames.ReturnsAnalysis, "Returns Analysis", "Analyze product returns",
            "M12 5V1L7 6l5 5V7c3.31 0 6 2.69 6 6s-2.69 6-6 6-6-2.69-6-6H4c0 4.42 3.58 8 8 8s8-3.58 8-8-3.58-8-8-8z",
            "#F44336", "#FFEBEE"),
        new(ReportTemplateFactory.TemplateNames.GeographicAnalysis, "Geographic Analysis", "Sales by region",
            "M12 2C8.13 2 5 5.13 5 9c0 5.25 7 13 7 13s7-7.75 7-13c0-3.87-3.13-7-7-7zm0 9.5c-1.38 0-2.5-1.12-2.5-2.5s1.12-2.5 2.5-2.5 2.5 1.12 2.5 2.5-1.12 2.5-2.5 2.5z",
            "#9C27B0", "#F3E5F5"),
        new(ReportTemplateFactory.TemplateNames.Custom, "Blank Template", "Start from scratch",
            "M19 13h-6v6h-2v-6H5v-2h6V5h2v6h6v2z",
            "#9E9E9E", "#F5F5F5")
    ];

    #endregion

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

    public bool IsStep1 => CurrentStep == 1;
    public bool IsStep2 => CurrentStep == 2;
    public bool IsStep3 => CurrentStep == 3;

    // Step indicator state properties
    public bool IsStep1Active => CurrentStep >= 1;
    public bool IsStep2Active => CurrentStep >= 2 || Step1Completed;
    public bool IsStep3Active => CurrentStep >= 3 || Step2Completed;
    public bool IsConnector1Active => CurrentStep > 1 || Step1Completed;
    public bool IsConnector2Active => CurrentStep > 2 || Step2Completed;

    // Show checkmark only when completed AND past that step (not currently on it)
    public bool IsStep1CompletedAndPast => Step1Completed && CurrentStep > 1;
    public bool IsStep2CompletedAndPast => Step2Completed && CurrentStep > 2;

    public bool CanGoBack => CurrentStep > 1;
    public bool CanGoNext => CurrentStep < 3;
    public bool IsOnFinalStep => CurrentStep == 3;

    /// <summary>
    /// Function to confirm discarding unsaved changes. Set by AppShellViewModel.
    /// Returns true if changes should be discarded (continue), false to cancel.
    /// </summary>
    public Func<Task<bool>>? ConfirmDiscardChangesAsync { get; set; }

    [RelayCommand]
    private async Task GoToPreviousStepAsync()
    {
        if (CurrentStep > 1)
        {
            // Check for unsaved changes when going back from step 2 (Layout Designer)
            if (CurrentStep == 2 && HasUnsavedChanges)
            {
                if (ConfirmDiscardChangesAsync != null)
                {
                    var shouldContinue = await ConfirmDiscardChangesAsync();
                    if (!shouldContinue)
                    {
                        return; // User cancelled, don't go back
                    }
                }
            }

            // Reset completion flags when going back
            if (CurrentStep == 3)
            {
                Step2Completed = false;
            }
            else if (CurrentStep == 2)
            {
                Step1Completed = false;
                // Preserve chart selections before reloading template
                // (will be restored in ApplyConfigurationToPageSettings after template loads)
                _chartTypesToPreserve = Configuration.Filters.SelectedChartTypes.ToList();

                // Clear undo history and reload the template to discard layout changes
                UndoRedoManager.Clear();
                LoadTemplate(SelectedTemplateName);
            }

            CurrentStep--;
            NotifyStepChanged();
        }
    }

    /// <summary>
    /// Restores chart selections from a list of chart types.
    /// </summary>
    private void RestoreChartSelections(List<ChartDataType> chartTypes)
    {
        Configuration.Filters.SelectedChartTypes.Clear();
        Configuration.Filters.SelectedChartTypes.AddRange(chartTypes);

        foreach (var chart in AvailableCharts)
        {
            chart.IsSelected = chartTypes.Contains(chart.ChartType);
        }

        OnPropertyChanged(nameof(HasSelectedCharts));
    }

    private void NotifyStepChanged()
    {
        OnPropertyChanged(nameof(IsStep1));
        OnPropertyChanged(nameof(IsStep2));
        OnPropertyChanged(nameof(IsStep3));
        OnPropertyChanged(nameof(IsStep1Active));
        OnPropertyChanged(nameof(IsStep2Active));
        OnPropertyChanged(nameof(IsStep3Active));
        OnPropertyChanged(nameof(IsConnector1Active));
        OnPropertyChanged(nameof(IsConnector2Active));
        OnPropertyChanged(nameof(IsStep1CompletedAndPast));
        OnPropertyChanged(nameof(IsStep2CompletedAndPast));
        OnPropertyChanged(nameof(CanGoBack));
        OnPropertyChanged(nameof(CanGoNext));
        OnPropertyChanged(nameof(IsOnFinalStep));
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
            NotifyStepChanged();
        }
    }

    #endregion

    #region Step 1 - Template & Settings

    [ObservableProperty]
    private int _step1TabIndex;

    [ObservableProperty]
    private int _tablePropertiesTabIndex;

    public bool IsTemplatesTabSelected => Step1TabIndex == 0;
    public bool IsChartsTabSelected => Step1TabIndex == 1;

    partial void OnStep1TabIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsTemplatesTabSelected));
        OnPropertyChanged(nameof(IsChartsTabSelected));
    }

    [RelayCommand]
    private void SetStep1Tab(string tabIndex)
    {
        if (int.TryParse(tabIndex, out var index))
        {
            Step1TabIndex = index;
        }
    }

    [RelayCommand]
    private void SetTablePropertiesTab(string tabIndex)
    {
        if (int.TryParse(tabIndex, out var index))
        {
            TablePropertiesTabIndex = index;
        }
    }

    [ObservableProperty]
    private string _selectedTemplateName = ReportTemplateFactory.TemplateNames.Custom;

    [ObservableProperty]
    private string _reportName = "Untitled Report";

    /// <summary>
    /// Chart types to preserve during template reload (when going back from step 2).
    /// </summary>
    private List<ChartDataType>? _chartTypesToPreserve;

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
    public ObservableCollection<CustomTemplateOption> CustomTemplateNames { get; } = [];
    public ObservableCollection<DatePresetOption> DatePresets { get; } = [];

    // Chart selection - all charts in one list, use IsSelected property
    public ObservableCollection<ChartOption> AvailableCharts { get; } = [];

    // Chart categories for grouped display
    public ObservableCollection<ChartCategoryGroup> ChartCategories { get; } = [];

    public bool HasSelectedCharts => AvailableCharts.Any(c => c.IsSelected);

    partial void OnSelectedTemplateNameChanged(string value)
    {
        LoadTemplate(value);
    }

    partial void OnSelectedDatePresetChanged(string value)
    {
        IsCustomDateRange = value == DatePresetNames.Custom;
    }

    [RelayCommand]
    private void ToggleChart(ChartOption chart)
    {
        chart.IsSelected = !chart.IsSelected;
        OnPropertyChanged(nameof(HasSelectedCharts));
    }

    [RelayCommand]
    private void ToggleSelectAllInCategory(ChartCategoryGroup category)
    {
        category.ToggleSelectAll();
        OnPropertyChanged(nameof(HasSelectedCharts));
    }

    [RelayCommand]
    private void SelectTemplate(string? templateName)
    {
        if (!string.IsNullOrEmpty(templateName))
        {
            SelectedTemplateName = templateName;

            // Update IsSelected on all built-in template options
            foreach (var template in ReportTemplateOptions)
            {
                template.IsSelected = template.TemplateName == templateName;
            }

            // Update IsSelected on all custom template options
            foreach (var customTemplate in CustomTemplateNames)
            {
                customTemplate.IsSelected = customTemplate.Name == templateName;
            }
        }
    }

    [RelayCommand]
    private async Task OpenCustomTemplateAsync(string? templateName)
    {
        if (string.IsNullOrEmpty(templateName)) return;

        // Load the custom template
        SelectedTemplateName = templateName;

        // Wait briefly for template to load asynchronously
        await Task.Delay(50);

        // Set the report name to the template name
        ReportName = templateName;
        Configuration.Title = templateName;

        // Notify UI of configuration change (needed because ReportConfiguration doesn't implement INPC)
        OnPropertyChanged(nameof(Configuration));

        // Clear any unsaved changes indicator
        UndoRedoManager.Clear();

        // Go directly to step 2 (Layout Designer)
        Step1Completed = true;
        ApplyFiltersToConfiguration();
        CurrentStep = 2;
        NotifyStepChanged();
    }

    [RelayCommand]
    private void SelectDatePreset(DatePresetOption? preset)
    {
        if (preset != null)
        {
            SelectedDatePreset = preset.Name;

            // Update IsSelected on all date preset options
            foreach (var option in DatePresets)
            {
                option.IsSelected = option.Name == preset.Name;
            }
        }
    }

    #endregion

    #region Step 2 - Layout Designer

    [ObservableProperty]
    private ReportConfiguration _configuration = new();

    [ObservableProperty]
    private ReportElementBase? _selectedElement;

    /// <summary>
    /// Event raised when an element's properties change and the canvas needs to refresh.
    /// </summary>
    public event EventHandler<ReportElementBase>? ElementPropertyChanged;
    public event EventHandler? PageSettingsRefreshRequested;
    public event EventHandler? TemplateLoaded;
    public event EventHandler? PreviewFitToWindowRequested;
    public event EventHandler? CanvasRefreshRequested;

    partial void OnSelectedElementChanged(ReportElementBase? oldValue, ReportElementBase? newValue)
    {
        // Unsubscribe from old element
        if (oldValue != null)
        {
            oldValue.PropertyChanged -= OnElementPropertyChanged;
            oldValue.PropertyChanging -= OnElementPropertyChanging;
        }

        // Subscribe to new element
        if (newValue != null)
        {
            newValue.PropertyChanged += OnElementPropertyChanged;
            newValue.PropertyChanging += OnElementPropertyChanging;
        }

        OnPropertyChanged(nameof(SelectedChartElement));
        OnPropertyChanged(nameof(SelectedChartDataTypeOption));
        OnPropertyChanged(nameof(SelectedChartStyleOption));
        OnPropertyChanged(nameof(SelectedLabelElement));
        OnPropertyChanged(nameof(SelectedImageElement));
        OnPropertyChanged(nameof(SelectedTableElement));
        OnPropertyChanged(nameof(SelectedDateRangeElement));
        OnPropertyChanged(nameof(SelectedSummaryElement));
        OnPropertyChanged(nameof(IsChartSelected));
        OnPropertyChanged(nameof(IsLabelSelected));
        OnPropertyChanged(nameof(IsImageSelected));
        OnPropertyChanged(nameof(IsTableSelected));
        OnPropertyChanged(nameof(IsDateRangeSelected));
        OnPropertyChanged(nameof(IsSummarySelected));
    }

    private void OnElementPropertyChanging(object? sender, ElementPropertyChangingEventArgs e)
    {
        if (sender is ReportElementBase element)
        {
            // Skip position/size properties - these are tracked separately via drag/resize
            if (e.PropertyName is "X" or "Y" or "Width" or "Height" or "ZOrder" or "Bounds")
                return;

            // Record property change for undo/redo
            UndoRedoManager.RecordAction(new ElementPropertyChangeAction(
                Configuration,
                element.Id,
                element.DisplayName,
                e.PropertyName,
                e.OldValue,
                e.NewValue));
        }
    }

    private void OnElementPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is ReportElementBase element)
        {
            // Raise event to notify view to refresh element content
            ElementPropertyChanged?.Invoke(this, element);
        }
    }

    // Typed accessors for element-specific properties
    public ChartReportElement? SelectedChartElement => SelectedElement as ChartReportElement;
    public LabelReportElement? SelectedLabelElement => SelectedElement as LabelReportElement;
    public ImageReportElement? SelectedImageElement => SelectedElement as ImageReportElement;
    public TableReportElement? SelectedTableElement => SelectedElement as TableReportElement;
    public DateRangeReportElement? SelectedDateRangeElement => SelectedElement as DateRangeReportElement;
    public SummaryReportElement? SelectedSummaryElement => SelectedElement as SummaryReportElement;

    /// <summary>
    /// Gets or sets the selected chart style as a ChartStyleOption for the ComboBox binding.
    /// Converts between ChartStyleOption and ReportChartStyle.
    /// </summary>
    public ChartStyleOption? SelectedChartStyleOption
    {
        get => SelectedChartElement is { } chart
            ? ChartStyleOptions.FirstOrDefault(o => o.Value == chart.ChartStyle)
            : null;
        set
        {
            if (value != null && SelectedChartElement is { } chart)
            {
                chart.ChartStyle = value.Value;
                OnPropertyChanged();
            }
        }
    }

    // Type checking properties for conditional visibility
    public bool IsChartSelected => SelectedElement is ChartReportElement;
    public bool IsLabelSelected => SelectedElement is LabelReportElement;
    public bool IsImageSelected => SelectedElement is ImageReportElement;
    public bool IsTableSelected => SelectedElement is TableReportElement;
    public bool IsDateRangeSelected => SelectedElement is DateRangeReportElement;
    public bool IsSummarySelected => SelectedElement is SummaryReportElement;

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
    private bool _isElementPanelExpanded = true;

    #region Context Menu

    [ObservableProperty]
    private bool _isContextMenuOpen;

    [ObservableProperty]
    private double _contextMenuX;

    [ObservableProperty]
    private double _contextMenuY;

    /// <summary>
    /// Shows the context menu at the specified position.
    /// </summary>
    public void ShowContextMenu(double x, double y)
    {
        ContextMenuX = x;
        ContextMenuY = y;
        IsContextMenuOpen = true;
    }

    [RelayCommand]
    private void HideContextMenu()
    {
        IsContextMenuOpen = false;
    }

    [RelayCommand]
    private void ContextMenuBringToFront()
    {
        BringToFront();
        HideContextMenu();
    }

    [RelayCommand]
    private void ContextMenuSendToBack()
    {
        SendToBack();
        HideContextMenu();
    }

    [RelayCommand]
    private void ContextMenuDuplicate()
    {
        DuplicateSelectedElements();
        HideContextMenu();
    }

    [RelayCommand]
    private void ContextMenuDelete()
    {
        DeleteSelectedElements();
        HideContextMenu();
    }

    #endregion

    [ObservableProperty]
    private bool _isPageSettingsOpen;

    [ObservableProperty]
    private bool _isSaveTemplateOpen;

    [ObservableProperty]
    private bool _showSaveConfirmation;

    [ObservableProperty]
    private bool _showNoChangesMessage;

    public ReportUndoRedoManager UndoRedoManager { get; } = new();

    /// <summary>
    /// Gets whether the report has unsaved changes (changes since last save).
    /// </summary>
    public bool HasUnsavedChanges => UndoRedoManager.HasUnsavedChanges;

    /// <summary>
    /// ViewModel for the undo/redo button group control.
    /// </summary>
    public UndoRedoButtonGroupViewModel UndoRedoViewModel { get; }

    [ObservableProperty]
    private bool _isUndoDropdownOpen;

    [ObservableProperty]
    private bool _isRedoDropdownOpen;

    public ObservableCollection<UndoRedoHistoryItem> UndoHistoryItems { get; } = [];
    public ObservableCollection<UndoRedoHistoryItem> RedoHistoryItems { get; } = [];

    [RelayCommand]
    private void ToggleUndoDropdown()
    {
        if (!UndoRedoManager.CanUndo) return;
        IsRedoDropdownOpen = false;
        IsUndoDropdownOpen = !IsUndoDropdownOpen;
        if (IsUndoDropdownOpen)
        {
            RefreshUndoHistory();
        }
    }

    [RelayCommand]
    private void ToggleRedoDropdown()
    {
        if (!UndoRedoManager.CanRedo) return;
        IsUndoDropdownOpen = false;
        IsRedoDropdownOpen = !IsRedoDropdownOpen;
        if (IsRedoDropdownOpen)
        {
            RefreshRedoHistory();
        }
    }

    private void RefreshUndoHistory()
    {
        UndoHistoryItems.Clear();
        int index = 0;
        foreach (var desc in UndoRedoManager.UndoHistory)
        {
            UndoHistoryItems.Add(new UndoRedoHistoryItem { Index = index++, Description = desc });
        }
    }

    private void RefreshRedoHistory()
    {
        RedoHistoryItems.Clear();
        int index = 0;
        foreach (var desc in UndoRedoManager.RedoHistory)
        {
            RedoHistoryItems.Add(new UndoRedoHistoryItem { Index = index++, Description = desc });
        }
    }

    [RelayCommand]
    private void UndoToIndex(int index)
    {
        for (int i = 0; i <= index; i++)
        {
            UndoRedoManager.Undo();
        }
        IsUndoDropdownOpen = false;
        OnPropertyChanged(nameof(Configuration));
    }

    [RelayCommand]
    private void RedoToIndex(int index)
    {
        for (int i = 0; i <= index; i++)
        {
            UndoRedoManager.Redo();
        }
        IsRedoDropdownOpen = false;
        OnPropertyChanged(nameof(Configuration));
    }

    public ObservableCollection<ReportElementBase> SelectedElements { get; } = [];

    // Properties for toolbar button enabling
    public bool HasSelectedElements => SelectedElements.Count > 0;
    public bool HasMultipleSelectedElements => SelectedElements.Count > 1;

    private void NotifySelectionChanged()
    {
        OnPropertyChanged(nameof(HasSelectedElements));
        OnPropertyChanged(nameof(HasMultipleSelectedElements));
    }

    [RelayCommand]
    private void SelectElement(ReportElementBase? element)
    {
        SelectedElements.Clear();
        if (element != null)
        {
            SelectedElements.Add(element);
            SelectedElement = element;
        }
        else
        {
            SelectedElement = null;
        }
        NotifySelectionChanged();
    }

    [RelayCommand]
    private void ClearSelection()
    {
        SelectedElements.Clear();
        SelectedElement = null;
        NotifySelectionChanged();
    }

    /// <summary>
    /// Syncs selection from the canvas control.
    /// </summary>
    public void SyncSelection(List<ReportElementBase> selectedElements)
    {
        SelectedElements.Clear();
        foreach (var element in selectedElements)
        {
            SelectedElements.Add(element);
        }
        SelectedElement = selectedElements.FirstOrDefault();
        NotifySelectionChanged();
    }

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
        NotifySelectionChanged();
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
        NotifySelectionChanged();
        OnPropertyChanged(nameof(Configuration));
    }

    [RelayCommand]
    private void DuplicateSelectedElements()
    {
        if (SelectedElements.Count == 0) return;

        var newElements = new List<ReportElementBase>();
        const double offset = 20; // Offset for duplicated elements

        foreach (var element in SelectedElements.ToList())
        {
            var clone = element.Clone();
            clone.X += offset;
            clone.Y += offset;

            // Clamp position to stay within canvas bounds
            clone.X = Math.Max(0, Math.Min(clone.X, CanvasWidth - clone.Width));
            clone.Y = Math.Max(0, Math.Min(clone.Y, CanvasHeight - clone.Height));

            Configuration.AddElement(clone);
            UndoRedoManager.RecordAction(new AddElementAction(Configuration, clone));
            newElements.Add(clone);
        }

        // Select the duplicated elements
        SelectedElements.Clear();
        foreach (var element in newElements)
        {
            SelectedElements.Add(element);
        }
        SelectedElement = newElements.FirstOrDefault();
        NotifySelectionChanged();
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
        ZoomLevel = Math.Min(Controls.Reports.SkiaReportDesignCanvas.MaxZoom, ZoomLevel + 0.1);
    }

    [RelayCommand]
    private void ZoomOut()
    {
        ZoomLevel = Math.Max(Controls.Reports.SkiaReportDesignCanvas.MinZoom, ZoomLevel - 0.1);
    }

    [RelayCommand]
    private void ZoomFit()
    {
        ZoomLevel = 1.0;
    }

    [RelayCommand]
    private async Task BrowseImagePathAsync()
    {
        if (SelectedImageElement == null) return;

        var topLevel = Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;

        if (topLevel?.StorageProvider != null)
        {
            var filters = new[]
            {
                new Avalonia.Platform.Storage.FilePickerFileType("Image files") { Patterns = ["*.png", "*.jpg", "*.jpeg", "*.bmp", "*.gif"
                    ]
                },
                new Avalonia.Platform.Storage.FilePickerFileType("All files") { Patterns = ["*.*"] }
            };

            var result = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select Image",
                AllowMultiple = false,
                FileTypeFilter = filters
            });

            if (result.Count > 0)
            {
                SelectedImageElement.ImagePath = result[0].Path.LocalPath;
                OnPropertyChanged(nameof(SelectedImageElement));
                OnPropertyChanged(nameof(Configuration));
            }
        }
    }

    // Store original values for cancel
    private PageSize _originalPageSize;
    private PageOrientation _originalPageOrientation;
    private double _originalMarginTop, _originalMarginRight, _originalMarginBottom, _originalMarginLeft;
    private bool _originalShowHeader, _originalShowFooter, _originalShowPageNumbers;
    private string _originalBackgroundColor = "#FFFFFF";

    [RelayCommand]
    private void OpenPageSettings()
    {
        // Store original values
        _originalPageSize = PageSize;
        _originalPageOrientation = PageOrientation;
        _originalMarginTop = MarginTop;
        _originalMarginRight = MarginRight;
        _originalMarginBottom = MarginBottom;
        _originalMarginLeft = MarginLeft;
        _originalShowHeader = ShowHeader;
        _originalShowFooter = ShowFooter;
        _originalShowPageNumbers = ShowPageNumbers;
        _originalBackgroundColor = BackgroundColor;

        IsPageSettingsOpen = true;
    }

    [RelayCommand]
    private void ClosePageSettings()
    {
        // Restore original values on cancel
        PageSize = _originalPageSize;
        PageOrientation = _originalPageOrientation;
        MarginTop = _originalMarginTop;
        MarginRight = _originalMarginRight;
        MarginBottom = _originalMarginBottom;
        MarginLeft = _originalMarginLeft;
        ShowHeader = _originalShowHeader;
        ShowFooter = _originalShowFooter;
        ShowPageNumbers = _originalShowPageNumbers;
        BackgroundColor = _originalBackgroundColor;

        IsPageSettingsOpen = false;

        // Ensure canvas is refreshed after modal closes with original values
        PageSettingsRefreshRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Returns true if we are editing an existing custom template (not a built-in template).
    /// </summary>
    private bool IsEditingCustomTemplate =>
        !string.IsNullOrEmpty(SelectedTemplateName) &&
        !ReportTemplateFactory.IsBuiltInTemplate(SelectedTemplateName);

    /// <summary>
    /// Saves the current configuration to the current custom template.
    /// </summary>
    private async Task<bool> SaveToCurrentTemplateAsync()
    {
        var success = await _templateStorage.SaveTemplateAsync(Configuration, SelectedTemplateName);
        if (success)
        {
            LoadCustomTemplates();
            // Mark save point so asterisk disappears
            UndoRedoManager.MarkSaved();
            // Show save confirmation message
            ShowSaveConfirmation = true;
            await Task.Delay(2000);
            ShowSaveConfirmation = false;
        }
        return success;
    }

    [RelayCommand]
    private async Task OpenSaveTemplate()
    {
        // If editing an existing custom template, save directly without showing modal
        if (IsEditingCustomTemplate)
        {
            // Check if there are unsaved changes
            if (!HasUnsavedChanges)
            {
                // Show "No changes" message
                ShowNoChangesMessage = true;
                await Task.Delay(2000);
                ShowNoChangesMessage = false;
                return;
            }

            await SaveToCurrentTemplateAsync();
            return;
        }

        IsSaveTemplateOpen = true;
    }

    [RelayCommand]
    private void CloseSaveTemplate()
    {
        IsSaveTemplateOpen = false;
        _saveTemplateCompletionSource?.TrySetResult(false);
    }

    private TaskCompletionSource<bool>? _saveTemplateCompletionSource;

    /// <summary>
    /// Opens the save template modal and waits for it to close.
    /// Returns true if template was saved successfully, false if cancelled.
    /// </summary>
    public async Task<bool> OpenSaveTemplateAndWaitAsync()
    {
        // If editing an existing custom template, save directly without showing modal
        if (IsEditingCustomTemplate)
        {
            return await SaveToCurrentTemplateAsync();
        }

        _saveTemplateCompletionSource = new TaskCompletionSource<bool>();
        IsSaveTemplateOpen = true;
        return await _saveTemplateCompletionSource.Task;
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
    private double _previewDisplayWidth = 612;

    [ObservableProperty]
    private double _previewDisplayHeight = 792;

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
            var (width, height) = PageDimensions.GetDimensions(Configuration.PageSize, Configuration.PageOrientation);

            // Store display dimensions (original page size)
            PreviewDisplayWidth = width;
            PreviewDisplayHeight = height;

            // Render at 2x resolution for sharper zoom, but display at original size
            const int resolutionMultiplier = 2;
            using var renderer = new ReportRenderer(Configuration, companyData, 1f, LanguageServiceTranslationProvider.Instance);
            using var skBitmap = renderer.CreatePreview(width * resolutionMultiplier, height * resolutionMultiplier);
            PreviewImage = ConvertToBitmap(skBitmap);

            // Request fit-to-window after preview is generated
            PreviewFitToWindowRequested?.Invoke(this, EventArgs.Empty);
        }
        catch
        {
            PreviewImage = null;
        }
    }

    [RelayCommand]
    private void PreviewZoomIn()
    {
        PreviewZoom = Math.Min(Controls.Reports.SkiaReportDesignCanvas.MaxZoom, PreviewZoom + Controls.Reports.SkiaReportDesignCanvas.ZoomStep);
    }

    [RelayCommand]
    private void PreviewZoomOut()
    {
        PreviewZoom = Math.Max(Controls.Reports.SkiaReportDesignCanvas.MinZoom, PreviewZoom - Controls.Reports.SkiaReportDesignCanvas.ZoomStep);
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
            using var renderer = new ReportRenderer(Configuration, companyData, PageDimensions.RenderScale, LanguageServiceTranslationProvider.Instance);

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

                // Save export settings for next time
                await SaveExportSettingsAsync();

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
    private void SelectExportFormat(ExportFormat format)
    {
        SelectedExportFormat = format;
        // Update file extension if a path is already set
        if (!string.IsNullOrEmpty(ExportFilePath))
        {
            var dir = Path.GetDirectoryName(ExportFilePath) ?? "";
            var name = Path.GetFileNameWithoutExtension(ExportFilePath);
            var newExt = format switch
            {
                ExportFormat.PDF => ".pdf",
                ExportFormat.PNG => ".png",
                ExportFormat.JPEG => ".jpg",
                _ => ".pdf"
            };
            ExportFilePath = Path.Combine(dir, $"{name}{newExt}");
        }
    }

    [RelayCommand]
    private async Task BrowseExportPathAsync()
    {
        var defaultName = string.IsNullOrWhiteSpace(ReportName) ? "Report" : ReportName;
        var extension = SelectedExportFormat switch
        {
            ExportFormat.PDF => ".pdf",
            ExportFormat.PNG => ".png",
            ExportFormat.JPEG => ".jpg",
            _ => ".pdf"
        };

        // Use last export directory, or default to Desktop
        var lastDir = App.SettingsService?.GlobalSettings.ReportExport.LastExportDirectory;
        var defaultPath = !string.IsNullOrEmpty(lastDir) && Directory.Exists(lastDir)
            ? lastDir
            : Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        // Try to use the storage provider for a native save dialog
        var topLevel = Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;

        if (topLevel?.StorageProvider != null)
        {
            var filters = SelectedExportFormat switch
            {
                ExportFormat.PDF => new[] { new Avalonia.Platform.Storage.FilePickerFileType("PDF Document") { Patterns =
                    ["*.pdf"]
                } },
                ExportFormat.PNG => new[] { new Avalonia.Platform.Storage.FilePickerFileType("PNG Image") { Patterns =
                    ["*.png"]
                } },
                ExportFormat.JPEG => new[] { new Avalonia.Platform.Storage.FilePickerFileType("JPEG Image") { Patterns =
                    ["*.jpg", "*.jpeg"]
                } },
                _ => new[] { new Avalonia.Platform.Storage.FilePickerFileType("PDF Document") { Patterns = ["*.pdf"] } }
            };

            var result = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save Report As",
                SuggestedFileName = $"{defaultName}{extension}",
                FileTypeChoices = filters,
                DefaultExtension = extension.TrimStart('.')
            });

            if (result != null)
            {
                ExportFilePath = result.Path.LocalPath;
            }
        }
        else
        {
            // Fallback to default path
            ExportFilePath = Path.Combine(defaultPath, $"{defaultName}{extension}");
        }
    }

    private async Task SaveExportSettingsAsync()
    {
        if (App.SettingsService?.GlobalSettings.ReportExport is { } settings)
        {
            settings.LastExportDirectory = Path.GetDirectoryName(ExportFilePath);
            settings.OpenAfterExport = OpenAfterExport;
            settings.IncludeMetadata = IncludeMetadata;
            await App.SettingsService.SaveGlobalSettingsAsync();
        }
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

    // Element property enum collections - uses GetDisplayName() extension method for consistent naming
    // Wrapped with Loc.Tr() for translation support
    public ObservableCollection<ChartDataTypeOption> ChartTypeOptions { get; } =
        new(Enum.GetValues<ChartDataType>().Select(t => new ChartDataTypeOption(t, Loc.Tr(t.GetDisplayName()))));

    /// <summary>
    /// Gets or sets the selected chart data type option, syncing with SelectedChartElement.ChartType.
    /// </summary>
    public ChartDataTypeOption? SelectedChartDataTypeOption
    {
        get => SelectedChartElement != null
            ? ChartTypeOptions.FirstOrDefault(o => o.Value == SelectedChartElement.ChartType)
            : null;
        set
        {
            if (SelectedChartElement != null && value != null)
            {
                SelectedChartElement.ChartType = value.Value;
                OnPropertyChanged();
            }
        }
    }

    public ObservableCollection<ChartStyleOption> ChartStyleOptions { get; } =
    [
        new(ReportChartStyle.Bar, "Bar Chart"),
        new(ReportChartStyle.Line, "Line Chart"),
        new(ReportChartStyle.StepLine, "Step Line"),
        new(ReportChartStyle.Area, "Area Chart"),
        new(ReportChartStyle.Scatter, "Scatter Chart")
    ];

    public ObservableCollection<ImageScaleMode> ImageScaleModes { get; } =
        new(Enum.GetValues<ImageScaleMode>());

    public ObservableCollection<TableDataSelection> TableDataSelections { get; } =
        new(Enum.GetValues<TableDataSelection>());

    public ObservableCollection<TableSortOrder> TableSortOrders { get; } =
        new(Enum.GetValues<TableSortOrder>());

    public ObservableCollection<TransactionType> TransactionTypes { get; } =
        new(Enum.GetValues<TransactionType>());

    public ObservableCollection<HorizontalTextAlignment> HorizontalAlignments { get; } =
        new(Enum.GetValues<HorizontalTextAlignment>());

    public ObservableCollection<VerticalTextAlignment> VerticalAlignments { get; } =
        new(Enum.GetValues<VerticalTextAlignment>());

    public ObservableCollection<string> FontFamilies { get; } =
        ["Segoe UI", "Arial", "Times New Roman", "Calibri", "Courier New", "Georgia", "Verdana", "Trebuchet MS"];

    public ObservableCollection<string> DateFormats { get; } =
        ["MM/dd/yyyy", "dd/MM/yyyy", "yyyy-MM-dd", "MMMM d, yyyy", "MMM d, yyyy", "d MMMM yyyy", "d MMM yyyy"];

    // Partial methods to update Configuration immediately when page settings change
    partial void OnPageSizeChanged(PageSize value)
    {
        if (IsPageSettingsOpen)
        {
            Configuration.PageSize = value;
            UpdateCanvasDimensions();
            PageSettingsRefreshRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    partial void OnPageOrientationChanged(PageOrientation value)
    {
        if (IsPageSettingsOpen)
        {
            Configuration.PageOrientation = value;
            UpdateCanvasDimensions();
            PageSettingsRefreshRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    partial void OnMarginTopChanged(double value)
    {
        if (IsPageSettingsOpen)
        {
            Configuration.PageMargins = new ReportMargins(MarginLeft, value, MarginRight, MarginBottom);
            PageSettingsRefreshRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    partial void OnMarginRightChanged(double value)
    {
        if (IsPageSettingsOpen)
        {
            Configuration.PageMargins = new ReportMargins(MarginLeft, MarginTop, value, MarginBottom);
            PageSettingsRefreshRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    partial void OnMarginBottomChanged(double value)
    {
        if (IsPageSettingsOpen)
        {
            Configuration.PageMargins = new ReportMargins(MarginLeft, MarginTop, MarginRight, value);
            PageSettingsRefreshRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    partial void OnMarginLeftChanged(double value)
    {
        if (IsPageSettingsOpen)
        {
            Configuration.PageMargins = new ReportMargins(value, MarginTop, MarginRight, MarginBottom);
            PageSettingsRefreshRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    partial void OnShowHeaderChanged(bool value)
    {
        if (IsPageSettingsOpen)
        {
            Configuration.ShowHeader = value;
            PageSettingsRefreshRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    partial void OnShowFooterChanged(bool value)
    {
        if (IsPageSettingsOpen)
        {
            Configuration.ShowFooter = value;
            PageSettingsRefreshRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    partial void OnShowPageNumbersChanged(bool value)
    {
        if (IsPageSettingsOpen)
        {
            Configuration.ShowPageNumbers = value;
            PageSettingsRefreshRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    partial void OnBackgroundColorChanged(string value)
    {
        if (IsPageSettingsOpen)
        {
            Configuration.BackgroundColor = value;
            PageSettingsRefreshRequested?.Invoke(this, EventArgs.Empty);
        }
    }

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

        // Refresh the canvas after modal closes
        PageSettingsRefreshRequested?.Invoke(this, EventArgs.Empty);
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
            // Close modal immediately
            IsSaveTemplateOpen = false;
            SaveTemplateMessage = null;

            // Update SelectedTemplateName to the new template name so subsequent saves
            // will save to this template directly (without showing the save modal)
            SelectedTemplateName = SaveTemplateName;

            // Refresh custom templates list
            LoadCustomTemplates();

            // Mark save point so asterisk disappears
            UndoRedoManager.MarkSaved();

            // Show the "Saved" overlay notification
            ShowSaveConfirmation = true;
            await Task.Delay(2000);
            ShowSaveConfirmation = false;

            // Signal successful save to any waiting callers
            _saveTemplateCompletionSource?.TrySetResult(true);
        }
        else
        {
            SaveTemplateMessage = "Failed to save template. Please try again.";
        }
    }

    #endregion

    #region Delete Template Properties

    [ObservableProperty]
    private bool _isDeleteTemplateOpen;

    [ObservableProperty]
    private string _templateToDelete = string.Empty;

    [RelayCommand]
    private void OpenDeleteTemplate(string templateName)
    {
        TemplateToDelete = templateName;
        IsDeleteTemplateOpen = true;
    }

    [RelayCommand]
    private void CloseDeleteTemplate()
    {
        IsDeleteTemplateOpen = false;
        TemplateToDelete = string.Empty;
    }

    [RelayCommand]
    private void ConfirmDeleteTemplate()
    {
        if (string.IsNullOrEmpty(TemplateToDelete)) return;

        var success = _templateStorage.DeleteTemplate(TemplateToDelete);
        if (success)
        {
            // If we're deleting the currently selected template, switch to blank
            if (SelectedTemplateName == TemplateToDelete)
            {
                SelectedTemplateName = ReportTemplateFactory.TemplateNames.Custom;
            }

            // Refresh custom templates list
            LoadCustomTemplates();
        }

        IsDeleteTemplateOpen = false;
        TemplateToDelete = string.Empty;
    }

    #endregion

    #region Rename Template

    [ObservableProperty]
    private bool _isRenameTemplateOpen;

    [ObservableProperty]
    private string _templateToRename = string.Empty;

    [ObservableProperty]
    private string _renameTemplateNewName = string.Empty;

    [RelayCommand]
    private void OpenRenameTemplate(string templateName)
    {
        TemplateToRename = templateName;
        RenameTemplateNewName = templateName;
        IsRenameTemplateOpen = true;
    }

    [RelayCommand]
    private void CloseRenameTemplate()
    {
        IsRenameTemplateOpen = false;
        TemplateToRename = string.Empty;
        RenameTemplateNewName = string.Empty;
    }

    [RelayCommand]
    private async Task ConfirmRenameTemplateAsync()
    {
        if (string.IsNullOrEmpty(TemplateToRename) || string.IsNullOrEmpty(RenameTemplateNewName)) return;
        if (TemplateToRename == RenameTemplateNewName)
        {
            CloseRenameTemplate();
            return;
        }

        var success = await _templateStorage.RenameTemplateAsync(TemplateToRename, RenameTemplateNewName);
        if (success)
        {
            // If we're renaming the currently selected template, update the selection
            if (SelectedTemplateName == TemplateToRename)
            {
                SelectedTemplateName = RenameTemplateNewName;
            }

            // Refresh custom templates list
            LoadCustomTemplates();
        }

        IsRenameTemplateOpen = false;
        TemplateToRename = string.Empty;
        RenameTemplateNewName = string.Empty;
    }

    #endregion

    #region Constructor & Initialization

    private readonly ReportTemplateStorage _templateStorage = new();

    public ReportsPageViewModel()
    {
        // Initialize the undo/redo view model
        UndoRedoViewModel = new UndoRedoButtonGroupViewModel(UndoRedoManager);
        UndoRedoViewModel.ActionPerformed += (_, _) => OnPropertyChanged(nameof(Configuration));

        // Load element panel state from settings
        var uiSettings = App.SettingsService?.GlobalSettings.Ui;
        if (uiSettings != null)
        {
            _isElementPanelExpanded = !uiSettings.ReportsElementPanelCollapsed;
        }

        InitializeCollections();
        LoadTemplate(SelectedTemplateName);
        InitializeExportSettings();

        // Subscribe to language changes to refresh preview with updated translations
        LanguageService.Instance.LanguageChanged += OnLanguageChanged;
    }

    /// <summary>
    /// Called when the language changes. Refreshes the preview to update translations.
    /// </summary>
    private void OnLanguageChanged(object? sender, LanguageChangedEventArgs e)
    {
        // Notify property changes for any translated properties
        OnPropertyChanged(nameof(CurrentStepTitle));

        // Refresh the preview and canvas to show translated content
        GeneratePreview();
        CanvasRefreshRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Cleans up event subscriptions when the view model is no longer needed.
    /// </summary>
    public void Cleanup()
    {
        LanguageService.Instance.LanguageChanged -= OnLanguageChanged;
    }

    /// <summary>
    /// Saves the element panel state when it changes.
    /// </summary>
    partial void OnIsElementPanelExpandedChanged(bool value)
    {
        var settings = App.SettingsService?.GlobalSettings;
        if (settings != null)
        {
            settings.Ui.ReportsElementPanelCollapsed = !value;
            _ = App.SettingsService?.SaveGlobalSettingsAsync();
        }
    }

    private void InitializeExportSettings()
    {
        // Load settings from persisted storage
        var settings = App.SettingsService?.GlobalSettings.ReportExport;
        if (settings != null)
        {
            OpenAfterExport = settings.OpenAfterExport;
            IncludeMetadata = settings.IncludeMetadata;
        }

        // Set default export path to Desktop
        var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        var extension = SelectedExportFormat switch
        {
            ExportFormat.PDF => ".pdf",
            ExportFormat.PNG => ".png",
            ExportFormat.JPEG => ".jpg",
            _ => ".pdf"
        };
        ExportFilePath = Path.Combine(desktopPath, $"Report{extension}");
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
            DatePresets.Add(new DatePresetOption(preset));
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
            CustomTemplateNames.Add(new CustomTemplateOption(name));
        }
    }

    private void InitializeChartOptions()
    {
        AvailableCharts.Clear();
        ChartCategories.Clear();

        // Define category colors and icons
        const string revenueColor = "#2196F3";       // Blue
        const string revenueLight = "#E3F2FD";
        const string expenseColor = "#F44336";       // Red
        const string expenseLight = "#FFEBEE";
        const string financialColor = "#4CAF50";     // Green
        const string financialLight = "#E8F5E9";
        const string transactionColor = "#FF9800";   // Orange
        const string transactionLight = "#FFF3E0";
        const string geographicColor = "#9C27B0";    // Purple
        const string geographicLight = "#F3E5F5";
        const string accountantColor = "#00BCD4";    // Cyan
        const string accountantLight = "#E0F7FA";
        const string returnColor = "#E91E63";        // Pink
        const string returnLight = "#FCE4EC";
        const string lossColor = "#795548";          // Brown
        const string lossLight = "#EFEBE9";

        // SVG icons for different chart types
        const string lineChartIcon = "M3.5 18.49l6-6.01 4 4L22 6.92l-1.41-1.41-7.09 7.97-4-4L2 16.99z";
        const string barChartIcon = "M3 13.125C3 12.504 3.504 12 4.125 12H6.375C6.996 12 7.5 12.504 7.5 13.125V19.875C7.5 20.496 6.996 21 6.375 21H4.125C3.504 21 3 20.496 3 19.875V13.125ZM9.75 8.625C9.75 8.004 10.254 7.5 10.875 7.5H13.125C13.746 7.5 14.25 8.004 14.25 8.625V19.875C14.25 20.496 13.746 21 13.125 21H10.875C10.254 21 9.75 20.496 9.75 19.875V8.625ZM16.5 4.125C16.5 3.504 17.004 3 17.625 3H19.875C20.496 3 21 3.504 21 4.125V19.875C21 20.496 20.496 21 19.875 21H17.625C17.004 21 16.5 20.496 16.5 19.875V4.125Z";
        const string pieChartIcon = "M11 2v20c-5.07-.5-9-4.79-9-10s3.93-9.5 9-10zm2.03 0v8.99H22c-.47-4.74-4.24-8.52-8.97-8.99zm0 11.01V22c4.74-.47 8.5-4.25 8.97-8.99h-8.97z";
        const string trendUpIcon = "M16 6l2.29 2.29-4.88 4.88-4-4L2 16.59 3.41 18l6-6 4 4 6.3-6.29L22 12V6z";
        const string compareIcon = "M9.5 3H4C2.9 3 2 3.9 2 5v14c0 1.1.9 2 2 2h5.5V3zm5 0v18H20c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2h-5.5z";
        const string growthIcon = "M7.5 21H2V9h5.5v12zm7.25-18h-5.5v18h5.5V3zM22 11h-5.5v10H22V11z";
        const string transactionIcon = "M20 4H4c-1.11 0-1.99.89-1.99 2L2 18c0 1.11.89 2 2 2h16c1.11 0 2-.89 2-2V6c0-1.11-.89-2-2-2zm0 14H4v-6h16v6zm0-10H4V6h16v2z";
        const string shippingIcon = "M20 8h-3V4H3c-1.1 0-2 .9-2 2v11h2c0 1.66 1.34 3 3 3s3-1.34 3-3h6c0 1.66 1.34 3 3 3s3-1.34 3-3h2v-5l-3-4zM6 18.5c-.83 0-1.5-.67-1.5-1.5s.67-1.5 1.5-1.5 1.5.67 1.5 1.5-.67 1.5-1.5 1.5zm13.5-9l1.96 2.5H17V9.5h2.5zm-1.5 9c-.83 0-1.5-.67-1.5-1.5s.67-1.5 1.5-1.5 1.5.67 1.5 1.5-.67 1.5-1.5 1.5z";
        const string globeIcon = "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-1 17.93c-3.95-.49-7-3.85-7-7.93 0-.62.08-1.21.21-1.79L9 15v1c0 1.1.9 2 2 2v1.93zm6.9-2.54c-.26-.81-1-1.39-1.9-1.39h-1v-3c0-.55-.45-1-1-1H8v-2h2c.55 0 1-.45 1-1V7h2c1.1 0 2-.9 2-2v-.41c2.93 1.19 5 4.06 5 7.41 0 2.08-.8 3.97-2.1 5.39z";
        const string locationIcon = "M12 2C8.13 2 5 5.13 5 9c0 5.25 7 13 7 13s7-7.75 7-13c0-3.87-3.13-7-7-7zm0 9.5c-1.38 0-2.5-1.12-2.5-2.5s1.12-2.5 2.5-2.5 2.5 1.12 2.5 2.5-1.12 2.5-2.5 2.5z";
        const string buildingIcon = "M12 7V3H2v18h20V7H12zM6 19H4v-2h2v2zm0-4H4v-2h2v2zm0-4H4V9h2v2zm0-4H4V5h2v2zm4 12H8v-2h2v2zm0-4H8v-2h2v2zm0-4H8V9h2v2zm0-4H8V5h2v2zm10 12h-8v-2h2v-2h-2v-2h2v-2h-2V9h8v10zm-2-8h-2v2h2v-2zm0 4h-2v2h2v-2z";
        const string personIcon = "M12 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm0 2c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4z";
        const string returnIcon = "M12 5V1L7 6l5 5V7c3.31 0 6 2.69 6 6s-2.69 6-6 6-6-2.69-6-6H4c0 4.42 3.58 8 8 8s8-3.58 8-8-3.58-8-8-8z";
        const string reasonIcon = "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 15h-2v-2h2v2zm0-4h-2V7h2v6z";
        const string impactIcon = "M11.8 10.9c-2.27-.59-3-1.2-3-2.15 0-1.09 1.01-1.85 2.7-1.85 1.78 0 2.44.85 2.5 2.1h2.21c-.07-1.72-1.12-3.3-3.21-3.81V3h-3v2.16c-1.94.42-3.5 1.68-3.5 3.61 0 2.31 1.91 3.46 4.7 4.13 2.5.6 3 1.48 3 2.41 0 .69-.49 1.79-2.7 1.79-2.06 0-2.87-.92-2.98-2.1h-2.2c.12 2.19 1.76 3.42 3.68 3.83V21h3v-2.15c1.95-.37 3.5-1.5 3.5-3.55 0-2.84-2.43-3.81-4.7-4.4z";
        const string categoryIcon = "M3 3v8h8V3H3zm6 6H5V5h4v4zm-6 4v8h8v-8H3zm6 6H5v-4h4v4zm4-16v8h8V3h-8zm6 6h-4V5h4v4zm-6 4v8h8v-8h-8zm6 6h-4v-4h4v4z";
        const string productIcon = "M19 3H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm0 16H5V5h14v14zm-7-7l-4 5h8z";
        const string vsIcon = "M12 3C6.5 3 2 6.58 2 11c0 2.13 1.02 4.05 2.67 5.45v4.05l3.58-2.03c1.2.39 2.48.53 3.75.53 5.5 0 10-3.58 10-8s-4.5-8-10-8z";
        const string lossIcon = "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-2 15l-5-5 1.41-1.41L10 14.17l7.59-7.59L19 8l-9 9z";

        // Revenue charts
        var revenueCharts = new ObservableCollection<ChartOption>
        {
            new(ChartDataType.TotalRevenue, "Total Revenue", "Revenue over time", "Revenue", lineChartIcon, revenueColor, revenueLight),
            new(ChartDataType.RevenueDistribution, "Revenue Distribution", "Revenue by category", "Revenue", pieChartIcon, revenueColor, revenueLight)
        };

        // Expense charts
        var expenseCharts = new ObservableCollection<ChartOption>
        {
            new(ChartDataType.TotalExpenses, "Total Expenses", "Expenses over time", "Expenses", lineChartIcon, expenseColor, expenseLight),
            new(ChartDataType.ExpensesDistribution, "Expense Distribution", "Expenses by category", "Expenses", pieChartIcon, expenseColor, expenseLight)
        };

        // Financial charts
        var financialCharts = new ObservableCollection<ChartOption>
        {
            new(ChartDataType.TotalProfits, "Total Profits", "Profit over time", "Financial", trendUpIcon, financialColor, financialLight),
            new(ChartDataType.SalesVsExpenses, "Expenses vs Revenue", "Compare revenue and costs", "Financial", compareIcon, financialColor, financialLight),
            new(ChartDataType.GrowthRates, "Growth Rates", "Period-over-period growth", "Financial", growthIcon, financialColor, financialLight)
        };

        // Transaction charts
        var transactionCharts = new ObservableCollection<ChartOption>
        {
            new(ChartDataType.AverageTransactionValue, "Average Transaction", "Average transaction amounts", "Transactions", transactionIcon, transactionColor, transactionLight),
            new(ChartDataType.TotalTransactions, "Total Transactions", "Transaction volume", "Transactions", barChartIcon, transactionColor, transactionLight),
            new(ChartDataType.AverageShippingCosts, "Average Shipping Costs", "Average shipping costs", "Transactions", shippingIcon, transactionColor, transactionLight)
        };

        // Geographic charts
        var geographicCharts = new ObservableCollection<ChartOption>
        {
            new(ChartDataType.WorldMap, "Geographic Distribution", "Geographic distribution", "Geographic", globeIcon, geographicColor, geographicLight),
            new(ChartDataType.CountriesOfOrigin, "Countries of Origin", "Sales by origin country", "Geographic", locationIcon, geographicColor, geographicLight),
            new(ChartDataType.CountriesOfDestination, "Countries of Destination", "Sales by destination country", "Geographic", locationIcon, geographicColor, geographicLight),
            new(ChartDataType.CompaniesOfOrigin, "Companies of Origin", "Sales by supplier company", "Geographic", buildingIcon, geographicColor, geographicLight)
        };

        // Accountant charts
        var accountantCharts = new ObservableCollection<ChartOption>
        {
            new(ChartDataType.AccountantsTransactions, "Transactions by Accountant", "Transactions by accountant", "Personnel", personIcon, accountantColor, accountantLight)
        };

        // Return charts
        var returnCharts = new ObservableCollection<ChartOption>
        {
            new(ChartDataType.ReturnsOverTime, "Returns Over Time", "Return trends", "Returns", returnIcon, returnColor, returnLight),
            new(ChartDataType.ReturnReasons, "Return Reasons", "Why items are returned", "Returns", reasonIcon, returnColor, returnLight),
            new(ChartDataType.ReturnFinancialImpact, "Return Financial Impact", "Financial impact of returns", "Returns", impactIcon, returnColor, returnLight),
            new(ChartDataType.ReturnsByCategory, "Returns by Category", "Returns by category", "Returns", categoryIcon, returnColor, returnLight),
            new(ChartDataType.ReturnsByProduct, "Returns by Product", "Returns by product", "Returns", productIcon, returnColor, returnLight),
            new(ChartDataType.PurchaseVsSaleReturns, "Purchase vs Sale Returns", "Purchase vs sale returns", "Returns", vsIcon, returnColor, returnLight)
        };

        // Loss charts
        var lossCharts = new ObservableCollection<ChartOption>
        {
            new(ChartDataType.LossesOverTime, "Losses Over Time", "Loss trends", "Losses", lossIcon, lossColor, lossLight),
            new(ChartDataType.LossReasons, "Loss Reasons", "Why items are lost", "Losses", reasonIcon, lossColor, lossLight),
            new(ChartDataType.LossFinancialImpact, "Loss Financial Impact", "Financial impact of losses", "Losses", impactIcon, lossColor, lossLight),
            new(ChartDataType.LossesByCategory, "Losses by Category", "Losses by category", "Losses", categoryIcon, lossColor, lossLight),
            new(ChartDataType.LossesByProduct, "Losses by Product", "Losses by product", "Losses", productIcon, lossColor, lossLight),
            new(ChartDataType.PurchaseVsSaleLosses, "Purchase vs Sale Losses", "Purchase vs sale losses", "Losses", vsIcon, lossColor, lossLight)
        };

        // Add all charts to AvailableCharts (flat list for backward compatibility)
        foreach (var chart in revenueCharts) AvailableCharts.Add(chart);
        foreach (var chart in expenseCharts) AvailableCharts.Add(chart);
        foreach (var chart in financialCharts) AvailableCharts.Add(chart);
        foreach (var chart in transactionCharts) AvailableCharts.Add(chart);
        foreach (var chart in geographicCharts) AvailableCharts.Add(chart);
        foreach (var chart in accountantCharts) AvailableCharts.Add(chart);
        foreach (var chart in returnCharts) AvailableCharts.Add(chart);
        foreach (var chart in lossCharts) AvailableCharts.Add(chart);

        // Create category groups for grouped display
        ChartCategories.Add(new ChartCategoryGroup("Revenue", revenueColor, revenueCharts, () => OnPropertyChanged(nameof(HasSelectedCharts))));
        ChartCategories.Add(new ChartCategoryGroup("Expenses", expenseColor, expenseCharts, () => OnPropertyChanged(nameof(HasSelectedCharts))));
        ChartCategories.Add(new ChartCategoryGroup("Financial", financialColor, financialCharts, () => OnPropertyChanged(nameof(HasSelectedCharts))));
        ChartCategories.Add(new ChartCategoryGroup("Transactions", transactionColor, transactionCharts, () => OnPropertyChanged(nameof(HasSelectedCharts))));
        ChartCategories.Add(new ChartCategoryGroup("Geographic", geographicColor, geographicCharts, () => OnPropertyChanged(nameof(HasSelectedCharts))));
        ChartCategories.Add(new ChartCategoryGroup("Personnel", accountantColor, accountantCharts, () => OnPropertyChanged(nameof(HasSelectedCharts))));
        ChartCategories.Add(new ChartCategoryGroup("Returns", returnColor, returnCharts, () => OnPropertyChanged(nameof(HasSelectedCharts))));
        ChartCategories.Add(new ChartCategoryGroup("Losses", lossColor, lossCharts, () => OnPropertyChanged(nameof(HasSelectedCharts))));
    }

    private void LoadTemplate(string templateName)
    {
        if (ReportTemplateFactory.IsBuiltInTemplate(templateName))
        {
            Configuration = ReportTemplateFactory.CreateFromTemplate(templateName);
            ApplyConfigurationToPageSettings();
        }
        else
        {
            // Try to load custom template asynchronously
            Task.Run(async () =>
            {
                var config = await _templateStorage.LoadTemplateAsync(templateName);
                // Update configuration and page settings on UI thread
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    Configuration = config ?? new ReportConfiguration();
                    ApplyConfigurationToPageSettings();
                    // Fire event to refresh canvas after template loads
                    TemplateLoaded?.Invoke(this, EventArgs.Empty);
                });
            });
        }
    }

    /// <summary>
    /// Applies the current configuration settings to the page setting properties.
    /// </summary>
    private void ApplyConfigurationToPageSettings()
    {
        // Update report name from configuration
        if (!string.IsNullOrEmpty(Configuration.Title))
        {
            ReportName = Configuration.Title;
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

        // Update date preset and radio button selection
        if (!string.IsNullOrEmpty(Configuration.Filters.DatePresetName))
        {
            SelectedDatePreset = Configuration.Filters.DatePresetName;

            // Update IsSelected on all date preset options
            foreach (var option in DatePresets)
            {
                option.IsSelected = option.Name == Configuration.Filters.DatePresetName;
            }
        }

        // Update transaction type
        SelectedTransactionType = Configuration.Filters.TransactionType;

        // Check if we need to restore preserved chart selections (when going back from step 2)
        if (_chartTypesToPreserve != null)
        {
            RestoreChartSelections(_chartTypesToPreserve);
            _chartTypesToPreserve = null;
        }
        else
        {
            // Update selected charts - reset all and mark selected ones from configuration
            foreach (var chart in AvailableCharts)
            {
                chart.IsSelected = Configuration.Filters.SelectedChartTypes.Contains(chart.ChartType);
            }
        }

        UpdateCanvasDimensions();
        UndoRedoManager.Clear();
        OnPropertyChanged(nameof(Configuration));

        // Notify view to fit canvas to window
        TemplateLoaded?.Invoke(this, EventArgs.Empty);
    }

    private void ApplyFiltersToConfiguration()
    {
        Configuration.Title = ReportName;
        Configuration.Filters.TransactionType = SelectedTransactionType;
        Configuration.Filters.DatePresetName = SelectedDatePreset;

        // Set date format from DateFormatService for consistent X-axis labeling
        Configuration.Filters.DateFormat = DateFormatService.GetCurrentDotNetFormat();

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
        foreach (var chart in AvailableCharts.Where(c => c.IsSelected))
        {
            Configuration.Filters.SelectedChartTypes.Add(chart.ChartType);
        }

        // Sync chart elements with selection (add new, remove deselected)
        SyncChartElementsWithSelection();

        // Notify view to sync canvas with new elements
        OnPropertyChanged(nameof(Configuration));
    }

    /// <summary>
    /// Syncs ChartReportElement objects with the selected chart types.
    /// Creates elements for newly selected charts, removes elements for deselected charts,
    /// and rearranges all chart elements in a grid layout.
    /// </summary>
    private void SyncChartElementsWithSelection()
    {
        var selectedChartTypes = Configuration.Filters.SelectedChartTypes.ToHashSet();

        // Get existing chart elements
        var existingChartElements = Configuration.Elements
            .OfType<ChartReportElement>()
            .ToList();

        // Remove chart elements for deselected charts
        foreach (var chartElement in existingChartElements)
        {
            if (!selectedChartTypes.Contains(chartElement.ChartType))
            {
                Configuration.RemoveElement(chartElement.Id);
            }
        }

        // Get chart types that already have elements (after removal)
        var existingChartTypes = Configuration.Elements
            .OfType<ChartReportElement>()
            .Select(e => e.ChartType)
            .ToHashSet();

        // Add new chart elements for selected charts that don't have elements yet
        foreach (var chartType in Configuration.Filters.SelectedChartTypes)
        {
            if (!existingChartTypes.Contains(chartType))
            {
                Configuration.AddElement(new ChartReportElement
                {
                    ChartType = chartType,
                    X = 0,
                    Y = 0,
                    Width = 200,
                    Height = 150
                });
            }
        }

        // Rearrange all chart elements in a grid layout
        RearrangeChartElements();
    }

    /// <summary>
    /// Rearranges all chart elements in a grid layout.
    /// Only rearranges if HasManualChartLayout is false (no manual positioning has been done).
    /// </summary>
    private void RearrangeChartElements()
    {
        // Skip rearranging if user has manually positioned elements
        if (Configuration.HasManualChartLayout)
            return;

        var chartElements = Configuration.Elements
            .OfType<ChartReportElement>()
            .ToList();

        if (chartElements.Count == 0)
            return;

        // Calculate layout for charts using a grid layout
        var (pageWidth, pageHeight) = PageDimensions.GetDimensions(Configuration.PageSize, Configuration.PageOrientation);
        const double margin = PageDimensions.Margin;
        const double headerHeight = PageDimensions.HeaderHeight;
        const double footerHeight = PageDimensions.FooterHeight;
        const double spacing = 10;

        var contentWidth = pageWidth - (margin * 2);
        var contentHeight = pageHeight - headerHeight - footerHeight - (margin * 2);
        var startY = headerHeight + margin;

        // Determine grid dimensions based on number of charts
        var chartCount = chartElements.Count;
        int columns = chartCount <= 2 ? chartCount : (chartCount <= 4 ? 2 : 3);
        int rows = (int)Math.Ceiling((double)chartCount / columns);

        var cellWidth = (contentWidth - (spacing * (columns - 1))) / columns;
        var cellHeight = (contentHeight - (spacing * (rows - 1))) / rows;

        // Position each chart element in the grid
        for (int i = 0; i < chartElements.Count; i++)
        {
            int row = i / columns;
            int col = i % columns;

            chartElements[i].X = margin + (col * (cellWidth + spacing));
            chartElements[i].Y = startY + (row * (cellHeight + spacing));
            chartElements[i].Width = cellWidth;
            chartElements[i].Height = cellHeight;
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
/// Represents a chart option for selection with category and icon information.
/// </summary>
public partial class ChartOption(
    ChartDataType chartType,
    string name,
    string description,
    string category,
    string iconData,
    string iconForeground,
    string iconBackground)
    : ObservableObject
{
    public ChartDataType ChartType { get; } = chartType;
    public string Name { get; } = name;
    public string Description { get; } = description;
    public string Category { get; } = category;
    public string IconData { get; } = iconData;
    public string IconForeground { get; } = iconForeground;
    public string IconBackground { get; } = iconBackground;

    [ObservableProperty]
    private bool _isSelected;
}

/// <summary>
/// Represents a category group of chart options with "Select All" functionality.
/// </summary>
public partial class ChartCategoryGroup : ObservableObject
{
    public ChartCategoryGroup(string name, string accentColor, ObservableCollection<ChartOption> charts, Action onSelectionChanged)
    {
        Name = name;
        AccentColor = accentColor;
        Charts = charts;
        var onSelectionChanged1 = onSelectionChanged;

        // Subscribe to each chart's selection changes
        foreach (var chart in Charts)
        {
            chart.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ChartOption.IsSelected))
                {
                    OnPropertyChanged(nameof(IsAllSelected));
                    OnPropertyChanged(nameof(IsSomeSelected));
                    onSelectionChanged1.Invoke();
                }
            };
        }
    }

    public string Name { get; }
    public string AccentColor { get; }
    public ObservableCollection<ChartOption> Charts { get; }

    /// <summary>
    /// Gets whether all charts in this category are selected.
    /// </summary>
    public bool IsAllSelected => Charts.All(c => c.IsSelected);

    /// <summary>
    /// Gets whether some (but not all) charts in this category are selected.
    /// </summary>
    public bool IsSomeSelected => Charts.Any(c => c.IsSelected) && !IsAllSelected;

    /// <summary>
    /// Toggles selection of all charts in this category.
    /// </summary>
    public void ToggleSelectAll()
    {
        var newState = !IsAllSelected;
        foreach (var chart in Charts)
        {
            chart.IsSelected = newState;
        }
        OnPropertyChanged(nameof(IsAllSelected));
        OnPropertyChanged(nameof(IsSomeSelected));
    }
}

/// <summary>
/// Represents a date preset option for selection.
/// </summary>
public partial class DatePresetOption(string name) : ObservableObject
{
    public string Name { get; } = name;

    [ObservableProperty]
    private bool _isSelected;
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

/// <summary>
/// Represents a report template option for the Step 1 template grid.
/// </summary>
public partial class ReportTemplateOption(
    string templateName,
    string displayName,
    string description,
    string iconData,
    string iconForeground,
    string iconBackground)
    : ObservableObject
{
    public string TemplateName { get; } = templateName;
    public string DisplayName { get; } = displayName;
    public string Description { get; } = description;
    public string IconData { get; } = iconData;
    public string IconForeground { get; } = iconForeground;
    public string IconBackground { get; } = iconBackground;

    [ObservableProperty]
    private bool _isSelected;
}

/// <summary>
/// Represents a custom template option for the Step 1 template grid.
/// </summary>
public partial class CustomTemplateOption(string name) : ObservableObject
{
    public string Name { get; } = name;

    [ObservableProperty]
    private bool _isSelected;
}

/// <summary>
/// Represents a chart style option with a display name.
/// </summary>
public record ChartStyleOption(ReportChartStyle Value, string DisplayName)
{
    public override string ToString() => DisplayName;
}

/// <summary>
/// Represents a chart data type option with a display name.
/// </summary>
public record ChartDataTypeOption(ChartDataType Value, string DisplayName)
{
    public override string ToString() => DisplayName;
}
