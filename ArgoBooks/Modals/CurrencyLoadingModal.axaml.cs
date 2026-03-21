using Avalonia.Controls;

namespace ArgoBooks.Modals;

/// <summary>
/// Modal dialog showing progress while fetching exchange rates during currency change.
/// </summary>
public partial class CurrencyLoadingModal : UserControl
{
    public CurrencyLoadingModal()
    {
        InitializeComponent();
    }
}
