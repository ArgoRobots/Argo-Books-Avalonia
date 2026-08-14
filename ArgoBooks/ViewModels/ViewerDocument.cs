namespace ArgoBooks.ViewModels;

/// <summary>
/// One named document in a set the viewer can page between, for example one employee's pay stub.
///
/// The bytes are produced on demand rather than passed in. A hundred employees means a hundred
/// PDFs to compose and rasterise, and nobody reads a hundred stubs: they open one person. Holding
/// a factory rather than a byte array means the other ninety-nine are never built.
/// </summary>
public class ViewerDocument
{
    /// <summary>Shown in the picker. Usually the employee's name.</summary>
    public required string Name { get; init; }

    /// <summary>Suggested file name when this document is saved.</summary>
    public required string FileName { get; init; }

    /// <summary>
    /// Builds the PDF. Called on the UI thread, so implementations hand the actual rendering to
    /// a background task; QuestPDF composition is slow enough to be felt.
    /// </summary>
    public required Func<Task<byte[]>> LoadAsync { get; init; }

    /// <summary>Shown in the picker, so the display can be a plain string binding.</summary>
    public override string ToString() => Name;
}
