using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.Controls;

/// <summary>
/// Modal size presets.
/// </summary>
public enum ModalSize
{
    /// <summary>
    /// Small modal (400px).
    /// </summary>
    Small,

    /// <summary>
    /// Medium modal (500px). Default.
    /// </summary>
    Medium,

    /// <summary>
    /// Large modal (700px).
    /// </summary>
    Large,

    /// <summary>
    /// Extra large modal (900px).
    /// </summary>
    ExtraLarge,

    /// <summary>
    /// Full screen modal with margin.
    /// </summary>
    Full,

    /// <summary>
    /// Custom size - uses ModalWidth property.
    /// </summary>
    Custom
}

/// <summary>
/// A styled container for modal content with header, content, and footer sections.
/// </summary>
public partial class ModalContainer : UserControl
{
    #region Styled Properties

    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<ModalContainer, string?>(nameof(Title));

    public static readonly StyledProperty<string?> SubtitleProperty =
        AvaloniaProperty.Register<ModalContainer, string?>(nameof(Subtitle));

    public static readonly StyledProperty<object?> ModalContentProperty =
        AvaloniaProperty.Register<ModalContainer, object?>(nameof(ModalContent));

    public static readonly StyledProperty<ModalSize> SizeProperty =
        AvaloniaProperty.Register<ModalContainer, ModalSize>(nameof(Size), ModalSize.Medium);

    public static readonly StyledProperty<double> ModalWidthProperty =
        AvaloniaProperty.Register<ModalContainer, double>(nameof(ModalWidth), double.NaN);

    public new static readonly StyledProperty<double> MinWidthProperty =
        AvaloniaProperty.Register<ModalContainer, double>(nameof(MinWidth), 300);

    public new static readonly StyledProperty<double> MaxWidthProperty =
        AvaloniaProperty.Register<ModalContainer, double>(nameof(MaxWidth), double.PositiveInfinity);

    public new static readonly StyledProperty<double> MinHeightProperty =
        AvaloniaProperty.Register<ModalContainer, double>(nameof(MinHeight), 150);

    public new static readonly StyledProperty<double> MaxHeightProperty =
        AvaloniaProperty.Register<ModalContainer, double>(nameof(MaxHeight), double.PositiveInfinity);

    public static readonly StyledProperty<bool> ShowHeaderProperty =
        AvaloniaProperty.Register<ModalContainer, bool>(nameof(ShowHeader), true);

    public static readonly StyledProperty<bool> ShowFooterProperty =
        AvaloniaProperty.Register<ModalContainer, bool>(nameof(ShowFooter), true);

    public static readonly StyledProperty<bool> ShowCloseButtonProperty =
        AvaloniaProperty.Register<ModalContainer, bool>(nameof(ShowCloseButton), true);

    public static readonly StyledProperty<string?> PrimaryButtonTextProperty =
        AvaloniaProperty.Register<ModalContainer, string?>(nameof(PrimaryButtonText));

    public static readonly StyledProperty<string?> SecondaryButtonTextProperty =
        AvaloniaProperty.Register<ModalContainer, string?>(nameof(SecondaryButtonText));

    public static readonly StyledProperty<ICommand?> PrimaryCommandProperty =
        AvaloniaProperty.Register<ModalContainer, ICommand?>(nameof(PrimaryCommand));

    public static readonly StyledProperty<ICommand?> SecondaryCommandProperty =
        AvaloniaProperty.Register<ModalContainer, ICommand?>(nameof(SecondaryCommand));

    public static readonly StyledProperty<bool> IsPrimaryLoadingProperty =
        AvaloniaProperty.Register<ModalContainer, bool>(nameof(IsPrimaryLoading));

