using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media.Transformation;
using Avalonia.Metadata;
using ArgoBooks.Utilities;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.Controls;

/// <summary>
/// A modal overlay that displays content with a semi-transparent backdrop.
/// </summary>
public partial class ModalOverlay : UserControl
{
    #region Named Elements

    private Panel? _overlayPanel;
    private Border? _backdrop;
    private Border? _contentContainer;
    private ContentPresenter? _modalContentPresenter;

    #endregion

    #region Styled Properties

    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<ModalOverlay, bool>(nameof(IsOpen), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> CloseOnBackdropClickProperty =
        AvaloniaProperty.Register<ModalOverlay, bool>(nameof(CloseOnBackdropClick), true);

    public static readonly StyledProperty<bool> CloseOnEscapeProperty =
        AvaloniaProperty.Register<ModalOverlay, bool>(nameof(CloseOnEscape), true);

    public new static readonly StyledProperty<object?> ContentProperty =
        AvaloniaProperty.Register<ModalOverlay, object?>(nameof(Content));

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets whether the modal is open.
    /// </summary>
    public bool IsOpen
    {
        get => GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    /// <summary>
    /// Gets or sets whether clicking the backdrop closes the modal.
    /// </summary>
    public bool CloseOnBackdropClick
    {
        get => GetValue(CloseOnBackdropClickProperty);
        set => SetValue(CloseOnBackdropClickProperty, value);
    }

    /// <summary>
    /// Gets or sets whether pressing Escape closes the modal.
    /// </summary>
    public bool CloseOnEscape
    {
        get => GetValue(CloseOnEscapeProperty);
        set => SetValue(CloseOnEscapeProperty, value);
    }

    /// <summary>
    /// Gets or sets the modal content.
    /// </summary>
    [Content]
    public new object? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    /// <summary>
    /// Command executed when backdrop is clicked.
    /// </summary>
    public ICommand BackdropClickCommand { get; }

    #endregion

    #region Events

    /// <summary>
    /// Event raised when the modal is opened.
    /// </summary>
    public event EventHandler? Opened;

    /// <summary>
    /// Event raised when the modal is closed.
    /// </summary>
    public event EventHandler? Closed;

    /// <summary>
    /// Event raised when close is requested (can be cancelled).
    /// </summary>
    public event EventHandler<ModalClosingEventArgs>? Closing;

    #endregion

    public ModalOverlay()
    {
        BackdropClickCommand = new RelayCommand(OnBackdropClick);
        InitializeComponent();

        // Find named elements
        _overlayPanel = this.FindControl<Panel>("OverlayPanel");
        _backdrop = this.FindControl<Border>("Backdrop");
        _contentContainer = this.FindControl<Border>("ContentContainer");
        _modalContentPresenter = this.FindControl<ContentPresenter>("ModalContentPresenter");

        System.Diagnostics.Debug.WriteLine($"[ModalOverlay] Initialized - OverlayPanel: {_overlayPanel != null}, ContentPresenter: {_modalContentPresenter != null}");
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // Sync content and visibility when attached to visual tree
        if (_modalContentPresenter != null)
        {
            _modalContentPresenter.Content = Content;
        }
        if (_overlayPanel != null)
        {
            _overlayPanel.IsVisible = IsOpen;
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsOpenProperty)
        {
            OnIsOpenChanged(IsOpen);
        }
        else if (change.Property == ContentProperty)
        {
            if (_modalContentPresenter != null)
            {
                _modalContentPresenter.Content = Content;
            }
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key == Key.Escape && IsOpen && CloseOnEscape)
        {
            RequestClose();
            e.Handled = true;
        }
    }

    private void OnIsOpenChanged(bool isOpen)
    {
        System.Diagnostics.Debug.WriteLine($"[ModalOverlay] OnIsOpenChanged: isOpen={isOpen}, _overlayPanel={_overlayPanel != null}");

        if (_overlayPanel != null)
        {
            _overlayPanel.IsVisible = isOpen;
            System.Diagnostics.Debug.WriteLine($"[ModalOverlay] Set _overlayPanel.IsVisible = {isOpen}");
        }

        if (isOpen)
        {
            // Set initial state before animation
            if (_backdrop != null)
                _backdrop.Opacity = 0;
            if (_contentContainer != null)
            {
                _contentContainer.Opacity = 0;
                _contentContainer.RenderTransform = TransformOperations.Parse("scale(0.95)");
            }

            // Trigger animation to final state (on next frame so transitions kick in)
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                if (_backdrop != null)
                    _backdrop.Opacity = 1;
                if (_contentContainer != null)
                {
                    _contentContainer.Opacity = 1;
                    _contentContainer.RenderTransform = TransformOperations.Parse("scale(1)");
                }
            }, Avalonia.Threading.DispatcherPriority.Render);

            Focus();
            Opened?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            Closed?.Invoke(this, EventArgs.Empty);
            ModalHelper.ReturnFocusToAppShell(this);
        }
    }

    private void OnBackdropClick()
    {
        if (CloseOnBackdropClick)
        {
            RequestClose();
        }
    }

    private void OnBackdropPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        OnBackdropClick();
    }

    /// <summary>
    /// Requests to close the modal. Can be cancelled via Closing event.
    /// </summary>
    public void RequestClose()
    {
        var args = new ModalClosingEventArgs();
        Closing?.Invoke(this, args);

        if (!args.Cancel)
        {
            IsOpen = false;
        }
    }

    /// <summary>
    /// Opens the modal.
    /// </summary>
    public void Open()
    {
        IsOpen = true;
    }

    /// <summary>
    /// Closes the modal without raising the Closing event.
    /// </summary>
    public void Close()
    {
        IsOpen = false;
    }
}

/// <summary>
/// Event arguments for modal closing event.
/// </summary>
public class ModalClosingEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets whether to cancel the close operation.
    /// </summary>
    public bool Cancel { get; set; }
}
