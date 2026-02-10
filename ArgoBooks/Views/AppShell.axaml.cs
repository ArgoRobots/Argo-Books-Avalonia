using Avalonia;
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
    private const double CompactPageThreshold = 1200;
    private const double MinimalPageThreshold = 900;

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

        // Use tunnel strategy to catch all pointer presses on sidebar/header,
        // even when child controls handle the event. This ensures page-level
        // context menus (column visibility, chart) close on any sidebar/header click.
        AppSidebar.AddHandler(PointerPressedEvent, OnSidebarPointerPressed, RoutingStrategies.Tunnel);
        AppHeader.AddHandler(PointerPressedEvent, OnHeaderPointerPressed, RoutingStrategies.Tunnel);

        // Responsive page content margin
        AppContent.SizeChanged += OnContentSizeChanged;
    }

    private void OnContentSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        var width = e.NewSize.Width;
        if (width < MinimalPageThreshold)
            PageContentControl.Margin = new Thickness(0);
        else if (width < CompactPageThreshold)
            PageContentControl.Margin = new Thickness(12);
        else
            PageContentControl.Margin = new Thickness(30);
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
    /// Closes page-level context menus when the sidebar is clicked.
    /// </summary>
    private void OnSidebarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        (DataContext as AppShellViewModel)?.ClosePageContextMenus();
    }

    /// <summary>
    /// Closes page-level context menus when the header is clicked.
    /// </summary>
    private void OnHeaderPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        (DataContext as AppShellViewModel)?.ClosePageContextMenus();
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
