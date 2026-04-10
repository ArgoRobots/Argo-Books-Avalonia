using System.Collections.ObjectModel;
using ArgoBooks.Core.Models.Dashboard;
using ArgoBooks.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels.Dashboard;

public partial class CatalogItem : ObservableObject
{
    public WidgetDefinition Definition { get; }
    [ObservableProperty] private bool _isAlreadyAdded;
    public CatalogItem(WidgetDefinition definition) { Definition = definition; }
}

public partial class WidgetCatalogViewModel : ObservableObject
{
    [ObservableProperty] private bool _isOpen;
    public ObservableCollection<CatalogItem> StatCards { get; } = [];
    public ObservableCollection<CatalogItem> Charts { get; } = [];
    public ObservableCollection<CatalogItem> Tables { get; } = [];

    public event EventHandler<WidgetType>? WidgetAddRequested;

    private static readonly HashSet<string> StatCardCategories = ["Statistics"];
    private static readonly HashSet<string> ChartCategories = ["Charts", "Insights"];

    public void Refresh(IReadOnlyList<WidgetHostViewModel> currentWidgets)
    {
        StatCards.Clear();
        Charts.Clear();
        Tables.Clear();

        var placedTypes = currentWidgets.Select(w => w.WidgetType).ToHashSet();

        foreach (var d in WidgetFactory.GetAllDefinitions())
        {
            var item = new CatalogItem(d) { IsAlreadyAdded = placedTypes.Contains(d.Type) };

            if (StatCardCategories.Contains(d.Category))
                StatCards.Add(item);
            else if (ChartCategories.Contains(d.Category))
                Charts.Add(item);
            else
                Tables.Add(item);
        }
    }

    [RelayCommand]
    private void AddWidget(CatalogItem item)
    {
        if (item.IsAlreadyAdded) return;
        WidgetAddRequested?.Invoke(this, item.Definition.Type);
        IsOpen = false;
    }

    [RelayCommand]
    private void Close() => IsOpen = false;
}
