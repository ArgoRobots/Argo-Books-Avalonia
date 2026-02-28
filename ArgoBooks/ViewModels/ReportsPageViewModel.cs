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
        new(ReportTemplateFactory.TemplateNames.MonthlyRevenue, "Monthly Sales", "Summarize monthly revenue data",
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
        new(ReportTemplateFactory.TemplateNames.LossesAnalysis, "Losses Analysis", "Analyze losses and damages",
            "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 15h-2v-2h2v2zm0-4h-2V7h2v6z",
            "#E91E63", "#FCE4EC"),
        new(ReportTemplateFactory.TemplateNames.CustomerAnalysis, "Customer Analysis", "Analyze customer trends",
            "M16 11c1.66 0 2.99-1.34 2.99-3S17.66 5 16 5c-1.66 0-3 1.34-3 3s1.34 3 3 3zm-8 0c1.66 0 2.99-1.34 2.99-3S9.66 5 8 5C6.34 5 5 6.34 5 8s1.34 3 3 3zm0 2c-2.33 0-7 1.17-7 3.5V19h14v-2.5c0-2.33-4.67-3.5-7-3.5zm8 0c-.29 0-.62.02-.97.05 1.16.84 1.97 1.97 1.97 3.45V19h6v-2.5c0-2.33-4.67-3.5-7-3.5z",
            "#00BCD4", "#E0F7FA"),
        new(ReportTemplateFactory.TemplateNames.ExpenseBreakdown, "Expense Breakdown", "Analyze spending patterns",
            "M11.8 10.9c-2.27-.59-3-1.2-3-2.15 0-1.09 1.01-1.85 2.7-1.85 1.78 0 2.44.85 2.5 2.1h2.21c-.07-1.72-1.12-3.3-3.21-3.81V3h-3v2.16c-1.94.42-3.5 1.68-3.5 3.61 0 2.31 1.91 3.46 4.7 4.13 2.5.6 3 1.48 3 2.41 0 .69-.49 1.79-2.7 1.79-2.06 0-2.87-.92-2.98-2.1h-2.2c.12 2.19 1.76 3.42 3.68 3.83V21h3v-2.15c1.95-.37 3.5-1.5 3.5-3.55 0-2.84-2.43-3.81-4.7-4.4z",
            "#FF5722", "#FBE9E7"),
        new(ReportTemplateFactory.TemplateNames.Custom, "Blank Template", "Start from scratch",
            "M19 13h-6v6h-2v-6H5v-2h6V5h2v6h6v2z",
            "#9E9E9E", "#F5F5F5")
    ];

    /// <summary>
    /// Accounting template options for the Step 1 template grid (Accounting section).
    /// </summary>
    public ObservableCollection<ReportTemplateOption> AccountingTemplateOptions { get; } =
    [
        new(ReportTemplateFactory.TemplateNames.IncomeStatement, "Income Statement", "Shows if your business made or lost money",
            "M14 2H6c-1.1 0-2 .9-2 2v16c0 1.1.9 2 2 2h12c1.1 0 2-.9 2-2V8l-6-6zm-1 7V3.5L18.5 9H13zM7 17h5v-1H7v1zm0-2h10v-1H7v1zm0-2h10v-1H7v1z",
            "#27AE60", "#E8F8F5"),
        new(ReportTemplateFactory.TemplateNames.BalanceSheet, "Balance Sheet", "Snapshot of what you own and owe",
            "M4 6H2v14c0 1.1.9 2 2 2h14v-2H4V6zm16-4H8c-1.1 0-2 .9-2 2v12c0 1.1.9 2 2 2h12c1.1 0 2-.9 2-2V4c0-1.1-.9-2-2-2zm0 14H8V4h12v12zM10 9h8v1.5h-8V9zm0 3h4v1.5h-4V12zm0-6h8v1.5h-8V6z",
            "#2980B9", "#EBF5FB"),
        new(ReportTemplateFactory.TemplateNames.CashFlowStatement, "Cash Flow", "Where your cash came from and went",
            "M11.8 10.9c-2.27-.59-3-1.2-3-2.15 0-1.09 1.01-1.85 2.7-1.85 1.78 0 2.44.85 2.5 2.1h2.21c-.07-1.72-1.12-3.3-3.21-3.81V3h-3v2.16c-1.94.42-3.5 1.68-3.5 3.61 0 2.31 1.91 3.46 4.7 4.13 2.5.6 3 1.48 3 2.41 0 .69-.49 1.79-2.7 1.79-2.06 0-2.87-.92-2.98-2.1h-2.2c.12 2.19 1.76 3.42 3.68 3.83V21h3v-2.15c1.95-.37 3.5-1.5 3.5-3.55 0-2.84-2.43-3.81-4.7-4.4z",
            "#8E44AD", "#F5EEF8"),
        new(ReportTemplateFactory.TemplateNames.TrialBalance, "Trial Balance", "Checks that all your books add up",
            "M19 3H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm0 16H5V5h14v14zM12 6v2h5v2h-5v2l-4-3 4-3zm-1 6v2H6v-2h5v-2l4 3-4 3v-2H6v-2h5z",
            "#E67E22", "#FDF2E9"),
        new(ReportTemplateFactory.TemplateNames.GeneralLedger, "General Ledger", "Full list of every transaction",
            "M4 6H2v14c0 1.1.9 2 2 2h14v-2H4V6zm16-4H8c-1.1 0-2 .9-2 2v12c0 1.1.9 2 2 2h12c1.1 0 2-.9 2-2V4c0-1.1-.9-2-2-2zm-1 9h-4v4h-2v-4H9V9h4V5h2v4h4v2z",
            "#2C3E50", "#EBEDEF"),
        new(ReportTemplateFactory.TemplateNames.ARaging, "AR Aging", "Who owes you and for how long",
            "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1.41 16.09V20h-2.67v-1.93c-1.71-.36-3.16-1.46-3.27-3.4h1.96c.1 1.05.82 1.87 2.65 1.87 1.96 0 2.4-.98 2.4-1.59 0-.83-.44-1.61-2.67-2.14-2.48-.6-4.18-1.62-4.18-3.67 0-1.72 1.39-2.84 3.11-3.21V4h2.67v1.95c1.86.45 2.79 1.86 2.85 3.39H14.3c-.05-1.11-.64-1.87-2.22-1.87-1.5 0-2.4.68-2.4 1.64 0 .84.65 1.39 2.67 1.94s4.18 1.36 4.18 3.85c0 1.89-1.44 2.96-3.12 3.19z",
            "#16A085", "#E8F6F3"),
        new(ReportTemplateFactory.TemplateNames.APaging, "AP Aging", "What you owe and when it's due",
            "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm0 18c-4.41 0-8-3.59-8-8s3.59-8 8-8 8 3.59 8 8-3.59 8-8 8zm.31-8.86c-1.77-.45-2.34-.94-2.34-1.67 0-.84.79-1.43 2.1-1.43 1.38 0 1.9.66 1.94 1.64h1.71c-.05-1.34-.87-2.57-2.49-2.97V5H11.5v1.69c-1.51.32-2.72 1.3-2.72 2.81 0 1.79 1.49 2.69 3.66 3.21 1.95.46 2.34 1.15 2.34 1.87 0 .53-.39 1.39-2.1 1.39-1.6 0-2.23-.72-2.32-1.64H8.65c.09 1.71 1.37 2.66 2.85 2.97V19h1.72v-1.67c1.52-.29 2.72-1.16 2.72-2.74 0-2.22-1.9-2.97-3.63-3.45z",
            "#C0392B", "#FDEDEC"),
        new(ReportTemplateFactory.TemplateNames.TaxSummary, "Tax Summary", "Tax you collected vs. tax you paid",
            "M19 3H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zM9 17H7v-7h2v7zm4 0h-2V7h2v10zm4 0h-2v-4h2v4z",
            "#D35400", "#FBEEE6")
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
                // Preserve chart selections and date preset before reloading template
                // (will be restored in ApplyConfigurationToPageSettings after template loads)
                _chartTypesToPreserve = Configuration.Filters.SelectedChartTypes.ToList();
                _datePresetToPreserve = SelectedDatePreset;

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

    partial void OnReportNameChanged(string value)
    {
        // Keep export file path in sync with report name so switching templates
        // doesn't overwrite a previously exported file.
        if (!string.IsNullOrEmpty(ExportFilePath))
        {
            var dir = Path.GetDirectoryName(ExportFilePath) ?? "";
            var ext = Path.GetExtension(ExportFilePath);
            var newName = string.IsNullOrWhiteSpace(value) ? "Report" : value;
            ExportFilePath = Path.Combine(dir, $"{newName}{ext}");
        }
    }

    /// <summary>
    /// Chart types to preserve during template reload (when going back from step 2).
    /// </summary>
    private List<ChartDataType>? _chartTypesToPreserve;

    /// <summary>
    /// Date preset to preserve during template reload (when going back from step 2).
    /// </summary>
    private string? _datePresetToPreserve;

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

            // Update IsSelected on all accounting template options
            foreach (var template in AccountingTemplateOptions)
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

        // Update IsSelected on all built-in template options
        foreach (var template in ReportTemplateOptions)
        {
            template.IsSelected = template.TemplateName == templateName;
        }

        // Update IsSelected on all accounting template options
        foreach (var template in AccountingTemplateOptions)
        {
            template.IsSelected = template.TemplateName == templateName;
        }

        // Update IsSelected on all custom template options
        foreach (var customTemplate in CustomTemplateNames)
        {
            customTemplate.IsSelected = customTemplate.Name == templateName;
        }

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
        OnPropertyChanged(nameof(SelectedImageFileName));
        OnPropertyChanged(nameof(HasSelectedImage));
        OnPropertyChanged(nameof(SelectedTableElement));
        OnPropertyChanged(nameof(SelectedDateRangeElement));
        OnPropertyChanged(nameof(SelectedSummaryElement));
        OnPropertyChanged(nameof(IsChartSelected));
        OnPropertyChanged(nameof(IsLabelSelected));
        OnPropertyChanged(nameof(IsImageSelected));
        OnPropertyChanged(nameof(IsTableSelected));
        OnPropertyChanged(nameof(IsDateRangeSelected));
        OnPropertyChanged(nameof(IsSummarySelected));
        OnPropertyChanged(nameof(SelectedAccountingTableElement));
        OnPropertyChanged(nameof(IsAccountingTableSelected));
        OnPropertyChanged(nameof(IsDistributionChartSelected));
    }

    private void OnElementPropertyChanging(object? sender, ElementPropertyChangingEventArgs e)
    {
        if (sender is ReportElementBase element)
        {
            // Skip ZOrder and Bounds - these are tracked through other mechanisms
            if (e.PropertyName is "ZOrder" or "Bounds")
                return;

            // For position/size changes, create a coalescing move/resize action.
            // Rapid changes (e.g., scrolling spinner controls) will be merged into
            // a single undo entry by the undo manager's coalescing logic.
            // During canvas drag/resize, SuppressRecording is set so these are skipped.
            if (e.PropertyName is "X" or "Y" or "Width" or "Height")
            {
                // PropertyChanging fires before the change, so element still has old values
                var oldBounds = (element.X, element.Y, element.Width, element.Height);
                var newBounds = oldBounds;
                switch (e.PropertyName)
                {
                    case "X": newBounds.Item1 = (double)e.NewValue!; break;
                    case "Y": newBounds.Item2 = (double)e.NewValue!; break;
                    case "Width": newBounds.Item3 = (double)e.NewValue!; break;
                    case "Height": newBounds.Item4 = (double)e.NewValue!; break;
                }

                var isResize = e.PropertyName is "Width" or "Height";
                UndoRedoManager.RecordAction(new MoveResizeElementAction(
                    Configuration, element.Id, oldBounds, newBounds, isResize));
                return;
            }

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
    public string SelectedImageFileName => string.IsNullOrEmpty(SelectedImageElement?.ImagePath)
        ? string.Empty
        : Path.GetFileName(SelectedImageElement.ImagePath);
    public bool HasSelectedImage => !string.IsNullOrEmpty(SelectedImageElement?.ImagePath);
    public TableReportElement? SelectedTableElement => SelectedElement as TableReportElement;
    public DateRangeReportElement? SelectedDateRangeElement => SelectedElement as DateRangeReportElement;
    public SummaryReportElement? SelectedSummaryElement => SelectedElement as SummaryReportElement;
    public AccountingTableReportElement? SelectedAccountingTableElement => SelectedElement as AccountingTableReportElement;

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
    public bool IsAccountingTableSelected => SelectedElement is AccountingTableReportElement;

    /// <summary>
    /// Whether the selected chart is a distribution/pie chart that benefits from legend display.
    /// </summary>
    public bool IsDistributionChartSelected => SelectedChartElement?.ChartType is
        ChartDataType.RevenueDistribution or
        ChartDataType.ExpensesDistribution or
        ChartDataType.ReturnReasons or
        ChartDataType.LossReasons or
        ChartDataType.ReturnsByCategory or
        ChartDataType.LossesByCategory or
        ChartDataType.ReturnsByProduct or
        ChartDataType.LossesByProduct or
        ChartDataType.CountriesOfOrigin or
        ChartDataType.CountriesOfDestination or
        ChartDataType.CompaniesOfOrigin or
        ChartDataType.CompaniesOfDestination or
        ChartDataType.AccountantsTransactions or
        ChartDataType.TopCustomersByRevenue or
        ChartDataType.CustomerPaymentStatus or
        ChartDataType.ActiveVsInactiveCustomers;

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

    #region Page Management

    [ObservableProperty]
    private int _currentDesignerPage = 1;

    /// <summary>
    /// Callback to get the viewport center position from the canvas (page number, local X, local Y).
    /// Set by the View code-behind when the canvas is available.
    /// </summary>
    public Func<(int PageNumber, double LocalX, double LocalY)>? GetViewportCenter { get; set; }

    public string CurrentDesignerPageDisplay =>
        $"Page {CurrentDesignerPage} of {Configuration.PageCount}";

    public bool CanDeletePage => Configuration.PageCount > 1;

    public bool CanGoToPreviousPage => CurrentDesignerPage > 1;

    public bool CanGoToNextPage => CurrentDesignerPage < Configuration.PageCount;

    partial void OnCurrentDesignerPageChanged(int value)
    {
        OnPropertyChanged(nameof(CurrentDesignerPageDisplay));
        OnPropertyChanged(nameof(CanGoToPreviousPage));
        OnPropertyChanged(nameof(CanGoToNextPage));
    }

    [RelayCommand]
    private void NextDesignerPage()
    {
        if (CurrentDesignerPage < Configuration.PageCount)
        {
            CurrentDesignerPage++;
        }
    }

    [RelayCommand]
    private void PreviousDesignerPage()
    {
        if (CurrentDesignerPage > 1)
        {
            CurrentDesignerPage--;
        }
    }

    [RelayCommand]
    private void AddPage()
    {
        UndoRedoManager.RecordAction(new AddPageAction(Configuration));
        Configuration.PageCount++;
        CurrentDesignerPage = Configuration.PageCount;
        OnPropertyChanged(nameof(CurrentDesignerPageDisplay));
        OnPropertyChanged(nameof(CanDeletePage));
        OnPropertyChanged(nameof(CanGoToNextPage));
        OnPropertyChanged(nameof(CanGoToPreviousPage));
        CanvasRefreshRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void DeletePage()
    {
        if (Configuration.PageCount <= 1) return;

        var pageToDelete = CurrentDesignerPage;
        var elementsOnPage = Configuration.Elements
            .Where(e => e.PageNumber == pageToDelete).ToList();

        // Record undo action before making changes
        UndoRedoManager.RecordAction(new DeletePageAction(Configuration, pageToDelete, elementsOnPage));

        // Remove elements on this page
        foreach (var element in elementsOnPage)
        {
            Configuration.Elements.Remove(element);
        }

        // Renumber elements on higher pages
        foreach (var element in Configuration.Elements.Where(e => e.PageNumber > pageToDelete))
        {
            element.PageNumber--;
        }

        Configuration.PageCount--;

        // Navigate to a valid page
        if (CurrentDesignerPage > Configuration.PageCount)
        {
            CurrentDesignerPage = Configuration.PageCount;
        }
        else
        {
            // Force refresh even if page number didn't change
            OnPropertyChanged(nameof(CurrentDesignerPageDisplay));
        }

        SelectedElement = null;
        OnPropertyChanged(nameof(CanDeletePage));
        OnPropertyChanged(nameof(CanGoToNextPage));
        OnPropertyChanged(nameof(CanGoToPreviousPage));
        CanvasRefreshRequested?.Invoke(this, EventArgs.Empty);
    }

    #endregion

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
    private void AddElement(ReportElementType elementType)
    {
        // Get element dimensions first so we can center it
        var (width, height) = elementType switch
        {
            ReportElementType.Chart => (300.0, 200.0),
            ReportElementType.Table => (400.0, 200.0),
            ReportElementType.Label => (200.0, 40.0),
            ReportElementType.Image => (150.0, 150.0),
            ReportElementType.DateRange => (200.0, 30.0),
            ReportElementType.Summary => (200.0, 120.0),
            ReportElementType.AccountingTable => (500.0, 600.0),
            _ => (200.0, 40.0)
        };

        // Place at the center of the user's current viewport
        var viewportCenter = GetViewportCenter?.Invoke();
        int pageNumber;
        double centerX, centerY;

        if (viewportCenter.HasValue)
        {
            pageNumber = viewportCenter.Value.PageNumber;
            centerX = viewportCenter.Value.LocalX;
            centerY = viewportCenter.Value.LocalY;
        }
        else
        {
            // Fallback: top-left of content area on current page
            pageNumber = CurrentDesignerPage;
            var margins = Configuration.PageMargins;
            var headerHeight = Configuration.ShowHeader
                ? PageDimensions.GetHeaderHeight(Configuration.ShowCompanyDetails)
                : 0;
            centerX = margins.Left + width / 2;
            centerY = headerHeight + margins.Top + height / 2;
        }

        // Center the element on the viewport center point
        var elementX = centerX - width / 2;
        var elementY = centerY - height / 2;

        // Clamp to content bounds
        var (pageWidth, pageHeight) = PageDimensions.GetDimensions(Configuration.PageSize, Configuration.PageOrientation);
        var ml = Configuration.PageMargins.Left;
        var mr = Configuration.PageMargins.Right;
        var mt = Configuration.PageMargins.Top;
        var mb = Configuration.PageMargins.Bottom;
        var hh = Configuration.ShowHeader ? PageDimensions.GetHeaderHeight(Configuration.ShowCompanyDetails) : 0;
        var fh = Configuration.ShowFooter ? PageDimensions.FooterHeight : 0;
        var contentLeft = ml;
        var contentTop = hh + mt;
        var contentRight = pageWidth - mr;
        var contentBottom = pageHeight - fh - mb;

        elementX = Math.Max(contentLeft, Math.Min(elementX, contentRight - width));
        elementY = Math.Max(contentTop, Math.Min(elementY, contentBottom - height));

        ReportElementBase element = elementType switch
        {
            ReportElementType.Chart => new ChartReportElement { X = elementX, Y = elementY, Width = width, Height = height },
            ReportElementType.Table => new TableReportElement { X = elementX, Y = elementY, Width = width, Height = height },
            ReportElementType.Label => new LabelReportElement { X = elementX, Y = elementY, Width = width, Height = height },
            ReportElementType.Image => new ImageReportElement { X = elementX, Y = elementY, Width = width, Height = height },
            ReportElementType.DateRange => new DateRangeReportElement { X = elementX, Y = elementY, Width = width, Height = height },
            ReportElementType.Summary => new SummaryReportElement { X = elementX, Y = elementY, Width = width, Height = height },
            ReportElementType.AccountingTable => new AccountingTableReportElement { X = elementX, Y = elementY, Width = width, Height = height },
            _ => new LabelReportElement()
        };

        element.PageNumber = pageNumber;
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

        UndoRedoManager.RecordAction(new ZOrderChangeAction(Configuration, oldZOrders, newZOrders, "Bring to front".Translate()));
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
        UndoRedoManager.RecordAction(new ZOrderChangeAction(Configuration, oldZOrders, newZOrders, "Send to back".Translate()));
        OnPropertyChanged(nameof(Configuration));
    }

    [RelayCommand]
    private void AlignElements(ElementAlignment alignment)
    {
        if (SelectedElements.Count == 0) return;

        var oldBounds = SelectedElements.ToDictionary(e => e.Id, e => e.BoundsWithPage);

        if (SelectedElements.Count == 1)
        {
            // Single element: align to canvas
            var element = SelectedElements[0];
            switch (alignment)
            {
                case ElementAlignment.Left:
                    element.X = 0;
                    break;
                case ElementAlignment.Right:
                    element.X = CanvasWidth - element.Width;
                    break;
                case ElementAlignment.Top:
                    element.Y = 0;
                    break;
                case ElementAlignment.Bottom:
                    element.Y = CanvasHeight - element.Height;
                    break;
                case ElementAlignment.CenterH:
                    element.X = (CanvasWidth - element.Width) / 2;
                    break;
                case ElementAlignment.CenterV:
                    element.Y = (CanvasHeight - element.Height) / 2;
                    break;
            }
        }
        else
        {
            // Multiple elements: align to first selected element
            var reference = SelectedElements[0];
            foreach (var element in SelectedElements.Skip(1))
            {
                switch (alignment)
                {
                    case ElementAlignment.Left:
                        element.X = reference.X;
                        break;
                    case ElementAlignment.Right:
                        element.X = reference.X + reference.Width - element.Width;
                        break;
                    case ElementAlignment.Top:
                        element.Y = reference.Y;
                        break;
                    case ElementAlignment.Bottom:
                        element.Y = reference.Y + reference.Height - element.Height;
                        break;
                    case ElementAlignment.CenterH:
                        element.X = reference.X + (reference.Width - element.Width) / 2;
                        break;
                    case ElementAlignment.CenterV:
                        element.Y = reference.Y + (reference.Height - element.Height) / 2;
                        break;
                }
            }
        }

        var newBounds = SelectedElements.ToDictionary(e => e.Id, e => e.BoundsWithPage);
        UndoRedoManager.RecordAction(new BatchMoveResizeAction(Configuration, oldBounds, newBounds, "Align {0}".TranslateFormat(alignment)));
        OnPropertyChanged(nameof(Configuration));
    }

    [RelayCommand]
    private void DistributeElements(DistributeDirection direction)
    {
        if (SelectedElements.Count < 3) return;

        var oldBounds = SelectedElements.ToDictionary(e => e.Id, e => e.BoundsWithPage);

        var sorted = direction == DistributeDirection.Horizontal
            ? SelectedElements.OrderBy(e => e.X).ToList()
            : SelectedElements.OrderBy(e => e.Y).ToList();

        if (direction == DistributeDirection.Horizontal)
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

        var newBounds = SelectedElements.ToDictionary(e => e.Id, e => e.BoundsWithPage);
        UndoRedoManager.RecordAction(new BatchMoveResizeAction(Configuration, oldBounds, newBounds, "Distribute {0}".TranslateFormat(direction.ToString())));
        OnPropertyChanged(nameof(Configuration));
    }

    [RelayCommand]
    private void MatchSize(MatchSizeMode mode)
    {
        if (SelectedElements.Count < 2) return;

        var oldBounds = SelectedElements.ToDictionary(e => e.Id, e => e.BoundsWithPage);
        var reference = SelectedElements[0];

        foreach (var element in SelectedElements.Skip(1))
        {
            switch (mode)
            {
                case MatchSizeMode.Width:
                    element.Width = reference.Width;
                    break;
                case MatchSizeMode.Height:
                    element.Height = reference.Height;
                    break;
                case MatchSizeMode.Both:
                    element.Width = reference.Width;
                    element.Height = reference.Height;
                    break;
            }
        }

        var newBounds = SelectedElements.ToDictionary(e => e.Id, e => e.BoundsWithPage);
        UndoRedoManager.RecordAction(new BatchMoveResizeAction(Configuration, oldBounds, newBounds, "Match {0}".TranslateFormat(mode.ToString())));
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
                new FilePickerFileType("Image files") { Patterns = ["*.png", "*.jpg", "*.jpeg", "*.bmp", "*.gif"
                    ]
                },
                new FilePickerFileType("All files") { Patterns = ["*.*"] }
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
                OnPropertyChanged(nameof(SelectedImageFileName));
                OnPropertyChanged(nameof(HasSelectedImage));
                OnPropertyChanged(nameof(Configuration));
            }
        }
    }

    [RelayCommand]
    private void RemoveImage()
    {
        if (SelectedImageElement == null) return;
        SelectedImageElement.ImagePath = string.Empty;
        OnPropertyChanged(nameof(SelectedImageElement));
        OnPropertyChanged(nameof(SelectedImageFileName));
        OnPropertyChanged(nameof(HasSelectedImage));
        OnPropertyChanged(nameof(Configuration));
    }

    // Store original values for cancel
    private PageSize _originalPageSize;
    private PageOrientation _originalPageOrientation;
    private double _originalMarginTop, _originalMarginRight, _originalMarginBottom, _originalMarginLeft;
    private bool _originalShowHeader, _originalShowFooter, _originalShowPageNumbers, _originalShowCompanyDetails;
    private string _originalBackgroundColor = "#FFFFFF";
    private double _originalTitleFontSize = 18;
    private string _originalPageSettingsDatePreset = DatePresetNames.ThisMonth;

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
        _originalShowCompanyDetails = ShowCompanyDetails;
        _originalBackgroundColor = BackgroundColor;
        _originalTitleFontSize = TitleFontSize;
        _originalPageSettingsDatePreset = PageSettingsDatePreset;

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
        ShowCompanyDetails = _originalShowCompanyDetails;
        BackgroundColor = _originalBackgroundColor;
        TitleFontSize = _originalTitleFontSize;
        PageSettingsDatePreset = _originalPageSettingsDatePreset;

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
        else
        {
            var dialog = App.ConfirmationDialog;
            if (dialog != null)
            {
                await dialog.ShowAsync(new ConfirmationDialogOptions
                {
                    Title = "Save Failed".Translate(),
                    Message = "Failed to save the template. Please check that you have write permissions to the templates folder.".Translate(),
                    PrimaryButtonText = "OK".Translate(),
                    SecondaryButtonText = null,
                    CancelButtonText = null
                });
            }
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

    public ObservableCollection<Bitmap> PreviewPageImages { get; } = new();

    [ObservableProperty]
    private ExportFormat _selectedExportFormat = ExportFormat.PDF;

    [ObservableProperty]
    private int _exportQuality = 95;

    [ObservableProperty]
    private string _exportFilePath = string.Empty;

    [ObservableProperty]
    private string _exportFileName = "Report";

    partial void OnExportFilePathChanged(string value)
    {
        // Sync file name from file path (without triggering a loop)
        var nameFromPath = Path.GetFileNameWithoutExtension(value);
        if (!string.IsNullOrEmpty(nameFromPath) && nameFromPath != _exportFileName)
        {
            _exportFileName = nameFromPath;
            OnPropertyChanged(nameof(ExportFileName));
        }
    }

    partial void OnExportFileNameChanged(string value)
    {
        // Sync file path from file name (without triggering a loop)
        if (!string.IsNullOrEmpty(ExportFilePath))
        {
            var dir = Path.GetDirectoryName(ExportFilePath) ?? "";
            var ext = Path.GetExtension(ExportFilePath);
            var newName = string.IsNullOrWhiteSpace(value) ? "Report" : value;
            var newPath = Path.Combine(dir, $"{newName}{ext}");
            if (newPath != _exportFilePath)
            {
                _exportFilePath = newPath;
                OnPropertyChanged(nameof(ExportFilePath));
            }
        }
    }

    [ObservableProperty]
    private bool _openAfterExport = true;

    [ObservableProperty]
    private bool _includeMetadata = true;

    [ObservableProperty]
    private double _previewZoom = 1.0;

    [ObservableProperty]
    private double _previewDisplayWidth = 612;

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

            // Store display width (original page width for zoom calculations)
            PreviewDisplayWidth = width;

            // Render at 2x resolution for sharper zoom, but display at original size
            const int resolutionMultiplier = 2;
            Configuration.Use24HourFormat = TimeZoneService.Is24HourFormat;
            Configuration.CompanyLogoPath = App.CompanyManager?.CurrentCompanyLogoPath;
            using var renderer = new ReportRenderer(Configuration, companyData, 1f, LanguageServiceTranslationProvider.Instance, App.ErrorLogger);

            // Dispose previous page bitmaps
            foreach (var bmp in PreviewPageImages)
                bmp.Dispose();
            PreviewPageImages.Clear();

            // Compute continuation plan for overflow handling
            renderer.ComputeContinuationPlan();
            var continuationPlan = renderer.GetContinuationPlan();
            var effectivePages = continuationPlan?.Pages ?? [];
            var pageCount = Math.Max(1, effectivePages.Count > 0 ? effectivePages.Count : Configuration.PageCount);

            if (effectivePages.Count > 0)
            {
                foreach (var effectivePage in effectivePages)
                {
                    using var skBitmap = renderer.CreateEffectivePagePreview(effectivePage, width * resolutionMultiplier, height * resolutionMultiplier);
                    var bitmap = ConvertToBitmap(skBitmap);
                    if (bitmap != null)
                        PreviewPageImages.Add(bitmap);
                }
            }
            else
            {
                for (int page = 1; page <= pageCount; page++)
                {
                    using var skBitmap = renderer.CreatePagePreview(page, width * resolutionMultiplier, height * resolutionMultiplier);
                    var bitmap = ConvertToBitmap(skBitmap);
                    if (bitmap != null)
                        PreviewPageImages.Add(bitmap);
                }
            }

            // Request fit-to-window after preview is generated
            PreviewFitToWindowRequested?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            App.ErrorLogger?.LogError(ex, ArgoBooks.Core.Models.Telemetry.ErrorCategory.Unknown, "Failed to generate report preview");
            foreach (var bmp in PreviewPageImages)
                bmp.Dispose();
            PreviewPageImages.Clear();
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
            Configuration.Use24HourFormat = TimeZoneService.Is24HourFormat;
            Configuration.CompanyLogoPath = App.CompanyManager?.CurrentCompanyLogoPath;
            using var renderer = new ReportRenderer(Configuration, companyData, PageDimensions.RenderScale, LanguageServiceTranslationProvider.Instance, App.ErrorLogger);

            bool success;
            if (SelectedExportFormat == ExportFormat.PDF)
            {
                // PDF handles multi-page internally
                success = await renderer.ExportToPdfAsync(ExportFilePath);
            }
            else
            {
                // Compute continuation plan for multi-page support
                renderer.ComputeContinuationPlan();
                var plan = renderer.GetContinuationPlan();
                var effectivePageCount = renderer.EffectivePageCount;

                if (effectivePageCount > 1)
                {
                    // Multi-page image export: export each page as a separate file
                    var dir = Path.GetDirectoryName(ExportFilePath) ?? "";
                    var baseName = Path.GetFileNameWithoutExtension(ExportFilePath);
                    var ext = Path.GetExtension(ExportFilePath);
                    success = true;

                    if (plan?.Pages.Count > 0)
                    {
                        foreach (var effectivePage in plan.Pages)
                        {
                            var pageFilePath = Path.Combine(dir, $"{baseName}_p{effectivePage.EffectivePageNumber}{ext}");
                            var pageSuccess = await renderer.ExportEffectivePageToImageAsync(pageFilePath, effectivePage, SelectedExportFormat, ExportQuality);
                            if (!pageSuccess) success = false;
                        }
                    }
                    else
                    {
                        for (int page = 1; page <= Configuration.PageCount; page++)
                        {
                            var pageFilePath = Path.Combine(dir, $"{baseName}_p{page}{ext}");
                            var pageSuccess = await renderer.ExportPageToImageAsync(pageFilePath, page, SelectedExportFormat, ExportQuality);
                            if (!pageSuccess) success = false;
                        }
                    }
                }
                else
                {
                    success = await renderer.ExportToImageAsync(ExportFilePath, SelectedExportFormat, ExportQuality);
                }
            }

            if (success)
            {
                ExportMessage = "Export completed successfully!";

                // Auto-dismiss success message after 5 seconds
                _ = Task.Run(async () =>
                {
                    await Task.Delay(5000);
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        if (ExportMessage == "Export completed successfully!")
                            ExportMessage = string.Empty;
                    });
                });

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
        // Use last export directory, or default to Desktop
        var lastDir = App.SettingsService?.GlobalSettings.ReportExport.LastExportDirectory;
        var defaultPath = !string.IsNullOrEmpty(lastDir) && Directory.Exists(lastDir)
            ? lastDir
            : Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        var topLevel = Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;

        if (topLevel?.StorageProvider != null)
        {
            var startFolder = await topLevel.StorageProvider.TryGetFolderFromPathAsync(defaultPath);
            var result = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select Export Folder",
                SuggestedStartLocation = startFolder,
                AllowMultiple = false
            });

            if (result is { Count: > 0 })
            {
                var selectedDir = result[0].Path.LocalPath;
                var fileName = string.IsNullOrWhiteSpace(ExportFileName) ? "Report" : ExportFileName;
                var extension = SelectedExportFormat switch
                {
                    ExportFormat.PDF => ".pdf",
                    ExportFormat.PNG => ".png",
                    ExportFormat.JPEG => ".jpg",
                    _ => ".pdf"
                };
                ExportFilePath = Path.Combine(selectedDir, $"{fileName}{extension}");
            }
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
    private bool _showCompanyDetails;

    [ObservableProperty]
    private string _backgroundColor = "#FFFFFF";

    [ObservableProperty]
    private double _titleFontSize = 18;

    [ObservableProperty]
    private string _pageSettingsDatePreset = DatePresetNames.ThisMonth;

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
                OnPropertyChanged(nameof(IsDistributionChartSelected));
            }
        }
    }

    public ObservableCollection<ChartStyleOption> ChartStyleOptions { get; } =
    [
        new(ReportChartStyle.Bar, "Column"),
        new(ReportChartStyle.Line, "Line"),
        new(ReportChartStyle.StepLine, "Step Line"),
        new(ReportChartStyle.Area, "Area"),
        new(ReportChartStyle.Scatter, "Scatter")
    ];

    public ObservableCollection<ImageScaleMode> ImageScaleModes { get; } =
        new(Enum.GetValues<ImageScaleMode>());

    public ObservableCollection<TableDataSelection> TableDataSelections { get; } =
        new(Enum.GetValues<TableDataSelection>());

    public ObservableCollection<AccountingReportType> AccountingReportTypes { get; } =
        new(Enum.GetValues<AccountingReportType>());

    public ObservableCollection<TableSortOrder> TableSortOrders { get; } =
        new(Enum.GetValues<TableSortOrder>());

    public ObservableCollection<TransactionType> TransactionTypes { get; } =
        new(Enum.GetValues<TransactionType>());

    public ObservableCollection<HorizontalTextAlignment> HorizontalAlignments { get; } =
        new(Enum.GetValues<HorizontalTextAlignment>());

    public ObservableCollection<VerticalTextAlignment> VerticalAlignments { get; } =
        new(Enum.GetValues<VerticalTextAlignment>());

    public ObservableCollection<string> FontFamilies { get; } =
        ["Segoe UI", "Arial", "Times New Roman", "Calibri", "Courier New", "Georgia", "Verdana", "Trebuchet MS", "DejaVu Sans", "Liberation Sans", "Noto Sans"];

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

    partial void OnShowCompanyDetailsChanged(bool value)
    {
        if (IsPageSettingsOpen)
        {
            Configuration.ShowCompanyDetails = value;
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

    partial void OnTitleFontSizeChanged(double value)
    {
        if (IsPageSettingsOpen)
        {
            Configuration.TitleFontSize = value;
            PageSettingsRefreshRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    partial void OnPageSettingsDatePresetChanged(string value)
    {
        if (IsPageSettingsOpen)
        {
            Configuration.Filters.DatePresetName = value;

            // Sync the step-1 SelectedDatePreset so everything stays consistent
            SelectedDatePreset = value;
            foreach (var option in DatePresets)
            {
                option.IsSelected = option.Name == value;
            }

            PageSettingsRefreshRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    [RelayCommand]
    private void ApplyPageSettings()
    {
        var oldSettings = new PageSettingsSnapshot(
            _originalPageSize, _originalPageOrientation,
            _originalMarginTop, _originalMarginRight, _originalMarginBottom, _originalMarginLeft,
            _originalShowHeader, _originalShowFooter, _originalShowPageNumbers, _originalShowCompanyDetails,
            _originalBackgroundColor, _originalTitleFontSize, _originalPageSettingsDatePreset);

        var newSettings = new PageSettingsSnapshot(
            PageSize, PageOrientation,
            MarginTop, MarginRight, MarginBottom, MarginLeft,
            ShowHeader, ShowFooter, ShowPageNumbers, ShowCompanyDetails,
            BackgroundColor, TitleFontSize, PageSettingsDatePreset);

        Configuration.PageSize = PageSize;
        Configuration.PageOrientation = PageOrientation;
        Configuration.PageMargins = new ReportMargins(MarginLeft, MarginTop, MarginRight, MarginBottom);
        Configuration.ShowHeader = ShowHeader;
        Configuration.ShowFooter = ShowFooter;
        Configuration.ShowPageNumbers = ShowPageNumbers;
        Configuration.ShowCompanyDetails = ShowCompanyDetails;
        Configuration.BackgroundColor = BackgroundColor;
        Configuration.TitleFontSize = TitleFontSize;
        Configuration.Filters.DatePresetName = PageSettingsDatePreset;

        // Sync the step-1 date preset
        SelectedDatePreset = PageSettingsDatePreset;
        foreach (var option in DatePresets)
        {
            option.IsSelected = option.Name == PageSettingsDatePreset;
        }

        if (oldSettings != newSettings)
        {
            UndoRedoManager.RecordAction(new PageSettingsChangeAction(
                Configuration, oldSettings, newSettings, ApplyPageSettingsSnapshot));
        }

        UpdateCanvasDimensions();
        IsPageSettingsOpen = false;

        // Refresh the canvas after modal closes
        PageSettingsRefreshRequested?.Invoke(this, EventArgs.Empty);
        OnPropertyChanged(nameof(Configuration));
    }

    private void ApplyPageSettingsSnapshot(PageSettingsSnapshot s)
    {
        PageSize = s.PageSize;
        PageOrientation = s.PageOrientation;
        MarginTop = s.MarginTop;
        MarginRight = s.MarginRight;
        MarginBottom = s.MarginBottom;
        MarginLeft = s.MarginLeft;
        ShowHeader = s.ShowHeader;
        ShowFooter = s.ShowFooter;
        ShowPageNumbers = s.ShowPageNumbers;
        ShowCompanyDetails = s.ShowCompanyDetails;
        BackgroundColor = s.BackgroundColor;
        TitleFontSize = s.TitleFontSize;
        PageSettingsDatePreset = s.DatePreset;

        SelectedDatePreset = s.DatePreset;
        foreach (var option in DatePresets)
        {
            option.IsSelected = option.Name == s.DatePreset;
        }

        UpdateCanvasDimensions();
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

        // Load element panel and grid state from settings
        var uiSettings = App.SettingsService?.GlobalSettings.Ui;
        if (uiSettings != null)
        {
            _isElementPanelExpanded = !uiSettings.ReportsElementPanelCollapsed;
            _showGrid = uiSettings.ReportsShowGrid;
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

        // Force ItemsControls to re-render with new translations by notifying collection changes
        OnPropertyChanged(nameof(ReportTemplateOptions));
        OnPropertyChanged(nameof(AccountingTemplateOptions));
        OnPropertyChanged(nameof(DatePresets));

        // Refresh the preview and canvas to show translated content
        GeneratePreview();
        CanvasRefreshRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Refreshes the report design canvas (e.g. after company logo changes).
    /// </summary>
    public void RefreshCanvas()
    {
        CanvasRefreshRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Cleans up event subscriptions when the view model is no longer needed.
    /// </summary>
    public void Cleanup()
    {
        LanguageService.Instance.LanguageChanged -= OnLanguageChanged;

        foreach (var bmp in PreviewPageImages)
            bmp.Dispose();
        PreviewPageImages.Clear();
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

    /// <summary>
    /// Saves the grid visibility state when it changes.
    /// </summary>
    partial void OnShowGridChanged(bool value)
    {
        var settings = App.SettingsService?.GlobalSettings;
        if (settings != null)
        {
            settings.Ui.ReportsShowGrid = value;
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
        var defaultName = string.IsNullOrWhiteSpace(ReportName) ? "Report" : ReportName;
        ExportFilePath = Path.Combine(desktopPath, $"{defaultName}{extension}");
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
        const string customerColor = "#3F51B5";      // Indigo
        const string customerLight = "#E8EAF6";

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
        const string groupIcon = "M16 11c1.66 0 2.99-1.34 2.99-3S17.66 5 16 5c-1.66 0-3 1.34-3 3s1.34 3 3 3zm-8 0c1.66 0 2.99-1.34 2.99-3S9.66 5 8 5C6.34 5 5 6.34 5 8s1.34 3 3 3zm0 2c-2.33 0-7 1.17-7 3.5V19h14v-2.5c0-2.33-4.67-3.5-7-3.5zm8 0c-.29 0-.62.02-.97.05 1.16.84 1.97 1.97 1.97 3.45V19h6v-2.5c0-2.33-4.67-3.5-7-3.5z";
        const string paymentIcon = "M20 4H4c-1.11 0-1.99.89-1.99 2L2 18c0 1.11.89 2 2 2h16c1.11 0 2-.89 2-2V6c0-1.11-.89-2-2-2zm0 14H4v-6h16v6zm0-10H4V6h16v2z";
        const string heartIcon = "M12 21.35l-1.45-1.32C5.4 15.36 2 12.28 2 8.5 2 5.42 4.42 3 7.5 3c1.74 0 3.41.81 4.5 2.09C13.09 3.81 14.76 3 16.5 3 19.58 3 22 5.42 22 8.5c0 3.78-3.4 6.86-8.55 11.54L12 21.35z";
        const string rentalIcon = "M17 3H7c-1.1 0-2 .9-2 2v16l7-3 7 3V5c0-1.1-.9-2-2-2z";

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
            new(ChartDataType.RevenueVsExpenses, "Expenses vs Revenue", "Compare revenue and costs", "Financial", compareIcon, financialColor, financialLight)
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
            new(ChartDataType.CompaniesOfOrigin, "Companies of Origin", "Sales by supplier company", "Geographic", buildingIcon, geographicColor, geographicLight),
            new(ChartDataType.CompaniesOfDestination, "Companies of Destination", "Sales by destination company", "Geographic", buildingIcon, geographicColor, geographicLight)
        };

        // Accountant charts
        var accountantCharts = new ObservableCollection<ChartOption>
        {
            new(ChartDataType.AccountantsTransactions, "Transactions by Accountant", "Transactions by accountant", "Personnel", personIcon, accountantColor, accountantLight)
        };

        // Customer charts
        var customerCharts = new ObservableCollection<ChartOption>
        {
            new(ChartDataType.TopCustomersByRevenue, "Top Customers by Revenue", "Highest revenue customers", "Customers", groupIcon, customerColor, customerLight),
            new(ChartDataType.CustomerPaymentStatus, "Customer Payment Status", "Payment status breakdown", "Customers", paymentIcon, customerColor, customerLight),
            new(ChartDataType.CustomerGrowth, "Customer Growth", "Customer acquisition trends", "Customers", growthIcon, customerColor, customerLight),
            new(ChartDataType.CustomerLifetimeValue, "Customer Lifetime Value", "Average customer value", "Customers", heartIcon, customerColor, customerLight),
            new(ChartDataType.ActiveVsInactiveCustomers, "Active vs Inactive", "Customer activity status", "Customers", groupIcon, customerColor, customerLight),
            new(ChartDataType.RentalsPerCustomer, "Rentals per Customer", "Rental frequency by customer", "Customers", rentalIcon, customerColor, customerLight)
        };

        // Return charts
        var returnCharts = new ObservableCollection<ChartOption>
        {
            new(ChartDataType.ReturnsOverTime, "Returns Over Time", "Return trends", "Returns", returnIcon, returnColor, returnLight),
            new(ChartDataType.ReturnReasons, "Return Reasons", "Why items are returned", "Returns", reasonIcon, returnColor, returnLight),
            new(ChartDataType.ReturnFinancialImpact, "Return Financial Impact", "Financial impact of returns", "Returns", impactIcon, returnColor, returnLight),
            new(ChartDataType.ReturnsByCategory, "Returns by Category", "Returns by category", "Returns", categoryIcon, returnColor, returnLight),
            new(ChartDataType.ReturnsByProduct, "Returns by Product", "Returns by product", "Returns", productIcon, returnColor, returnLight),
            new(ChartDataType.ExpenseVsRevenueReturns, "Expense vs Revenue Returns", "Expense vs revenue returns", "Returns", vsIcon, returnColor, returnLight)
        };

        // Loss charts
        var lossCharts = new ObservableCollection<ChartOption>
        {
            new(ChartDataType.LossesOverTime, "Losses Over Time", "Loss trends", "Losses", lossIcon, lossColor, lossLight),
            new(ChartDataType.LossReasons, "Loss Reasons", "Why items are lost", "Losses", reasonIcon, lossColor, lossLight),
            new(ChartDataType.LossFinancialImpact, "Loss Financial Impact", "Financial impact of losses", "Losses", impactIcon, lossColor, lossLight),
            new(ChartDataType.LossesByCategory, "Losses by Category", "Losses by category", "Losses", categoryIcon, lossColor, lossLight),
            new(ChartDataType.LossesByProduct, "Losses by Product", "Losses by product", "Losses", productIcon, lossColor, lossLight),
            new(ChartDataType.ExpenseVsRevenueLosses, "Expense vs Revenue Losses", "Expense vs revenue losses", "Losses", vsIcon, lossColor, lossLight)
        };

        // Add all charts to AvailableCharts (flat list for backward compatibility)
        foreach (var chart in revenueCharts) AvailableCharts.Add(chart);
        foreach (var chart in expenseCharts) AvailableCharts.Add(chart);
        foreach (var chart in financialCharts) AvailableCharts.Add(chart);
        foreach (var chart in transactionCharts) AvailableCharts.Add(chart);
        foreach (var chart in geographicCharts) AvailableCharts.Add(chart);
        foreach (var chart in accountantCharts) AvailableCharts.Add(chart);
        foreach (var chart in customerCharts) AvailableCharts.Add(chart);
        foreach (var chart in returnCharts) AvailableCharts.Add(chart);
        foreach (var chart in lossCharts) AvailableCharts.Add(chart);

        // Create category groups for grouped display
        ChartCategories.Add(new ChartCategoryGroup("Revenue", revenueColor, revenueCharts, () => OnPropertyChanged(nameof(HasSelectedCharts))));
        ChartCategories.Add(new ChartCategoryGroup("Expenses", expenseColor, expenseCharts, () => OnPropertyChanged(nameof(HasSelectedCharts))));
        ChartCategories.Add(new ChartCategoryGroup("Financial", financialColor, financialCharts, () => OnPropertyChanged(nameof(HasSelectedCharts))));
        ChartCategories.Add(new ChartCategoryGroup("Transactions", transactionColor, transactionCharts, () => OnPropertyChanged(nameof(HasSelectedCharts))));
        ChartCategories.Add(new ChartCategoryGroup("Geographic", geographicColor, geographicCharts, () => OnPropertyChanged(nameof(HasSelectedCharts))));
        ChartCategories.Add(new ChartCategoryGroup("Personnel", accountantColor, accountantCharts, () => OnPropertyChanged(nameof(HasSelectedCharts))));
        ChartCategories.Add(new ChartCategoryGroup("Customers", customerColor, customerCharts, () => OnPropertyChanged(nameof(HasSelectedCharts))));
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
                Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
                {
                    if (config == null)
                    {
                        // Show error message if template couldn't be loaded
                        var dialog = App.ConfirmationDialog;
                        if (dialog != null)
                        {
                            await dialog.ShowAsync(new ConfirmationDialogOptions
                            {
                                Title = "Load Failed".Translate(),
                                Message = "Failed to load the template '{0}'. The file may be corrupted or missing.".TranslateFormat(templateName),
                                PrimaryButtonText = "OK".Translate(),
                                SecondaryButtonText = null,
                                CancelButtonText = null
                            });
                        }
                    }
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
        ShowCompanyDetails = Configuration.ShowCompanyDetails;
        BackgroundColor = Configuration.BackgroundColor;
        TitleFontSize = Configuration.TitleFontSize;

        // Check if we need to restore a preserved date preset (when going back from step 2)
        if (_datePresetToPreserve != null)
        {
            SelectedDatePreset = _datePresetToPreserve;
            PageSettingsDatePreset = _datePresetToPreserve;

            // Update IsSelected on all date preset options
            foreach (var option in DatePresets)
            {
                option.IsSelected = option.Name == _datePresetToPreserve;
            }

            _datePresetToPreserve = null;
        }
        else if (!string.IsNullOrEmpty(Configuration.Filters.DatePresetName))
        {
            // Update date preset and radio button selection from configuration
            SelectedDatePreset = Configuration.Filters.DatePresetName;
            PageSettingsDatePreset = Configuration.Filters.DatePresetName;

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
        var marginLeft = Configuration.PageMargins.Left;
        var marginRight = Configuration.PageMargins.Right;
        var marginBottom = Configuration.PageMargins.Bottom;
        double headerHeight = PageDimensions.GetHeaderHeight(Configuration.ShowCompanyDetails);
        const double footerHeight = PageDimensions.FooterHeight;
        const double spacing = 10;

        var contentWidth = pageWidth - marginLeft - marginRight;
        var contentHeight = pageHeight - headerHeight - footerHeight - marginBottom;
        var startY = headerHeight;

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

            chartElements[i].X = marginLeft + (col * (cellWidth + spacing));
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
public class ChartCategoryGroup : ObservableObject
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
            chart.PropertyChanged += (_, e) =>
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
