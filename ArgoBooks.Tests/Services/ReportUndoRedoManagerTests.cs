using ArgoBooks.Core.Models.Reports;
using ArgoBooks.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// Tests for the ReportUndoRedoManager class.
/// </summary>
public class ReportUndoRedoManagerTests
{
    #region RecordAction Tests

    [Fact]
    public void RecordAction_EnablesUndo()
    {
        var manager = new ReportUndoRedoManager();
        var action = new MockAction("Test");

        manager.RecordAction(action);

        Assert.True(manager.CanUndo);
    }

    [Fact]
    public void RecordAction_SetsUndoDescription()
    {
        var manager = new ReportUndoRedoManager();
        var action = new MockAction("Move element");

        manager.RecordAction(action);

        Assert.Equal("Move element", manager.UndoDescription);
    }

    [Fact]
    public void RecordAction_SetsHasUnsavedChanges()
    {
        var manager = new ReportUndoRedoManager();
        var action = new MockAction("Test");

        manager.RecordAction(action);

        Assert.True(manager.HasUnsavedChanges);
    }

    [Fact]
    public void RecordAction_ClearsRedoStack()
    {
        var manager = new ReportUndoRedoManager();
        manager.RecordAction(new MockAction("First"));
        manager.Undo();
        Assert.True(manager.CanRedo);

        manager.RecordAction(new MockAction("Second"));

        Assert.False(manager.CanRedo);
    }

    [Fact]
    public void RecordAction_WhenSuppressed_DoesNotRecord()
    {
        var manager = new ReportUndoRedoManager();
        manager.SuppressRecording = true;

        manager.RecordAction(new MockAction("Test"));

        Assert.False(manager.CanUndo);
    }

    #endregion

    #region Undo Tests

    [Fact]
    public void Undo_CallsUndoOnAction()
    {
        var manager = new ReportUndoRedoManager();
        var action = new MockAction("Test");
        manager.RecordAction(action);

        manager.Undo();

        Assert.True(action.UndoCalled);
    }

    [Fact]
    public void Undo_EnablesRedo()
    {
        var manager = new ReportUndoRedoManager();
        manager.RecordAction(new MockAction("Test"));

        manager.Undo();

        Assert.True(manager.CanRedo);
    }

    [Fact]
    public void Undo_DisablesUndoWhenEmpty()
    {
        var manager = new ReportUndoRedoManager();
        manager.RecordAction(new MockAction("Test"));

        manager.Undo();

        Assert.False(manager.CanUndo);
    }

    [Fact]
    public void Undo_WhenCannotUndo_DoesNothing()
    {
        var manager = new ReportUndoRedoManager();

        manager.Undo(); // Should not throw

        Assert.False(manager.CanUndo);
    }

    #endregion

    #region Redo Tests

    [Fact]
    public void Redo_CallsRedoOnAction()
    {
        var manager = new ReportUndoRedoManager();
        var action = new MockAction("Test");
        manager.RecordAction(action);
        manager.Undo();

        manager.Redo();

        Assert.True(action.RedoCalled);
    }

    [Fact]
    public void Redo_ReEnablesUndo()
    {
        var manager = new ReportUndoRedoManager();
        manager.RecordAction(new MockAction("Test"));
        manager.Undo();

        manager.Redo();

        Assert.True(manager.CanUndo);
    }

    [Fact]
    public void Redo_WhenCannotRedo_DoesNothing()
    {
        var manager = new ReportUndoRedoManager();

        manager.Redo(); // Should not throw

        Assert.False(manager.CanRedo);
    }

    #endregion

    #region Clear Tests

    [Fact]
    public void Clear_ResetsUndoStack()
    {
        var manager = new ReportUndoRedoManager();
        manager.RecordAction(new MockAction("Test"));

        manager.Clear();

        Assert.False(manager.CanUndo);
    }

    [Fact]
    public void Clear_ResetsRedoStack()
    {
        var manager = new ReportUndoRedoManager();
        manager.RecordAction(new MockAction("Test"));
        manager.Undo();

        manager.Clear();

        Assert.False(manager.CanRedo);
    }

    [Fact]
    public void Clear_ResetsHasUnsavedChanges()
    {
        var manager = new ReportUndoRedoManager();
        manager.RecordAction(new MockAction("Test"));

        manager.Clear();

        Assert.False(manager.HasUnsavedChanges);
    }

    #endregion

    #region MarkSaved Tests

    [Fact]
    public void MarkSaved_ClearsHasUnsavedChanges()
    {
        var manager = new ReportUndoRedoManager();
        manager.RecordAction(new MockAction("Test"));
        Assert.True(manager.HasUnsavedChanges);

        manager.MarkSaved();

        Assert.False(manager.HasUnsavedChanges);
    }

    [Fact]
    public void MarkSaved_NewChangeAfter_SetsUnsaved()
    {
        var manager = new ReportUndoRedoManager();
        manager.RecordAction(new MockAction("First"));
        manager.MarkSaved();

        manager.RecordAction(new MockAction("Second"));

        Assert.True(manager.HasUnsavedChanges);
    }

    [Fact]
    public void MarkSaved_UndoAfter_SetsUnsaved()
    {
        var manager = new ReportUndoRedoManager();
        manager.RecordAction(new MockAction("Test"));
        manager.MarkSaved();

        manager.Undo();

        Assert.True(manager.HasUnsavedChanges);
    }

