using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

public partial class UnsavedChangesDialog : UserControl
{
    private bool _eventsSubscribed;

    public UnsavedChangesDialog()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is UnsavedChangesDialogViewModel vm && !_eventsSubscribed)
        {
            _eventsSubscribed = true;

            vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(UnsavedChangesDialogViewModel.IsOpen))
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
        // Don't close on backdrop click for unsaved changes - user must make a choice
        // This prevents accidental data loss
    }

    private void Dialog_KeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is UnsavedChangesDialogViewModel vm)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    vm.CancelCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.Enter:
                    vm.SaveCommand.Execute(null);
                    e.Handled = true;
                    break;
            }
        }
    }
}
