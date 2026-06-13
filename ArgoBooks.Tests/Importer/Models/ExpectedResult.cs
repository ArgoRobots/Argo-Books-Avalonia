using System.Text.Json.Serialization;

namespace ArgoBooks.Tests.Importer.Models;

public sealed class ExpectedResult
{
    [JsonPropertyName("sheets")] public List<ExpectedSheet> Sheets { get; set; } = [];
    [JsonPropertyName("import")] public ExpectedImport Import { get; set; } = new();
    [JsonPropertyName("keyRecords")] public List<ExpectedKeyRecord> KeyRecords { get; set; } = [];
    [JsonPropertyName("expectedDropReasonSubstrings")] public List<string> ExpectedDropReasonSubstrings { get; set; } = [];
    [JsonPropertyName("unsupportedSheets")] public List<string> UnsupportedSheets { get; set; } = [];
}

public sealed class ExpectedSheet
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("detectedType")] public string DetectedType { get; set; } = "";
    [JsonPropertyName("tier")] public string Tier { get; set; } = "";
}

public sealed class ExpectedImport
{
    [JsonPropertyName("totalImported")] public int TotalImported { get; set; }
    [JsonPropertyName("totalUpdated")] public int TotalUpdated { get; set; }
}

public sealed class ExpectedKeyRecord
{
    [JsonPropertyName("type")] public string Type { get; set; } = "";
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("name")] public string? Name { get; set; }
}
