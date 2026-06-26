using ArgoBooks.Core.Models;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// The bank-statement quick action must default to OFF. "Reset to default" on the dashboard
/// reads this value (via GlobalSettings.Ui.QuickActions), so the compiled default decides
/// whether the button reappears.
/// </summary>
public class QuickActionsDefaultTests
{
    [Fact]
    public void ShowImportBankStatement_DefaultsToFalse()
    {
        Assert.False(new QuickActionsSettings().ShowImportBankStatement);
    }
}
