using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Media;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Views;

/// <summary>
/// Code-behind for the Insights page.
/// </summary>
public partial class InsightsPage : UserControl
{
    private static readonly ImmutableBlurEffect BlurEffect = new(6);

    public InsightsPage()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private InsightsPageViewModel? _previousViewModel;

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_previousViewModel != null)
        {
            _previousViewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _previousViewModel = null;
        }

        if (DataContext is InsightsPageViewModel vm)
        {
            vm.PropertyChanged += OnViewModelPropertyChanged;
            _previousViewModel = vm;
            UpdateBlur(vm.ShowTeaser);
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(InsightsPageViewModel.ShowTeaser) && DataContext is InsightsPageViewModel vm)
        {
            UpdateBlur(vm.ShowTeaser);
        }
    }

    private void UpdateBlur(bool showTeaser)
    {
        ContentScrollViewer.Effect = showTeaser ? BlurEffect : null;
    }
}
