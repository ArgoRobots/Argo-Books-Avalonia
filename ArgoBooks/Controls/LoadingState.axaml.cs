using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;

namespace ArgoBooks.Controls;

/// <summary>
/// The single, consistent loading indicator used across the app. Two modes:
/// <list type="bullet">
///   <item><b>Spinner only</b> (default): a spinner + message for quick or unmeasurable
///   operations.</item>
///   <item><b>Determinate</b> (<see cref="ShowProgress"/> = true): spinner + a progress bar +
///   a percent label, driven by accurate progress. Set <see cref="IsIndeterminate"/> for a
///   moving bar with no percent.</item>
/// </list>
/// Embed it inside a modal's content area (it centers itself). The parent toggles its
/// <see cref="Control.IsVisible"/>.
/// </summary>
public partial class LoadingState : UserControl
{
    public static readonly StyledProperty<bool> IsActiveProperty =
        AvaloniaProperty.Register<LoadingState, bool>(nameof(IsActive), true);

    public static readonly StyledProperty<string?> MessageProperty =
        AvaloniaProperty.Register<LoadingState, string?>(nameof(Message));

    public static readonly StyledProperty<string?> DetailProperty =
        AvaloniaProperty.Register<LoadingState, string?>(nameof(Detail));

    public static readonly StyledProperty<bool> ShowProgressProperty =
        AvaloniaProperty.Register<LoadingState, bool>(nameof(ShowProgress));

    public static readonly StyledProperty<bool> IsIndeterminateProperty =
        AvaloniaProperty.Register<LoadingState, bool>(nameof(IsIndeterminate));

    public static readonly StyledProperty<double> PercentProperty =
        AvaloniaProperty.Register<LoadingState, double>(nameof(Percent));

    public static readonly StyledProperty<bool> IsCancellableProperty =
        AvaloniaProperty.Register<LoadingState, bool>(nameof(IsCancellable));

    public static readonly StyledProperty<ICommand?> CancelCommandProperty =
        AvaloniaProperty.Register<LoadingState, ICommand?>(nameof(CancelCommand));

    public static readonly StyledProperty<SpinnerSize> SpinnerPresetProperty =
        AvaloniaProperty.Register<LoadingState, SpinnerSize>(nameof(SpinnerPreset), SpinnerSize.Large);

    /// <summary>Whether the spinner animates (true while the operation runs).</summary>
    public bool IsActive
    {
        get => GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    /// <summary>Primary status line (e.g. "Importing data...").</summary>
    public string? Message
    {
        get => GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    /// <summary>Optional secondary line shown below the message/progress.</summary>
    public string? Detail
    {
        get => GetValue(DetailProperty);
        set => SetValue(DetailProperty, value);
    }

    /// <summary>Shows the progress bar (+ percent unless <see cref="IsIndeterminate"/>).</summary>
    public bool ShowProgress
    {
        get => GetValue(ShowProgressProperty);
        set => SetValue(ShowProgressProperty, value);
    }

    /// <summary>When the bar is shown, makes it an indeterminate (moving) bar with no percent.</summary>
    public bool IsIndeterminate
    {
        get => GetValue(IsIndeterminateProperty);
        set => SetValue(IsIndeterminateProperty, value);
    }

    /// <summary>Progress percent (0-100) for the determinate bar and label.</summary>
    public double Percent
    {
        get => GetValue(PercentProperty);
        set => SetValue(PercentProperty, value);
    }

    /// <summary>Shows the cancel button.</summary>
    public bool IsCancellable
    {
        get => GetValue(IsCancellableProperty);
        set => SetValue(IsCancellableProperty, value);
    }

    /// <summary>Command invoked by the cancel button.</summary>
    public ICommand? CancelCommand
    {
        get => GetValue(CancelCommandProperty);
        set => SetValue(CancelCommandProperty, value);
    }

    /// <summary>Spinner size (default Large).</summary>
    public SpinnerSize SpinnerPreset
    {
        get => GetValue(SpinnerPresetProperty);
        set => SetValue(SpinnerPresetProperty, value);
    }

    public LoadingState()
    {
        InitializeComponent();
    }
}
