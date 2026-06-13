using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;
using ArgoBooks.Localization;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// A single flattened preview row for the Tier 2 preview dialog. Each row summarizes
/// one AI-normalized entity as a readable one-line string.
/// </summary>
public sealed class Tier2PreviewRow
{
    public required string Sheet { get; init; }
    public required string Summary { get; init; }
}

/// <summary>
/// ViewModel for the Tier 2 preview dialog. Lets the user review a capped sample of
/// AI-normalized rows before they are committed to the company data, and choose to
/// commit or cancel the Tier 2 import.
/// </summary>
public partial class Tier2PreviewDialogViewModel : ViewModelBase
{
    /// <summary>Maximum number of sample rows shown in the preview.</summary>
    public const int MaxSampleSize = 50;

    [ObservableProperty]
    private bool _isOpen;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private string _header = string.Empty;

    public ObservableCollection<Tier2PreviewRow> Rows { get; } = [];

    private TaskCompletionSource<bool>? _completionSource;

    /// <summary>
    /// Shows the preview dialog with a capped sample of AI-processed entities.
    /// Returns true when the user commits the import, false when they cancel.
    /// </summary>
    /// <param name="sample">Entities paired with their source sheet name. Capped at <see cref="MaxSampleSize"/>.</param>
    /// <param name="totalCount">Total number of processed entities across all Tier 2 sheets.</param>
    public Task<bool> ShowAsync(
        IReadOnlyList<(string SheetName, JsonElement Entity)> sample,
        int totalCount)
    {
        ArgumentNullException.ThrowIfNull(sample);

        Rows.Clear();
        foreach (var (sheetName, entity) in sample.Take(MaxSampleSize))
        {
            Rows.Add(new Tier2PreviewRow
            {
                Sheet = sheetName,
                Summary = FlattenEntity(entity)
            });
        }

        TotalCount = totalCount;
        Header = "Showing {0} of {1}".TranslateFormat(Rows.Count, totalCount);

        IsOpen = true;
        _completionSource = new TaskCompletionSource<bool>();
        return _completionSource.Task;
    }

    /// <summary>
    /// Flattens a JSON entity object into a readable one-line summary. Prefers an id/name
    /// property up front, then joins the first few key=value properties.
    /// </summary>
    public static string FlattenEntity(JsonElement entity)
    {
        if (entity.ValueKind != JsonValueKind.Object)
            return entity.ToString();

        var props = entity.EnumerateObject().ToList();

        // Lead with an id or name if present, so each row is easy to recognize.
        var leadKeys = new[] { "id", "name", "title", "description" };
        var ordered = props
            .OrderBy(p =>
            {
                var idx = Array.FindIndex(leadKeys, k => string.Equals(k, p.Name, StringComparison.OrdinalIgnoreCase));
                return idx < 0 ? int.MaxValue : idx;
            })
            .ToList();

        var parts = new List<string>();
        foreach (var prop in ordered.Take(5))
        {
            var value = FormatValue(prop.Value);
            if (string.IsNullOrWhiteSpace(value)) continue;
            parts.Add($"{prop.Name}={value}");
        }

        return parts.Count > 0 ? string.Join("  ", parts) : "(empty)";
    }

    private static string FormatValue(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? string.Empty,
            JsonValueKind.Null or JsonValueKind.Undefined => string.Empty,
            JsonValueKind.Object or JsonValueKind.Array => value.GetRawText(),
            _ => value.ToString()
        };
    }

    /// <summary>
    /// Builds CSV text for the displayed sample with header Sheet,Summary.
    /// </summary>
    public string BuildSampleCsv()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Sheet,Summary");
        foreach (var row in Rows)
        {
            sb.Append(CsvQuote(row.Sheet));
            sb.Append(',');
            sb.AppendLine(CsvQuote(row.Summary));
        }
        return sb.ToString();

        static string CsvQuote(string field)
        {
            if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
                return "\"" + field.Replace("\"", "\"\"") + "\"";
            return field;
        }
    }

    [RelayCommand]
    private async Task ExportSample()
    {
        try
        {
            var topLevel = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;
            if (topLevel?.StorageProvider == null) return;

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Export AI Preview Sample",
                SuggestedFileName = "ai-preview-sample.csv",
                DefaultExtension = "csv",
                FileTypeChoices = [new FilePickerFileType("CSV file") { Patterns = ["*.csv"] }]
            });

            if (file == null) return;

            var csv = BuildSampleCsv();
            await File.WriteAllTextAsync(file.Path.LocalPath, csv, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            App.ErrorLogger?.LogError(ex, Core.Models.Telemetry.ErrorCategory.Export, "ExportSample");
        }
    }

    [RelayCommand]
    private void Commit()
    {
        IsOpen = false;
        _completionSource?.TrySetResult(true);
    }

    [RelayCommand]
    private void Cancel()
    {
        IsOpen = false;
        _completionSource?.TrySetResult(false);
    }
}
