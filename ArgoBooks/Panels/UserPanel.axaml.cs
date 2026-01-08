using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Panels;

public partial class UserPanel : UserControl
{
    private int _focusedIndex = -1;
    // Menu item names in visual order
    private readonly string[] _menuItemNames = ["MenuItem0", "MenuItemPlan", "MenuItem1", "MenuItem2", "MenuItem3"];

    public UserPanel()
    {
        InitializeComponent();

        // Animate and focus the panel when it opens
        DataContextChanged += (_, _) =>
        {
            if (DataContext is UserPanelViewModel vm)
            {
                vm.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(UserPanelViewModel.IsOpen))
                    {
                        if (vm.IsOpen)
                        {
                            Dispatcher.UIThread.Post(() =>
                            {
                                if (UserPanelBorder != null)
                                {
                                    UserPanelBorder.Opacity = 1;
                                    UserPanelBorder.RenderTransform = new TranslateTransform(0, 0);
                                }
                                _focusedIndex = -1;
                                UserPanelBorder?.Focus();
                            }, DispatcherPriority.Render);
                        }
                        else
                        {
                            Dispatcher.UIThread.Post(() =>
                            {
                                if (UserPanelBorder != null)
                                {
                                    UserPanelBorder.Opacity = 0;
                                    UserPanelBorder.RenderTransform = new TranslateTransform(0, -8);
                                }
                            }, DispatcherPriority.Background);
                        }
                    }
                };
            }
        };
    }

    private void Backdrop_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is UserPanelViewModel vm)
        {
            vm.CloseCommand.Execute(null);
        }
    }

    private void UserPanel_KeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not UserPanelViewModel vm) return;

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
                vm.CloseCommand.Execute(null);
                e.Handled = true;
                break;
        }
    }

    private void NavigateUp()
    {
        var visibleItems = GetVisibleMenuItems();
        if (visibleItems.Count == 0) return;

        if (_focusedIndex <= 0)
            _focusedIndex = visibleItems.Count - 1;
        else
            _focusedIndex--;
        FocusMenuItem(visibleItems[_focusedIndex]);
    }

    private void NavigateDown()
    {
        var visibleItems = GetVisibleMenuItems();
        if (visibleItems.Count == 0) return;

        if (_focusedIndex >= visibleItems.Count - 1)
            _focusedIndex = 0;
        else
            _focusedIndex++;
        FocusMenuItem(visibleItems[_focusedIndex]);
    }

    private List<Button> GetVisibleMenuItems()
    {
        var items = new List<Button>();
        foreach (var name in _menuItemNames)
        {
            var menuItem = this.FindControl<Button>(name);
            if (menuItem?.IsVisible == true)
                items.Add(menuItem);
        }
        return items;
    }

    private void FocusMenuItem(Button? menuItem)
    {
        menuItem?.Focus();
    }

    private void ActivateCurrentItem()
    {
        var visibleItems = GetVisibleMenuItems();
        if (_focusedIndex < 0 || _focusedIndex >= visibleItems.Count) return;

        var menuItem = visibleItems[_focusedIndex];
        if (menuItem.Command != null && menuItem.Command.CanExecute(menuItem.CommandParameter))
        {
            menuItem.Command.Execute(menuItem.CommandParameter);
        }
    }
}
