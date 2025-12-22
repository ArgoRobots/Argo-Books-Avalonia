using Avalonia.Controls;
using Avalonia.Input;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Views;

public partial class ExpensesPage : UserControl
{
    public ExpensesPage()
    {
        InitializeComponent();
    }

    private void OnTableHeaderPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
        {
            if (DataContext is ExpensesPageViewModel viewModel)
            {
                viewModel.IsColumnMenuOpen = true;
            }
        }
    }
}
