using System.Net.Http.Headers;
using System.Text.Json;
using ArgoBooks.Core.Models.BankMatching;
using ArgoBooks.Core.Models.Telemetry;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Sends a PDF bank statement to the AI proxy and returns parsed rows. Mirrors the receipt
/// proxy's auth/multipart contract. Premium-gated and usage-counted by the caller.
/// </summary>
public class PdfStatementExtractor(LicenseService? licenseService, IErrorLogger? errorLogger = null) : IPdfStatementExtractor
{
    // NOTE: endpoint path /api/bank/extract.php is a placeholder pending backend confirmation.
    private static readonly string ExtractEndpoint = $"{ApiConfig.BaseUrl}/api/bank/extract.php";
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(120) };

    public bool IsConfigured => licenseService?.LoadLicense() == true;

    public async Task<List<BankStatementLine>> ExtractAsync(byte[] pdfData, string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            var licenseKey = licenseService?.GetLicenseKey() ?? "";
            var deviceId = licenseService?.GetDeviceId() ?? "";

            using var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(pdfData);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            content.Add(fileContent, "statement", fileName);

            using var request = new HttpRequestMessage(HttpMethod.Post, ExtractEndpoint) { Content = content };
            if (!string.IsNullOrEmpty(licenseKey))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", licenseKey);
                request.Headers.Add("X-License-Key", licenseKey);
            }
            if (!string.IsNullOrEmpty(deviceId))
                request.Headers.Add("X-Device-Id", deviceId);

            using var response = await Http.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParseRows(body);
        }
        catch (Exception ex)
        {
            errorLogger?.LogError(ex, ErrorCategory.Api, "PDF statement extraction failed");
            return [];
        }
    }

    public static List<BankStatementLine> ParseRows(string json)
    {
        var rows = new List<BankStatementLine>();
        var root = JsonSerializer.Deserialize<JsonElement>(json);
        if (!root.TryGetProperty("success", out var ok) || !ok.GetBoolean()) return rows;
        if (!root.TryGetProperty("lines", out var lines) || lines.ValueKind != JsonValueKind.Array) return rows;

        foreach (var el in lines.EnumerateArray())
        {
            var line = new BankStatementLine
            {
                Id = Guid.NewGuid().ToString("N"),
                Description = el.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "",
                Amount = el.TryGetProperty("amount", out var a) && a.TryGetDecimal(out var amt) ? amt : 0m
            };
            if (el.TryGetProperty("date", out var dt) && DateTime.TryParse(dt.GetString(), out var parsed))
                line.Date = parsed;
            rows.Add(line);
        }
        return rows;
    }
}
