using System.Text.Json;
using System.Text.Json.Serialization;

namespace ArgoBooks.Shared.Mobile;

/// <summary>
/// Manages the storage and retrieval of paired company records.
/// Stores a JSON list of companies under a fixed key, plus an active company UID key.
/// All secrets stay inside the ISecureStore (which uses Android Keystore on device).
/// </summary>
public class PairedCompanyStore
{
    private const string PairedCompaniesKey = "paired_companies";
    private const string ActiveCompanyUidKey = "active_company_uid";

    private readonly ISecureStore _secureStore;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// Initializes a new instance of PairedCompanyStore.
    /// </summary>
    /// <param name="secureStore">The secure storage implementation (ISecureStore).</param>
    public PairedCompanyStore(ISecureStore secureStore)
    {
        _secureStore = secureStore ?? throw new ArgumentNullException(nameof(secureStore));
    }

    /// <summary>
    /// Saves or updates a paired company record (upsert by CompanyUid).
    /// </summary>
    /// <param name="record">The company record to save.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SaveAsync(PairedCompanyRecord record)
    {
        if (record == null) throw new ArgumentNullException(nameof(record));
        if (string.IsNullOrWhiteSpace(record.CompanyUid))
            throw new ArgumentException("CompanyUid cannot be empty.", nameof(record));

        var companies = await GetAllAsync();

        // Remove existing record with same CompanyUid if present
        companies.RemoveAll(c => c.CompanyUid == record.CompanyUid);

        // Add the new record
        companies.Add(record);

        // Serialize and store
        var json = JsonSerializer.Serialize(companies, JsonOptions);
        await _secureStore.SetAsync(PairedCompaniesKey, json);
    }

    /// <summary>
    /// Retrieves all paired company records.
    /// </summary>
    /// <returns>A list of all paired companies, or an empty list if none exist.</returns>
    public async Task<List<PairedCompanyRecord>> GetAllAsync()
    {
        var json = await _secureStore.GetAsync(PairedCompaniesKey);

        if (string.IsNullOrEmpty(json))
        {
            return new List<PairedCompanyRecord>();
        }

        try
        {
            var companies = JsonSerializer.Deserialize<List<PairedCompanyRecord>>(json, JsonOptions);
            return companies ?? new List<PairedCompanyRecord>();
        }
        catch (JsonException)
        {
            // If the stored JSON is malformed, return empty list
            return new List<PairedCompanyRecord>();
        }
    }

    /// <summary>
    /// Retrieves the currently active paired company record.
    /// </summary>
    /// <returns>The active company record, or null if none is set.</returns>
    public async Task<PairedCompanyRecord?> GetActiveAsync()
    {
        var activeUid = await _secureStore.GetAsync(ActiveCompanyUidKey);

        if (string.IsNullOrEmpty(activeUid))
        {
            return null;
        }

        var companies = await GetAllAsync();
        return companies.FirstOrDefault(c => c.CompanyUid == activeUid);
    }

    /// <summary>
    /// Sets the active company by UID.
    /// </summary>
    /// <param name="companyUid">The UID of the company to set as active.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentException">Thrown if the company UID is not found.</exception>
    public async Task SetActiveAsync(string companyUid)
    {
        if (string.IsNullOrWhiteSpace(companyUid))
            throw new ArgumentException("Company UID cannot be empty.", nameof(companyUid));

        var companies = await GetAllAsync();
        if (!companies.Any(c => c.CompanyUid == companyUid))
        {
            throw new ArgumentException($"Company with UID '{companyUid}' not found.", nameof(companyUid));
        }

        await _secureStore.SetAsync(ActiveCompanyUidKey, companyUid);
    }

    /// <summary>
    /// Removes a paired company record by UID.
    /// If the removed company was the active one, clears the active selection.
    /// </summary>
    /// <param name="companyUid">The UID of the company to remove.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task RemoveAsync(string companyUid)
    {
        if (string.IsNullOrWhiteSpace(companyUid))
            throw new ArgumentException("Company UID cannot be empty.", nameof(companyUid));

        var companies = await GetAllAsync();
        var removed = companies.RemoveAll(c => c.CompanyUid == companyUid);

        if (removed > 0)
        {
            // Serialize and store updated list
            var json = JsonSerializer.Serialize(companies, JsonOptions);
            await _secureStore.SetAsync(PairedCompaniesKey, json);

            // If this was the active company, clear the active selection
            var activeUid = await _secureStore.GetAsync(ActiveCompanyUidKey);
            if (activeUid == companyUid)
            {
                await _secureStore.RemoveAsync(ActiveCompanyUidKey);
            }
        }
    }
}
