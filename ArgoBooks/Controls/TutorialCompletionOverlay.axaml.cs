using System.Windows.Input;
using ArgoBooks.Services;
using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.Controls;

public partial class TutorialCompletionOverlay : UserControl
{
    public static readonly StyledProperty<bool> IsVisibleProperty =
        AvaloniaProperty.Register<TutorialCompletionOverlay, bool>(nameof(IsVisible));

    public new bool IsVisible
    {
        get => GetValue(IsVisibleProperty);
        set => SetValue(IsVisibleProperty, value);
    }

    public static readonly StyledProperty<ICommand?> DismissCommandProperty =
        AvaloniaProperty.Register<TutorialCompletionOverlay, ICommand?>(nameof(DismissCommand));

    public ICommand? DismissCommand
    {
        get => GetValue(DismissCommandProperty);
        set => SetValue(DismissCommandProperty, value);
    }

    public TutorialCompletionOverlay()
    {
        InitializeComponent();

        // Set default dismiss command
        DismissCommand = new RelayCommand(() =>
        {
            TutorialService.Instance.DismissCompletionGuidance();
        });

        // Subscribe to tutorial service changes
        TutorialService.Instance.CompletionGuidanceChanged += OnCompletionGuidanceChanged;

        // Set initial state
        IsVisible = TutorialService.Instance.ShowCompletionGuidance;
    }

    private void OnCompletionGuidanceChanged(object? sender, bool show)
    {
        IsVisible = show;
    }

    protected override void OnUnloaded(Avalonia.Interactivity.RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        TutorialService.Instance.CompletionGuidanceChanged -= OnCompletionGuidanceChanged;
    }
}
