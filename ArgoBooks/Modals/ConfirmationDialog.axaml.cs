using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

/// <summary>
/// Reusable confirmation dialog for user prompts and decisions.
/// </summary>
public partial class ConfirmationDialog : UserControl
{
    private bool _eventsSubscribed;

    public ConfirmationDialog()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is ConfirmationDialogViewModel vm && !_eventsSubscribed)
        {
            _eventsSubscribed = true;

            vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(ConfirmationDialogViewModel.IsOpen))
                {
                    if (vm.IsOpen)
                    {
                        // Animate in
                        Dispatcher.UIThread.Post(() =>
                        {
                            if (DialogBorder != null)
                            {
                                DialogBorder.Opacity = 1;
                                DialogBorder.RenderTransform = new ScaleTransform(1, 1);
                            }
                            DialogBorder?.Focus();
                        }, DispatcherPriority.Render);
                    }
                    else
                    {
                        // Reset for next open and return focus to AppShell
                        Dispatcher.UIThread.Post(() =>
                        {
                            if (DialogBorder != null)
                            {
                                DialogBorder.Opacity = 0;
                                DialogBorder.RenderTransform = new ScaleTransform(0.95, 0.95);
                            }

                            // Return focus to AppShell so Ctrl+K works again
                            var topLevel = TopLevel.GetTopLevel(this);
                            if (topLevel != null)
                            {
                                var appShell = topLevel.GetVisualDescendants()
                                    .OfType<UserControl>()
                                    .FirstOrDefault(x => x.GetType().Name == "AppShell");
                                appShell?.Focus();
                            }
                        }, DispatcherPriority.Background);
                    }
                }
            };
        }
    }

    private void Backdrop_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is ConfirmationDialogViewModel vm)
        {
            vm.Close();
        }
    }

    private void Dialog_KeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is ConfirmationDialogViewModel vm)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    vm.CancelActionCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.Enter:
                    vm.PrimaryActionCommand.Execute(null);
                    e.Handled = true;
                    break;
            }
        }
    }
}
