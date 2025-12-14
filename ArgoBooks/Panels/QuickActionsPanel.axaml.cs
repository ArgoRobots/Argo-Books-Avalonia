using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Panels;

public partial class QuickActionsPanel : UserControl
{
    public QuickActionsPanel()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Closes the panel when clicking on the backdrop.
    /// </summary>
    private void Backdrop_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is QuickActionsViewModel vm)
        {
            vm.CloseCommand.Execute(null);
        }
    }

    /// <inheritdoc />
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is QuickActionsViewModel vm)
        {
            vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(QuickActionsViewModel.IsOpen))
                {
                    if (vm.IsOpen)
                    {
                        // Animate in
                        Dispatcher.UIThread.Post(() =>
                        {
                            if (vm.IsDropdownMode)
                            {
                                if (DropdownBorder != null)
                                {
                                    DropdownBorder.Opacity = 1;
                                    DropdownBorder.RenderTransform = new TranslateTransform(0, 0);
                                }
                            }
                            else
                            {
                                if (ModalBorder != null)
                                {
                                    ModalBorder.Opacity = 1;
                                    ModalBorder.RenderTransform = new ScaleTransform(1, 1);
                                }
                                var searchInput = FindDescendantOfType<TextBox>();
                                searchInput?.Focus();
                            }
                        }, DispatcherPriority.Render);
                    }
                    else
                    {
                        // Reset for next open
                        Dispatcher.UIThread.Post(() =>
                        {
                            if (DropdownBorder != null)
                            {
                                DropdownBorder.Opacity = 0;
                                DropdownBorder.RenderTransform = new TranslateTransform(0, -8);
                            }
                            if (ModalBorder != null)
                            {
                                ModalBorder.Opacity = 0;
                                ModalBorder.RenderTransform = new ScaleTransform(0.95, 0.95);
                            }
                        }, DispatcherPriority.Background);
                    }
                }
                else if (args.PropertyName == nameof(QuickActionsViewModel.SelectedIndex))
                {
                    // Focus the selected action item
                    Dispatcher.UIThread.Post(() => FocusSelectedItem(vm.SelectedIndex), DispatcherPriority.Background);
                }
            };
        }
    }

    /// <summary>
    /// Focuses the button at the specified index.
    /// </summary>
    private void FocusSelectedItem(int index)
    {
        if (index < 0) return;

        var buttons = this.GetVisualDescendants()
            .OfType<Button>()
            .Where(b => b.Classes.Contains("action-item"))
            .ToList();

        if (index < buttons.Count)
        {
            buttons[index].Focus();
        }
    }

    /// <summary>
    /// Finds the first descendant of a specific type in the visual tree.
    /// </summary>
    private T? FindDescendantOfType<T>() where T : class
    {
        foreach (var descendant in this.GetVisualDescendants())
        {
            if (descendant is T typed)
                return typed;
        }
        return null;
    }
}
