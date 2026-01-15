using ArgoBooks.Core.Models.AI;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Service interface for OpenAI API interactions.
/// </summary>
public interface IOpenAiService
{
    /// <summary>
    /// Whether the service is configured with valid API credentials.
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Gets AI suggestions for supplier and category based on receipt data.
    /// </summary>
    /// <param name="request">Receipt analysis request with vendor info and existing data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Supplier and category suggestions.</returns>
    Task<SupplierCategorySuggestion?> GetSupplierCategorySuggestionAsync(
        ReceiptAnalysisRequest request,
        CancellationToken cancellationToken = default);
}
