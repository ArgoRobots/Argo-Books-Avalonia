using Avalonia.Controls;
using Avalonia.Input;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Views;

public partial class CustomersPage : UserControl
{
    public CustomersPage()
    {
        InitializeComponent();
    }

    private void Backdrop_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is CustomersPageViewModel vm)
            vm.CloseAddModalCommand.Execute(null);
    }

    private void EditBackdrop_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is CustomersPageViewModel vm)
            vm.CloseEditModalCommand.Execute(null);
    }

    private void DeleteBackdrop_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is CustomersPageViewModel vm)
            vm.CloseDeleteConfirmCommand.Execute(null);
    }

    private void FilterBackdrop_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is CustomersPageViewModel vm)
            vm.CloseFilterModalCommand.Execute(null);
    }

    private void HistoryBackdrop_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is CustomersPageViewModel vm)
            vm.CloseHistoryModalCommand.Execute(null);
    }

    private void HistoryFilterBackdrop_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is CustomersPageViewModel vm)
            vm.CloseHistoryFilterModalCommand.Execute(null);
    }
}
