namespace ArgoBooks.Core;

/// <summary>
/// Centralized color constants for use in C# services.
/// Mirrors the Colors.axaml resource dictionary so that backend/SkiaSharp code
/// uses the same palette as the UI layer.
/// </summary>
public static class AppColors
{
    // ── Primary Brand ───────────────────────────────────────────────
    public const string Primary = "#3B82F6";
    public const string PrimaryLight = "#DBEAFE";
    public const string PrimaryLighter = "#93C5FD";
    public const string PrimaryLightest = "#EFF6FF";
    public const string PrimaryDark = "#1D4ED8";
    public const string PrimaryHover = "#2563EB";
    public const string PrimaryDarkBg = "#1E3A5F";
    public const string PrimaryText = "#1E40AF";

    // ── Semantic: Success ───────────────────────────────────────────
    public const string Success = "#22C55E";
    public const string SuccessLight = "#DCFCE7";
    public const string SuccessDark = "#16A34A";
    public const string SuccessText = "#166534";

    // ── Semantic: Warning / Amber ───────────────────────────────────
    public const string Warning = "#F59E0B";
    public const string WarningLight = "#FEF3C7";
    public const string WarningDark = "#D97706";
    public const string WarningText = "#92400E";

    // ── Semantic: Error / Danger ────────────────────────────────────
    public const string Error = "#DC2626";
    public const string ErrorLight = "#FEE2E2";
    public const string ErrorLightest = "#FEF2F2";
    public const string ErrorDark = "#B91C1C";
    public const string ErrorDarkest = "#991B1B";

    // ── Semantic: Info ──────────────────────────────────────────────
    public const string Info = "#0369A1";
    public const string InfoLight = "#F0F9FF";
    public const string InfoDark = "#0C4A6E";

    // ── Semantic: Purple ────────────────────────────────────────────
    public const string Purple = "#A855F7";
    public const string PurpleLight = "#F3E8FF";
    public const string PurpleDark = "#9333EA";
    public const string PurpleDarkest = "#3B2066";

    // ── Semantic: Violet ────────────────────────────────────────────
    public const string Violet = "#8B5CF6";
    public const string VioletLight = "#EDE9FE";
    public const string VioletLightest = "#F5F3FF";
    public const string VioletHover = "#7C3AED";
    public const string VioletDark = "#6D28D9";

    // ── Additional Named Colors ─────────────────────────────────────
    public const string Emerald = "#10B981";
    public const string EmeraldHover = "#059669";
    public const string EmeraldLight = "#D1FAE5";
    public const string EmeraldLightest = "#ECFDF5";
    public const string EmeraldDark = "#047857";

    public const string Cyan = "#06B6D4";
    public const string CyanLight = "#CFFAFE";

    public const string Yellow = "#EAB308";
    public const string Amber = "#FBBF24";

    public const string Pink = "#EC4899";
    public const string PinkLight = "#FCE7F3";
    public const string PinkLightest = "#FDF2F8";
    public const string PinkMedium = "#F472B6";
    public const string PinkHover = "#DB2777";
    public const string PinkDark = "#BE185D";
    public const string PinkDarkest = "#5C1A3D";

    public const string Teal = "#14B8A6";
    public const string TealLight = "#CCFBF1";
    public const string TealLightest = "#F0FDFA";
    public const string TealHover = "#0D9488";
    public const string TealDark = "#0F766E";
    public const string TealDarkest = "#134E4A";

    public const string Orange = "#F97316";
    public const string OrangeLight = "#FFEDD5";
    public const string OrangeLightest = "#FFF7ED";
    public const string OrangeHover = "#EA580C";
    public const string OrangeDark = "#C2410C";
    public const string OrangeDarkest = "#5C2E0A";

    public const string Indigo = "#6366F1";
    public const string IndigoLight = "#E0E7FF";
    public const string IndigoText = "#4F46E5";

    public const string Lime = "#84CC16";

    // ── Neutral / Gray Scale ────────────────────────────────────────
    public const string White = "#FFFFFF";
    public const string Black = "#000000";
    public const string GrayLightest = "#F3F4F6";
    public const string GrayLighter = "#E5E7EB";
    public const string Gray = "#9CA3AF";
    public const string GrayMedium = "#6B7280";
    public const string GrayText = "#4B5563";
    public const string GrayDark = "#555555";
    public const string TextDark = "#F9FAFB";
    public const string TextLight = "#1F2937";
    public const string TextLightAlt = "#111827";
    public const string NearWhite = "#F8F9FA";

    // ── Chart-specific colors ───────────────────────────────────────
    public const string ChartBar = "#6495ED";
    public const string ChartAxis = "#374151";
    public const string ChartGrid = "#E5E7EB";
    public const string ExpenseRed = "#EF4444";

    // ── Report Element Defaults ─────────────────────────────────────
    public const string ReportGray = "#808080";
    public const string ReportBorder = "#D3D3D3";
    public const string ReportBackground = "#F5F5F5";
    public const string ReportBackgroundAlt = "#F8F8F8";
    public const string ReportHighlight = "#5E94FF";
    public const string ReportHeaderBg = "#4A7FD4";
    public const string ReportSlate = "#2C3E50";
    public const string ReportSlateLight = "#ECF0F1";
    public const string ReportSilver = "#D5D8DC";
    public const string CategoryDefault = "#4A90D9";
    public const string DarkGrayText = "#333333";

    // ── Invoice Template Colors ─────────────────────────────────────
    public const string SlateDark = "#0F172A";
    public const string SlateLight = "#F1F5F9";
    public const string SkyBlue = "#0EA5E9";
    public const string GrayBorder = "#D1D5DB";
    public const string LightBlue = "#29B6F6";
    public const string YellowBright = "#FFEE58";
    public const string LightGreen = "#7CB342";
    public const string NavyBlue = "#1A5276";

    // ── Material Design Palette (report chart categories) ───────────
    public const string MdBlue = "#2196F3";
    public const string MdBlueLight = "#E3F2FD";
    public const string MdGreen = "#4CAF50";
    public const string MdGreenLight = "#E8F5E9";
    public const string MdOrange = "#FF9800";
    public const string MdOrangeLight = "#FFF3E0";
    public const string MdRed = "#F44336";
    public const string MdRedLight = "#FFEBEE";
    public const string MdPurple = "#9C27B0";
    public const string MdPurpleLight = "#F3E5F5";
    public const string MdPink = "#E91E63";
    public const string MdPinkLight = "#FCE4EC";
    public const string MdCyan = "#00BCD4";
    public const string MdCyanLight = "#E0F7FA";
    public const string MdDeepOrange = "#FF5722";
    public const string MdDeepOrangeLight = "#FBE9E7";
    public const string MdGray = "#9E9E9E";
    public const string MdIndigo = "#3F51B5";
    public const string MdIndigoLight = "#E8EAF6";
    public const string MdBrown = "#795548";
    public const string MdBrownLight = "#EFEBE9";

    // ── Flat UI Palette (accounting report templates) ────────────────
    public const string FlatGreen = "#27AE60";
    public const string FlatGreenLight = "#E8F8F5";
    public const string FlatBlue = "#2980B9";
    public const string FlatBlueLight = "#EBF5FB";
    public const string FlatPurple = "#8E44AD";
    public const string FlatPurpleLight = "#F5EEF8";
    public const string FlatSlateLight = "#EBEDEF";
    public const string FlatTeal = "#16A085";
    public const string FlatTealLight = "#E8F6F3";
    public const string FlatOrange = "#D35400";
    public const string FlatOrangeLight = "#FBEEE6";

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
