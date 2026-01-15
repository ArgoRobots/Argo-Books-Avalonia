using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Views;

/// <summary>
/// Code-behind for the Receipts page.
/// </summary>
public partial class ReceiptsPage : UserControl
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

    private static readonly FilePickerFileType AllSupportedTypes = new("Receipt Files")
    {
        Patterns = ["*.jpg", "*.jpeg", "*.png", "*.pdf"],
        MimeTypes = ["image/jpeg", "image/png", "application/pdf"]
    };

    public ReceiptsPage()
    {
        InitializeComponent();

        // Subscribe to drag-drop events
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);
        AddHandler(DragDrop.DropEvent, OnDrop);
    }

    private void OnReceiptCardPressed(object? sender, PointerPressedEventArgs e)
    {
        // Ignore if clicking on checkbox
        if (e.Source is CheckBox) return;

        if (sender is Border { DataContext: ReceiptDisplayItem receipt })
        {
            if (DataContext is ReceiptsPageViewModel viewModel)
            {
                viewModel.OpenPreviewCommand.Execute(receipt);
            }
        }
    }

    private void OnTableHeaderPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
        {
            if (DataContext is ReceiptsPageViewModel viewModel)
            {
                var position = e.GetPosition(this);
                viewModel.ColumnMenuX = position.X;
                viewModel.ColumnMenuY = position.Y;
                viewModel.IsColumnMenuOpen = true;
            }
        }
    }

    private void OnTableSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (DataContext is ReceiptsPageViewModel viewModel && e.WidthChanged)
        {
            viewModel.ColumnWidths.SetAvailableWidth(e.NewSize.Width);
        }
    }

    private async void OnAiScanButtonClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var viewModel = DataContext as ReceiptsPageViewModel;
        if (viewModel == null) return;

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
            if (!string.IsNullOrEmpty(path))
            {
                await viewModel.HandleFileSelectedAsync(path);
            }
        }
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        if (DataContext is not ReceiptsPageViewModel viewModel) return;

        // Check if the data contains files
        if (e.DataTransfer.Contains(DataFormat.File))
        {
            var files = e.DataTransfer.Get<IEnumerable<IStorageItem>>(DataFormat.File);
            if (files != null)
            {
                foreach (var file in files)
                {
                    var path = file.TryGetLocalPath();
                    if (!string.IsNullOrEmpty(path))
                    {
                        var extension = Path.GetExtension(path).ToLowerInvariant();
                        if (extension is ".jpg" or ".jpeg" or ".png" or ".pdf")
                        {
                            e.DragEffects = DragDropEffects.Copy;
                            viewModel.IsDragOver = true;
                            return;
                        }
                    }
                }
            }
        }

        e.DragEffects = DragDropEffects.None;
        viewModel.IsDragOver = false;
    }

    private void OnDragLeave(object? sender, DragEventArgs e)
    {
        if (DataContext is ReceiptsPageViewModel viewModel)
        {
            viewModel.IsDragOver = false;
        }
    }

    private async void OnDrop(object? sender, DragEventArgs e)
    {
        if (DataContext is not ReceiptsPageViewModel viewModel) return;

        viewModel.IsDragOver = false;

        if (e.DataTransfer.Contains(DataFormat.File))
        {
            var files = e.DataTransfer.Get<IEnumerable<IStorageItem>>(DataFormat.File);
            if (files != null)
            {
                var filePaths = new List<string>();
                foreach (var file in files)
                {
                    var path = file.TryGetLocalPath();
                    if (!string.IsNullOrEmpty(path))
                    {
                        filePaths.Add(path);
                    }
                }

                if (filePaths.Count > 0)
                {
                    await viewModel.HandleFilesDroppedAsync(filePaths);
                }
            }
        }
    }
}
