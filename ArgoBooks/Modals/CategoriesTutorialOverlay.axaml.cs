using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ArgoBooks.Helpers;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

public partial class CategoriesTutorialOverlay : UserControl
{
    private CategoriesTutorialViewModel? _viewModel;
    private Avalonia.Controls.Shapes.Path? _backdropPath;

    public CategoriesTutorialOverlay()
    {
        InitializeComponent();

        _backdropPath = this.FindControl<Avalonia.Controls.Shapes.Path>("BackdropPath");

        DataContextChanged += OnDataContextChanged;
        PropertyChanged += OnPropertyChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.HighlightAreaChanged -= OnHighlightAreaChanged;
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        _viewModel = DataContext as CategoriesTutorialViewModel;
        if (_viewModel != null)
        {
            _viewModel.HighlightAreaChanged += OnHighlightAreaChanged;
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CategoriesTutorialViewModel.IsOpen) && _viewModel?.IsOpen == true)
        {
            Dispatcher.UIThread.Post(UpdateHighlightBounds, DispatcherPriority.Loaded);
        }
    }

    private void OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == BoundsProperty && _viewModel?.IsOpen == true)
        {
            UpdateHighlightBounds();
        }
    }

    private void OnHighlightAreaChanged(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(UpdateHighlightBounds, DispatcherPriority.Render);
    }

    private void UpdateHighlightBounds()
    {
        if (_viewModel == null || !_viewModel.IsOpen)
            return;

        var highlightArea = _viewModel.CurrentHighlightArea;

        if (highlightArea == "none")
        {
            _viewModel.HideHighlight();
            UpdateBackdropGeometry(null, new CornerRadius(0));
            return;
        }

        var window = this.GetVisualRoot() as Window;
        if (window == null)
        {
            _viewModel.HideHighlight();
            UpdateBackdropGeometry(null, new CornerRadius(0));
            return;
        }

        Rect? bounds;
        CornerRadius cornerRadius;

        if (highlightArea == "tabs")
        {
            // Compute tight bounding box around actual TabItem elements
            bounds = TutorialHighlightHelper.GetTabItemsBounds(this, window, "CategoriesPageTabs");
            cornerRadius = new CornerRadius(8);
        }
        else
        {
            var element = TutorialHighlightHelper.FindElementByName<Control>(window, "AppContent");
            bounds = element != null
                ? TutorialHighlightHelper.GetHighlightBounds(this, element)
                : null;
            cornerRadius = new CornerRadius(8);
        }

        if (bounds == null)
        {
            _viewModel.HideHighlight();
            UpdateBackdropGeometry(null, new CornerRadius(0));
            return;
        }

        _viewModel.SetHighlightBounds(bounds.Value, cornerRadius);
        UpdateBackdropGeometry(bounds.Value, cornerRadius);
    }

    private void UpdateBackdropGeometry(Rect? highlightBounds, CornerRadius cornerRadius)
    {
        if (_backdropPath == null)
            return;

        _backdropPath.Data = TutorialHighlightHelper.CreateBackdropGeometry(
            Bounds.Size, highlightBounds, cornerRadius);
    }
}
