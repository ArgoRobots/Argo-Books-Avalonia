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

public partial class CatalogCategory : ObservableObject
{
    public string Name { get; }
    public ObservableCollection<CatalogItem> Items { get; }
    public CatalogCategory(string name, IEnumerable<CatalogItem> items)
    {
        Name = name;
        Items = new ObservableCollection<CatalogItem>(items);
    }
}

public partial class WidgetCatalogViewModel : ObservableObject
{
    [ObservableProperty] private bool _isOpen;
    public ObservableCollection<CatalogCategory> Categories { get; } = [];

    public event EventHandler<WidgetType>? WidgetAddRequested;

    public void Refresh(IReadOnlyList<WidgetHostViewModel> currentWidgets)
    {
        Categories.Clear();
        var placedTypes = currentWidgets.Select(w => w.WidgetType).ToHashSet();

        foreach (var categoryName in WidgetFactory.GetCategories())
        {
            var items = WidgetFactory.GetAllDefinitions()
                .Where(d => d.Category == categoryName)
                .Select(d => new CatalogItem(d) { IsAlreadyAdded = !d.AllowDuplicates && placedTypes.Contains(d.Type) })
                .ToList();
            Categories.Add(new CatalogCategory(categoryName, items));
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
