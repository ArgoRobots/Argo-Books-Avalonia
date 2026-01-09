using System.Windows.Input;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
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
        AvaloniaProperty.Register<SuccessAnimation, bool>(nameof(IsPlaying), false);

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
            // Reset circle
            if (SuccessCircle?.RenderTransform is ScaleTransform circleScale)
            {
                circleScale.ScaleX = 0;
                circleScale.ScaleY = 0;
            }

            // Reset glow
            if (SuccessGlow?.RenderTransform is ScaleTransform glowScale)
            {
                glowScale.ScaleX = 0;
                glowScale.ScaleY = 0;
            }

            // Reset text
            if (SuccessTextPanel != null)
            {
                SuccessTextPanel.Opacity = 0;
                if (SuccessTextPanel.RenderTransform is TranslateTransform textTranslate)
                {
                    textTranslate.Y = 20;
                }
            }

            // Reset continue button
            if (ContinueButtonPanel != null)
            {
                ContinueButtonPanel.Opacity = 0;
                if (ContinueButtonPanel.RenderTransform is TranslateTransform buttonTranslate)
                {
                    buttonTranslate.Y = 20;
                }
            }
        }, DispatcherPriority.Background);
    }

    private async Task PlayAnimationAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            // Animate the glow first
            if (SuccessGlow != null)
            {
                var glowAnimation = new Animation
                {
                    Duration = TimeSpan.FromMilliseconds(600),
                    Easing = new CubicEaseOut(),
                    FillMode = FillMode.Forward,
                    Children =
                    {
                        new KeyFrame
                        {
                            Cue = new Cue(0),
                            Setters =
                            {
                                new Setter(ScaleTransform.ScaleXProperty, 0.0),
                                new Setter(ScaleTransform.ScaleYProperty, 0.0)
                            }
                        },
                        new KeyFrame
                        {
                            Cue = new Cue(1),
                            Setters =
                            {
                                new Setter(ScaleTransform.ScaleXProperty, 1.15),
                                new Setter(ScaleTransform.ScaleYProperty, 1.15)
                            }
                        }
                    }
                };
                _ = glowAnimation.RunAsync(SuccessGlow);
            }

            // Animate the success circle with elastic bounce
            if (SuccessCircle != null)
            {
                var circleAnimation = new Animation
                {
                    Duration = TimeSpan.FromMilliseconds(600),
                    Easing = new ElasticEaseOut(),
                    FillMode = FillMode.Forward,
                    Children =
                    {
                        new KeyFrame
                        {
                            Cue = new Cue(0),
                            Setters =
                            {
                                new Setter(ScaleTransform.ScaleXProperty, 0.0),
                                new Setter(ScaleTransform.ScaleYProperty, 0.0)
                            }
                        },
                        new KeyFrame
                        {
                            Cue = new Cue(1),
                            Setters =
                            {
                                new Setter(ScaleTransform.ScaleXProperty, 1.0),
                                new Setter(ScaleTransform.ScaleYProperty, 1.0)
                            }
                        }
                    }
                };

                await circleAnimation.RunAsync(SuccessCircle);
            }

            // Animate text panel after circle
            if (SuccessTextPanel != null)
            {
                var textFadeAnimation = new Animation
                {
                    Duration = TimeSpan.FromMilliseconds(400),
                    Easing = new CubicEaseOut(),
                    FillMode = FillMode.Forward,
                    Children =
                    {
                        new KeyFrame
                        {
                            Cue = new Cue(0),
                            Setters =
                            {
                                new Setter(OpacityProperty, 0.0)
                            }
                        },
                        new KeyFrame
                        {
                            Cue = new Cue(1),
                            Setters =
                            {
                                new Setter(OpacityProperty, 1.0)
                            }
                        }
                    }
                };
                _ = textFadeAnimation.RunAsync(SuccessTextPanel);

                var textSlideAnimation = new Animation
                {
                    Duration = TimeSpan.FromMilliseconds(400),
                    Easing = new CubicEaseOut(),
                    FillMode = FillMode.Forward,
                    Children =
                    {
                        new KeyFrame
                        {
                            Cue = new Cue(0),
                            Setters =
                            {
                                new Setter(TranslateTransform.YProperty, 20.0)
                            }
                        },
                        new KeyFrame
                        {
                            Cue = new Cue(1),
                            Setters =
                            {
                                new Setter(TranslateTransform.YProperty, 0.0)
                            }
                        }
                    }
                };
                await textSlideAnimation.RunAsync(SuccessTextPanel);
            }

            // Show continue button after delay
            if (ShowContinueButton)
            {
                await Task.Delay(ContinueButtonDelayMs);
                await PlayContinueButtonAnimationAsync();
            }

            AnimationCompleted?.Invoke(this, EventArgs.Empty);
        });
    }

    private async Task PlayContinueButtonAnimationAsync()
    {
        if (ContinueButtonPanel != null)
        {
            var fadeAnimation = new Animation
            {
                Duration = TimeSpan.FromMilliseconds(400),
                Easing = new CubicEaseOut(),
                FillMode = FillMode.Forward,
                Children =
                {
                    new KeyFrame
                    {
                        Cue = new Cue(0),
                        Setters =
                        {
                            new Setter(OpacityProperty, 0.0)
                        }
                    },
                    new KeyFrame
                    {
                        Cue = new Cue(1),
                        Setters =
                        {
                            new Setter(OpacityProperty, 1.0)
                        }
                    }
                }
            };
            _ = fadeAnimation.RunAsync(ContinueButtonPanel);

            var slideAnimation = new Animation
            {
                Duration = TimeSpan.FromMilliseconds(400),
                Easing = new CubicEaseOut(),
                FillMode = FillMode.Forward,
                Children =
                {
                    new KeyFrame
                    {
                        Cue = new Cue(0),
                        Setters =
                        {
                            new Setter(TranslateTransform.YProperty, 20.0)
                        }
                    },
                    new KeyFrame
                    {
                        Cue = new Cue(1),
                        Setters =
                        {
                            new Setter(TranslateTransform.YProperty, 0.0)
                        }
                    }
                }
            };
            await slideAnimation.RunAsync(ContinueButtonPanel);
        }
    }
}
