using Avalonia.Controls;

namespace ArgoBooks.Controls;

/// <summary>
/// Inline notice explaining that a tab's controls are inactive because the
/// sample company is open. Callers set <c>IsVisible="{Binding IsSampleCompany}"</c>,
/// keeping the control free of any view model dependency.
/// </summary>
public partial class SampleCompanyNotice : UserControl
{
    public SampleCompanyNotice()
    {
        InitializeComponent();
    }
}
