using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ArgoBooks.Utilities;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

/// <summary>
/// Modal dialog for exporting data to various file formats.
/// </summary>
public partial class ExportAsModal : UserControl
{
    public ExportAsModal()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is ExportAsModalViewModel vm)
        {
            vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(vm.IsOpen))
                {
                    if (vm.IsOpen)
                        AnimateOpen();
                    else
                        ModalHelper.ReturnFocusToAppShell(this);
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

    private void Modal_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && DataContext is ExportAsModalViewModel vm)
        {
            vm.CloseCommand.Execute(null);
            e.Handled = true;
        }
    }
}
