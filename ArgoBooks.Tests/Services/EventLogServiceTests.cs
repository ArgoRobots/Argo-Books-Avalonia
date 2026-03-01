using ArgoBooks.Core.Models;
using ArgoBooks.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// Tests for the EventLogService class.
/// </summary>
public class EventLogServiceTests
{
    private readonly EventLogService _service = new();

    #region Helper

    private static IUndoableAction CreateMockAction(string description = "Test")
    {
        return new DelegateAction(description, () => { }, () => { });
    }

    #endregion

    #region RecordEvent Tests

    [Fact]
    public void RecordEvent_AddsToEventList()
    {
        _service.RecordEvent(CreateMockAction("Added customer"), "Added customer", AuditAction.Added, "Customer", "CUS-001", "John Doe");

        var events = _service.GetEvents();
        Assert.Single(events);
    }

    [Fact]
    public void RecordEvent_Multiple_AccumulatesEvents()
    {
        _service.RecordEvent(CreateMockAction("Added customer 1"), "Added customer 1", AuditAction.Added, "Customer", "CUS-001", "John");
        _service.RecordEvent(CreateMockAction("Added customer 2"), "Added customer 2", AuditAction.Added, "Customer", "CUS-002", "Jane");

        var events = _service.GetEvents();
        Assert.Equal(2, events.Count);
    }

    [Fact]
    public void RecordEvent_RaisesEventsChanged()
    {
        var eventRaised = false;
        _service.EventsChanged += (_, _) => eventRaised = true;

        _service.RecordEvent(CreateMockAction("Added customer"), "Added customer", AuditAction.Added, "Customer", "CUS-001", "John");

        Assert.True(eventRaised);
    }

    #endregion

    #region Clear Tests

    [Fact]
    public void Clear_RemovesAllEvents()
    {
        _service.RecordEvent(CreateMockAction("Added customer"), "Added customer", AuditAction.Added, "Customer", "CUS-001", "John");
        _service.Clear();

        var events = _service.GetEvents();
        Assert.Empty(events);
    }

    #endregion

    #region GetFilteredEvents Tests

    [Fact]
    public void GetFilteredEvents_ByEntityType_FiltersCorrectly()
    {
        _service.RecordEvent(CreateMockAction("Added customer"), "Added customer", AuditAction.Added, "Customer", "CUS-001", "John");
        _service.RecordEvent(CreateMockAction("Added product"), "Added product", AuditAction.Added, "Product", "PRD-001", "Widget");

        var filtered = _service.GetFilteredEvents(entityTypeFilter: "Customer");
        Assert.Single(filtered);
    }

    #endregion
}
