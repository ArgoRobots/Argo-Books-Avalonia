using System.Text;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Reduces a noisy bank-statement description to a stable merchant token suitable for
/// rule matching: lowercased, punctuation stripped, bare numbers and long alphanumeric
/// reference/auth codes removed, whitespace collapsed.
/// </summary>
public static class MerchantNormalizer
{
    public static string Normalize(string description)
    {
        if (string.IsNullOrWhiteSpace(description)) return string.Empty;

        // Replace any non-alphanumeric with a space.
        var sb = new StringBuilder(description.Length);
        foreach (var ch in description)
            sb.Append(char.IsLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : ' ');

        var tokens = sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var kept = new List<string>(tokens.Length);
        foreach (var t in tokens)
        {
            // Drop pure numbers (store #, dates, auth) and long mixed codes (e.g. "2h8kl").
            if (t.All(char.IsDigit)) continue;
            if (t.Length >= 4 && t.Any(char.IsDigit)) continue;
            kept.Add(t);
        }
        return string.Join(' ', kept);
    }
}
