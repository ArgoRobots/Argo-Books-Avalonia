using ArgoBooks.Services;
using Avalonia.Controls;

namespace ArgoBooks.Controls;

public partial class TutorialCompletionOverlay : UserControl
{
    private ModalOverlay? _overlay;

    public TutorialCompletionOverlay()
    {
        InitializeComponent();

        _overlay = this.FindControl<ModalOverlay>("Overlay");

        // Subscribe to tutorial service changes
        TutorialService.Instance.CompletionGuidanceChanged += OnCompletionGuidanceChanged;

        // Set initial state
        if (_overlay != null)
        {
            _overlay.IsOpen = TutorialService.Instance.ShowCompletionGuidance;
        }
    }

    private void OnCompletionGuidanceChanged(object? sender, bool show)
    {
        if (_overlay != null)
        {
            _overlay.IsOpen = show;
        }
    }

    protected override void OnUnloaded(Avalonia.Interactivity.RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        TutorialService.Instance.CompletionGuidanceChanged -= OnCompletionGuidanceChanged;
    }
}
