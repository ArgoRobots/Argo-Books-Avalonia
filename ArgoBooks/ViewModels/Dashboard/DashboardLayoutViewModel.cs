using System.Collections.ObjectModel;
using ArgoBooks.Core.Models.Dashboard;
using ArgoBooks.Core.Services;
using ArgoBooks.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels.Dashboard;

public partial class DashboardLayoutViewModel : ObservableObject
{
    private CompanyManager? _companyManager;
    private DashboardLayout? _savedLayout;

    [ObservableProperty] private bool _isEditMode;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasWidgets))]
    private ObservableCollection<DashboardRowViewModel> _rows = [];

    public bool HasWidgets => Rows.Any(r => r.Widgets.Count > 0);
    public WidgetCatalogViewModel Catalog { get; } = new();

    private DashboardRowViewModel? _targetRowForAdd;

    public void Initialize(CompanyManager companyManager)
    {
        _companyManager = companyManager;
        Catalog.WidgetAddRequested += OnWidgetAddRequested;

        var settings = App.SettingsService?.GlobalSettings;
        var layout = settings?.Ui.DashboardLayout ?? DashboardLayout.CreateDefault();
        layout.MigrateIfNeeded();

        _savedLayout = layout.Clone();
        LoadFromLayout(layout);
    }

    private void LoadFromLayout(DashboardLayout layout)
    {
        UnwireWidgetEvents();
        foreach (var row in Rows)
            foreach (var w in row.Widgets)
                w.Cleanup();
        Rows.Clear();

        foreach (var row in layout.Rows)
        {
            var rowVm = new DashboardRowViewModel();
            foreach (var entry in row.Widgets)
            {
                if (!WidgetFactory.IsKnownType(entry.WidgetType))
                    continue;
                var host = WidgetFactory.CreateWidgetHost(entry);
                host.SetCompanyManager(_companyManager);
                WireUpWidgetEvents(host);
                rowVm.Widgets.Add(host);
            }
            Rows.Add(rowVm);
        }
    }

    private void WireUpWidgetEvents(WidgetHostViewModel host)
    {
        if (host.WidgetViewModel is SetupChecklistWidgetViewModel checklistVm)
            checklistVm.NavigationRequested += OnChecklistNavigationRequested;
    }

    private void UnwireWidgetEvents()
    {
        foreach (var row in Rows)
            foreach (var w in row.Widgets)
                UnwireWidgetEvent(w);
    }

    private void UnwireWidgetEvent(WidgetHostViewModel w)
    {
        if (w.WidgetViewModel is SetupChecklistWidgetViewModel checklistVm)
            checklistVm.NavigationRequested -= OnChecklistNavigationRequested;
    }

    private void OnChecklistNavigationRequested(object? sender, string pageName)
    {
        _ = App.NavigationService?.NavigateToAsync(pageName);
    }

    public void LoadAllWidgetData()
    {
        foreach (var row in Rows)
            foreach (var w in row.Widgets)
                w.LoadData();
    }

    [RelayCommand]
    private void EnterEditMode()
    {
        _savedLayout = GetCurrentLayout();
        IsEditMode = true;
        foreach (var row in Rows)
        {
            row.IsEditMode = true;
            foreach (var w in row.Widgets)
                w.IsEditMode = true;
        }
        Catalog.Refresh(Rows.SelectMany(r => r.Widgets));
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditMode = false;
        foreach (var row in Rows)
        {
            row.IsEditMode = false;
            foreach (var w in row.Widgets)
                w.IsEditMode = false;
        }
        if (_savedLayout != null) LoadFromLayout(_savedLayout);
        LoadAllWidgetData();
    }

    [RelayCommand]
    private async Task SaveEdit()
    {
        IsEditMode = false;

        for (int i = Rows.Count - 1; i >= 0; i--)
        {
            if (Rows[i].Widgets.Count == 0)
                Rows.RemoveAt(i);
        }

        foreach (var row in Rows)
        {
            row.IsEditMode = false;
            foreach (var w in row.Widgets)
                w.IsEditMode = false;
        }

        var layout = GetCurrentLayout();
        _savedLayout = layout.Clone();

        var settings = App.SettingsService?.GlobalSettings;
        if (settings != null)
        {
            settings.Ui.DashboardLayout = layout;
            await App.SettingsService!.SaveGlobalSettingsAsync();
        }
    }

    [RelayCommand]
    private void ResetToDefault()
    {
        var layout = DashboardLayout.CreateDefault();
        LoadFromLayout(layout);
        LoadAllWidgetData();
        foreach (var row in Rows)
        {
            row.IsEditMode = true;
            foreach (var w in row.Widgets)
                w.IsEditMode = true;
        }
        Catalog.Refresh(Rows.SelectMany(r => r.Widgets));
    }

    [RelayCommand]
    private void AddRow()
    {
        var row = new DashboardRowViewModel { IsEditMode = true };
        Rows.Add(row);
        OnPropertyChanged(nameof(HasWidgets));
    }

    public void RemoveRow(DashboardRowViewModel row)
    {
        foreach (var w in row.Widgets)
        {
            UnwireWidgetEvent(w);
            w.Cleanup();
        }
        Rows.Remove(row);
        Catalog.Refresh(Rows.SelectMany(r => r.Widgets));
        OnPropertyChanged(nameof(HasWidgets));
    }

    public void OpenCatalogForRow(DashboardRowViewModel row)
    {
        _targetRowForAdd = row;
        Catalog.Refresh(Rows.SelectMany(r => r.Widgets));
        Catalog.IsOpen = true;
    }

    [RelayCommand]
    private void OpenCatalog()
    {
        var row = new DashboardRowViewModel { IsEditMode = true };
        Rows.Add(row);
        _targetRowForAdd = row;
        Catalog.Refresh(Rows.SelectMany(r => r.Widgets));
        Catalog.IsOpen = true;
    }

    private void OnWidgetAddRequested(object? sender, WidgetType type)
    {
        var targetRow = _targetRowForAdd ?? Rows.LastOrDefault();
        if (targetRow == null)
        {
            targetRow = new DashboardRowViewModel { IsEditMode = true };
            Rows.Add(targetRow);
        }

        var def = WidgetFactory.GetDefinition(type);
        var entry = new DashboardWidgetEntry(type, def.DefaultSize);

        if (!targetRow.CanFit(entry.Size))
        {
            _targetRowForAdd = null;
            return;
        }

        var host = WidgetFactory.CreateWidgetHost(entry);
        host.SetCompanyManager(_companyManager);
        WireUpWidgetEvents(host);
        host.IsEditMode = true;
        host.LoadData();
        targetRow.Widgets.Add(host);
        Catalog.Refresh(Rows.SelectMany(r => r.Widgets));
        _targetRowForAdd = null;
        OnPropertyChanged(nameof(HasWidgets));
    }

    [RelayCommand]
    private void RemoveWidget(WidgetHostViewModel widget)
    {
        foreach (var row in Rows)
        {
            if (row.Widgets.Remove(widget))
            {
                UnwireWidgetEvent(widget);
                widget.Cleanup();

                if (row.Widgets.Count == 0)
                    Rows.Remove(row);

                break;
            }
        }
        Catalog.Refresh(Rows.SelectMany(r => r.Widgets));
        OnPropertyChanged(nameof(HasWidgets));
    }

    public void SwapWidgetsInRow(DashboardRowViewModel row, int indexA, int indexB)
    {
        if (indexA < 0 || indexA >= row.Widgets.Count) return;
        if (indexB < 0 || indexB >= row.Widgets.Count) return;
        if (indexA == indexB) return;

        int a = Math.Min(indexA, indexB);
        int b = Math.Max(indexA, indexB);
        row.Widgets.Move(b, a);
        row.Widgets.Move(a + 1, b);
    }

    public bool MoveWidgetToRow(DashboardRowViewModel sourceRow, int widgetIndex,
        DashboardRowViewModel targetRow)
    {
        if (widgetIndex < 0 || widgetIndex >= sourceRow.Widgets.Count) return false;
        if (sourceRow == targetRow) return false;

        var widget = sourceRow.Widgets[widgetIndex];
        if (!targetRow.CanFit(widget.Size)) return false;

        sourceRow.Widgets.RemoveAt(widgetIndex);
        targetRow.Widgets.Add(widget);

        if (sourceRow.Widgets.Count == 0)
            Rows.Remove(sourceRow);

        OnPropertyChanged(nameof(HasWidgets));
        return true;
    }

    private DashboardLayout GetCurrentLayout()
    {
        return new DashboardLayout
        {
            Rows = Rows.Select(r => r.ToRow()).ToList()
        };
    }

    public void Cleanup()
    {
        Catalog.WidgetAddRequested -= OnWidgetAddRequested;
        UnwireWidgetEvents();
        foreach (var row in Rows)
            foreach (var w in row.Widgets)
                w.Cleanup();
    }
}
