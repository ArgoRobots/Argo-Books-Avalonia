using CommunityToolkit.Mvvm.ComponentModel;

namespace ArgoBooks.ViewModels;

/// <summary>
/// One API key as shown in Settings.
///
/// Carries the hint (ab_1a2b...wxyz) rather than the secret, because the secret
/// is only ever in memory once, at creation. Everything here is safe to display
/// and safe to put in a screenshot on a support ticket.
/// </summary>
public partial class ArgoApiKeyRow : ObservableObject
{
    public string Id { get; init; } = string.Empty;

    public string Hint { get; init; } = string.Empty;

    public string Label { get; init; } = string.Empty;

    public string Scopes { get; init; } = string.Empty;

    /// <summary>Pre-formatted for display, so the row has no date-formatting logic of its own.</summary>
    public string LastUsedDisplay { get; init; } = string.Empty;

    [ObservableProperty]
    private bool _isRevoked;

    /// <summary>What the row shows when it has a name, falling back to the hint.</summary>
    public string DisplayName => string.IsNullOrWhiteSpace(Label) ? Hint : Label;

    public bool CanRevoke => !IsRevoked;

    partial void OnIsRevokedChanged(bool value) => OnPropertyChanged(nameof(CanRevoke));
}
