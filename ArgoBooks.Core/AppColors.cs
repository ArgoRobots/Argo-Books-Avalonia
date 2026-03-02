namespace ArgoBooks.Core;

/// <summary>
/// Centralized color constants for use in C# services.
/// Mirrors the Colors.axaml resource dictionary so that backend/SkiaSharp code
/// uses the same palette as the UI layer.
/// </summary>
public static class AppColors
{
    // ── Primary Brand ───────────────────────────────────────────────
    public const string Primary = "#3B82F6";      // Blue
    public const string PrimaryLight = "#DBEAFE";
    public const string PrimaryDark = "#1D4ED8";
    public const string PrimaryHover = "#2563EB";

    // ── Semantic: Success ───────────────────────────────────────────
    public const string Success = "#22C55E";       // Green
    public const string SuccessLight = "#DCFCE7";
    public const string SuccessDark = "#16A34A";

    // ── Semantic: Warning / Amber ───────────────────────────────────
    public const string Warning = "#F59E0B";       // Amber
    public const string WarningLight = "#FEF3C7";
    public const string WarningDark = "#D97706";

    // ── Semantic: Error / Danger ────────────────────────────────────
    public const string Error = "#DC2626";         // Red
    public const string ErrorLight = "#FEE2E2";
    public const string ErrorDark = "#B91C1C";

    // ── Semantic: Info ──────────────────────────────────────────────
    public const string Info = "#0369A1";
    public const string InfoLight = "#F0F9FF";
    public const string InfoDark = "#0C4A6E";

    // ── Semantic: Purple ────────────────────────────────────────────
    public const string Purple = "#A855F7";
    public const string PurpleLight = "#F3E8FF";
    public const string PurpleDark = "#9333EA";

    // ── Semantic: Violet ────────────────────────────────────────────
    public const string Violet = "#8B5CF6";

    // ── Neutral / Theme colors ──────────────────────────────────────
    public const string TextDark = "#F9FAFB";      // Light text on dark backgrounds
    public const string TextLight = "#1F2937";     // Dark text on light backgrounds
    public const string TextLightAlt = "#111827";  // Darker text variant for light theme
    public const string Gray = "#9CA3AF";          // Neutral gray (e.g. "Other" in pie charts)
    public const string GrayMedium = "#6B7280";     // Medium gray (e.g. default/neutral state)
    public const string GrayDark = "#555555";      // Darker gray (e.g. subtitle text in reports)

    /// <summary>Light blue for geo map gradient start.</summary>
    public const string PrimaryLighter = "#93C5FD";

    // ── Chart-specific colors ───────────────────────────────────────
    /// <summary>Cornflower Blue – default bar/line color for generic charts.</summary>
    public const string ChartBar = "#6495ED";

    /// <summary>Gray – axis labels and tick marks.</summary>
    public const string ChartAxis = "#374151";

    /// <summary>Light gray – grid lines.</summary>
    public const string ChartGrid = "#E5E7EB";

    /// <summary>Expense red used in analytics charts (slightly lighter than Error).</summary>
    public const string ExpenseRed = "#EF4444";

    // ── Additional palette colors (for pie charts, multi-series, etc.) ──
    public const string Pink = "#EC4899";
    public const string Teal = "#14B8A6";
    public const string Orange = "#F97316";
    public const string Indigo = "#6366F1";
    public const string Lime = "#84CC16";

    /// <summary>
    /// Ordered color palette for indexed series (pie slices, multi-category charts, etc.).
    /// </summary>
    public static readonly string[] Palette =
    [
        Primary,     // Blue
        ExpenseRed,  // Red
        Success,     // Green
        Warning,     // Amber
        Violet,      // Purple
        Pink,        // Pink
        Teal,        // Teal
        Orange,      // Orange
        Indigo,      // Indigo
        Lime,        // Lime
    ];
}
