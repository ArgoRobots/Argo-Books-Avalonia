using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.BankMatching;
using ArgoBooks.Core.Models.Integrations;
using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

public class BankMatchingStripePayoutTests
{
    [Fact]
    public void Deposit_MatchingRememberedPayout_IsAutoIgnored()
    {
        var data = new CompanyData();
        data.Settings.Integrations.Stripe.ImportedPayouts.Add(
            new StripePayoutRecord { StripePayoutId = "po_1", AmountCents = 4825, Date = new DateTime(2026, 1, 15) });

        var line = new BankStatementLine { Id = "L1", Date = new DateTime(2026, 1, 16), Description = "STRIPE PAYOUT", Amount = 48.25m };
        var result = new BankMatchingService().MatchDeterministic(new[] { line }, data, new BankMatchingOptions());

        Assert.Equal(BankLineMatchStatus.Ignored, line.MatchStatus);
        Assert.Contains("Stripe", line.IgnoreReason ?? "");
    }

    [Fact]
    public void Deposit_NotMatchingAnyPayout_IsNotAutoIgnored()
    {
        var data = new CompanyData();
        data.Settings.Integrations.Stripe.ImportedPayouts.Add(
            new StripePayoutRecord { StripePayoutId = "po_1", AmountCents = 4825, Date = new DateTime(2026, 1, 15) });

        var line = new BankStatementLine { Id = "L2", Date = new DateTime(2026, 1, 16), Description = "OTHER DEPOSIT", Amount = 999.99m };
        new BankMatchingService().MatchDeterministic(new[] { line }, data, new BankMatchingOptions());

        Assert.NotEqual(BankLineMatchStatus.Ignored, line.MatchStatus);
    }

    [Fact]
    public void MoneyOut_LineIsNeverAutoIgnoredAsPayout()
    {
        var data = new CompanyData();
        data.Settings.Integrations.Stripe.ImportedPayouts.Add(
            new StripePayoutRecord { StripePayoutId = "po_1", AmountCents = 4825, Date = new DateTime(2026, 1, 15) });

        var line = new BankStatementLine { Id = "L3", Date = new DateTime(2026, 1, 15), Description = "PAYMENT", Amount = -48.25m };
        new BankMatchingService().MatchDeterministic(new[] { line }, data, new BankMatchingOptions());

        Assert.NotEqual(BankLineMatchStatus.Ignored, line.MatchStatus);
    }
}
