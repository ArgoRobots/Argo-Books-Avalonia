using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ArgoBooks.Views;

/// <summary>
/// Full-page invoice editor host. Reuses <see cref="ViewModels.InvoiceModalsViewModel"/> for all
/// invoice logic; the page is prepared via <c>PrepareForEditor</c> in the navigation factory.
/// </summary>
public partial class InvoiceEditorPage : UserControl
{
    public InvoiceEditorPage()
    {
        InitializeComponent();
    }

    private void OnBackClick(object? sender, RoutedEventArgs e)
    {
        App.NavigationService?.GoBack();
    }
}
