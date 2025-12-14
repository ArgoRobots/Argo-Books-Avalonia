using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
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

        // Focus the search input when the panel opens
        if (DataContext is QuickActionsViewModel vm)
        {
            vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(QuickActionsViewModel.IsOpen) && vm.IsOpen)
                {
                    // Use dispatcher to ensure visual tree is ready
                    Dispatcher.UIThread.Post(() =>
                    {
                        // Find and focus the search input (it's inside a DataTemplate)
                        var searchInput = FindDescendantOfType<TextBox>();
                        searchInput?.Focus();
                    }, DispatcherPriority.Background);
                }
            };
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
