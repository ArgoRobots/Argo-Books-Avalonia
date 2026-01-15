using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ArgoBooks.Helpers;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

/// <summary>
/// Modal dialog for editing company information and logo.
/// </summary>
public partial class EditCompanyModal : UserControl
{
    private bool _eventsSubscribed;

    public EditCompanyModal()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is EditCompanyModalViewModel vm && !_eventsSubscribed)
        {
            _eventsSubscribed = true;
            vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(EditCompanyModalViewModel.IsOpen))
                {
                    if (vm.IsOpen)
                        ModalAnimationHelper.AnimateIn(ModalBorder);
                    else
                        ModalAnimationHelper.AnimateOut(ModalBorder);
                }
            };
        }
    }

    private void Backdrop_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is EditCompanyModalViewModel viewModel)
        {
            viewModel.RequestClose();
        }
    }

    private void Modal_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && DataContext is EditCompanyModalViewModel viewModel)
        {
            viewModel.RequestClose();
            e.Handled = true;
        }
    }
}
