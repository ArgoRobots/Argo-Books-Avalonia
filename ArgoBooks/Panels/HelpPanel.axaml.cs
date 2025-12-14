using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Panels;

public partial class HelpPanel : UserControl
{
    private int _focusedIndex = -1;
    private readonly int _menuItemCount = 5; // Total menu items (0-4)

    public HelpPanel()
    {
        InitializeComponent();

        // Focus the panel when it opens
        DataContextChanged += (_, _) =>
        {
            if (DataContext is HelpPanelViewModel vm)
            {
                vm.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(HelpPanelViewModel.IsOpen) && vm.IsOpen)
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            _focusedIndex = -1;
                            HelpPanelBorder?.Focus();
                        }, DispatcherPriority.Background);
                    }
                };
            }
        };
    }

    private void Backdrop_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is HelpPanelViewModel vm)
        {
            vm.CloseCommand.Execute(null);
        }
    }

    private void HelpPanel_KeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not HelpPanelViewModel vm) return;

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
        if (_focusedIndex <= 0)
            _focusedIndex = _menuItemCount - 1;
        else
            _focusedIndex--;
        FocusMenuItem(_focusedIndex);
    }

    private void NavigateDown()
    {
        if (_focusedIndex >= _menuItemCount - 1)
            _focusedIndex = 0;
        else
            _focusedIndex++;
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
