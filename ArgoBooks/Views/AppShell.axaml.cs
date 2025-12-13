using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Views;

/// <summary>
/// The main application shell containing sidebar and content area.
/// </summary>
public partial class AppShell : UserControl
{
    public AppShell()
    {
        InitializeComponent();
    }

    /// <inheritdoc />
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        // Focus the shell to receive keyboard events
        Focus();
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        // Handle Ctrl+K to open quick actions panel
        if (e.Key == Key.K && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            if (DataContext is AppShellViewModel vm)
            {
                vm.OpenQuickActionsCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}
