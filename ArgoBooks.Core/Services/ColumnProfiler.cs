using System.Globalization;

namespace ArgoBooks.Core.Services;

public sealed record ColumnProfile(
    string Header,
    string InferredType,
    int DistinctCount,
    int EmptyCount,
    string? Min,
    string? Max,
    IReadOnlyList<string> Examples);

public sealed record ColumnRelationship(string Description);

public static class ColumnProfiler
{
    public static List<ColumnProfile> Profile(List<string> headers, List<List<string>> rows)
    {
        var profiles = new List<ColumnProfile>();
        for (int c = 0; c < headers.Count; c++)
        {
            var values = rows.Where(r => c < r.Count).Select(r => r[c]).ToList();
            var nonEmpty = values.Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
            var nums = nonEmpty.Select(TryNum).Where(n => n != null).Select(n => n!.Value).ToList();
            var allNumeric = nonEmpty.Count > 0 && nums.Count == nonEmpty.Count;
            var type = allNumeric ? "number"
                       : nonEmpty.Count > 0 && nonEmpty.All(LooksLikeDate) ? "date"
                       : "string";
            profiles.Add(new ColumnProfile(
                headers[c],
                type,
                nonEmpty.Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                values.Count - nonEmpty.Count,
                allNumeric && nums.Count > 0 ? nums.Min().ToString(CultureInfo.InvariantCulture) : null,
                allNumeric && nums.Count > 0 ? nums.Max().ToString(CultureInfo.InvariantCulture) : null,
                nonEmpty.Take(3).ToList()));
        }
        return profiles;
    }

    public static List<ColumnRelationship> DetectRelationships(List<string> headers, List<List<string>> rows)
    {
        var rels = new List<ColumnRelationship>();
        var numericCols = Enumerable.Range(0, headers.Count)
            .Where(c => rows.All(r => c >= r.Count || string.IsNullOrWhiteSpace(r[c]) || TryNum(r[c]) != null))
            .ToList();

        foreach (var t in numericCols)
        foreach (var a in numericCols)
        foreach (var b in numericCols)
        {
            if (t == a || t == b || a >= b) continue;
            if (HoldsOver(rows, t, a, b, (x, y) => x * y))
                rels.Add(new ColumnRelationship($"{headers[t]} ~= {headers[a]} * {headers[b]}"));
            else if (HoldsOver(rows, t, a, b, (x, y) => x + y))
                rels.Add(new ColumnRelationship($"{headers[t]} ~= {headers[a]} + {headers[b]}"));
        }
        return rels;
    }

    private static bool HoldsOver(List<List<string>> rows, int t, int a, int b, Func<decimal, decimal, decimal> op)
    {
        int confirmed = 0;
        foreach (var r in rows)
        {
            if (t >= r.Count || a >= r.Count || b >= r.Count) continue;
            var vt = TryNum(r[t]);
            var va = TryNum(r[a]);
            var vb = TryNum(r[b]);
            if (vt == null || va == null || vb == null) continue;
            if (Math.Abs(op(va.Value, vb.Value) - vt.Value) > 0.01m) return false;
            confirmed++;
        }
        return confirmed >= 2;
    }

    private static decimal? TryNum(string s) =>
        decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : null;

    private static bool LooksLikeDate(string s) =>
        DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
}
