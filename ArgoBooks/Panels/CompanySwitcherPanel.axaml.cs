using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Panels;

public partial class CompanySwitcherPanel : UserControl
{
    public CompanySwitcherPanel()
    {
        InitializeComponent();

        // Focus the panel when it opens
        DataContextChanged += (_, _) =>
        {
            if (DataContext is CompanySwitcherPanelViewModel vm)
            {
                vm.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(CompanySwitcherPanelViewModel.IsOpen) && vm.IsOpen)
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            SwitcherBorder?.Focus();
                        }, DispatcherPriority.Background);
                    }
                };
            }
        };
    }

    /// <summary>
    /// Closes the panel when clicking on the backdrop.
    /// </summary>
    private void Backdrop_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is CompanySwitcherPanelViewModel vm)
        {
            vm.CloseCommand.Execute(null);
        }
    }

    /// <summary>
    /// Handles keyboard navigation in the panel.
    /// </summary>
    private void Panel_KeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not CompanySwitcherPanelViewModel vm) return;

        switch (e.Key)
        {
            case Key.Escape:
                vm.CloseCommand.Execute(null);
                e.Handled = true;
                break;
        }
    }
}
