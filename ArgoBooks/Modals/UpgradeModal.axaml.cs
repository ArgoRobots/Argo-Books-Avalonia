using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

public partial class UpgradeModal : UserControl
{
    public UpgradeModal()
    {
        InitializeComponent();

        // Animate the modal when it opens
        DataContextChanged += (_, _) =>
        {
            if (DataContext is UpgradeModalViewModel vm)
            {
                vm.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(UpgradeModalViewModel.IsOpen))
                    {
                        if (vm.IsOpen)
                        {
                            Dispatcher.UIThread.Post(() =>
                            {
                                if (ModalBorder != null)
                                {
                                    ModalBorder.Opacity = 1;
                                    ModalBorder.RenderTransform = new ScaleTransform(1, 1);
                                }
                            }, DispatcherPriority.Render);
                        }
                        else
                        {
                            Dispatcher.UIThread.Post(() =>
                            {
                                if (ModalBorder != null)
                                {
                                    ModalBorder.Opacity = 0;
                                    ModalBorder.RenderTransform = new ScaleTransform(0.95, 0.95);
                                }
                            }, DispatcherPriority.Background);
                        }
                    }
                    else if (e.PropertyName == nameof(UpgradeModalViewModel.IsVerificationSuccess))
                    {
                        if (vm.IsVerificationSuccess)
                        {
                            PlaySuccessAnimation();
                        }
                        else
                        {
                            ResetSuccessAnimation();
                        }
                    }
                };
            }
        };
    }

    private void ResetSuccessAnimation()
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
        }, DispatcherPriority.Background);
    }

    private async void PlaySuccessAnimation()
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
                                new Setter(ScaleTransform.ScaleXProperty, 1.5),
                                new Setter(ScaleTransform.ScaleYProperty, 1.5)
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

                await circleAnimation.RunAsync(SuccessCircle);  // Run on control
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
                                new Setter(Visual.OpacityProperty, 0.0)
                            }
                        },
                        new KeyFrame
                        {
                            Cue = new Cue(1),
                            Setters =
                            {
                                new Setter(Visual.OpacityProperty, 1.0)
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
                      await textSlideAnimation.RunAsync(SuccessTextPanel);  // Run on control
            }
        });
    }

    private void Backdrop_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is UpgradeModalViewModel vm)
        {
            vm.CloseCommand.Execute(null);
        }
    }

    private void EnterKeyBackdrop_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is UpgradeModalViewModel vm)
        {
            vm.CloseEnterKeyCommand.Execute(null);
        }
    }

    private bool _isFormatting;

    private void LicenseKeyTextBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_isFormatting || sender is not TextBox textBox)
            return;

        _isFormatting = true;
        try
        {
            var text = textBox.Text ?? string.Empty;
            var caretIndex = textBox.CaretIndex;

            // Count digits before cursor in original text
            var digitsBeforeCaret = text.Take(caretIndex).Count(char.IsLetterOrDigit);

            // Remove all non-alphanumeric characters and convert to uppercase
            var digitsOnly = new string(text.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();

            // Limit to 20 characters
            if (digitsOnly.Length > 20)
                digitsOnly = digitsOnly[..20];

            // Insert dashes every 4 characters
            var formatted = new System.Text.StringBuilder();
            for (int i = 0; i < digitsOnly.Length; i++)
            {
                if (i > 0 && i % 4 == 0)
                    formatted.Append('-');
                formatted.Append(digitsOnly[i]);
            }

            var formattedText = formatted.ToString();

            // Only update if different to avoid infinite loop
            if (text != formattedText)
            {
                textBox.Text = formattedText;

                // Calculate new caret position based on digits before caret
                var newCaretIndex = 0;
                var digitCount = 0;
                for (int i = 0; i < formattedText.Length && digitCount < digitsBeforeCaret; i++)
                {
                    newCaretIndex = i + 1;
                    if (char.IsLetterOrDigit(formattedText[i]))
                        digitCount++;
                }

                textBox.CaretIndex = Math.Min(newCaretIndex, formattedText.Length);
            }
        }
        finally
        {
            _isFormatting = false;
        }
    }
}
