using System.ComponentModel;
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
    private NotificationPanelViewModel? _previousVm;
    private PropertyChangedEventHandler? _propertyChangedHandler;

    public NotificationPanel()
    {
        InitializeComponent();

        // Animate the panel when it opens
        DataContextChanged += (_, _) =>
        {
            // Unsubscribe from previous ViewModel to prevent leak
            if (_previousVm != null && _propertyChangedHandler != null)
            {
                _previousVm.PropertyChanged -= _propertyChangedHandler;
            }

            if (DataContext is NotificationPanelViewModel vm)
            {
                _previousVm = vm;
                _propertyChangedHandler = (_, e) =>
                {
                    if (e.PropertyName == nameof(NotificationPanelViewModel.IsOpen))
                    {
                        if (vm.IsOpen)
                        {
                            Dispatcher.UIThread.Post(() =>
                            {
                                NotificationBorder.Opacity = 1;
                                NotificationBorder.RenderTransform = new TranslateTransform(0, 0);
                            }, DispatcherPriority.Render);
                        }
                        else
                        {
                            Dispatcher.UIThread.Post(() =>
                            {
                                NotificationBorder.Opacity = 0;
                                NotificationBorder.RenderTransform = new TranslateTransform(0, -8);
                            }, DispatcherPriority.Background);
                        }
                    }
                };
                vm.PropertyChanged += _propertyChangedHandler;
            }
            else
            {
                _previousVm = null;
                _propertyChangedHandler = null;
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
