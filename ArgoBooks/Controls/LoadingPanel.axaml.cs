using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace ArgoBooks.Controls;

/// <summary>
/// A panel that can display a loading overlay over its content.
/// </summary>
public partial class LoadingPanel : UserControl
{
    #region Styled Properties

    public static readonly StyledProperty<bool> IsLoadingProperty =
        AvaloniaProperty.Register<LoadingPanel, bool>(nameof(IsLoading));

    public static readonly StyledProperty<string?> LoadingTextProperty =
        AvaloniaProperty.Register<LoadingPanel, string?>(nameof(LoadingText));

    public static readonly StyledProperty<SpinnerSize> SpinnerSizeProperty =
        AvaloniaProperty.Register<LoadingPanel, SpinnerSize>(nameof(SpinnerSize), SpinnerSize.Large);

    public static readonly StyledProperty<IBrush?> SpinnerBrushProperty =
        AvaloniaProperty.Register<LoadingPanel, IBrush?>(nameof(SpinnerBrush));

    public static readonly new StyledProperty<object?> ContentProperty =
        AvaloniaProperty.Register<LoadingPanel, object?>(nameof(Content));

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets whether the loading overlay is visible.
    /// </summary>
    public bool IsLoading
    {
        get => GetValue(IsLoadingProperty);
        set => SetValue(IsLoadingProperty, value);
    }

    /// <summary>
    /// Gets or sets the loading message text.
    /// </summary>
    public string? LoadingText
    {
        get => GetValue(LoadingTextProperty);
        set => SetValue(LoadingTextProperty, value);
    }

    /// <summary>
    /// Gets or sets the spinner size.
    /// </summary>
    public SpinnerSize SpinnerSize
    {
        get => GetValue(SpinnerSizeProperty);
        set => SetValue(SpinnerSizeProperty, value);
    }

    /// <summary>
    /// Gets or sets the spinner brush color.
    /// </summary>
    public IBrush? SpinnerBrush
    {
        get => GetValue(SpinnerBrushProperty);
        set => SetValue(SpinnerBrushProperty, value);
    }

    /// <summary>
    /// Gets or sets the content to display.
    /// </summary>
    public new object? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    #endregion

    public LoadingPanel()
    {
        InitializeComponent();
    }
}
