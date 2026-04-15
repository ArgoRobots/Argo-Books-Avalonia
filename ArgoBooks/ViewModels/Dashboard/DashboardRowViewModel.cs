using System.Collections.ObjectModel;
using ArgoBooks.Core.Models.Dashboard;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ArgoBooks.ViewModels.Dashboard;

public partial class DashboardRowViewModel : ObservableObject
{
    [ObservableProperty] private bool _isEditMode;
    [ObservableProperty] private bool _isEmpty;

    public ObservableCollection<WidgetHostViewModel> Widgets { get; } = [];

    public double TotalFraction => Widgets.Sum(w => w.Size.ToFraction());

    public bool CanFit(WidgetSize size) => TotalFraction + size.ToFraction() <= 1.001;

    public DashboardRowViewModel()
    {
        Widgets.CollectionChanged += (_, _) => UpdateIsEmpty();
        UpdateIsEmpty();
    }

    private void UpdateIsEmpty() => IsEmpty = Widgets.Count == 0;

    public DashboardRow ToRow() => new()
    {
        Widgets = Widgets.Select(w => w.ToEntry()).ToList()
    };
}
