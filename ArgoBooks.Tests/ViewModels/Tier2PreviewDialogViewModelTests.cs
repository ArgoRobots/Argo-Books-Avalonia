using System.Text.Json;
using ArgoBooks.ViewModels;
using Xunit;

namespace ArgoBooks.Tests.ViewModels;

/// <summary>
/// Tests for Tier2PreviewDialogViewModel (Task 1B-1): capped sample, commit/cancel results,
/// and entity flattening.
/// </summary>
public class Tier2PreviewDialogViewModelTests
{
    private static JsonElement Entity(string json) => JsonDocument.Parse(json).RootElement.Clone();

    private static List<(string SheetName, JsonElement Entity)> MakeSample(int count, string sheet = "Sheet1")
    {
        var list = new List<(string, JsonElement)>();
        for (int i = 0; i < count; i++)
            list.Add((sheet, Entity($"{{\"id\":\"E{i}\",\"name\":\"Item {i}\"}}")));
        return list;
    }

    [Fact]
    public void ShowAsync_WithMoreThan50Entries_CapsDisplayedRowsAt50()
    {
        var vm = new Tier2PreviewDialogViewModel();
        var sample = MakeSample(120);

        _ = vm.ShowAsync(sample, totalCount: 120);

        Assert.Equal(50, vm.Rows.Count);
    }

    [Fact]
    public void ShowAsync_SetsTotalCountToFullTotal_NotCappedCount()
    {
        var vm = new Tier2PreviewDialogViewModel();
        var sample = MakeSample(120);

        _ = vm.ShowAsync(sample, totalCount: 120);

        Assert.Equal(120, vm.TotalCount);
    }

    [Fact]
    public void ShowAsync_HeaderReflectsShownAndTotal()
    {
        var vm = new Tier2PreviewDialogViewModel();
        var sample = MakeSample(120);

        _ = vm.ShowAsync(sample, totalCount: 120);

        Assert.Contains("50", vm.Header);
        Assert.Contains("120", vm.Header);
    }

    [Fact]
    public void ShowAsync_FewerThan50Entries_ShowsAll()
    {
        var vm = new Tier2PreviewDialogViewModel();
        var sample = MakeSample(7);

        _ = vm.ShowAsync(sample, totalCount: 7);

        Assert.Equal(7, vm.Rows.Count);
        Assert.True(vm.IsOpen);
    }

    [Fact]
    public async Task Commit_CompletesTaskWithTrueAndClosesDialog()
    {
        var vm = new Tier2PreviewDialogViewModel();
        var task = vm.ShowAsync(MakeSample(3), totalCount: 3);

        vm.CommitCommand.Execute(null);

        Assert.True(await task);
        Assert.False(vm.IsOpen);
    }

    [Fact]
    public async Task Cancel_CompletesTaskWithFalseAndClosesDialog()
    {
        var vm = new Tier2PreviewDialogViewModel();
        var task = vm.ShowAsync(MakeSample(3), totalCount: 3);

        vm.CancelCommand.Execute(null);

        Assert.False(await task);
        Assert.False(vm.IsOpen);
    }

    [Fact]
    public void FlattenEntity_LeadsWithIdAndName()
    {
        var entity = Entity("{\"amount\":42,\"id\":\"C1\",\"name\":\"Acme\"}");

        var summary = Tier2PreviewDialogViewModel.FlattenEntity(entity);

        // id should appear before name, and name before amount.
        var idIdx = summary.IndexOf("id=C1", StringComparison.Ordinal);
        var nameIdx = summary.IndexOf("name=Acme", StringComparison.Ordinal);
        var amountIdx = summary.IndexOf("amount=42", StringComparison.Ordinal);
        Assert.True(idIdx >= 0 && nameIdx > idIdx && amountIdx > nameIdx);
    }

    [Fact]
    public void FlattenEntity_EmptyObject_ReturnsPlaceholder()
    {
        var summary = Tier2PreviewDialogViewModel.FlattenEntity(Entity("{}"));

        Assert.Equal("(empty)", summary);
    }
}
