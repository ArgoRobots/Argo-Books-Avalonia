namespace ArgoBooks.Core.Models.Invoices;

/// <summary>
/// Represents a customizable invoice template.
/// </summary>
public class InvoiceTemplate
{
    /// <summary>
    /// Unique identifier for this template.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name for this template.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The base template type to use.
    /// </summary>
    [JsonPropertyName("baseTemplate")]
    public InvoiceTemplateType BaseTemplate { get; set; } = InvoiceTemplateType.Professional;

    /// <summary>
    /// Whether this is the default template for new invoices.
    /// </summary>
    [JsonPropertyName("isDefault")]
    public bool IsDefault { get; set; }

    /// <summary>
    /// Primary brand color (header background, accents).
    /// </summary>
    [JsonPropertyName("primaryColor")]
    public string PrimaryColor { get; set; } = "#2563eb";

    /// <summary>
    /// Secondary color (borders, subtle backgrounds).
    /// </summary>
    [JsonPropertyName("secondaryColor")]
    public string SecondaryColor { get; set; } = "#e5e7eb";

    /// <summary>
    /// Accent color for highlights and important text.
    /// </summary>
    [JsonPropertyName("accentColor")]
    public string AccentColor { get; set; } = "#059669";

    /// <summary>
    /// Text color for primary content.
    /// </summary>
    [JsonPropertyName("textColor")]
    public string TextColor { get; set; } = "#1f2937";

    /// <summary>
    /// Background color for the invoice.
    /// </summary>
    [JsonPropertyName("backgroundColor")]
    public string BackgroundColor { get; set; } = "#ffffff";

    /// <summary>
    /// Font family to use. Must be web-safe for email compatibility.
    /// </summary>
    [JsonPropertyName("fontFamily")]
    public string FontFamily { get; set; } = "Arial, Helvetica, sans-serif";

    /// <summary>
    /// Company logo as base64-encoded image data.
    /// </summary>
    [JsonPropertyName("logoBase64")]
    public string? LogoBase64 { get; set; }

    /// <summary>
    /// Logo width in pixels (height auto-scales).
    /// </summary>
    [JsonPropertyName("logoWidth")]
    public int LogoWidth { get; set; } = 150;

    /// <summary>
    /// Custom header text (replaces "INVOICE" if set).
    /// </summary>
    [JsonPropertyName("headerText")]
    public string HeaderText { get; set; } = "INVOICE";

    /// <summary>
    /// Custom footer text (e.g., "Thank you for your business!").
    /// </summary>
    [JsonPropertyName("footerText")]
    public string FooterText { get; set; } = "Thank you for your business!";

    /// <summary>
    /// Payment terms text displayed on the invoice.
    /// </summary>
    [JsonPropertyName("paymentTermsText")]
    public string PaymentTermsText { get; set; } = string.Empty;

    /// <summary>
    /// Bank details or payment instructions.
    /// </summary>
    [JsonPropertyName("paymentInstructions")]
    public string PaymentInstructions { get; set; } = string.Empty;

    /// <summary>
    /// Whether to show the company logo.
    /// </summary>
    [JsonPropertyName("showLogo")]
    public bool ShowLogo { get; set; } = true;

    /// <summary>
    /// Whether to show the company address.
    /// </summary>
    [JsonPropertyName("showCompanyAddress")]
    public bool ShowCompanyAddress { get; set; } = true;

    /// <summary>
    /// Whether to show the company phone number.
    /// </summary>
    [JsonPropertyName("showCompanyPhone")]
    public bool ShowCompanyPhone { get; set; } = true;

    /// <summary>
    /// Whether to show the company city.
    /// </summary>
    [JsonPropertyName("showCompanyCity")]
    public bool ShowCompanyCity { get; set; } = true;

    /// <summary>
    /// Whether to show the company country.
    /// </summary>
    [JsonPropertyName("showCompanyCountry")]
    public bool ShowCompanyCountry { get; set; } = true;

    /// <summary>
    /// Whether to show the tax breakdown.
    /// </summary>
    [JsonPropertyName("showTaxBreakdown")]
    public bool ShowTaxBreakdown { get; set; } = true;

    /// <summary>
    /// Whether to show product/item descriptions.
    /// </summary>
    [JsonPropertyName("showItemDescriptions")]
    public bool ShowItemDescriptions { get; set; } = true;

    /// <summary>
    /// Whether to show the notes section.
    /// </summary>
    [JsonPropertyName("showNotes")]
    public bool ShowNotes { get; set; } = true;

    /// <summary>
    /// Whether to show payment instructions.
    /// </summary>
    [JsonPropertyName("showPaymentInstructions")]
    public bool ShowPaymentInstructions { get; set; } = true;

    /// <summary>
    /// Whether to show the due date prominently.
    /// </summary>
    [JsonPropertyName("showDueDateProminent")]
    public bool ShowDueDateProminent { get; set; } = true;

    /// <summary>
    /// When this template was created.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this template was last modified.
    /// </summary>
    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Creates a deep copy of this template.
    /// </summary>
    public InvoiceTemplate Clone()
    {
        return new InvoiceTemplate
        {
            Id = Id,
            Name = Name,
            BaseTemplate = BaseTemplate,
            IsDefault = IsDefault,
            PrimaryColor = PrimaryColor,
            SecondaryColor = SecondaryColor,
            AccentColor = AccentColor,
            TextColor = TextColor,
            BackgroundColor = BackgroundColor,
            FontFamily = FontFamily,
            LogoBase64 = LogoBase64,
            LogoWidth = LogoWidth,
            HeaderText = HeaderText,
            FooterText = FooterText,
            PaymentTermsText = PaymentTermsText,
            PaymentInstructions = PaymentInstructions,
            ShowLogo = ShowLogo,
            ShowCompanyAddress = ShowCompanyAddress,
            ShowCompanyPhone = ShowCompanyPhone,
            ShowCompanyCity = ShowCompanyCity,
            ShowCompanyCountry = ShowCompanyCountry,
            ShowTaxBreakdown = ShowTaxBreakdown,
            ShowItemDescriptions = ShowItemDescriptions,
            ShowNotes = ShowNotes,
            ShowPaymentInstructions = ShowPaymentInstructions,
            ShowDueDateProminent = ShowDueDateProminent,
            CreatedAt = CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Base template types that define the overall layout structure.
/// </summary>
public enum InvoiceTemplateType
{
    /// <summary>
    /// Professional: Corporate look with header banner, structured layout.
    /// </summary>
    Professional,

    /// <summary>
    /// Modern: Clean, minimalist design with plenty of whitespace.
    /// </summary>
    Modern,

    /// <summary>
    /// Classic: Traditional invoice layout with borders and lines.
    /// </summary>
    Classic,

    /// <summary>
    /// Minimal: Bare essentials, very simple and clean.
    /// </summary>
    Minimal
}
