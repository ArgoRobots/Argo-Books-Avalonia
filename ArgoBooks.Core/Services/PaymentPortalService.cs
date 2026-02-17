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
public class PaymentPortalService
{
    private readonly HttpClient _httpClient;

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
                Message = $"Payment portal is not configured. Please add {PortalSettings.ApiKeyEnvVar} to your .env file."
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
                Message = $"Payment portal is not configured. Please add {PortalSettings.ApiKeyEnvVar} to your .env file.",
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

            var json = JsonSerializer.Serialize(publishRequest, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

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
        catch (Exception ex)
        {
            return new PortalPublishResponse { Success = false, Message = $"Error: {ex.Message}", ErrorCode = "UNKNOWN_ERROR" };
        }
    }

    #endregion

    #region Sync Payments

    /// <summary>
    /// Syncs new payments from the portal. Returns payment records that haven't been synced yet.
    /// </summary>
    public async Task<PortalSyncResponse> SyncPaymentsAsync(
        DateTime? since = null,
        CancellationToken cancellationToken = default)
    {
        if (!PortalSettings.IsConfigured)
        {
            return new PortalSyncResponse
            {
                Success = false,
                Message = $"Payment portal is not configured. Please add {PortalSettings.ApiKeyEnvVar} to your .env file.",
                ErrorCode = "NOT_CONFIGURED"
            };
        }

        try
        {
            var url = "/payments/sync";
            if (since.HasValue)
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
            var json = JsonSerializer.Serialize(confirmRequest, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

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
            // Skip if we already have this portal payment (duplicate prevention)
            var alreadySynced = companyData.Payments.Any(p =>
                p.PortalPaymentId == portalPayment.Id.ToString());
            if (alreadySynced) continue;

            // Find the matching invoice
            var invoice = companyData.GetInvoice(portalPayment.InvoiceId);
            if (invoice == null) continue;

            // Map payment method
            var method = portalPayment.PaymentMethod.ToLowerInvariant() switch
            {
                "stripe" => PaymentMethod.CreditCard,
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
            var paymentId = $"PAY-{DateTime.Now:yyyy}-{nextId:D5}";

            var payment = new Payment
            {
                Id = paymentId,
                InvoiceId = portalPayment.InvoiceId,
                CustomerId = invoice.CustomerId,
                Date = portalPayment.CreatedAt,
                Amount = portalPayment.Amount,
                PaymentMethod = method,
                ReferenceNumber = portalPayment.ReferenceNumber,
                Notes = $"Online payment via {providerName}",
                CreatedAt = DateTime.UtcNow,
                OriginalCurrency = portalPayment.Currency,
                AmountUSD = portalPayment.Currency.Equals("USD", StringComparison.OrdinalIgnoreCase)
                    ? portalPayment.Amount : 0,
                Source = "Online",
                PortalPaymentId = portalPayment.Id.ToString()
            };

            // Add to company data
            companyData.Payments.Add(payment);
            newPayments.Add(payment);

            // Update invoice balance
            var totalPaid = companyData.Payments
                .Where(p => p.InvoiceId == invoice.Id && p.Amount > 0)
                .Sum(p => p.Amount);

            invoice.AmountPaid = totalPaid;
            invoice.Balance = invoice.Total - totalPaid;

            if (invoice.Balance <= 0)
            {
                invoice.Status = Enums.InvoiceStatus.Paid;
            }
            else if (totalPaid > 0)
            {
                invoice.Status = Enums.InvoiceStatus.Partial;
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
                Message = $"Payment portal is not configured. Please add {PortalSettings.ApiKeyEnvVar} to your .env file.",
                ErrorCode = "NOT_CONFIGURED"
            };
        }

        try
        {
            using var request = CreateRequest(HttpMethod.Post, $"/connect/{provider}");
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
                ? "Authentication failed. Your portal API key may be invalid or the company has not been registered. Please check your .env file."
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
    /// </summary>
    public async Task<bool> DisconnectProviderAsync(
        string provider,
        CancellationToken cancellationToken = default)
    {
        if (!PortalSettings.IsConfigured) return false;

        try
        {
            using var request = CreateRequest(HttpMethod.Delete, $"/connect/{provider}");
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region Registration

    /// <summary>
    /// Registers the company with the payment portal using the master registration key.
    /// On success, saves the returned per-company API key to .env.
    /// </summary>
    public async Task<PortalRegisterResponse> RegisterCompanyAsync(
        string registrationKey,
        string companyName,
        string? ownerEmail,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = PortalSettings.ApiBaseUrl.TrimEnd('/') + "/register";
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", registrationKey);
            request.Headers.Add("X-Api-Key", registrationKey);

            var body = new { companyName, ownerEmail };
            var json = JsonSerializer.Serialize(body, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = DeserializeResponse<PortalRegisterResponse>(content);
                if (result != null && !string.IsNullOrEmpty(result.ApiKey))
                {
                    // Persist the API key to .env
                    DotEnv.Set(PortalSettings.ApiKeyEnvVar, result.ApiKey);
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

            if ((int)response.StatusCode == 401)
                message = "Invalid registration key. Please check that the key matches your server's PORTAL_REGISTRATION_KEY.";

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
            return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return default;
        }
    }

    #endregion
}
