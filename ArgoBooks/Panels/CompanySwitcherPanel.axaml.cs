using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Panels;

/// <summary>
/// Dropdown panel for switching between recent company files.
/// </summary>
public partial class CompanySwitcherPanel : UserControl
{
    public CompanySwitcherPanel()
    {
        InitializeComponent();

        // Animate and focus the panel when it opens
        DataContextChanged += (_, _) =>
        {
            if (DataContext is CompanySwitcherPanelViewModel vm)
            {
                vm.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(CompanySwitcherPanelViewModel.IsOpen))
                    {
                        if (vm.IsOpen)
                        {
                            // Animate in
                            Dispatcher.UIThread.Post(() =>
                            {
                                if (SwitcherBorder != null)
                                {
                                    SwitcherBorder.Opacity = 1;
                                    SwitcherBorder.RenderTransform = new TranslateTransform(0, 0);
                                }
                                SwitcherBorder?.Focus();
                            }, DispatcherPriority.Render);
                        }
                        else
                        {
                            // Reset for next open
                            Dispatcher.UIThread.Post(() =>
                            {
                                if (SwitcherBorder != null)
                                {
                                    SwitcherBorder.Opacity = 0;
                                    SwitcherBorder.RenderTransform = new TranslateTransform(0, -8);
                                }
                            }, DispatcherPriority.Background);
                        }
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
