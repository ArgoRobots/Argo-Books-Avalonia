using Avalonia.Controls;
using Avalonia.Platform.Storage;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Views;

/// <summary>
/// Code-behind for the Revenue page.
/// </summary>
public partial class RevenuePage : UserControl
{
    private static readonly FilePickerFileType ImageFileType = new("Images")
    {
        Patterns = ["*.jpg", "*.jpeg", "*.png"],
        MimeTypes = ["image/jpeg", "image/png"]
    };

    private static readonly FilePickerFileType PdfFileType = new("PDF Documents")
    {
        Patterns = ["*.pdf"],
        MimeTypes = ["application/pdf"]
    };

    private static readonly FilePickerFileType AllSupportedTypes = new("All Supported")
    {
        Patterns = ["*.jpg", "*.jpeg", "*.png", "*.pdf"],
        MimeTypes = ["image/jpeg", "image/png", "application/pdf"]
    };

    public RevenuePage()
    {
        InitializeComponent();
    }

    private void OnTableSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (DataContext is RevenuePageViewModel viewModel && e.WidthChanged)
        {
            viewModel.ColumnWidths.SetAvailableWidth(e.NewSize.Width);
        }
    }

    private void OnHeaderSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (DataContext is RevenuePageViewModel viewModel && e.WidthChanged)
        {
            viewModel.ResponsiveHeader.HeaderWidth = e.NewSize.Width;
        }
    }

    private async void OnAiScanButtonClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Receipt to Scan",
            AllowMultiple = false,
            FileTypeFilter = [AllSupportedTypes, ImageFileType, PdfFileType]
        });

        if (files.Count > 0)
        {
            var file = files[0];
            var path = file.TryGetLocalPath();
            if (!string.IsNullOrEmpty(path) && App.ReceiptsModalsViewModel != null)
            {
                await App.ReceiptsModalsViewModel.OpenScanModalAsync(path);
            }
        }
    }
}