    public static readonly StyledProperty<object?> FooterLeftContentProperty =
        AvaloniaProperty.Register<ModalContainer, object?>(nameof(FooterLeftContent));

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the modal title.
    /// </summary>
    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>
    /// Gets or sets the modal subtitle.
    /// </summary>
    public string? Subtitle
    {
        get => GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    /// <summary>
    /// Gets or sets the modal content.
    /// </summary>
    public object? ModalContent
    {
        get => GetValue(ModalContentProperty);
        set => SetValue(ModalContentProperty, value);
    }

    /// <summary>
    /// Gets or sets the modal size preset.
    /// </summary>
    public ModalSize Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    /// <summary>
    /// Gets or sets the modal width (for custom size).
    /// </summary>
    public double ModalWidth
    {
        get => GetValue(ModalWidthProperty);
        set => SetValue(ModalWidthProperty, value);
    }

    /// <summary>
    /// Gets or sets the minimum modal width.
    /// </summary>
    public new double MinWidth
    {
        get => GetValue(MinWidthProperty);
        set => SetValue(MinWidthProperty, value);
    }

    /// <summary>
    /// Gets or sets the maximum modal width.
    /// </summary>
    public new double MaxWidth
    {
        get => GetValue(MaxWidthProperty);
        set => SetValue(MaxWidthProperty, value);
    }

    /// <summary>
    /// Gets or sets the minimum modal height.
    /// </summary>
    public new double MinHeight
    {
        get => GetValue(MinHeightProperty);
        set => SetValue(MinHeightProperty, value);
    }

    /// <summary>
    /// Gets or sets the maximum modal height.
    /// </summary>
    public new double MaxHeight
    {
        get => GetValue(MaxHeightProperty);
        set => SetValue(MaxHeightProperty, value);
    }

    /// <summary>
    /// Gets or sets whether to show the header.
    /// </summary>
    public bool ShowHeader
    {
        get => GetValue(ShowHeaderProperty);
        set => SetValue(ShowHeaderProperty, value);
    }

    /// <summary>
    /// Gets or sets whether to show the footer.
    /// </summary>
    public bool ShowFooter
    {
        get => GetValue(ShowFooterProperty);
        set => SetValue(ShowFooterProperty, value);
    }

    /// <summary>
    /// Gets or sets whether to show the close button.
    /// </summary>
    public bool ShowCloseButton
    {
        get => GetValue(ShowCloseButtonProperty);
        set => SetValue(ShowCloseButtonProperty, value);
    }

    /// <summary>
    /// Gets or sets the primary button text.
    /// </summary>
    public string? PrimaryButtonText
    {
        get => GetValue(PrimaryButtonTextProperty);
        set => SetValue(PrimaryButtonTextProperty, value);
    }

    /// <summary>
    /// Gets or sets the secondary button text.
    /// </summary>
    public string? SecondaryButtonText
    {
        get => GetValue(SecondaryButtonTextProperty);
        set => SetValue(SecondaryButtonTextProperty, value);
    }

    /// <summary>
    /// Gets or sets the primary button command.
    /// </summary>
    public ICommand? PrimaryCommand
    {
        get => GetValue(PrimaryCommandProperty);
        set => SetValue(PrimaryCommandProperty, value);
    }

    /// <summary>
    /// Gets or sets the secondary button command.
    /// </summary>
    public ICommand? SecondaryCommand
    {
        get => GetValue(SecondaryCommandProperty);
        set => SetValue(SecondaryCommandProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the primary button shows loading state.
    /// </summary>
    public bool IsPrimaryLoading
    {
        get => GetValue(IsPrimaryLoadingProperty);
        set => SetValue(IsPrimaryLoadingProperty, value);
    }

    /// <summary>
    /// Gets or sets the left footer content.
    /// </summary>
    public object? FooterLeftContent
    {
        get => GetValue(FooterLeftContentProperty);
        set => SetValue(FooterLeftContentProperty, value);
    }

    /// <summary>
    /// Command to close the modal.
    /// </summary>
    public ICommand CloseCommand { get; }

    #endregion

    #region Events

    /// <summary>
    /// Event raised when close is requested.
    /// </summary>
    public event EventHandler? CloseRequested;

    #endregion

    public ModalContainer()
    {
        CloseCommand = new RelayCommand(OnCloseRequested);
        InitializeComponent();
    }

    private void OnCloseRequested()
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}
