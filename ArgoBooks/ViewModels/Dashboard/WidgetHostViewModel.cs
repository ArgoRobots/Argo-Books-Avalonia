using ArgoBooks.Core.Models.Dashboard;
using ArgoBooks.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels.Dashboard;

public partial class WidgetHostViewModel : ObservableObject
{
    [ObservableProperty] private WidgetViewModelBase _widgetViewModel;
    [ObservableProperty] private WidgetSize _size;
    [ObservableProperty] private bool _isEditMode;
    [ObservableProperty] private bool _isConfigOpen;

    public string Id { get; }
    public WidgetType WidgetType { get; }
    public Dictionary<string, string> Config { get; private set; }
    public WidgetSize[] AvailableSizes { get; }
    public double StartOffset { get; set; }

    public WidgetHostViewModel(DashboardWidgetEntry entry, WidgetViewModelBase widgetVm, WidgetSize[] availableSizes)
    {
        Id = entry.Id;
        WidgetType = entry.WidgetType;
        Size = entry.Size;
        Config = new Dictionary<string, string>(entry.Config);
        AvailableSizes = availableSizes;
        _widgetViewModel = widgetVm;

        if (entry.Config.TryGetValue("StartOffset", out var offsetStr)
            && double.TryParse(offsetStr, System.Globalization.CultureInfo.InvariantCulture, out var offset))
            StartOffset = offset;

        widgetVm.Initialize(entry.Config);
    }

    [RelayCommand]
    private void ToggleConfig()
    {
        IsConfigOpen = !IsConfigOpen;
    }

    public void SetCompanyManager(CompanyManager? companyManager) =>
        WidgetViewModel.SetCompanyManager(companyManager);

    public void LoadData() => WidgetViewModel.LoadData();
    public void Cleanup() => WidgetViewModel.Cleanup();

    public DashboardWidgetEntry ToEntry()
    {
        var config = WidgetViewModel.GetConfig();
        if (StartOffset > 0.001)
            config["StartOffset"] = StartOffset.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return new()
        {
            Id = Id,
            WidgetType = WidgetType,
            Size = Size,
            Config = config
        };
    }
}
