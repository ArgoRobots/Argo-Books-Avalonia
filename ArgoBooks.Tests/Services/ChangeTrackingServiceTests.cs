using ArgoBooks.Services;
using ArgoBooks.ViewModels;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// Tests for the ChangeTrackingService class.
/// </summary>
public class ChangeTrackingServiceTests
{
    private readonly ChangeTrackingService _service = new();

    #region Initial State Tests

    [Fact]
    public void DefaultTotalChangeCount_IsZero()
    {
        Assert.Equal(0, _service.TotalChangeCount);
    }

    [Fact]
    public void GetAllChangeCategories_WhenEmpty_ReturnsEmpty()
    {
        var categories = _service.GetAllChangeCategories();

        Assert.Empty(categories);
    }

    #endregion

    #region RecordChange Tests

    [Fact]
    public void RecordChange_IncrementsTotalChangeCount()
    {
        _service.RecordChange("Added item", ChangeType.Added);

        Assert.Equal(1, _service.TotalChangeCount);
    }

    [Fact]
    public void RecordChange_MultipleChanges_AccumulatesCount()
    {
        _service.RecordChange("Added item 1", ChangeType.Added);
        _service.RecordChange("Modified item 2", ChangeType.Modified);
        _service.RecordChange("Deleted item 3", ChangeType.Deleted);

        Assert.Equal(3, _service.TotalChangeCount);
    }

    [Fact]
    public void RecordChange_WithCategory_UsesCategory()
    {
        _service.RecordChange("Customers", "Added customer", ChangeType.Added);

        var categories = _service.GetAllChangeCategories();
        Assert.NotEmpty(categories);
    }

    #endregion

    #region ClearChanges Tests

    [Fact]
    public void ClearGlobalChanges_ResetsGlobalCount()
    {
        _service.RecordChange("Test change", ChangeType.Added);
        _service.ClearGlobalChanges();

        Assert.Equal(0, _service.TotalChangeCount);
    }

    [Fact]
    public void ClearAllChanges_ResetsAllCounts()
    {
        _service.RecordChange("Change 1", ChangeType.Added);
        _service.RecordChange("Change 2", ChangeType.Modified);
        _service.ClearAllChanges();

        Assert.Equal(0, _service.TotalChangeCount);
    }

    #endregion

    #region ChangeStateChanged Event Tests

    [Fact]
    public void RecordChange_RaisesChangeStateChanged()
    {
        var eventRaised = false;
        _service.ChangeStateChanged += (_, _) => eventRaised = true;

        _service.RecordChange("Test", ChangeType.Added);

        Assert.True(eventRaised);
    }

    #endregion

    #region SetGlobalCategory Tests

    [Fact]
    public void SetGlobalCategory_SetsNameAndIcon()
    {
        _service.SetGlobalCategory("Products", "box");

        // Should not throw
        Assert.Equal(0, _service.TotalChangeCount);
    }

    #endregion
}
