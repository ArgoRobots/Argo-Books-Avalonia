using ArgoBooks.Core.Models.AI;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Service interface for Gemini API interactions.
/// </summary>
public interface IGeminiService
{
    /// <summary>
    /// Whether the service is configured with valid API credentials.
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Gets AI suggestions for supplier and category based on receipt data.
    /// </summary>
    /// <param name="request">Receipt analysis request with supplier info and existing data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Supplier and category suggestions.</returns>
    Task<SupplierCategorySuggestion?> GetSupplierCategorySuggestionAsync(
        ReceiptAnalysisRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a generic chat completion request to the Gemini API.
    /// </summary>
    /// <param name="systemPrompt">The system prompt.</param>
    /// <param name="userPrompt">The user prompt.</param>
    /// <param name="maxTokens">Maximum tokens in the response.</param>
    /// <param name="temperature">Sampling temperature (0.0 = deterministic, 1.0 = creative).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The model's response text, or null on failure.</returns>
    Task<string?> SendChatAsync(
        string systemPrompt,
        string userPrompt,
        int maxTokens = 4000,
        double temperature = 0.1,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a vision chat completion request with a base64-encoded image.
    /// </summary>
    /// <param name="systemPrompt">The system prompt.</param>
    /// <param name="userPrompt">The user prompt.</param>
    /// <param name="base64Image">Base64-encoded image data.</param>
    /// <param name="mimeType">MIME type of the image (e.g., "image/jpeg").</param>
    /// <param name="maxTokens">Maximum tokens in the response.</param>
    /// <param name="temperature">Sampling temperature.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The model's response text, or null on failure.</returns>
    Task<string?> SendVisionChatAsync(
        string systemPrompt,
        string userPrompt,
        string base64Image,
        string mimeType,
        int maxTokens = 4000,
        double temperature = 0.1,
        string? model = null,
        CancellationToken cancellationToken = default);
}
