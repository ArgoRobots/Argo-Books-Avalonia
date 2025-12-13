using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
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
                    // Focus the search input
                    SearchInput?.Focus();
                }
            };
        }
    }
}
