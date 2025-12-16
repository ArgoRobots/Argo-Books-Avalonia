using Avalonia.Controls;
using Avalonia.Input;
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

    /// <summary>
    /// Closes the Move modal when backdrop is clicked.
    /// </summary>
    private void MoveBackdrop_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        ViewModel?.CloseMoveModalCommand.Execute(null);
    }
}
