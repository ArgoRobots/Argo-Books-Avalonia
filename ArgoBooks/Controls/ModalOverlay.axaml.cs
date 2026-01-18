using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Data;
using Avalonia.Input;
using ArgoBooks.Utilities;

namespace ArgoBooks.Controls;

/// <summary>
/// A modal overlay that displays content with a semi-transparent backdrop.
/// </summary>
public partial class ModalOverlay : UserControl
{
    private Panel? _overlayPanel;
    private ContentPresenter? _modalContentPresenter;

    #region Styled Properties

    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<ModalOverlay, bool>(nameof(IsOpen), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> CloseOnBackdropClickProperty =
        AvaloniaProperty.Register<ModalOverlay, bool>(nameof(CloseOnBackdropClick));

    public static readonly StyledProperty<bool> CloseOnEscapeProperty =
        AvaloniaProperty.Register<ModalOverlay, bool>(nameof(CloseOnEscape), true);

    public static readonly StyledProperty<object?> ModalContentProperty =
        AvaloniaProperty.Register<ModalOverlay, object?>(nameof(ModalContent));

    #endregion

    #region Properties

    public bool IsOpen
    {
        get => GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public bool CloseOnBackdropClick
    {
        get => GetValue(CloseOnBackdropClickProperty);
        set => SetValue(CloseOnBackdropClickProperty, value);
    }

    public bool CloseOnEscape
    {
        get => GetValue(CloseOnEscapeProperty);
        set => SetValue(CloseOnEscapeProperty, value);
    }

    /// <summary>
    /// Gets or sets the modal content. Use ModalOverlay.ModalContent property element syntax in XAML.
    /// </summary>
    public object? ModalContent
    {
        get => GetValue(ModalContentProperty);
        set => SetValue(ModalContentProperty, value);
    }

    #endregion

    #region Events

    public event EventHandler? Opened;
    public event EventHandler? Closed;
    public event EventHandler<ModalClosingEventArgs>? Closing;

    #endregion

    public ModalOverlay()
    {
        InitializeComponent();
        _overlayPanel = this.FindControl<Panel>("OverlayPanel");
        _modalContentPresenter = this.FindControl<ContentPresenter>("ModalContentPresenter");
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        if (_modalContentPresenter != null)
            _modalContentPresenter.Content = ModalContent;

        if (_overlayPanel != null)
        {
            _overlayPanel.Opacity = IsOpen ? 1 : 0;
            _overlayPanel.IsHitTestVisible = IsOpen;
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsOpenProperty)
        {
            OnIsOpenChanged(IsOpen);
        }
        else if (change.Property == ModalContentProperty && _modalContentPresenter != null)
        {
            _modalContentPresenter.Content = ModalContent;
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
        if (_overlayPanel != null)
        {
            _overlayPanel.Opacity = isOpen ? 1 : 0;
            _overlayPanel.IsHitTestVisible = isOpen;
        }

        if (isOpen)
        {
            Focus();
            Opened?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            Closed?.Invoke(this, EventArgs.Empty);
            ModalHelper.ReturnFocusToAppShell(this);
        }
    }

    private void OnBackdropPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (CloseOnBackdropClick)
            RequestClose();
    }

    public void RequestClose()
    {
        var args = new ModalClosingEventArgs();
        Closing?.Invoke(this, args);

        if (!args.Cancel)
            IsOpen = false;
    }
}

public class ModalClosingEventArgs : EventArgs
{
    public bool Cancel { get; set; }
}
