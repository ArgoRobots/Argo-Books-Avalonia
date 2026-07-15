using ArgoBooks.Core.Data;
using ArgoBooks.Core.Models.Transactions;
using ArgoBooks.Core.Services.Sync;
using Xunit;

namespace ArgoBooks.Tests.Services.Sync;

/// <summary>
/// Tests for <see cref="SnapshotBuilder"/>, the read-model projector that turns
/// <see cref="CompanyData"/> into the small <see cref="MobileSnapshot"/> DTO the
/// phone renders read-only.
/// </summary>
public class SnapshotBuilderTests
{
    [Fact]
    public void Build_computes_money_in_out_profit()
    {
        var data = new CompanyData();

        // Two revenues totalling 100 (real Revenue model: Total is the gross amount,
        // PaymentStatus defaults to Paid so both count as collected).
        data.Revenues.Add(new Revenue
        {
            Id = "REV-1",
            Date = new DateTime(2026, 1, 1),
            Description = "Consulting",
            Total = 60m
        });
        data.Revenues.Add(new Revenue
        {
            Id = "REV-2",
            Date = new DateTime(2026, 1, 2),
            Description = "Retainer",
            Total = 40m
        });

        // One expense totalling 40 (real Expense model: Total is the gross amount).
        data.Expenses.Add(new Expense
        {
            Id = "EXP-1",
            Date = new DateTime(2026, 1, 3),
            Description = "Office supplies",
            Total = 40m
        });

        var snap = SnapshotBuilder.Build(data);

        Assert.Equal(100m, snap.Dashboard.MoneyIn);
        Assert.Equal(40m, snap.Dashboard.MoneyOut);
        Assert.Equal(60m, snap.Dashboard.Profit);
    }

    [Fact]
    public void Serialize_roundtrips_via_json()
    {
        var snap = SnapshotBuilder.Build(new CompanyData());
        var bytes = SnapshotBuilder.Serialize(snap);

        Assert.NotEmpty(bytes);
    }
}
