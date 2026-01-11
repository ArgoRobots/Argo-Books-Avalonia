using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Controls;

/// <summary>
/// Dropdown panel displaying undo/redo action history with selection support.
/// </summary>
public partial class UndoRedoHistoryPanel : UserControl
{
    public UndoRedoHistoryPanel()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is UndoRedoHistoryPanelViewModel vm)
        {
            vm.PropertyChanged += (s, args) =>
            {
                if (args.PropertyName == nameof(vm.IsOpen) && vm.IsOpen)
                {
                    AnimateOpen();
                }
            };
        }
    }

    private void AnimateOpen()
    {
        if (this.FindControl<Border>("DropdownBorder") is { } border)
        {
            border.Opacity = 1;
            border.RenderTransform = new Avalonia.Media.TranslateTransform(0, 0);
        }
    }

    private void Backdrop_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is UndoRedoHistoryPanelViewModel vm)
        {
            vm.CloseCommand.Execute(null);
        }
    }

    private void HistoryItem_PointerEntered(object? sender, PointerEventArgs e)
    {
        if (sender is Button { DataContext: HistoryItemViewModel item })
        {
            if (DataContext is UndoRedoHistoryPanelViewModel vm)
            {
                vm.HighlightUpTo(item);
            }
        }
    }
}