    [Fact]
    public void MarkSaved_UndoThenRedo_ClearsUnsaved()
    {
        var manager = new ReportUndoRedoManager();
        manager.RecordAction(new MockAction("Test"));
        manager.MarkSaved();

        manager.Undo();
        manager.Redo();

        Assert.False(manager.HasUnsavedChanges);
    }

    #endregion

    #region History Tests

    [Fact]
    public void UndoHistory_ReturnsDescriptions()
    {
        var manager = new ReportUndoRedoManager();
        manager.RecordAction(new MockAction("First"));
        manager.RecordAction(new MockAction("Second"));

        var history = manager.UndoHistory.ToList();

        Assert.Equal(2, history.Count);
        Assert.Contains("First", history);
        Assert.Contains("Second", history);
    }

    [Fact]
    public void RedoHistory_ReturnsDescriptions()
    {
        var manager = new ReportUndoRedoManager();
        manager.RecordAction(new MockAction("First"));
        manager.RecordAction(new MockAction("Second"));
        manager.Undo();

        var history = manager.RedoHistory.ToList();

        Assert.Single(history);
        Assert.Equal("Second", history[0]);
    }

    #endregion

    #region MaxStackSize Tests

    [Fact]
    public void RecordAction_ExceedsMaxSize_RemovesOldest()
    {
        var manager = new ReportUndoRedoManager(3);

        manager.RecordAction(new MockAction("First"));
        manager.RecordAction(new MockAction("Second"));
        manager.RecordAction(new MockAction("Third"));
        manager.RecordAction(new MockAction("Fourth"));

        var history = manager.UndoHistory.ToList();
        Assert.Equal(3, history.Count);
        Assert.DoesNotContain("First", history);
    }

    #endregion

    #region StateChanged Event Tests

    [Fact]
    public void RecordAction_FiresStateChanged()
    {
        var manager = new ReportUndoRedoManager();
        var fired = false;
        manager.StateChanged += (_, _) => fired = true;

        manager.RecordAction(new MockAction("Test"));

        Assert.True(fired);
    }

    [Fact]
    public void Undo_FiresStateChanged()
    {
        var manager = new ReportUndoRedoManager();
        manager.RecordAction(new MockAction("Test"));
        var fired = false;
        manager.StateChanged += (_, _) => fired = true;

        manager.Undo();

        Assert.True(fired);
    }

    [Fact]
    public void Redo_FiresStateChanged()
    {
        var manager = new ReportUndoRedoManager();
        manager.RecordAction(new MockAction("Test"));
        manager.Undo();
        var fired = false;
        manager.StateChanged += (_, _) => fired = true;

        manager.Redo();

        Assert.True(fired);
    }

    [Fact]
    public void Clear_FiresStateChanged()
    {
        var manager = new ReportUndoRedoManager();
        manager.RecordAction(new MockAction("Test"));
        var fired = false;
        manager.StateChanged += (_, _) => fired = true;

        manager.Clear();

        Assert.True(fired);
    }

    #endregion

    #region PropertyChanged Event Tests

    [Fact]
    public void RecordAction_FiresPropertyChanged_ForCanUndo()
    {
        var manager = new ReportUndoRedoManager();
        var changedProps = new List<string>();
        manager.PropertyChanged += (_, e) => changedProps.Add(e.PropertyName!);

        manager.RecordAction(new MockAction("Test"));

        Assert.Contains(nameof(ReportUndoRedoManager.CanUndo), changedProps);
    }

    #endregion

    #region Add/Remove Element Action Integration Tests

    [Fact]
    public void AddThenDelete_UndoBoth_ElementIsRemoved()
    {
        // Regression test: add element, delete it, undo both → element should be gone
        var config = new ReportConfiguration();
        var manager = new ReportUndoRedoManager();

        // Step 1: Add element
        var element = new LabelReportElement { X = 10, Y = 20, Width = 100, Height = 50 };
        config.AddElement(element);
        manager.RecordAction(new AddElementAction(config, element));
        Assert.Single(config.Elements);

        // Step 2: Delete element
        manager.RecordAction(new RemoveElementAction(config, element));
        config.RemoveElement(element.Id);
        Assert.Empty(config.Elements);

        // Step 3: Undo delete → element should reappear
        manager.Undo();
        Assert.Single(config.Elements);

        // Step 4: Undo add → element should be gone
        manager.Undo();
        Assert.Empty(config.Elements);
    }

    [Fact]
    public void AddThenDelete_UndoBoth_RedoBoth_ElementIsRemoved()
    {
        // Full round-trip: add, delete, undo×2, redo×2
        var config = new ReportConfiguration();
        var manager = new ReportUndoRedoManager();

        var element = new LabelReportElement { X = 10, Y = 20, Width = 100, Height = 50 };
        config.AddElement(element);
        manager.RecordAction(new AddElementAction(config, element));

        manager.RecordAction(new RemoveElementAction(config, element));
        config.RemoveElement(element.Id);

        // Undo both
        manager.Undo(); // undo delete
        manager.Undo(); // undo add
        Assert.Empty(config.Elements);

        // Redo both
        manager.Redo(); // redo add
        Assert.Single(config.Elements);
        manager.Redo(); // redo delete
        Assert.Empty(config.Elements);
    }

    #endregion

    #region Mock Classes

    private class MockAction : IReportUndoableAction
    {
        public string Description { get; }
        public bool UndoCalled { get; private set; }
        public bool RedoCalled { get; private set; }

        public MockAction(string description)
        {
            Description = description;
        }

        public void Undo() => UndoCalled = true;
        public void Redo() => RedoCalled = true;
    }

    #endregion
}
