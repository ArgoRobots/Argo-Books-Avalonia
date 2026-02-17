using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;

namespace ArgoBooks.Controls;

/// <summary>
/// A reusable inline warning banner shown when the payment portal is not configured.
/// </summary>
public partial class PortalWarningBanner : UserControl
{
    public static readonly StyledProperty<ICommand?> ConfigureCommandProperty =
        AvaloniaProperty.Register<PortalWarningBanner, ICommand?>(nameof(ConfigureCommand));

    public ICommand? ConfigureCommand
    {
        get => GetValue(ConfigureCommandProperty);
        set => SetValue(ConfigureCommandProperty, value);
    }

    public PortalWarningBanner()
    {
        InitializeComponent();
    }
}
