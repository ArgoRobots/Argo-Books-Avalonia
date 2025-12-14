using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Panels;

public partial class FileMenuPanel : UserControl
{
    private int _focusedIndex = 0;
    private readonly int _menuItemCount = 9; // Total menu items (0-8)
    private bool _isOverSubmenu;
    private bool _isOverRecentButton;

    public FileMenuPanel()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Closes the panel when clicking on the backdrop.
    /// </summary>
    private void Backdrop_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is FileMenuPanelViewModel vm)
        {
            vm.IsRecentSubmenuOpen = false;
            vm.CloseCommand.Execute(null);
        }
    }

    /// <summary>
    /// Shows the recent submenu on hover.
    /// </summary>
    private void OpenRecent_PointerEntered(object? sender, PointerEventArgs e)
    {
        _isOverRecentButton = true;
        if (DataContext is FileMenuPanelViewModel vm)
        {
            vm.IsRecentSubmenuOpen = true;
        }
    }

    /// <summary>
    /// Hides the recent submenu when leaving with delay.
    /// </summary>
    private void OpenRecent_PointerExited(object? sender, PointerEventArgs e)
    {
        _isOverRecentButton = false;
        DelayedCloseSubmenu();
    }

    /// <summary>
    /// Keeps the recent submenu open when hovering over it.
    /// </summary>
    private void RecentSubmenu_PointerEntered(object? sender, PointerEventArgs e)
    {
        _isOverSubmenu = true;
    }

    /// <summary>
    /// Hides the recent submenu when leaving.
    /// </summary>
    private void RecentSubmenu_PointerExited(object? sender, PointerEventArgs e)
    {
        _isOverSubmenu = false;
        DelayedCloseSubmenu();
    }

    private async void DelayedCloseSubmenu()
    {
        await Task.Delay(100);
        if (!_isOverSubmenu && !_isOverRecentButton)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (DataContext is FileMenuPanelViewModel vm)
                {
                    vm.IsRecentSubmenuOpen = false;
                }
            });
        }
    }

    /// <summary>
    /// Handles keyboard navigation in the file menu.
    /// </summary>
    private void FileMenu_KeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not FileMenuPanelViewModel vm) return;

        switch (e.Key)
        {
            case Key.Up:
                NavigateUp();
                e.Handled = true;
                break;
            case Key.Down:
                NavigateDown();
                e.Handled = true;
                break;
            case Key.Enter:
            case Key.Space:
                ActivateCurrentItem();
                e.Handled = true;
                break;
            case Key.Escape:
                vm.IsRecentSubmenuOpen = false;
                vm.CloseCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.Right:
                if (_focusedIndex == 2) // Open Recent
                {
                    vm.IsRecentSubmenuOpen = true;
                }
                e.Handled = true;
                break;
            case Key.Left:
                vm.IsRecentSubmenuOpen = false;
                e.Handled = true;
                break;
        }
    }

    private void NavigateUp()
    {
        _focusedIndex = (_focusedIndex - 1 + _menuItemCount) % _menuItemCount;
        FocusMenuItem(_focusedIndex);
    }

    private void NavigateDown()
    {
        _focusedIndex = (_focusedIndex + 1) % _menuItemCount;
        FocusMenuItem(_focusedIndex);
    }

    private void FocusMenuItem(int index)
    {
        var menuItem = this.FindControl<Button>($"MenuItem{index}");
        menuItem?.Focus();
    }

    private void ActivateCurrentItem()
    {
        var menuItem = this.FindControl<Button>($"MenuItem{_focusedIndex}");
        if (menuItem?.Command != null && menuItem.Command.CanExecute(menuItem.CommandParameter))
        {
            menuItem.Command.Execute(menuItem.CommandParameter);
        }
    }
}
