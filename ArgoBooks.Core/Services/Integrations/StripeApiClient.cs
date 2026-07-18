using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace ArgoBooks.Core.Services.Integrations;

/// <summary>Result of validating a pasted Stripe key against the account endpoint.</summary>
public record StripeValidationResult(bool Ok, string? AccountLabel, string? ErrorMessage);

/// <summary>
/// Minimal Stripe REST client. Phase 1 only validates a key by reading the
/// account; later phases add balance-transaction fetching.
/// </summary>
public class StripeApiClient
{
    private const string AccountUrl = "https://api.stripe.com/v1/account";
    private readonly HttpClient _http;

    public StripeApiClient(HttpClient http) => _http = http;

    public async Task<StripeValidationResult> ValidateKeyAsync(string apiKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return new StripeValidationResult(false, null, "Enter your Stripe key first.");

        using var req = new HttpRequestMessage(HttpMethod.Get, AccountUrl);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());

        HttpResponseMessage resp;
        try
        {
            resp = await _http.SendAsync(req, ct);
        }
        catch (HttpRequestException)
        {
            return new StripeValidationResult(false, null,
                "Could not reach Stripe. Check your internet connection and try again.");
        }

        using (resp)
        {
            if (resp.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                return new StripeValidationResult(false, null,
                    "That key was rejected by Stripe. Check you pasted a valid key.");

            if (!resp.IsSuccessStatusCode)
                return new StripeValidationResult(false, null,
                    $"Stripe returned an unexpected error (HTTP {(int)resp.StatusCode}).");

            var body = await resp.Content.ReadAsStringAsync(ct);
            var label = ExtractLabel(body);
            return new StripeValidationResult(true, label, null);
        }
    }

    private static string? ExtractLabel(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("business_profile", out var bp)
                && bp.ValueKind == JsonValueKind.Object
                && bp.TryGetProperty("name", out var name)
                && name.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(name.GetString()))
                return name.GetString();

            if (root.TryGetProperty("email", out var email)
                && email.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(email.GetString()))
                return email.GetString();

            if (root.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.String)
                return id.GetString();
        }
        catch (JsonException) { /* fall through */ }
        return null;
    }
}
