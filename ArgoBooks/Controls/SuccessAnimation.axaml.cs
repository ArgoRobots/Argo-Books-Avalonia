using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Transformation;
using Avalonia.Threading;

namespace ArgoBooks.Controls;

/// <summary>
/// An animated success indicator with green circle, checkmark, and customizable text.
/// </summary>
public partial class SuccessAnimation : UserControl
{
    #region Styled Properties

    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<SuccessAnimation, string>(nameof(Title), "Success!");

    public static readonly StyledProperty<string?> MessageProperty =
        AvaloniaProperty.Register<SuccessAnimation, string?>(nameof(Message));

    public static readonly StyledProperty<bool> ShowContinueButtonProperty =
        AvaloniaProperty.Register<SuccessAnimation, bool>(nameof(ShowContinueButton), true);

    public static readonly StyledProperty<string> ContinueButtonTextProperty =
        AvaloniaProperty.Register<SuccessAnimation, string>(nameof(ContinueButtonText), "Continue");

    public static readonly StyledProperty<ICommand?> ContinueCommandProperty =
        AvaloniaProperty.Register<SuccessAnimation, ICommand?>(nameof(ContinueCommand));

    public static readonly StyledProperty<bool> IsPlayingProperty =
        AvaloniaProperty.Register<SuccessAnimation, bool>(nameof(IsPlaying));

    public static readonly StyledProperty<int> ContinueButtonDelayMsProperty =
        AvaloniaProperty.Register<SuccessAnimation, int>(nameof(ContinueButtonDelayMs), 1500);

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the title text displayed below the checkmark.
    /// </summary>
    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>
    /// Gets or sets the optional message text displayed below the title.
    /// </summary>
    public string? Message
    {
        get => GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    /// <summary>
    /// Gets or sets whether to show the continue button.
    /// </summary>
    public bool ShowContinueButton
    {
        get => GetValue(ShowContinueButtonProperty);
        set => SetValue(ShowContinueButtonProperty, value);
    }

    /// <summary>
    /// Gets or sets the continue button text.
    /// </summary>
    public string ContinueButtonText
    {
        get => GetValue(ContinueButtonTextProperty);
        set => SetValue(ContinueButtonTextProperty, value);
    }

    /// <summary>
    /// Gets or sets the command to execute when continue is clicked.
    /// </summary>
    public ICommand? ContinueCommand
    {
        get => GetValue(ContinueCommandProperty);
        set => SetValue(ContinueCommandProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the animation is currently playing.
    /// Set to true to trigger the animation.
    /// </summary>
    public bool IsPlaying
    {
        get => GetValue(IsPlayingProperty);
        set => SetValue(IsPlayingProperty, value);
    }

    /// <summary>
    /// Gets or sets the delay in milliseconds before showing the continue button.
    /// </summary>
    public int ContinueButtonDelayMs
    {
        get => GetValue(ContinueButtonDelayMsProperty);
        set => SetValue(ContinueButtonDelayMsProperty, value);
    }

    #endregion

    /// <summary>
    /// Event raised when the animation completes (after continue button appears).
    /// </summary>
    public event EventHandler? AnimationCompleted;

    public SuccessAnimation()
    {
        InitializeComponent();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsPlayingProperty)
        {
            if (IsPlaying)
            {
                PlayAnimation();
            }
            else
            {
                ResetAnimation();
            }
        }
    }

    /// <summary>
    /// Plays the success animation sequence.
    /// </summary>
    public void PlayAnimation()
    {
        _ = PlayAnimationAsync();
    }

    /// <summary>
    /// Resets the animation to its initial state.
    /// </summary>
    public void ResetAnimation()
    {
        Dispatcher.UIThread.Post(() =>
        {
            // Reset glow
            if (SuccessGlow != null)
            {
                SuccessGlow.Opacity = 0;
                SuccessGlow.RenderTransform = TransformOperations.Parse("scale(0)");
            }

            // Reset circle
            if (SuccessCircle != null)
            {
                SuccessCircle.RenderTransform = TransformOperations.Parse("scale(0)");
            }

            // Reset text
            if (SuccessTextPanel != null)
            {
                SuccessTextPanel.Opacity = 0;
                SuccessTextPanel.RenderTransform = TransformOperations.Parse("translateY(20px)");
            }

            // Reset continue button
            if (ContinueButtonPanel != null)
            {
                ContinueButtonPanel.Opacity = 0;
                ContinueButtonPanel.RenderTransform = TransformOperations.Parse("translateY(20px)");
            }
        }, DispatcherPriority.Background);
    }

    private async Task PlayAnimationAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            // Animate the glow
            if (SuccessGlow != null)
            {
                SuccessGlow.Opacity = 0.4;
                SuccessGlow.RenderTransform = TransformOperations.Parse("scale(1.15)");
            }

            // Animate the success circle with elastic bounce
            if (SuccessCircle != null)
            {
                SuccessCircle.RenderTransform = TransformOperations.Parse("scale(1)");
            }

            // Wait for circle animation
            await Task.Delay(500);

            // Animate text panel
            if (SuccessTextPanel != null)
            {
                SuccessTextPanel.Opacity = 1;
                SuccessTextPanel.RenderTransform = TransformOperations.Parse("translateY(0)");
            }

            // Wait for text animation
            await Task.Delay(300);

            // Show continue button after additional delay
            if (ShowContinueButton)
            {
                await Task.Delay(ContinueButtonDelayMs);

                if (ContinueButtonPanel != null)
                {
                    ContinueButtonPanel.Opacity = 1;
                    ContinueButtonPanel.RenderTransform = TransformOperations.Parse("translateY(0)");
                }
            }

            AnimationCompleted?.Invoke(this, EventArgs.Empty);
        });
    }
}
