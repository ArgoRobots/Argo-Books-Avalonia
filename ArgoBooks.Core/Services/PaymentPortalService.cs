using System.Net.Http.Headers;
using System.Text;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Common;
using ArgoBooks.Core.Models.Invoices;
using ArgoBooks.Core.Models.Portal;
using ArgoBooks.Core.Models.Transactions;
using ArgoBooks.Core.Services.InvoiceTemplates;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Service for communicating with the payment portal API on argorobots.com.
/// Handles publishing invoices, syncing payments, and checking portal status.
/// </summary>
public class PaymentPortalService : IDisposable
{
    private static readonly JsonSerializerOptions SerializeOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly JsonSerializerOptions DeserializeOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private bool _disposed;

    public PaymentPortalService()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    #region Portal Status

    /// <summary>
    /// Checks if the portal is connected and returns status info.
    /// </summary>
    public async Task<PortalStatusResponse> CheckStatusAsync(CancellationToken cancellationToken = default)
    {
        if (!PortalSettings.IsConfigured)
        {
            return new PortalStatusResponse
            {
                Success = false,
                Connected = false,
                Message = "Payment portal is not configured. Please register your company first."
            };
        }

        try
        {
            using var request = CreateRequest(HttpMethod.Get, "/status");
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return DeserializeResponse<PortalStatusResponse>(content) ?? new PortalStatusResponse
                {
                    Success = false,
                    Connected = false,
                    Message = "Unexpected response from server."
                };
            }

            return new PortalStatusResponse
            {
                Success = false,
                Connected = false,
                Message = $"Portal returned status {(int)response.StatusCode}"
            };
        }
        catch (TaskCanceledException)
        {
            return new PortalStatusResponse { Success = false, Connected = false, Message = "Connection timed out." };
        }
        catch (HttpRequestException ex)
        {
            return new PortalStatusResponse { Success = false, Connected = false, Message = $"Connection failed: {ex.Message}" };
        }
    }

    #endregion

    #region Publish Invoice

    /// <summary>
    /// Publishes an invoice to the payment portal so customers can view and pay it online.
    /// </summary>
    public async Task<PortalPublishResponse> PublishInvoiceAsync(
        Invoice invoice,
        CompanyData companyData,
        InvoiceTemplate? template = null,
        string currencySymbol = "$",
        CancellationToken cancellationToken = default)
    {
        if (!PortalSettings.IsConfigured)
        {
            return new PortalPublishResponse
            {
                Success = false,
                Message = "Payment portal is not configured. Please register your company first.",
                ErrorCode = "NOT_CONFIGURED"
            };
        }

        var customer = companyData.GetCustomer(invoice.CustomerId);
        if (customer == null)
        {
            return new PortalPublishResponse
            {
                Success = false,
                Message = "Customer not found for this invoice.",
                ErrorCode = "CUSTOMER_NOT_FOUND"
            };
        }

        try
        {
            var publishRequest = new PortalPublishRequest
            {
                InvoiceId = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                CustomerId = invoice.CustomerId,
                CustomerName = customer.Name,
                CustomerEmail = customer.Email,
                CompanyName = companyData.Settings.Company.Name,
                IssueDate = invoice.IssueDate,
                DueDate = invoice.DueDate,
                Subtotal = invoice.Subtotal,
                TaxRate = invoice.TaxRate,
                TaxAmount = invoice.TaxAmount,
                SecurityDeposit = invoice.SecurityDeposit,
                Total = invoice.Total,
                AmountPaid = invoice.AmountPaid,
                Balance = invoice.Balance,
                Currency = invoice.OriginalCurrency,
                Notes = invoice.Notes,
                Status = invoice.Status.ToString().ToLowerInvariant(),
                SendEmail = !string.IsNullOrWhiteSpace(customer.Email),
                PassProcessingFee = template?.PassProcessingFee ?? true,
                LineItems = invoice.LineItems.Select(li => new PortalLineItem
                {
                    Description = li.Description,
                    Quantity = li.Quantity,
                    UnitPrice = li.UnitPrice,
                    Amount = li.Quantity * li.UnitPrice
                }).ToList()
            };

            // Render the invoice HTML so the portal displays the exact desktop template
            if (template != null)
            {
                var renderer = new InvoiceHtmlRenderer();
                publishRequest.CustomInvoiceHtml = renderer.RenderInvoice(
                    invoice, template, companyData, currencySymbol);
            }

            var json = JsonSerializer.Serialize(publishRequest, SerializeOptions);

            using var request = CreateRequest(HttpMethod.Post, "/invoices");
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return DeserializeResponse<PortalPublishResponse>(content) ?? new PortalPublishResponse
                {
                    Success = true,
                    Message = "Invoice published to portal."
                };
            }

            var errorResponse = DeserializeResponse<PortalPublishResponse>(content);
            return errorResponse ?? new PortalPublishResponse
            {
                Success = false,
                Message = $"Portal returned status {(int)response.StatusCode}",
                ErrorCode = ((int)response.StatusCode).ToString()
            };
        }
        catch (TaskCanceledException)
        {
            return new PortalPublishResponse { Success = false, Message = "Request timed out.", ErrorCode = "TIMEOUT" };
        }
        catch (HttpRequestException ex)
        {
            return new PortalPublishResponse { Success = false, Message = $"Network error: {ex.Message}", ErrorCode = "NETWORK_ERROR" };
        }
        catch (Exception)
        {
            return new PortalPublishResponse { Success = false, Message = "An unexpected error occurred. Please try again.", ErrorCode = "UNKNOWN_ERROR" };
        }
    }

    #endregion

    #region Sync Payments

    /// <summary>
    /// Syncs new payments from the portal. Returns payment records that haven't been synced yet.
    /// When force is true, returns ALL payments (including already-synced) to recover
    /// payments that were confirmed server-side but not saved locally.
    /// </summary>
    public async Task<PortalSyncResponse> SyncPaymentsAsync(
        DateTime? since = null,
        bool force = false,
        CancellationToken cancellationToken = default)
    {
        if (!PortalSettings.IsConfigured)
        {
            return new PortalSyncResponse
            {
                Success = false,
                Message = "Payment portal is not configured. Please register your company first.",
                ErrorCode = "NOT_CONFIGURED"
            };
        }

        try
        {
            var url = "/payments/sync";
            if (force)
            {
                url += "?force=1";
            }
            else if (since.HasValue)
            {
                url += $"?since={since.Value:O}";
            }

            using var request = CreateRequest(HttpMethod.Get, url);
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return DeserializeResponse<PortalSyncResponse>(content) ?? new PortalSyncResponse
                {
                    Success = true,
                    Payments = []
                };
            }

            return new PortalSyncResponse
            {
                Success = false,
                Message = $"Sync failed with status {(int)response.StatusCode}",
                ErrorCode = ((int)response.StatusCode).ToString()
            };
        }
        catch (TaskCanceledException)
        {
            return new PortalSyncResponse { Success = false, Message = "Sync timed out.", ErrorCode = "TIMEOUT" };
        }
        catch (HttpRequestException ex)
        {
            return new PortalSyncResponse { Success = false, Message = $"Network error: {ex.Message}", ErrorCode = "NETWORK_ERROR" };
        }
    }

    /// <summary>
    /// Confirms that payments have been synced to Argo Books so the server marks them as synced.
    /// </summary>
    public async Task<bool> ConfirmSyncAsync(
        List<int> paymentIds,
        CancellationToken cancellationToken = default)
    {
        if (!PortalSettings.IsConfigured || paymentIds.Count == 0)
            return false;

        try
        {
            var confirmRequest = new PortalSyncConfirmRequest { PaymentIds = paymentIds };
            var json = JsonSerializer.Serialize(confirmRequest, SerializeOptions);

            using var request = CreateRequest(HttpMethod.Post, "/payments/sync/confirm");
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Processes synced payment records into local Payment objects and updates invoice balances.
    /// </summary>
    public static List<Payment> ProcessSyncedPayments(
        List<PortalPaymentRecord> portalPayments,
        CompanyData companyData)
    {
        var newPayments = new List<Payment>();

        foreach (var portalPayment in portalPayments)
        {
            // Skip if we already have this portal payment (duplicate prevention).
            // BUT first backfill any new fields the local row is missing so the
            // refund feature works for payments that were synced before this
            // release — without re-creating duplicate rows.
            var existing = companyData.Payments.FirstOrDefault(p =>
                p.PortalPaymentId == portalPayment.Id.ToString());
            if (existing != null)
            {
                if (string.IsNullOrEmpty(existing.ProviderPaymentId)
                    && !string.IsNullOrEmpty(portalPayment.ProviderPaymentId))
                {
                    existing.ProviderPaymentId = portalPayment.ProviderPaymentId;
                }
                continue;
            }

            // Find the matching invoice
            var invoice = companyData.GetInvoice(portalPayment.InvoiceId);
            if (invoice == null) continue;

            // Map payment method
            var method = portalPayment.PaymentMethod.ToLowerInvariant() switch
            {
                "stripe" => PaymentMethod.Stripe,
                "paypal" => PaymentMethod.PayPal,
                "square" => PaymentMethod.Square,
                _ => PaymentMethod.Other
            };

            // Map provider name for display
            var providerName = portalPayment.PaymentMethod.ToLowerInvariant() switch
            {
                "stripe" => "Stripe",
                "paypal" => "PayPal",
                "square" => "Square",
                _ => portalPayment.PaymentMethod
            };

            // Generate payment ID
            var nextId = companyData.IdCounters.Payment + 1;
            companyData.IdCounters.Payment = nextId;
            var paymentId = $"PAY-{DateTime.UtcNow:yyyy}-{nextId:D5}";

            Payment payment;

            if (portalPayment.IsRefund)
            {
                // ----- Refund row -----
                // The server's amount is already negative for refund rows. Find the
                // local Payment that this refund offsets via ProviderPaymentId,
                // which the original payment carries from sync.
                string? refundedFromLocalId = null;
                if (!string.IsNullOrEmpty(portalPayment.RefundedProviderPaymentId))
                {
                    refundedFromLocalId = companyData.Payments
                        .FirstOrDefault(p => !p.IsRefund
                                          && !string.IsNullOrEmpty(p.ProviderPaymentId)
                                          && p.ProviderPaymentId == portalPayment.RefundedProviderPaymentId)
                        ?.Id;
                }

                payment = new Payment
                {
                    Id = paymentId,
                    InvoiceId = portalPayment.InvoiceId,
                    CustomerId = invoice.CustomerId,
                    Date = portalPayment.CreatedAt,
                    Amount = portalPayment.Amount, // already negative
                    PaymentMethod = method,
                    ReferenceNumber = portalPayment.ReferenceNumber,
                    Notes = string.IsNullOrEmpty(portalPayment.RefundReason)
                        ? $"Refund issued via {providerName}"
                        : $"Refund issued via {providerName} — {portalPayment.RefundReason}",
                    CreatedAt = DateTime.UtcNow,
                    OriginalCurrency = portalPayment.Currency,
                    AmountUSD = portalPayment.Currency.Equals("USD", StringComparison.OrdinalIgnoreCase)
                        ? portalPayment.Amount
                        : (invoice.TotalUSD > 0 && invoice.Total > 0
                            ? Math.Round(portalPayment.Amount * (invoice.TotalUSD / invoice.Total), 2)
                            : 0m),
                    Source = "Online",
                    PortalPaymentId = portalPayment.Id.ToString(),
                    IsRefund = true,
                    RefundedFromPaymentId = refundedFromLocalId,
                    RefundRequestId = portalPayment.RefundRequestId?.ToString(),
                    RefundReason = portalPayment.RefundReason,
                };

                companyData.Payments.Add(payment);
                newPayments.Add(payment);

                // Update invoice.AmountRefunded (absolute sum of refund Payments for this invoice)
                var invoiceCurrencyRf = string.IsNullOrEmpty(invoice.OriginalCurrency) ? "USD" : invoice.OriginalCurrency;
                invoice.AmountRefunded = companyData.Payments
                    .Where(p => p.InvoiceId == invoice.Id && p.IsRefund
                        && string.Equals(string.IsNullOrEmpty(p.OriginalCurrency) ? "USD" : p.OriginalCurrency,
                                         invoiceCurrencyRf, StringComparison.OrdinalIgnoreCase))
                    .Sum(p => Math.Abs(p.Amount));

                // Status recompute (refunded > 0 with payments > 0 → Refunded/PartiallyRefunded)
                if (invoice.AmountPaid > 0)
                {
                    if (invoice.AmountRefunded >= invoice.AmountPaid)
                    {
                        invoice.Status = InvoiceStatus.Refunded;
                    }
                    else if (invoice.AmountRefunded > 0)
                    {
                        invoice.Status = InvoiceStatus.PartiallyRefunded;
                    }
                }

                invoice.History.Add(new InvoiceHistoryEntry
                {
                    Action = "Refund Issued",
                    Details = string.IsNullOrEmpty(portalPayment.RefundReason)
                        ? $"Refund of {portalPayment.Currency} {Math.Abs(portalPayment.Amount):N2} via {providerName}"
                        : $"Refund of {portalPayment.Currency} {Math.Abs(portalPayment.Amount):N2} via {providerName} — \"{portalPayment.RefundReason}\"",
                    Timestamp = DateTime.UtcNow
                });

                invoice.UpdatedAt = DateTime.UtcNow;
                continue;
            }

            // ----- Regular payment row (existing logic) -----
            // The invoice amount is the total charged minus any processing fee
            var invoiceAmount = Math.Max(0m, portalPayment.Amount - portalPayment.ProcessingFee);

            // Convert non-USD payment amount to USD using the invoice's conversion ratio
            decimal amountUSD;
            if (portalPayment.Currency.Equals("USD", StringComparison.OrdinalIgnoreCase))
            {
                amountUSD = invoiceAmount;
            }
            else if (invoice.TotalUSD > 0 && invoice.Total > 0)
            {
                // Use invoice's known USD conversion ratio
                amountUSD = Math.Round(invoiceAmount * (invoice.TotalUSD / invoice.Total), 2);
            }
            else
            {
                // No conversion ratio available — defer by setting AmountUSD to 0
                amountUSD = 0m;
            }

            // Build payment notes with fee info if applicable
            var notes = portalPayment.ProcessingFee > 0
                ? $"Online payment via {providerName} (processing fee: {portalPayment.Currency} {portalPayment.ProcessingFee:N2})"
                : $"Online payment via {providerName}";

            payment = new Payment
            {
                Id = paymentId,
                InvoiceId = portalPayment.InvoiceId,
                CustomerId = invoice.CustomerId,
                Date = portalPayment.CreatedAt,
                Amount = invoiceAmount,
                PaymentMethod = method,
                ReferenceNumber = portalPayment.ReferenceNumber,
                Notes = notes,
                CreatedAt = DateTime.UtcNow,
                OriginalCurrency = portalPayment.Currency,
                AmountUSD = amountUSD,
                Source = "Online",
                PortalPaymentId = portalPayment.Id.ToString(),
                ProviderPaymentId = portalPayment.ProviderPaymentId
            };

            // Add to company data
            companyData.Payments.Add(payment);
            newPayments.Add(payment);

            // Update invoice balance using USD-normalized amounts to avoid currency mixing
            var totalPaidUSD = companyData.Payments
                .Where(p => p.InvoiceId == invoice.Id && p.Amount > 0)
                .Sum(p => p.EffectiveAmountUSD);

            // Also track original-currency paid for display
            // Normalize currency comparison: treat null/empty as "USD" and compare case-insensitively
            var invoiceCurrency = string.IsNullOrEmpty(invoice.OriginalCurrency) ? "USD" : invoice.OriginalCurrency;
            var totalPaidOriginal = companyData.Payments
                .Where(p => p.InvoiceId == invoice.Id && p.Amount > 0
                    && string.Equals(
                        string.IsNullOrEmpty(p.OriginalCurrency) ? "USD" : p.OriginalCurrency,
                        invoiceCurrency,
                        StringComparison.OrdinalIgnoreCase))
                .Sum(p => p.Amount);

            invoice.AmountPaid = totalPaidOriginal;
            invoice.Balance = Math.Max(0, invoice.Total - totalPaidOriginal);

            // Keep USD fields in sync
            if (invoice.TotalUSD > 0)
            {
                invoice.BalanceUSD = Math.Max(0, invoice.TotalUSD - totalPaidUSD);
            }

            // If this invoice has prior refunds, factor them into the status
            // so a new positive payment after a refund correctly lands on
            // PartiallyRefunded rather than masking the refund history.
            var refundedSoFar = companyData.Payments
                .Where(p => p.InvoiceId == invoice.Id && p.IsRefund
                    && string.Equals(string.IsNullOrEmpty(p.OriginalCurrency) ? "USD" : p.OriginalCurrency,
                                     invoiceCurrency, StringComparison.OrdinalIgnoreCase))
                .Sum(p => Math.Abs(p.Amount));
            invoice.AmountRefunded = refundedSoFar;

            if (refundedSoFar > 0 && totalPaidOriginal > 0)
            {
                invoice.Status = refundedSoFar >= totalPaidOriginal
                    ? InvoiceStatus.Refunded
                    : InvoiceStatus.PartiallyRefunded;
            }
            else if (invoice.Balance <= 0)
            {
                invoice.Status = InvoiceStatus.Paid;
            }
            else if (totalPaidOriginal > 0)
            {
                invoice.Status = InvoiceStatus.Partial;
            }

            // Update linked revenue records
            var linkedRevenues = companyData.Revenues
                .Where(r => r.InvoiceId == invoice.Id);
            foreach (var revenue in linkedRevenues)
            {
                revenue.PaymentStatus = invoice.Status == InvoiceStatus.Paid ? "Paid" : "Unpaid";
            }

            // Add history entry
            invoice.History.Add(new InvoiceHistoryEntry
            {
                Action = "Payment Received",
                Details = $"Online payment of {portalPayment.Currency} {portalPayment.Amount:N2} received via {providerName}",
                Timestamp = DateTime.UtcNow
            });

            invoice.UpdatedAt = DateTime.UtcNow;
        }

        return newPayments;
    }

    #endregion

    #region OAuth Connect

    /// <summary>
    /// Initiates the OAuth flow for connecting a payment provider account.
    /// Returns a URL that should be opened in the user's browser.
    /// </summary>
    public async Task<PortalOAuthResponse> InitiateConnectAsync(
        string provider,
        CancellationToken cancellationToken = default)
    {
        if (!PortalSettings.IsConfigured)
        {
            return new PortalOAuthResponse
            {
                Success = false,
                Message = "Payment portal is not configured. Please register your company first.",
                ErrorCode = "NOT_CONFIGURED"
            };
        }

        try
        {
            using var request = CreateRequest(HttpMethod.Post, $"/connect/{Uri.EscapeDataString(provider)}");
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return DeserializeResponse<PortalOAuthResponse>(content) ?? new PortalOAuthResponse
                {
                    Success = false,
                    Message = "Unexpected response from server."
                };
            }

            var statusCode = (int)response.StatusCode;
            var message = statusCode == 401
                ? "Authentication failed. Your portal API key may be invalid or the company has not been registered."
                : $"Failed to initiate connection (HTTP {statusCode}).";

            // Try to extract server error message
            var serverResponse = DeserializeResponse<PortalOAuthResponse>(content);
            if (serverResponse != null && !string.IsNullOrEmpty(serverResponse.Message))
            {
                message = serverResponse.Message;
            }

            return new PortalOAuthResponse
            {
                Success = false,
                Message = message,
                ErrorCode = statusCode.ToString()
            };
        }
        catch (TaskCanceledException)
        {
            return new PortalOAuthResponse { Success = false, Message = "Request timed out.", ErrorCode = "TIMEOUT" };
        }
        catch (HttpRequestException ex)
        {
            return new PortalOAuthResponse { Success = false, Message = $"Network error: {ex.Message}", ErrorCode = "NETWORK_ERROR" };
        }
    }

    /// <summary>
    /// Disconnects a payment provider account.
    /// Returns the full response including updated provider state.
    /// </summary>
    public async Task<PortalDisconnectResponse> DisconnectProviderAsync(
        string provider,
        CancellationToken cancellationToken = default)
    {
        if (!PortalSettings.IsConfigured)
        {
            return new PortalDisconnectResponse { Success = false, Message = "Portal not configured." };
        }

        try
        {
            using var request = CreateRequest(HttpMethod.Delete, $"/connect/{Uri.EscapeDataString(provider)}");
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return DeserializeResponse<PortalDisconnectResponse>(content)
                    ?? new PortalDisconnectResponse { Success = true };
            }

            return new PortalDisconnectResponse
            {
                Success = false,
                Message = $"Server returned status {(int)response.StatusCode}"
            };
        }
        catch (TaskCanceledException)
        {
            return new PortalDisconnectResponse { Success = false, Message = "Request timed out." };
        }
        catch (HttpRequestException ex)
        {
            return new PortalDisconnectResponse { Success = false, Message = $"Network error: {ex.Message}" };
        }
    }

    #endregion

    #region Registration

    /// <summary>
    /// Registers the company with the payment portal using a premium license key.
    /// On success, saves the returned per-company API key to .env.
    /// </summary>
    public async Task<PortalRegisterResponse> RegisterCompanyAsync(
        string licenseKey,
        string deviceId,
        string companyName,
        string? ownerEmail,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = PortalSettings.ApiBaseUrl.TrimEnd('/') + "/register";
            using var request = new HttpRequestMessage(HttpMethod.Post, url);

            var body = new { licenseKey, deviceId, companyName, ownerEmail };
            var json = JsonSerializer.Serialize(body, SerializeOptions);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = DeserializeResponse<PortalRegisterResponse>(content);
                if (result != null && !string.IsNullOrEmpty(result.ApiKey))
                {
                    // Activate the key in-memory for immediate use (persisted to .argo by caller)
                    DotEnv.SetInMemory(PortalSettings.ApiKeyEnvVar, result.ApiKey);
                    return result;
                }

                return new PortalRegisterResponse
                {
                    Success = false,
                    Message = "Server returned success but no API key."
                };
            }

            var errorResponse = DeserializeResponse<PortalRegisterResponse>(content);
            var message = errorResponse?.Error ?? errorResponse?.Message
                ?? $"Registration failed (HTTP {(int)response.StatusCode}).";

            if ((int)response.StatusCode == 401 && !string.IsNullOrEmpty(licenseKey))
                message = "Invalid or expired license key. Please check your premium subscription.";

            return new PortalRegisterResponse { Success = false, Message = message };
        }
        catch (TaskCanceledException)
        {
            return new PortalRegisterResponse { Success = false, Message = "Request timed out." };
        }
        catch (HttpRequestException ex)
        {
            return new PortalRegisterResponse { Success = false, Message = $"Network error: {ex.Message}" };
        }
    }

    #endregion

    #region Company Name

    /// <summary>
    /// Updates the company display name on the payment portal.
    /// </summary>
    public async Task<PortalCompanyNameResponse> UpdateCompanyNameAsync(
        string companyName,
        CancellationToken cancellationToken = default)
    {
        if (!PortalSettings.IsConfigured)
        {
            return new PortalCompanyNameResponse { Success = false, Message = "Portal not configured." };
        }

        try
        {
            var json = JsonSerializer.Serialize(new { companyName }, SerializeOptions);
            using var request = CreateRequest(HttpMethod.Put, "/company-name");
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return DeserializeResponse<PortalCompanyNameResponse>(content) ?? new PortalCompanyNameResponse
                {
                    Success = true,
                    CompanyName = companyName,
                    Message = "Company name updated."
                };
            }

            var errorResponse = DeserializeResponse<PortalCompanyNameResponse>(content);
            return errorResponse ?? new PortalCompanyNameResponse
            {
                Success = false,
                Message = $"Update failed with status {(int)response.StatusCode}"
            };
        }
        catch (TaskCanceledException)
        {
            return new PortalCompanyNameResponse { Success = false, Message = "Request timed out." };
        }
        catch (HttpRequestException ex)
        {
            return new PortalCompanyNameResponse { Success = false, Message = $"Network error: {ex.Message}" };
        }
    }

    #endregion

    #region Company Logo

    /// <summary>
    /// Uploads a company logo to the payment portal.
    /// </summary>
    public async Task<PortalLogoResponse> UploadCompanyLogoAsync(
        string logoFilePath,
        CancellationToken cancellationToken = default)
    {
        if (!PortalSettings.IsConfigured)
        {
            return new PortalLogoResponse { Success = false, Message = "Portal not configured." };
        }

        if (!File.Exists(logoFilePath))
        {
            return new PortalLogoResponse { Success = false, Message = "Logo file not found." };
        }

        try
        {
            var fileBytes = await File.ReadAllBytesAsync(logoFilePath, cancellationToken);
            var fileName = Path.GetFileName(logoFilePath);
            var extension = Path.GetExtension(logoFilePath).ToLowerInvariant();
            var contentType = extension switch
            {
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".bmp" => "image/bmp",
                ".svg" => "image/svg+xml",
                _ => "application/octet-stream"
            };

            using var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            content.Add(fileContent, "logo", fileName);

            using var request = CreateRequest(HttpMethod.Post, "/logo");
            request.Content = content;

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return DeserializeResponse<PortalLogoResponse>(responseContent) ?? new PortalLogoResponse
                {
                    Success = true,
                    Message = "Logo uploaded."
                };
            }

            var errorResponse = DeserializeResponse<PortalLogoResponse>(responseContent);
            return errorResponse ?? new PortalLogoResponse
            {
                Success = false,
                Message = $"Upload failed with status {(int)response.StatusCode}"
            };
        }
        catch (TaskCanceledException)
        {
            return new PortalLogoResponse { Success = false, Message = "Request timed out." };
        }
        catch (HttpRequestException ex)
        {
            return new PortalLogoResponse { Success = false, Message = $"Network error: {ex.Message}" };
        }
        catch (Exception)
        {
            return new PortalLogoResponse { Success = false, Message = "An unexpected error occurred while uploading the logo." };
        }
    }

    /// <summary>
    /// Deletes the company logo from the payment portal.
    /// </summary>
    public async Task<PortalLogoResponse> DeleteCompanyLogoAsync(
        CancellationToken cancellationToken = default)
    {
        if (!PortalSettings.IsConfigured)
        {
            return new PortalLogoResponse { Success = false, Message = "Portal not configured." };
        }

        try
        {
            using var request = CreateRequest(HttpMethod.Delete, "/logo");
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return DeserializeResponse<PortalLogoResponse>(content) ?? new PortalLogoResponse
                {
                    Success = true,
                    Message = "Logo removed."
                };
            }

            return new PortalLogoResponse
            {
                Success = false,
                Message = $"Delete failed with status {(int)response.StatusCode}"
            };
        }
        catch (TaskCanceledException)
        {
            return new PortalLogoResponse { Success = false, Message = "Request timed out." };
        }
        catch (HttpRequestException ex)
        {
            return new PortalLogoResponse { Success = false, Message = $"Network error: {ex.Message}" };
        }
    }

    #endregion

    #region Helpers

    private static HttpRequestMessage CreateRequest(HttpMethod method, string path)
    {
        var url = PortalSettings.ApiBaseUrl.TrimEnd('/') + path;
        var request = new HttpRequestMessage(method, url);

        var apiKey = PortalSettings.ApiKey;
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Headers.Add("X-Api-Key", apiKey);

        return request;
    }

    private static T? DeserializeResponse<T>(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, DeserializeOptions);
        }
        catch
        {
            return default;
        }
    }

    #endregion

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _httpClient.Dispose();
            }
            _disposed = true;
        }
    }
}
