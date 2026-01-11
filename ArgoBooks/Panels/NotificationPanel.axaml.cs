using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Panels;

/// <summary>
/// Dropdown panel displaying application notifications and alerts.
/// </summary>
public partial class NotificationPanel : UserControl
{
    public NotificationPanel()
    {
        InitializeComponent();

        // Animate the panel when it opens
        DataContextChanged += (_, _) =>
        {
            if (DataContext is NotificationPanelViewModel vm)
            {
                vm.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(NotificationPanelViewModel.IsOpen))
                    {
                        if (vm.IsOpen)
                        {
                            Dispatcher.UIThread.Post(() =>
                            {
                                if (NotificationBorder != null)
                                {
                                    NotificationBorder.Opacity = 1;
                                    NotificationBorder.RenderTransform = new TranslateTransform(0, 0);
                                }
                            }, DispatcherPriority.Render);
                        }
                        else
                        {
                            Dispatcher.UIThread.Post(() =>
                            {
                                if (NotificationBorder != null)
                                {
                                    NotificationBorder.Opacity = 0;
                                    NotificationBorder.RenderTransform = new TranslateTransform(0, -8);
                                }
                            }, DispatcherPriority.Background);
                        }
                    }
                };
            }
        };
    }

    /// <summary>
    /// Closes the panel when clicking on the backdrop.
    /// </summary>
    private void Backdrop_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is NotificationPanelViewModel vm)
        {
            vm.CloseCommand.Execute(null);
        }
    }
}
