namespace ArgoBooks.Shared.Mobile;

/// <summary>
/// Pure decision for the multi-company switcher: whether picking a company in the switcher list
/// should actually trigger a store update + snapshot refresh, or is a no-op because the user
/// tapped the company that's already active. Kept separate from ShellViewModel so it's unit
/// testable without any Avalonia/device dependency.
/// </summary>
public static class CompanySwitchDecision
{
    /// <summary>
    /// True if switching to <paramref name="targetCompanyUid"/> requires setting it active and
    /// refreshing the snapshot, i.e. it differs from the currently active company.
    /// </summary>
    public static bool ShouldSwitch(string? currentActiveCompanyUid, string targetCompanyUid)
    {
        if (string.IsNullOrWhiteSpace(targetCompanyUid))
        {
            return false;
        }

        return !string.Equals(currentActiveCompanyUid, targetCompanyUid, System.StringComparison.Ordinal);
    }
}
