using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using ArgoBooks.Utilities;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Views;

/// <summary>
/// The main application shell containing sidebar and content area.
/// </summary>
public partial class AppShell : UserControl
{
    private const double CompactPageThreshold = 1200;
    private const double MinimalPageThreshold = 900;

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

        // Animate toast slide in/out from right
        DataContextChanged += (_, _) =>
        {
            if (DataContext is AppShellViewModel vm)
            {
                vm.HeaderViewModel.PropertyChanged += OnHeaderViewModelPropertyChanged;
            }
        };
    }

    private void OnHeaderViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(HeaderViewModel.ShowNotificationToast))
            return;

        var vm = (HeaderViewModel)sender!;
        if (vm.ShowNotificationToast)
        {
            Dispatcher.UIThread.Post(() =>
            {
                NotificationToastBorder.Opacity = 1;
                NotificationToastBorder.RenderTransform = TransformOperations.Parse("translate(0px, 0px)");
            }, DispatcherPriority.Render);
        }
        else
        {
            Dispatcher.UIThread.Post(() =>
            {
                NotificationToastBorder.Opacity = 0;
                NotificationToastBorder.RenderTransform = TransformOperations.Parse("translate(340px, 0px)");
            }, DispatcherPriority.Background);
        }
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
