using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Views;

/// <summary>
/// The main application shell containing sidebar and content area.
/// </summary>
public partial class AppShell : UserControl
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

        // Subscribe to file scan request from quick action
        if (DataContext is AppShellViewModel vm)
        {
            vm.OpenFileScanRequested += OnOpenFileScanRequested;
        }
    }

    /// <summary>
    /// Handles the request to open a file for scanning.
    /// </summary>
    private async void OnOpenFileScanRequested(object? sender, EventArgs e)
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
