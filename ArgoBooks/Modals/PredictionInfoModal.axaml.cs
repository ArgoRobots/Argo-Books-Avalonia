using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

/// <summary>
/// Modal dialog explaining how predictions and confidence scores are calculated.
/// </summary>
public partial class PredictionInfoModal : UserControl
{
    public PredictionInfoModal()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is InsightsPageViewModel vm)
        {
            vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(vm.IsInfoModalVisible) && vm.IsInfoModalVisible)
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
        if (DataContext is InsightsPageViewModel vm)
        {
            vm.CloseInfoModalCommand.Execute(null);
        }
    }

    private void Modal_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && DataContext is InsightsPageViewModel vm)
        {
            vm.CloseInfoModalCommand.Execute(null);
            e.Handled = true;
        }
    }
}
