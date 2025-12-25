using Avalonia.Controls;
using Avalonia.Input;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Views;

public partial class ReceiptsPage : UserControl
{
    public ReceiptsPage()
    {
        InitializeComponent();
    }

    private void OnReceiptCardPressed(object? sender, PointerPressedEventArgs e)
    {
        // Ignore if clicking on checkbox
        if (e.Source is CheckBox) return;

        if (sender is Border border && border.DataContext is ReceiptDisplayItem receipt)
        {
            if (DataContext is ReceiptsPageViewModel viewModel)
            {
                viewModel.OpenPreviewCommand.Execute(receipt);
            }
        }
    }
}
