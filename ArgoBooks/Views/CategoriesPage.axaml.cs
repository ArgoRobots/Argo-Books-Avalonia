using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Views;

/// <summary>
/// Code-behind for the Categories page.
/// </summary>
public partial class CategoriesPage : UserControl
{
    public CategoriesPage()
    {
        InitializeComponent();
    }

    private CategoriesPageViewModel? ViewModel => DataContext as CategoriesPageViewModel;

    /// <summary>
    /// Handles the Expenses tab click.
    /// </summary>
    private void ExpensesTab_Click(object? sender, RoutedEventArgs e)
    {
        if (ViewModel != null)
        {
            ViewModel.SelectedTabIndex = 0;
        }
    }

    /// <summary>
    /// Handles the Revenue tab click.
    /// </summary>
    private void RevenueTab_Click(object? sender, RoutedEventArgs e)
    {
        if (ViewModel != null)
        {
            ViewModel.SelectedTabIndex = 1;
        }
    }

    /// <summary>
    /// Closes the Add modal when backdrop is clicked.
    /// </summary>
    private void Backdrop_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        ViewModel?.CloseAddModalCommand.Execute(null);
    }

    /// <summary>
    /// Closes the Edit modal when backdrop is clicked.
    /// </summary>
    private void EditBackdrop_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        ViewModel?.CloseEditModalCommand.Execute(null);
    }

    /// <summary>
    /// Closes the Delete confirmation when backdrop is clicked.
    /// </summary>
    private void DeleteBackdrop_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        ViewModel?.CloseDeleteConfirmCommand.Execute(null);
    }
}
