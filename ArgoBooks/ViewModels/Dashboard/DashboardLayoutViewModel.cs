using System.Collections.ObjectModel;
using ArgoBooks.Core.Models;
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
    private ObservableCollection<WidgetHostViewModel> _widgets = [];

    public bool HasWidgets => Widgets.Count > 0;
    public WidgetCatalogViewModel Catalog { get; } = new();

    public void Initialize(CompanyManager companyManager)
    {
        _companyManager = companyManager;
        Catalog.WidgetAddRequested += OnWidgetAddRequested;

        var settings = App.SettingsService?.GlobalSettings;
        var layout = settings?.Ui.DashboardLayout ?? DashboardLayout.CreateDefault();

        // Migrate quick actions settings on first load
        if (settings?.Ui.DashboardLayout == null)
            MigrateQuickActionsSettings(layout, settings?.Ui.QuickActions);

        _savedLayout = layout.Clone();
        LoadWidgetsFromLayout(layout);
    }

    private void MigrateQuickActionsSettings(DashboardLayout layout, QuickActionsSettings? qa)
    {
        if (qa == null) return;
        var widget = layout.Widgets.FirstOrDefault(w => w.WidgetType == WidgetType.QuickActions);
        if (widget == null) return;

        widget.Config["ShowNewInvoice"] = qa.ShowNewInvoice.ToString();
        widget.Config["ShowNewExpense"] = qa.ShowNewExpense.ToString();
        widget.Config["ShowNewRevenue"] = qa.ShowNewRevenue.ToString();
        widget.Config["ShowScanReceipt"] = qa.ShowScanReceipt.ToString();
        widget.Config["ShowNewCustomer"] = qa.ShowNewCustomer.ToString();
        widget.Config["ShowNewSupplier"] = qa.ShowNewSupplier.ToString();
        widget.Config["ShowNewProduct"] = qa.ShowNewProduct.ToString();
        widget.Config["ShowRecordPayment"] = qa.ShowRecordPayment.ToString();
        widget.Config["ShowNewRentalItem"] = qa.ShowNewRentalItem.ToString();
        widget.Config["ShowNewRentalRecord"] = qa.ShowNewRentalRecord.ToString();
        widget.Config["ShowNewCategory"] = qa.ShowNewCategory.ToString();
        widget.Config["ShowNewDepartment"] = qa.ShowNewDepartment.ToString();
        widget.Config["ShowNewLocation"] = qa.ShowNewLocation.ToString();
        widget.Config["ShowNewPurchaseOrder"] = qa.ShowNewPurchaseOrder.ToString();
        widget.Config["ShowNewStockAdjustment"] = qa.ShowNewStockAdjustment.ToString();
    }

    private void LoadWidgetsFromLayout(DashboardLayout layout)
    {
        foreach (var w in Widgets) w.Cleanup();
        Widgets.Clear();
        foreach (var entry in layout.Widgets)
        {
            var host = WidgetFactory.CreateWidgetHost(entry);
            host.SetCompanyManager(_companyManager);
            WireUpWidgetEvents(host);
            Widgets.Add(host);
        }
    }

    private void WireUpWidgetEvents(WidgetHostViewModel host)
    {
        // Wire setup checklist navigation
        if (host.WidgetViewModel is SetupChecklistWidgetViewModel checklistVm)
            checklistVm.NavigationRequested += OnChecklistNavigationRequested;
    }

    private void OnChecklistNavigationRequested(object? sender, string pageName)
    {
        _ = App.NavigationService?.NavigateToAsync(pageName);
    }

    public void LoadAllWidgetData()
    {
        foreach (var w in Widgets) w.LoadData();
    }

    [RelayCommand]
    private void EnterEditMode()
    {
        _savedLayout = GetCurrentLayout();
        IsEditMode = true;
        foreach (var w in Widgets) w.IsEditMode = true;
        Catalog.Refresh(Widgets);
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditMode = false;
        foreach (var w in Widgets) w.IsEditMode = false;
        if (_savedLayout != null) LoadWidgetsFromLayout(_savedLayout);
        LoadAllWidgetData();
    }

    [RelayCommand]
    private async Task SaveEdit()
    {
        IsEditMode = false;
        foreach (var w in Widgets) w.IsEditMode = false;

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
        LoadWidgetsFromLayout(layout);
        LoadAllWidgetData();
        foreach (var w in Widgets) w.IsEditMode = true;
        Catalog.Refresh(Widgets);
    }

    [RelayCommand]
    private void OpenCatalog()
    {
        Catalog.Refresh(Widgets);
        Catalog.IsOpen = true;
    }

    [RelayCommand]
    private void RemoveWidget(WidgetHostViewModel widget)
    {
        widget.Cleanup();
        Widgets.Remove(widget);
        Catalog.Refresh(Widgets);
    }

    private void OnWidgetAddRequested(object? sender, WidgetType type)
    {
        var def = WidgetFactory.GetDefinition(type);
        var entry = new DashboardWidgetEntry(type, def.DefaultSize);
        var host = WidgetFactory.CreateWidgetHost(entry);
        host.SetCompanyManager(_companyManager);
        host.IsEditMode = true;
        host.LoadData();
        Widgets.Add(host);
        Catalog.Refresh(Widgets);
    }

    public void MoveWidget(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= Widgets.Count) return;
        if (toIndex < 0 || toIndex > Widgets.Count) return;
        if (fromIndex == toIndex) return;
        Widgets.Move(fromIndex, toIndex > fromIndex ? toIndex - 1 : toIndex);
    }

    private DashboardLayout GetCurrentLayout() => new()
    {
        Widgets = Widgets.Select(w => w.ToEntry()).ToList()
    };

    public void Cleanup()
    {
        Catalog.WidgetAddRequested -= OnWidgetAddRequested;
        foreach (var w in Widgets) w.Cleanup();
    }
}
