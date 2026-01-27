using System.Runtime.InteropServices;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform;

namespace ArgoBooks.Controls;

/// <summary>
/// Cross-platform invoice preview control.
/// On Windows: Uses WebView2 for high-quality HTML rendering.
/// On Mac/Linux: Shows a fallback message with option to view in browser.
/// </summary>
public partial class InvoicePreviewControl : UserControl
{
    /// <summary>
    /// Defines the Html property for binding HTML content.
    /// </summary>
    public static readonly StyledProperty<string?> HtmlProperty =
        AvaloniaProperty.Register<InvoicePreviewControl, string?>(nameof(Html));

    /// <summary>
    /// Defines the OpenInBrowserCommand property.
    /// </summary>
    public static readonly StyledProperty<ICommand?> OpenInBrowserCommandProperty =
        AvaloniaProperty.Register<InvoicePreviewControl, ICommand?>(nameof(OpenInBrowserCommand));

    /// <summary>
    /// Gets or sets the HTML content to display.
    /// </summary>
    public string? Html
    {
        get => GetValue(HtmlProperty);
        set => SetValue(HtmlProperty, value);
    }

    /// <summary>
    /// Gets or sets the command to execute when "Open in Browser" is clicked.
    /// </summary>
    public ICommand? OpenInBrowserCommand
    {
        get => GetValue(OpenInBrowserCommandProperty);
        set => SetValue(OpenInBrowserCommandProperty, value);
    }

    private Panel? _rootPanel;
    private Border? _fallbackPanel;
    private bool _isInitialized;

#if WINDOWS
    private Microsoft.Web.WebView2.WinForms.WebView2? _webView;
    private WebView2Host? _webViewHost;
    private bool _isWebViewInitialized;
#endif

    public InvoicePreviewControl()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (_isInitialized)
            return;

        _rootPanel = this.FindControl<Panel>("RootPanel");
        _fallbackPanel = this.FindControl<Border>("FallbackPanel");

        if (OperatingSystem.IsWindows())
        {
            InitializeWindowsWebView();
        }
        else
        {
            ShowFallback();
        }

        _isInitialized = true;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == HtmlProperty && _isInitialized)
        {
            UpdateWebViewContent();
        }
    }

    private void ShowFallback()
    {
        if (_fallbackPanel != null)
        {
            _fallbackPanel.IsVisible = true;
        }
    }

    private void InitializeWindowsWebView()
    {
#if WINDOWS
        try
        {
            _webView = new Microsoft.Web.WebView2.WinForms.WebView2();
            _webViewHost = new WebView2Host(_webView);

            // Set the host to fill the panel
            _webViewHost.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
            _webViewHost.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch;

            if (_rootPanel != null)
            {
                _rootPanel.Children.Insert(0, _webViewHost);
            }

            // Initialize WebView2 asynchronously
            InitializeWebView2Async();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to create WebView2: {ex.Message}");
            ShowFallback();
        }
#else
        ShowFallback();
#endif
    }

#if WINDOWS
    private async void InitializeWebView2Async()
    {
        try
        {
            if (_webView == null) return;

            await _webView.EnsureCoreWebView2Async();

            // Disable context menu and other browser features for clean preview
            if (_webView.CoreWebView2 != null)
            {
                _webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                _webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
                _webView.CoreWebView2.Settings.IsZoomControlEnabled = true; // Allow zoom for better viewing

                _isWebViewInitialized = true;

                // Set initial content if available
                UpdateWebViewContent();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"WebView2 initialization failed: {ex.Message}");
            // Show fallback on UI thread
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(ShowFallback);
        }
    }
#endif

    private void UpdateWebViewContent()
    {
#if WINDOWS
        if (_isWebViewInitialized && _webView?.CoreWebView2 != null && !string.IsNullOrEmpty(Html))
        {
            _webView.CoreWebView2.NavigateToString(Html);
        }
#endif
    }

    private void OpenInBrowserButton_Click(object? sender, RoutedEventArgs e)
    {
        OpenInBrowserCommand?.Execute(null);
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

#if WINDOWS
        if (_webView != null)
        {
            _webView.Dispose();
            _webView = null;
            _isWebViewInitialized = false;
        }
#endif
    }
}

#if WINDOWS
/// <summary>
/// NativeControlHost that wraps a WebView2 control for embedding in Avalonia.
/// This allows the Windows WebView2 control to be rendered within the Avalonia visual tree.
/// </summary>
internal class WebView2Host : NativeControlHost
{
    private readonly Microsoft.Web.WebView2.WinForms.WebView2 _webView;

    public WebView2Host(Microsoft.Web.WebView2.WinForms.WebView2 webView)
    {
        _webView = webView;
    }

    protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
    {
        // Ensure the WinForms control is created
        if (!_webView.IsHandleCreated)
        {
            _webView.CreateControl();
        }

        // Set the parent window handle for proper embedding
        if (parent.Handle != IntPtr.Zero)
        {
            SetParent(_webView.Handle, parent.Handle);
        }

        return new WinPlatformHandle(_webView.Handle, "HWND");
    }

    protected override void DestroyNativeControlCore(IPlatformHandle control)
    {
        // WebView2 disposal is handled by the parent InvoicePreviewControl
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);
}

/// <summary>
/// Simple platform handle implementation for Windows HWND.
/// </summary>
internal class WinPlatformHandle : IPlatformHandle
{
    public WinPlatformHandle(IntPtr handle, string descriptor)
    {
        Handle = handle;
        HandleDescriptor = descriptor;
    }

    public IntPtr Handle { get; }
    public string HandleDescriptor { get; }
}
#endif
