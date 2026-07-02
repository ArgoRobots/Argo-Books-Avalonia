using System;
using ArgoBooks;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Services;

namespace ArgoBooks.Tests.ViewModels;

/// <summary>
/// Test harness for the transaction modal ViewModels (expenses, revenues, invoices, payments,
/// purchase orders, rentals). These ViewModels read <c>App.CompanyManager.CompanyData</c> and record
/// undo/redo actions on the shared <c>App.UndoRedoManager</c>; without a harness there is no way to
/// exercise their save / edit / undo / redo flows, which is exactly where multi-currency and
/// undo-asymmetry bugs have repeatedly hidden. Each test gets a fresh in-memory company and a cleared
/// undo stack; the shared statics are reset on dispose so tests don't leak into each other.
///
/// xUnit constructs a new instance of the test class per test method, so the ctor/Dispose here act
/// as per-test setup/teardown.
/// </summary>
public abstract class ModalViewModelTestBase : IDisposable
{
    protected CompanyData Company { get; }

    protected ModalViewModelTestBase()
    {
        Company = new CompanyData();
        App.SetCompanyManagerForTesting(CompanyManager.CreateForTesting(Company));
        App.UndoRedoManager.Clear();
    }

    /// <summary>Undo the most recent recorded action.</summary>
    protected static void Undo() => App.UndoRedoManager.Undo();

    /// <summary>Redo the most recently undone action.</summary>
    protected static void Redo() => App.UndoRedoManager.Redo();

    public void Dispose()
    {
        App.UndoRedoManager.Clear();
        App.SetCompanyManagerForTesting(null);
        GC.SuppressFinalize(this);
    }
}
