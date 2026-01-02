using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using System.ComponentModel;

namespace ArgoBooks.Modals;

public partial class CustomDateRangeModal : UserControl
{
    private bool _eventsSubscribed;

    public CustomDateRangeModal()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is INotifyPropertyChanged vm && !_eventsSubscribed)
        {
            _eventsSubscribed = true;

            vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == "IsCustomDateRangeModalOpen")
                {
                    var isOpenProperty = DataContext?.GetType().GetProperty("IsCustomDateRangeModalOpen");
                    var isOpen = isOpenProperty?.GetValue(DataContext) as bool? ?? false;

                    if (isOpen)
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
                        // Reset for next open
                        Dispatcher.UIThread.Post(() =>
                        {
                            if (DialogBorder != null)
                            {
                                DialogBorder.Opacity = 0;
                                DialogBorder.RenderTransform = new ScaleTransform(0.95, 0.95);
                            }
                        }, DispatcherPriority.Background);
                    }
                }
            };
        }
    }

    private void Backdrop_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // Execute cancel command via reflection
        var cancelCommand = DataContext?.GetType().GetProperty("CancelCustomDateRangeCommand")?.GetValue(DataContext);
        if (cancelCommand is System.Windows.Input.ICommand command)
        {
            command.Execute(null);
        }
    }

    private void Dialog_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            var cancelCommand = DataContext?.GetType().GetProperty("CancelCustomDateRangeCommand")?.GetValue(DataContext);
            if (cancelCommand is System.Windows.Input.ICommand command)
            {
                command.Execute(null);
            }
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            var applyCommand = DataContext?.GetType().GetProperty("ApplyCustomDateRangeCommand")?.GetValue(DataContext);
            if (applyCommand is System.Windows.Input.ICommand command)
            {
                command.Execute(null);
            }
            e.Handled = true;
        }
    }
}
