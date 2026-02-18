namespace ArgoBooks.Core.Models.Portal;

/// <summary>
/// Response from publishing an invoice to the portal.
/// </summary>
public class PortalPublishResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The unique token for directly accessing this invoice on the portal.
    /// </summary>
    [JsonPropertyName("invoiceToken")]
    public string? InvoiceToken { get; set; }

    /// <summary>
    /// The unique token for the customer's portal (shows all their invoices).
    /// </summary>
    [JsonPropertyName("customerToken")]
    public string? CustomerToken { get; set; }

    /// <summary>
    /// The full URL to the invoice on the portal.
    /// </summary>
    [JsonPropertyName("invoiceUrl")]
    public string? InvoiceUrl { get; set; }

    /// <summary>
    /// The full URL to the customer's portal.
    /// </summary>
    [JsonPropertyName("portalUrl")]
    public string? PortalUrl { get; set; }

    /// <summary>
    /// Whether the server sent an email notification to the customer.
    /// </summary>
    [JsonPropertyName("emailSent")]
    public bool EmailSent { get; set; }

    /// <summary>
    /// The currently connected payment methods (e.g. ["stripe", "square"]).
    /// Returned by the server so the desktop app can stay in sync.
    /// </summary>
    [JsonPropertyName("payment_methods")]
    public List<string>? PaymentMethods { get; set; }

    [JsonPropertyName("errorCode")]
    public string? ErrorCode { get; set; }
}

/// <summary>
/// Response from disconnecting a payment provider.
/// </summary>
public class PortalDisconnectResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>
    /// The updated connected provider state after disconnection.
    /// </summary>
    [JsonPropertyName("connectedProviders")]
    public ConnectedPaymentAccounts? ConnectedProviders { get; set; }

    /// <summary>
    /// The currently connected payment methods (e.g. ["paypal", "square"]).
    /// </summary>
    [JsonPropertyName("payment_methods")]
    public List<string>? PaymentMethods { get; set; }

    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; set; }
}

/// <summary>
/// A payment record returned from the portal during sync.
/// </summary>
public class PortalPaymentRecord
{
    /// <summary>
    /// The server-side payment ID (used to prevent duplicate syncs).
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// The invoice ID this payment is for (matches Argo Books invoice ID).
    /// </summary>
    [JsonPropertyName("invoiceId")]
    public string InvoiceId { get; set; } = string.Empty;

    /// <summary>
    /// The customer name (for display/verification).
    /// </summary>
    [JsonPropertyName("customerName")]
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// The payment amount.
    /// </summary>
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    /// <summary>
    /// The currency code (e.g., "USD", "CAD").
    /// </summary>
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// The payment provider used: "stripe", "paypal", or "square".
    /// </summary>
    [JsonPropertyName("paymentMethod")]
    public string PaymentMethod { get; set; } = string.Empty;

    /// <summary>
    /// The provider's transaction/payment ID.
    /// </summary>
    [JsonPropertyName("providerTransactionId")]
    public string? ProviderTransactionId { get; set; }

    /// <summary>
    /// Human-readable confirmation/reference number.
    /// </summary>
    [JsonPropertyName("referenceNumber")]
    public string ReferenceNumber { get; set; } = string.Empty;

    /// <summary>
    /// When the payment was made.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Response from the payment sync endpoint.
/// </summary>
public class PortalSyncResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>
    /// New payment records since the last sync.
    /// </summary>
    [JsonPropertyName("payments")]
    public List<PortalPaymentRecord> Payments { get; set; } = [];

    /// <summary>
    /// Server timestamp for this sync (use as "since" for next sync).
    /// </summary>
    [JsonPropertyName("syncTimestamp")]
    public DateTime? SyncTimestamp { get; set; }

    [JsonPropertyName("errorCode")]
    public string? ErrorCode { get; set; }
}

