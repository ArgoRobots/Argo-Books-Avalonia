using ArgoBooks.Core.Models.Payroll;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Fetches a CRA rate edition from the server, so a changeover is a file upload rather than an
/// app release.
///
/// CRA publishes twice a year on dates nobody chooses, and until this existed the only way a
/// customer got a new edition was a release shipped inside a three week window in December and
/// June. Miss it and payroll stops for everyone at once. Delivered the same way language files
/// are: <see cref="PayrollRateService"/> already reads a cache directory before falling back to
/// the copy embedded in the assembly, so this only has to fill that directory.
///
/// Every refusal below leaves what is already on disk untouched. That is the whole design. An
/// app still calculating on last month's verified edition is in a far better state than one
/// that has replaced it with a truncated response, and a rate table is the one download here
/// where being wrong is both silent and expensive: it decides what comes off someone's pay,
/// and a plausible wrong number produces a plausible wrong deduction that nothing questions.
/// </summary>
public class PayrollRateUpdateService
{
    private static readonly string UrlTemplate = ApiConfig.BaseUrl + "/resources/downloads/payroll/{0}.json";

    private readonly PayrollRateService _rates;
    private readonly HttpClient _http;

    /// <param name="handler">
    /// Injected so the refusals can be tested without a server. Left null in the app, which
    /// gets the ordinary client.
    /// </param>
    public PayrollRateUpdateService(PayrollRateService rates, HttpMessageHandler? handler = null)
    {
        _rates = rates ?? throw new ArgumentNullException(nameof(rates));
        _http = handler == null
            ? new HttpClient { Timeout = TimeSpan.FromSeconds(30) }
            : new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
    }

    /// <summary>
    /// The edition a pay date needs, named the way CRA names them: the half of the year the
    /// edition takes effect in.
    ///
    /// Derived from the date rather than read from a table, because this is used in exactly the
    /// situation where no table is loaded. That is also why it is not a guess: an edition
    /// covers one half of one year by definition, so the id is a fact about the date.
    /// </summary>
    public static string EditionIdFor(DateTime payDate) =>
        $"{payDate.Year:D4}-{(payDate.Month <= 6 ? "01" : "07")}";

    /// <summary>
    /// Fetches whichever edition covers this pay date. The call to make when a pay run cannot
    /// find a table, which is the moment the user actually needs one.
    /// </summary>
    public Task<bool> TryUpdateForDateAsync(DateTime payDate, CancellationToken cancellationToken = default) =>
        TryUpdateAsync(EditionIdFor(payDate), cancellationToken);

    /// <summary>
    /// Downloads one edition and, if it survives every check, caches it and makes it usable
    /// immediately.
    /// </summary>
    /// <returns>
    /// True only when the cache now holds this edition because of this call. False covers
    /// everything else, including the entirely ordinary case of the file not being published
    /// yet, so a caller cannot treat it as an error worth showing anyone.
    /// </returns>
    public async Task<bool> TryUpdateAsync(string editionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(editionId))
        {
            return false;
        }

        string json;

        try
        {
            using HttpResponseMessage response = await _http.GetAsync(
                string.Format(UrlTemplate, editionId), cancellationToken);

            // A 404 is the normal answer between the reminder going out and the file being
            // uploaded, so it is not distinguished from any other refusal here.
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            json = await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (Exception)
        {
            // Offline, DNS failure, timeout, cancelled. This runs unattended in the background,
            // so there is nobody to tell and nothing to do but leave the cache alone.
            return false;
        }

        PayrollRateTable? table = Parse(json);

        if (table == null)
        {
            return false;
        }

        // A server misconfigured to serve one edition under every name would otherwise overwrite
        // the file for a period it does not cover.
        if (!string.Equals(table.EditionId, editionId, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (PayrollRateValidator.Validate(table).Count > 0)
        {
            return false;
        }

        return Write(editionId, json);
    }

    private static PayrollRateTable? Parse(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<PayrollRateTable>(json);
        }
        catch (JsonException)
        {
            // Truncated, or a sign-in page from a captive portal answering 200 with HTML.
            return null;
        }
    }

    /// <summary>
    /// Writes the edition and drops the parsed copies, so the new table is picked up without a
    /// restart. Via a temporary file and a move, because the reader has no lock against a
    /// half-written one and would treat it as corrupt.
    /// </summary>
    private bool Write(string editionId, string json)
    {
        try
        {
            Directory.CreateDirectory(_rates.CacheDirectory);

            string destination = Path.Combine(_rates.CacheDirectory, editionId + ".json");
            string temporary = destination + ".tmp";

            File.WriteAllText(temporary, json);
            File.Move(temporary, destination, overwrite: true);

            _rates.Invalidate();
            return true;
        }
        catch (Exception)
        {
            // A read-only or full disk. The embedded editions still work.
            return false;
        }
    }
}
