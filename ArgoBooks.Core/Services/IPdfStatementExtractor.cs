using ArgoBooks.Core.Models.BankMatching;

namespace ArgoBooks.Core.Services;

public interface IPdfStatementExtractor
{
    bool IsConfigured { get; }
    Task<List<BankStatementLine>> ExtractAsync(byte[] pdfData, string fileName, CancellationToken cancellationToken = default);
}