/// <summary>
/// Response from the portal status check endpoint.
/// </summary>
public class PortalStatusResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("connected")]
    public bool Connected { get; set; }

    [JsonPropertyName("portalUrl")]
    public string? PortalUrl { get; set; }

    /// <summary>
    /// Which payment providers the business has connected.
    /// </summary>
    [JsonPropertyName("connectedProviders")]
    public ConnectedPaymentAccounts? ConnectedProviders { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

/// <summary>
/// Request payload for publishing an invoice to the portal.
/// </summary>
public class PortalPublishRequest
{
    [JsonPropertyName("invoiceId")]
    public string InvoiceId { get; set; } = string.Empty;

    [JsonPropertyName("invoiceNumber")]
    public string InvoiceNumber { get; set; } = string.Empty;

    [JsonPropertyName("customerId")]
    public string CustomerId { get; set; } = string.Empty;

    [JsonPropertyName("customerName")]
    public string CustomerName { get; set; } = string.Empty;

    [JsonPropertyName("customerEmail")]
    public string? CustomerEmail { get; set; }

    [JsonPropertyName("companyName")]
    public string CompanyName { get; set; } = string.Empty;

    [JsonPropertyName("issueDate")]
    public DateTime IssueDate { get; set; }

    [JsonPropertyName("dueDate")]
    public DateTime DueDate { get; set; }

    [JsonPropertyName("lineItems")]
    public List<PortalLineItem> LineItems { get; set; } = [];

    [JsonPropertyName("subtotal")]
    public decimal Subtotal { get; set; }

    [JsonPropertyName("taxRate")]
    public decimal TaxRate { get; set; }

    [JsonPropertyName("taxAmount")]
    public decimal TaxAmount { get; set; }

    [JsonPropertyName("securityDeposit")]
    public decimal SecurityDeposit { get; set; }

    [JsonPropertyName("totalAmount")]
    public decimal Total { get; set; }

    [JsonPropertyName("amountPaid")]
    public decimal AmountPaid { get; set; }

    [JsonPropertyName("balanceDue")]
    public decimal Balance { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "USD";

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "sent";

    /// <summary>
    /// When true, the server sends a notification email to the customer
    /// with a summary and "View &amp; Pay Invoice" link.
    /// </summary>
    [JsonPropertyName("sendEmail")]
    public bool SendEmail { get; set; }

    /// <summary>
    /// Pre-rendered invoice HTML from the desktop app's template engine.
    /// When present, the portal displays this in a sandboxed iframe instead of the generic template.
    /// </summary>
    [JsonPropertyName("customInvoiceHtml")]
    public string? CustomInvoiceHtml { get; set; }
}

/// <summary>
/// A line item in the portal publish request.
/// </summary>
public class PortalLineItem
{
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; }

    [JsonPropertyName("unitPrice")]
    public decimal UnitPrice { get; set; }

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }
}

/// <summary>
/// Request to confirm synced payments.
/// </summary>
public class PortalSyncConfirmRequest
{
    /// <summary>
    /// IDs of payments that were successfully synced to Argo Books.
    /// </summary>
    [JsonPropertyName("paymentIds")]
    public List<int> PaymentIds { get; set; } = [];
}

/// <summary>
/// Response from OAuth connect flow initiation.
/// </summary>
public class PortalOAuthResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// The URL to redirect the user to for OAuth authorization.
    /// </summary>
    [JsonPropertyName("authUrl")]
    public string? AuthUrl { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("errorCode")]
    public string? ErrorCode { get; set; }
}

/// <summary>
/// Response from the portal company registration endpoint.
/// </summary>
public class PortalRegisterResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("api_key")]
    public string? ApiKey { get; set; }

    [JsonPropertyName("company_id")]
    public int? CompanyId { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

/// <summary>
/// Response from logo upload/delete endpoints.
/// </summary>
public class PortalLogoResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("logo_url")]
    public string? LogoUrl { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; set; }
}
