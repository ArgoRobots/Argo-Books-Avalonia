using Avalonia.Controls;
using Avalonia.Platform.Storage;
using ArgoBooks.Utilities;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Views;

/// <summary>
/// Code-behind for the Expenses page.
/// </summary>
public partial class ExpensesPage : UserControl
{
    public ExpensesPage()
    {
        InitializeComponent();
    }

    private void OnHeaderSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (DataContext is ExpensesPageViewModel viewModel && e.WidthChanged)
        {
            viewModel.ResponsiveHeader.HeaderWidth = e.NewSize.Width;
        }
    }

    private void OnTableSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (DataContext is ExpensesPageViewModel viewModel && e.WidthChanged)
        {
            viewModel.ColumnWidths.SetAvailableWidth(e.NewSize.Width);
        }
    }

    private async void OnAiScanButtonClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        try
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select Receipt to Scan",
                AllowMultiple = false,
                FileTypeFilter = [FilePickerTypes.AllSupportedTypes, FilePickerTypes.ImageFileType, FilePickerTypes.PdfFileType]
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Unhandled exception in OnAiScanButtonClick: {ex}");
        }
    }
}
