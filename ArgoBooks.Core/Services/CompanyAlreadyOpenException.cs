namespace ArgoBooks.Core.Services;

/// <summary>
/// Thrown when an attempt is made to open a company file that is already open in another running
/// instance of the app. Opening the same company twice would let both instances auto-save over the
/// same <c>.argo</c> file and lose or corrupt data, so the second open is blocked.
/// </summary>
public sealed class CompanyAlreadyOpenException(string filePath)
    : Exception("This company is already open in another window.")
{
    /// <summary>The path of the company file that is already open elsewhere.</summary>
    public string FilePath { get; } = filePath;
}
