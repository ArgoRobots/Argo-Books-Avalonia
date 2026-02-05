using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ArgoBooks.Helpers;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

public partial class CategoriesTutorialOverlay : UserControl
{
    private CategoriesTutorialViewModel? _viewModel;
    private Path? _backdropPath;

    public CategoriesTutorialOverlay()
    {
        InitializeComponent();

        _backdropPath = this.FindControl<Path>("BackdropPath");

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

        var (element, cornerRadius) = FindTargetElement(window, highlightArea);
        if (element == null)
        {
            _viewModel.HideHighlight();
            UpdateBackdropGeometry(null, new CornerRadius(0));
            return;
        }

        var bounds = TutorialHighlightHelper.GetHighlightBounds(this, element, highlightArea);
        if (bounds == null)
        {
            _viewModel.HideHighlight();
            UpdateBackdropGeometry(null, new CornerRadius(0));
            return;
        }

        _viewModel.SetHighlightBounds(bounds.Value, cornerRadius);
        UpdateBackdropGeometry(bounds.Value, cornerRadius);
    }

    private static (Control? element, CornerRadius cornerRadius) FindTargetElement(Window window, string highlightArea)
    {
        return highlightArea switch
        {
            "tabs" => (TutorialHighlightHelper.FindTabItemsPresenter(window, "CategoriesPageTabs"), new CornerRadius(0)),
            "content" => (TutorialHighlightHelper.FindElementByName<Control>(window, "AppContent"), new CornerRadius(8)),
            _ => (null, new CornerRadius(0))
        };
    }

    private void UpdateBackdropGeometry(Rect? highlightBounds, CornerRadius cornerRadius)
    {
        if (_backdropPath == null)
            return;

        _backdropPath.Data = TutorialHighlightHelper.CreateBackdropGeometry(
            Bounds.Size, highlightBounds, cornerRadius);
    }
}
