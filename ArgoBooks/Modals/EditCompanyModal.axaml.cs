using Avalonia.Controls;
using Avalonia.Input;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

public partial class EditCompanyModal : UserControl
{
    public EditCompanyModal()
    {
        InitializeComponent();
    }

    private void Backdrop_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is EditCompanyModalViewModel viewModel)
        {
            viewModel.RequestClose();
        }
    }
}
