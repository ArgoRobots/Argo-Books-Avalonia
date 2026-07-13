using System;
using System.Threading.Tasks;

namespace ArgoBooks.Shared.Mobile;

/// <summary>
/// Best-effort local count of receipt scans used this calendar month, shown as the Capture
/// screen's scan counter. Not authoritative: the real free/Premium quota is enforced server-side
/// by the AI proxy (keyed off DeviceApiAuth's X-Device-Id, or later the paired owner's license),
/// and a later fast-follow wires an over-limit gate off that server response. This is just a
/// lightweight local approximation so the counter has something to show today; it resets
/// automatically once the stored month no longer matches the current one.
/// </summary>
public static class ScanUsageStore
{
    private const string CountKey = "scan_usage_count";
    private const string MonthKey = "scan_usage_month";

    /// <summary>Returns the number of scans recorded so far this calendar month (0 if the stored
    /// count belongs to an earlier month, or none has been recorded yet).</summary>
    public static async Task<int> GetCountAsync(ISecureStore secureStore)
    {
        var storedMonth = await secureStore.GetAsync(MonthKey);
        if (storedMonth != ScanQuota.MonthKey(DateTime.UtcNow))
        {
            return 0;
        }

        var raw = await secureStore.GetAsync(CountKey);
        return int.TryParse(raw, out var count) ? count : 0;
    }

    /// <summary>Records one more scan used this month (rolling the counter over automatically if
    /// the stored month has changed) and returns the new count.</summary>
    public static async Task<int> IncrementAsync(ISecureStore secureStore)
    {
        var current = await GetCountAsync(secureStore);
        var next = current + 1;
        await secureStore.SetAsync(MonthKey, ScanQuota.MonthKey(DateTime.UtcNow));
        await secureStore.SetAsync(CountKey, next.ToString());
        return next;
    }
}
