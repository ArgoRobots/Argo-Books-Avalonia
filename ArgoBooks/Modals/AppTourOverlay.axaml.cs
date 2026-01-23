using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

public partial class AppTourOverlay : UserControl
{
    private AppTourViewModel? _viewModel;

    public AppTourOverlay()
    {
        InitializeComponent();

        DataContextChanged += OnDataContextChanged;
        PropertyChanged += OnPropertyChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        // Unsubscribe from old ViewModel
        if (_viewModel != null)
        {
            _viewModel.TargetAreaChanged -= OnTargetAreaChanged;
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        // Subscribe to new ViewModel
        _viewModel = DataContext as AppTourViewModel;
        if (_viewModel != null)
        {
            _viewModel.TargetAreaChanged += OnTargetAreaChanged;
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AppTourViewModel.IsOpen) && _viewModel?.IsOpen == true)
        {
            // When tour opens, calculate initial bounds after layout
            Dispatcher.UIThread.Post(UpdateHighlightBounds, DispatcherPriority.Loaded);
        }
    }

    private void OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        // Recalculate when size changes
        if (e.Property == BoundsProperty && _viewModel?.IsOpen == true)
        {
            UpdateHighlightBounds();
        }
    }

    private void OnTargetAreaChanged(object? sender, EventArgs e)
    {
        // Delay slightly to ensure any animations have started
        Dispatcher.UIThread.Post(UpdateHighlightBounds, DispatcherPriority.Render);
    }

    private void UpdateHighlightBounds()
    {
        if (_viewModel == null || !_viewModel.IsOpen)
            return;

        var targetArea = _viewModel.CurrentTargetArea;

        // For "center" target, hide the highlight
        if (targetArea == "center")
        {
            _viewModel.HideHighlight();
            return;
        }

        // Find the target element
        var (element, cornerRadius) = FindTargetElement(targetArea);
        if (element == null)
        {
            _viewModel.HideHighlight();
            return;
        }

        // Get the bounds relative to this overlay
        var bounds = GetElementBoundsRelativeToOverlay(element);
        if (bounds == null)
        {
            _viewModel.HideHighlight();
            return;
        }

        _viewModel.SetHighlightBounds(bounds.Value, cornerRadius);
    }

    private (Control? element, CornerRadius cornerRadius) FindTargetElement(string targetArea)
    {
        // Get the main window
        var window = this.GetVisualRoot() as Window;
        if (window == null)
            return (null, new CornerRadius(8));

        return targetArea switch
        {
            "sidebar" => (FindElementByName<Control>(window, "AppSidebar"), new CornerRadius(0, 8, 8, 0)),
            "searchbar" => (FindElementByName<Control>(window, "SearchBox"), new CornerRadius(6)),
            "content" => (FindElementByName<Control>(window, "AppContent"), new CornerRadius(8)),
            "header" => (FindElementByName<Control>(window, "AppHeader"), new CornerRadius(0, 0, 8, 8)),
            _ => (null, new CornerRadius(8))
        };
    }

    private static T? FindElementByName<T>(Visual root, string name) where T : Control
    {
        // First try direct name lookup on the root if it's a named control
        if (root is Control control && control.Name == name)
            return control as T;

        // Search through the visual tree
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
            // Get the transform from the element to this overlay
            var transform = element.TransformToVisual(this);
            if (transform == null)
                return null;

            // Get element bounds
            var elementBounds = new Rect(0, 0, element.Bounds.Width, element.Bounds.Height);

            // Transform to overlay coordinates
            var topLeft = transform.Value.Transform(elementBounds.TopLeft);
            var bottomRight = transform.Value.Transform(elementBounds.BottomRight);

            // Inset the bounds by border thickness so the border draws INSIDE the element
            // This prevents overflow outside the window
            var left = Math.Max(0, topLeft.X) + borderThickness;
            var top = Math.Max(0, topLeft.Y) + borderThickness;
            var width = (bottomRight.X - topLeft.X) - (borderThickness * 2);
            var height = (bottomRight.Y - topLeft.Y) - (borderThickness * 2);

            // Ensure we have valid dimensions
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
