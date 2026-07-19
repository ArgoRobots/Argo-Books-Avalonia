using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace ArgoBooks.Core.Services.Integrations;

/// <summary>Result of validating a pasted Stripe key.</summary>
public record StripeValidationResult(bool Ok, string? AccountLabel, string? ErrorMessage);

/// <summary>A Stripe payout (the net deposit that lands in the bank).</summary>
public record StripePayoutSummary(string Id, long AmountCents, long CreatedUnix, string Status);

/// <summary>
/// Minimal Stripe REST client. Validates a key by reading balance transactions
/// (the same data the sync uses) and fetches balance transactions for import.
/// </summary>
public class StripeApiClient
{
    private const string BalanceTxUrl = "https://api.stripe.com/v1/balance_transactions";
    private const string PayoutsUrl = "https://api.stripe.com/v1/payouts";
    private const int MaxPages = 20;
    private readonly HttpClient _http;

    public StripeApiClient(HttpClient http) => _http = http;

    public async Task<StripeValidationResult> ValidateKeyAsync(string apiKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return new StripeValidationResult(false, null, "Enter your Stripe key first.");

        var key = apiKey.Trim();

        // Validate against the exact data the feature reads (balance transactions), so a
        // restricted key scoped only to Balance transactions / Charges / Payouts passes.
        // Validating against /v1/account would fail such a key with 403, since we do not
        // ask for account read access.
        using var req = new HttpRequestMessage(HttpMethod.Get, $"{BalanceTxUrl}?limit=1");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);

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
            if (resp.StatusCode == HttpStatusCode.Unauthorized)
                return new StripeValidationResult(false, null,
                    "That key was rejected by Stripe. Check you pasted a valid key.");

            if (resp.StatusCode == HttpStatusCode.Forbidden)
                return new StripeValidationResult(false, null,
                    "That key is missing read access. In Stripe, give it Read access to Balance transactions, Charges, and Payouts.");

            if (!resp.IsSuccessStatusCode)
                return new StripeValidationResult(false, null,
                    $"Stripe returned an unexpected error (HTTP {(int)resp.StatusCode}).");

            return new StripeValidationResult(true, ModeLabel(key), null);
        }
    }

    /// <summary>A friendly label from the key's mode, since a scoped key can't read the account name.</summary>
    private static string? ModeLabel(string key) =>
        key.Contains("_test_") ? "Test mode" : key.Contains("_live_") ? "Live account" : null;

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

    /// <summary>
    /// Lists the account's payouts (newest first). A payout is the net deposit that lands in the
    /// bank; its constituent charges/fees are fetched separately via <see cref="FetchPayoutTransactionsAsync"/>.
    /// </summary>
    public async Task<IReadOnlyList<StripePayoutSummary>> FetchPayoutsAsync(string apiKey, CancellationToken ct = default)
    {
        var results = new List<StripePayoutSummary>();
        string? after = null;

        for (var page = 0; page < MaxPages; page++)
        {
            var url = $"{PayoutsUrl}?limit=100";
            if (!string.IsNullOrEmpty(after))
                url += $"&starting_after={Uri.EscapeDataString(after)}";

            using var doc = await GetJsonAsync(apiKey, url, ct);
            var root = doc.RootElement;

            var count = 0;
            string? lastId = null;
            if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in data.EnumerateArray())
                {
                    var id = PropStr(el, "id");
                    if (string.IsNullOrEmpty(id)) continue;
                    results.Add(new StripePayoutSummary(id, PropNum(el, "amount"), PropNum(el, "created"), PropStr(el, "status")));
                    lastId = id;
                    count++;
                }
            }

            var hasMore = root.TryGetProperty("has_more", out var hm) && hm.ValueKind == JsonValueKind.True;
            if (!hasMore || count == 0 || lastId == null) break;
            after = lastId;
        }

        return results;
    }

    /// <summary>
    /// Fetches the balance transactions that make up a single payout (charges, fees, refunds, and the
    /// payout itself), using Stripe's <c>?payout=</c> filter. Stripe does not put a payout id on a
    /// balance transaction, so the payout id is stamped onto each returned item here.
    /// </summary>
    public async Task<IReadOnlyList<StripeBalanceTransaction>> FetchPayoutTransactionsAsync(
        string apiKey, string payoutId, CancellationToken ct = default)
    {
        var results = new List<StripeBalanceTransaction>();
        string? after = null;

        for (var page = 0; page < MaxPages; page++)
        {
            var url = $"{BalanceTxUrl}?limit=100&payout={Uri.EscapeDataString(payoutId)}";
            if (!string.IsNullOrEmpty(after))
                url += $"&starting_after={Uri.EscapeDataString(after)}";

            using var doc = await GetJsonAsync(apiKey, url, ct);
            var root = doc.RootElement;

            var count = 0;
            string? lastId = null;
            if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in data.EnumerateArray())
                {
                    // Stamp the payout id (the balance transaction itself doesn't carry it).
                    var tx = ParseTransaction(el) with { PayoutId = payoutId };
                    results.Add(tx);
                    lastId = tx.Id;
                    count++;
                }
            }

            var hasMore = root.TryGetProperty("has_more", out var hm) && hm.ValueKind == JsonValueKind.True;
            if (!hasMore || count == 0 || lastId == null) break;
            after = lastId;
        }

        return results;
    }

    private async Task<JsonDocument> GetJsonAsync(string apiKey, string url, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());
        using var resp = await _http.SendAsync(req, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
        {
            // Surface Stripe's own error message instead of a bare status code.
            var detail = ExtractStripeError(body) ?? resp.ReasonPhrase;
            throw new HttpRequestException($"Stripe {(int)resp.StatusCode}: {detail}");
        }
        return JsonDocument.Parse(body);
    }

    private static string? ExtractStripeError(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var err) && err.ValueKind == JsonValueKind.Object
                && err.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String)
                return m.GetString();
        }
        catch (JsonException) { /* not JSON */ }
        return null;
    }

    private static string PropStr(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString()! : "";

    private static long PropNum(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt64() : 0L;
}
