using ArgoBooks.Services;
using Avalonia;
using Avalonia.Controls;

namespace ArgoBooks.Controls;

public partial class TutorialCompletionOverlay : UserControl
{
    public static readonly StyledProperty<bool> IsOverlayVisibleProperty =
        AvaloniaProperty.Register<TutorialCompletionOverlay, bool>(nameof(IsOverlayVisible));

    public bool IsOverlayVisible
    {
        get => GetValue(IsOverlayVisibleProperty);
        set => SetValue(IsOverlayVisibleProperty, value);
    }

    public TutorialCompletionOverlay()
    {
        InitializeComponent();

        // Set DataContext to self so bindings work
        DataContext = this;

        // Subscribe to tutorial service changes
        TutorialService.Instance.CompletionGuidanceChanged += OnCompletionGuidanceChanged;

        // Set initial state
        IsOverlayVisible = TutorialService.Instance.ShowCompletionGuidance;
    }

    private void OnCompletionGuidanceChanged(object? sender, bool show)
    {
        IsOverlayVisible = show;
    }

    protected override void OnUnloaded(Avalonia.Interactivity.RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        TutorialService.Instance.CompletionGuidanceChanged -= OnCompletionGuidanceChanged;
    }
}
