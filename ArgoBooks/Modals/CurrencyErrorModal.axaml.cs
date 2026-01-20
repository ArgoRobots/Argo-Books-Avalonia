using Avalonia.Controls;

namespace ArgoBooks.Modals;

/// <summary>
/// Modal dialog for currency change errors (e.g., no internet for exchange rates).
/// </summary>
public partial class CurrencyErrorModal : UserControl
{
    public CurrencyErrorModal()
    {
        InitializeComponent();
    }
}
