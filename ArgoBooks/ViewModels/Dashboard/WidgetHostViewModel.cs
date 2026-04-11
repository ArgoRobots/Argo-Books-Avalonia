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
    [ObservableProperty] private bool _isDragging;

    public string Id { get; }
    public WidgetType WidgetType { get; }
    public Dictionary<string, string> Config { get; private set; }
    public WidgetSize[] AvailableSizes { get; }

    public WidgetHostViewModel(DashboardWidgetEntry entry, WidgetViewModelBase widgetVm, WidgetSize[] availableSizes)
    {
        Id = entry.Id;
        WidgetType = entry.WidgetType;
        Size = entry.Size;
        Config = new Dictionary<string, string>(entry.Config);
        AvailableSizes = availableSizes;
        _widgetViewModel = widgetVm;

        widgetVm.Initialize(entry.Config);
    }

    /// <summary>
    /// Whether the next size cycle will make the widget larger (true) or wrap back to smallest (false).
    /// </summary>
    public bool WillGrow
    {
        get
        {
            var currentIndex = Array.IndexOf(AvailableSizes, Size);
            var nextIndex = (currentIndex + 1) % AvailableSizes.Length;
            return AvailableSizes[nextIndex] > Size;
        }
    }

    [RelayCommand]
    private void CycleSize()
    {
        var currentIndex = Array.IndexOf(AvailableSizes, Size);
        var nextIndex = (currentIndex + 1) % AvailableSizes.Length;
        Size = AvailableSizes[nextIndex];
        OnPropertyChanged(nameof(WillGrow));
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

    public DashboardWidgetEntry ToEntry() => new()
    {
        Id = Id,
        WidgetType = WidgetType,
        Size = Size,
        Config = WidgetViewModel.GetConfig()
    };
}
