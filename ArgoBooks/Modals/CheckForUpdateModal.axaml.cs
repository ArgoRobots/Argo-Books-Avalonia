using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

/// <summary>
/// Modal dialog for checking and installing application updates.
/// </summary>
public partial class CheckForUpdateModal : UserControl
{
    public CheckForUpdateModal()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is CheckForUpdateModalViewModel vm)
        {
            vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(vm.IsOpen) && vm.IsOpen)
                {
                    AnimateOpen();
                }
            };
        }
    }

    private void AnimateOpen()
    {
        if (this.FindControl<Border>("ModalBorder") is { } border)
        {
            border.Opacity = 1;
            border.RenderTransform = new Avalonia.Media.ScaleTransform(1, 1);
        }
    }

    private void Backdrop_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is CheckForUpdateModalViewModel vm)
        {
            vm.CloseCommand.Execute(null);
        }
    }

    private void Modal_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && DataContext is CheckForUpdateModalViewModel vm)
        {
            vm.CloseCommand.Execute(null);
            e.Handled = true;
        }
    }
}
