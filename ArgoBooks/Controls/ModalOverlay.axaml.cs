using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media.Transformation;
using ArgoBooks.Utilities;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.Controls;

/// <summary>
/// A modal overlay that displays content with a semi-transparent backdrop.
/// </summary>
public partial class ModalOverlay : UserControl
{
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

        System.Diagnostics.Debug.WriteLine($"[ModalOverlay] Constructor: IsOpen={IsOpen}, OverlayPanel={OverlayPanel != null}, Content={Content}");

        // Set initial visibility based on IsOpen (which defaults to false)
        if (OverlayPanel != null)
        {
            OverlayPanel.IsVisible = IsOpen;
            System.Diagnostics.Debug.WriteLine($"[ModalOverlay] Constructor: Set OverlayPanel.IsVisible to {IsOpen}");
        }

        // Set initial content
        if (ModalContentPresenter != null)
            ModalContentPresenter.Content = Content;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsOpenProperty)
        {
            System.Diagnostics.Debug.WriteLine($"[ModalOverlay] PropertyChanged: IsOpen changed from {change.OldValue} to {change.NewValue}");
            OnIsOpenChanged(IsOpen);
        }
        else if (change.Property == ContentProperty)
        {
            System.Diagnostics.Debug.WriteLine($"[ModalOverlay] PropertyChanged: Content changed to {Content?.GetType().Name ?? "null"}");
            if (ModalContentPresenter != null)
                ModalContentPresenter.Content = Content;
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
        System.Diagnostics.Debug.WriteLine($"[ModalOverlay] OnIsOpenChanged: isOpen={isOpen}, OverlayPanel={OverlayPanel != null}");

        // Update panel visibility
        if (OverlayPanel != null)
        {
            OverlayPanel.IsVisible = isOpen;
            System.Diagnostics.Debug.WriteLine($"[ModalOverlay] Set OverlayPanel.IsVisible to {isOpen}");
        }

        if (isOpen)
        {
            // Set initial state before animation
            if (Backdrop != null)
                Backdrop.Opacity = 0;
            if (ContentContainer != null)
            {
                ContentContainer.Opacity = 0;
                ContentContainer.RenderTransform = TransformOperations.Parse("scale(0.95)");
            }

            // Trigger animation to final state (on next frame so transitions kick in)
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                if (Backdrop != null)
                    Backdrop.Opacity = 1;
                if (ContentContainer != null)
                {
                    ContentContainer.Opacity = 1;
                    ContentContainer.RenderTransform = TransformOperations.Parse("scale(1)");
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
