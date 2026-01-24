using ArgoBooks.Localization;
using ArgoBooks.Services;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ArgoBooks.Controls;

public partial class TutorialCompletionOverlay : UserControl
{
    private ModalOverlay? _overlay;
    private readonly TutorialCompletionOverlayViewModel _viewModel;

    public TutorialCompletionOverlay()
    {
        InitializeComponent();

        _viewModel = new TutorialCompletionOverlayViewModel();
        DataContext = _viewModel;

        _overlay = this.FindControl<ModalOverlay>("Overlay");

        // Subscribe to tutorial service changes
        TutorialService.Instance.CompletionGuidanceChanged += OnCompletionGuidanceChanged;

        // Set initial state
        UpdateOverlay(TutorialService.Instance.ShowCompletionGuidance);
    }

    private void OnCompletionGuidanceChanged(object? sender, bool show)
    {
        UpdateOverlay(show);
    }

    private void UpdateOverlay(bool show)
    {
        if (_overlay != null)
        {
            _overlay.IsOpen = show;
        }

        if (show)
        {
            _viewModel.UpdateForGuidanceType(TutorialService.Instance.CurrentGuidanceType);
        }
    }

    protected override void OnUnloaded(Avalonia.Interactivity.RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        TutorialService.Instance.CompletionGuidanceChanged -= OnCompletionGuidanceChanged;
    }
}

public partial class TutorialCompletionOverlayViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "Nice work!";

    [ObservableProperty]
    private string _message = "Use the navigation bar to go back to the Dashboard for the next step.";

    public void UpdateForGuidanceType(CompletionGuidanceType type)
    {
        switch (type)
        {
            case CompletionGuidanceType.Analytics:
                Title = LanguageService.Instance.Translate("You're all set!");
                Message = LanguageService.Instance.Translate("As you add expenses, revenue, and other data, these charts will automatically update to show your business insights. Head back to the Dashboard to complete the tutorial.");
                break;
            case CompletionGuidanceType.Standard:
            default:
                Title = LanguageService.Instance.Translate("Nice work!");
                Message = LanguageService.Instance.Translate("Use the navigation bar to go back to the Dashboard for the next step.");
                break;
        }
    }
}
