using ArgoBooks.ViewModels;
using Avalonia.Controls;

namespace ArgoBooks.Views;

public partial class BankMatchingPage : UserControl
{
    public BankMatchingPage()
    {
        InitializeComponent();
    }

    private void OnHeaderSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (DataContext is BankMatchingPageViewModel vm && e.WidthChanged)
            vm.ResponsiveHeader.HeaderWidth = e.NewSize.Width;
    }

    private void OnBankLinesTableSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (DataContext is BankMatchingPageViewModel vm && e.WidthChanged)
            vm.BankLineColumns.SetAvailableWidth(e.NewSize.Width);
    }

    private void OnMissingTableSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (DataContext is BankMatchingPageViewModel vm && e.WidthChanged)
            vm.MissingColumns.SetAvailableWidth(e.NewSize.Width);
    }
}
