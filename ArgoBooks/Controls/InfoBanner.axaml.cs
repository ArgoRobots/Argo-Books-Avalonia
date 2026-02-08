using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace ArgoBooks.Controls;

/// <summary>
/// A reusable banner control that displays an icon, message, optional action link, and dismiss button.
/// Used for non-blocking notifications that overlay the top of the content area.
/// </summary>
public partial class InfoBanner : UserControl
{
    #region Styled Properties

    public static readonly StyledProperty<string?> MessageProperty =
        AvaloniaProperty.Register<InfoBanner, string?>(nameof(Message));

    public static readonly StyledProperty<string?> ActionTextProperty =
        AvaloniaProperty.Register<InfoBanner, string?>(nameof(ActionText));

    public static readonly StyledProperty<ICommand?> ActionCommandProperty =
        AvaloniaProperty.Register<InfoBanner, ICommand?>(nameof(ActionCommand));

    public static readonly StyledProperty<ICommand?> DismissCommandProperty =
        AvaloniaProperty.Register<InfoBanner, ICommand?>(nameof(DismissCommand));

    public static readonly StyledProperty<Geometry?> IconDataProperty =
        AvaloniaProperty.Register<InfoBanner, Geometry?>(nameof(IconData));

    public static readonly StyledProperty<IBrush?> BannerBackgroundProperty =
        AvaloniaProperty.Register<InfoBanner, IBrush?>(nameof(BannerBackground));

    public static readonly StyledProperty<IBrush?> DismissBackgroundProperty =
        AvaloniaProperty.Register<InfoBanner, IBrush?>(nameof(DismissBackground));

    public static readonly StyledProperty<bool> ShowBannerProperty =
        AvaloniaProperty.Register<InfoBanner, bool>(nameof(ShowBanner));

    #endregion

    #region Properties

    public string? Message
    {
        get => GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public string? ActionText
    {
        get => GetValue(ActionTextProperty);
        set => SetValue(ActionTextProperty, value);
    }

    public ICommand? ActionCommand
    {
        get => GetValue(ActionCommandProperty);
        set => SetValue(ActionCommandProperty, value);
    }

    public ICommand? DismissCommand
    {
        get => GetValue(DismissCommandProperty);
        set => SetValue(DismissCommandProperty, value);
    }

    public Geometry? IconData
    {
        get => GetValue(IconDataProperty);
        set => SetValue(IconDataProperty, value);
    }

    public IBrush? BannerBackground
    {
        get => GetValue(BannerBackgroundProperty);
        set => SetValue(BannerBackgroundProperty, value);
    }

    public IBrush? DismissBackground
    {
        get => GetValue(DismissBackgroundProperty);
        set => SetValue(DismissBackgroundProperty, value);
    }

    public bool ShowBanner
    {
        get => GetValue(ShowBannerProperty);
        set => SetValue(ShowBannerProperty, value);
    }

    #endregion

    public InfoBanner()
    {
        InitializeComponent();
    }
}
