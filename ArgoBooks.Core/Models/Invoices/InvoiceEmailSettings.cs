using ArgoBooks.Core.Services;

namespace ArgoBooks.Core.Models.Invoices;

/// <summary>
/// Settings for the invoice email API integration.
/// API key is loaded from .env file for security.
/// Company-specific settings (from name, email, etc.) are stored in company data.
/// </summary>
public class InvoiceEmailSettings
{
    /// <summary>
    /// The invoice email API endpoint URL.
    /// </summary>
    public const string ApiEndpoint = "https://argorobots.com/api/invoice/send-email.php";

    /// <summary>
    /// Environment variable name for the API key.
    /// </summary>
    public const string ApiKeyEnvVar = "INVOICE_EMAIL_API_KEY";

    /// <summary>
    /// Gets the API key from .env file.
    /// </summary>
    [JsonIgnore]
    public static string ApiKey => DotEnv.Get(ApiKeyEnvVar);

    /// <summary>
    /// Default "From" name for invoice emails.
    /// </summary>
    [JsonPropertyName("fromName")]
    public string FromName { get; set; } = string.Empty;

    /// <summary>
    /// Default "From" email address.
    /// </summary>
    [JsonPropertyName("fromEmail")]
    public string FromEmail { get; set; } = string.Empty;

    /// <summary>
    /// Default "Reply-To" email address.
    /// </summary>
    [JsonPropertyName("replyToEmail")]
    public string ReplyToEmail { get; set; } = string.Empty;

    /// <summary>
    /// BCC email address for invoice copies (e.g., for records).
    /// </summary>
    [JsonPropertyName("bccEmail")]
    public string BccEmail { get; set; } = string.Empty;

    /// <summary>
    /// Default email subject template. Use placeholders like {InvoiceNumber}, {CustomerName}, {Total}.
    /// </summary>
    [JsonPropertyName("subjectTemplate")]
    public string SubjectTemplate { get; set; } = "Invoice {InvoiceNumber} from {CompanyName}";

    /// <summary>
    /// Whether to include a PDF attachment of the invoice.
    /// </summary>
    [JsonPropertyName("includePdfAttachment")]
    public bool IncludePdfAttachment { get; set; } = true;

    /// <summary>
    /// Whether the email API is configured (API key in .env file).
    /// </summary>
    [JsonIgnore]
    public static bool IsConfigured => DotEnv.HasValue(ApiKeyEnvVar);
}

/// <summary>
/// Request payload for sending an invoice email via the API.
/// </summary>
public class InvoiceEmailRequest
{
    /// <summary>
    /// Recipient email address.
    /// </summary>
    [JsonPropertyName("to")]
    public string To { get; set; } = string.Empty;

    /// <summary>
    /// Recipient name.
    /// </summary>
    [JsonPropertyName("toName")]
    public string ToName { get; set; } = string.Empty;

    /// <summary>
    /// Sender email address.
    /// </summary>
    [JsonPropertyName("from")]
    public string From { get; set; } = string.Empty;

    /// <summary>
    /// Sender name.
    /// </summary>
    [JsonPropertyName("fromName")]
    public string FromName { get; set; } = string.Empty;

    /// <summary>
    /// Reply-to email address.
    /// </summary>
    [JsonPropertyName("replyTo")]
    public string? ReplyTo { get; set; }

    /// <summary>
    /// BCC email address.
    /// </summary>
    [JsonPropertyName("bcc")]
    public string? Bcc { get; set; }

    /// <summary>
    /// Email subject line.
    /// </summary>
    [JsonPropertyName("subject")]
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// HTML body content of the email.
    /// </summary>
    [JsonPropertyName("html")]
    public string Html { get; set; } = string.Empty;

    /// <summary>
    /// Plain text fallback content.
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    /// <summary>
    /// Invoice ID for tracking purposes.
    /// </summary>
    [JsonPropertyName("invoiceId")]
    public string InvoiceId { get; set; } = string.Empty;

    /// <summary>
    /// Optional PDF attachment as base64-encoded data.
    /// </summary>
    [JsonPropertyName("pdfAttachment")]
    public string? PdfAttachment { get; set; }

    /// <summary>
    /// Filename for the PDF attachment.
    /// </summary>
    [JsonPropertyName("pdfFilename")]
    public string? PdfFilename { get; set; }
}

/// <summary>
/// Response from the invoice email API.
/// </summary>
public class InvoiceEmailResponse
{
    /// <summary>
    /// Whether the email was sent successfully.
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// Message from the API (success confirmation or error details).
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Unique message ID from the email provider (for tracking).
    /// </summary>
    [JsonPropertyName("messageId")]
    public string? MessageId { get; set; }

    /// <summary>
    /// Error code if the request failed.
    /// </summary>
    [JsonPropertyName("errorCode")]
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Timestamp when the email was queued/sent.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime? Timestamp { get; set; }
}
