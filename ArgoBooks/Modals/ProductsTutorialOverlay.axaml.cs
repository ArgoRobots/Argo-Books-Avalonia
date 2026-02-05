using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

public partial class ProductsTutorialOverlay : UserControl
{
    private ProductsTutorialViewModel? _viewModel;

    public ProductsTutorialOverlay()
    {
        InitializeComponent();

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

        _viewModel = DataContext as ProductsTutorialViewModel;
        if (_viewModel != null)
        {
            _viewModel.HighlightAreaChanged += OnHighlightAreaChanged;
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ProductsTutorialViewModel.IsOpen) && _viewModel?.IsOpen == true)
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
            return;
        }

        var element = FindTargetElement(highlightArea);
        if (element == null)
        {
            _viewModel.HideHighlight();
            return;
        }

        var bounds = GetElementBoundsRelativeToOverlay(element);
        if (bounds == null)
        {
            _viewModel.HideHighlight();
            return;
        }

        var cornerRadius = highlightArea == "tabs" ? new CornerRadius(0) : new CornerRadius(8);
        _viewModel.SetHighlightBounds(bounds.Value, cornerRadius);
    }

    private Control? FindTargetElement(string highlightArea)
    {
        var window = this.GetVisualRoot() as Window;
        if (window == null)
            return null;

        return highlightArea switch
        {
            "tabs" => FindElementByName<Control>(window, "ProductsTabBar"),
            "content" => FindElementByName<Control>(window, "AppContent"),
            _ => null
        };
    }

    private static T? FindElementByName<T>(Visual root, string name) where T : Control
    {
        if (root is Control control && control.Name == name)
            return control as T;

        foreach (var child in root.GetVisualDescendants())
        {
            if (child is T typedChild && typedChild.Name == name)
                return typedChild;
        }

        return null;
    }

    private Rect? GetElementBoundsRelativeToOverlay(Control element)
    {
        const double borderThickness = 3;

        try
        {
            var transform = element.TransformToVisual(this);
            if (transform == null)
                return null;

            var elementBounds = new Rect(0, 0, element.Bounds.Width, element.Bounds.Height);
            var topLeft = transform.Value.Transform(elementBounds.TopLeft);
            var bottomRight = transform.Value.Transform(elementBounds.BottomRight);

            // Draw border outside the element
            var left = topLeft.X - borderThickness;
            var top = topLeft.Y - borderThickness;
            var width = (bottomRight.X - topLeft.X) + (borderThickness * 2);
            var height = (bottomRight.Y - topLeft.Y) + (borderThickness * 2);

            if (width <= 0 || height <= 0)
                return null;

            return new Rect(left, top, width, height);
        }
        catch
        {
            return null;
        }
    }
}
