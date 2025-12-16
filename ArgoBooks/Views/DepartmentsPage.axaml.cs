using Avalonia.Controls;
using Avalonia.Input;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Views;

/// <summary>
/// Code-behind for the Departments page.
/// </summary>
public partial class DepartmentsPage : UserControl
{
    public DepartmentsPage()
    {
        InitializeComponent();
    }

    private DepartmentsPageViewModel? ViewModel => DataContext as DepartmentsPageViewModel;

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
    /// Prevents modal content clicks from closing the modal.
    /// </summary>
    private void Modal_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        e.Handled = true;
    }
}
