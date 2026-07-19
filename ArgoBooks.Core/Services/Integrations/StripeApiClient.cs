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
    private const string BalanceTxUrl = "https://api.stripe.com/v1/balance_transactions";
    private const int MaxPages = 20;
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

    public async Task<IReadOnlyList<StripeBalanceTransaction>> FetchBalanceTransactionsSinceAsync(
        string apiKey, string? afterCursor, CancellationToken ct = default)
    {
        var results = new List<StripeBalanceTransaction>();
        string? cursor = afterCursor;

        for (var page = 0; page < MaxPages; page++)
        {
            var url = $"{BalanceTxUrl}?limit=100";
            if (!string.IsNullOrEmpty(cursor))
                url += $"&starting_after={Uri.EscapeDataString(cursor)}";

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());

            using var resp = await _http.SendAsync(req, ct);
            resp.EnsureSuccessStatusCode();
            var body = await resp.Content.ReadAsStringAsync(ct);

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            var pageCount = 0;
            string? lastId = null;
            if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in data.EnumerateArray())
                {
                    var tx = ParseTransaction(el);
                    results.Add(tx);
                    lastId = tx.Id;
                    pageCount++;
                }
            }

            var hasMore = root.TryGetProperty("has_more", out var hm)
                          && hm.ValueKind == JsonValueKind.True;
            if (!hasMore || pageCount == 0 || lastId == null)
                break;
            cursor = lastId;
        }

        return results;
    }

    private static StripeBalanceTransaction ParseTransaction(JsonElement el)
    {
        static string Str(JsonElement e, string name) =>
            e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString()! : "";
        static string? StrOrNull(JsonElement e, string name) =>
            e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
        static long Num(JsonElement e, string name) =>
            e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt64() : 0L;
        static string? PayoutId(JsonElement e)
        {
            if (!e.TryGetProperty("payout", out var p)) return null;
            if (p.ValueKind == JsonValueKind.String) return p.GetString();
            if (p.ValueKind == JsonValueKind.Object && p.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.String)
                return id.GetString();
            return null;
        }

        return new StripeBalanceTransaction(
            Id: Str(el, "id"),
            Type: Str(el, "type"),
            AmountCents: Num(el, "amount"),
            FeeCents: Num(el, "fee"),
            NetCents: Num(el, "net"),
            CreatedUnix: Num(el, "created"),
            Currency: Str(el, "currency"),
            Description: StrOrNull(el, "description"),
            PayoutId: PayoutId(el));
    }
}
