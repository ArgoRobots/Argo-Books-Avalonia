using System.Net.Http.Headers;
using System.Text;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Models;
using ArgoBooks.Core.Models.Invoices;
using ArgoBooks.Core.Models.Transactions;

namespace ArgoBooks.Core.Services.InvoiceTemplates;

/// <summary>
/// Service for sending invoice emails via the configured API endpoint.
/// </summary>
public class InvoiceEmailService : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly InvoiceHtmlRenderer _htmlRenderer;
    private bool _disposed;

    public InvoiceEmailService()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        _htmlRenderer = new InvoiceHtmlRenderer();
    }

    /// <summary>
    /// Sends an invoice email to the customer.
    /// </summary>
    /// <param name="invoice">The invoice to send.</param>
    /// <param name="template">The email template to use.</param>
    /// <param name="companyData">Company data for customer lookup and company info.</param>
    /// <param name="emailSettings">Email API settings.</param>
    /// <param name="currencySymbol">Currency symbol for formatting.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The API response.</returns>
    public async Task<InvoiceEmailResponse> SendInvoiceAsync(
        Invoice invoice,
        InvoiceTemplate template,
        CompanyData companyData,
        InvoiceEmailSettings emailSettings,
        string currencySymbol = "$",
        CancellationToken cancellationToken = default)
    {
        if (!InvoiceEmailSettings.IsConfigured)
        {
            return new InvoiceEmailResponse
            {
                Success = false,
                Message = $"Email API is not configured. Please add {InvoiceEmailSettings.ApiEndpointEnvVar} and {InvoiceEmailSettings.ApiKeyEnvVar} to your .env file.",
                ErrorCode = "NOT_CONFIGURED"
            };
        }

        var customer = companyData.GetCustomer(invoice.CustomerId);
        if (customer == null)
        {
            return new InvoiceEmailResponse
            {
                Success = false,
                Message = "Customer not found for this invoice.",
                ErrorCode = "CUSTOMER_NOT_FOUND"
            };
        }

        if (string.IsNullOrWhiteSpace(customer.Email))
        {
            return new InvoiceEmailResponse
            {
                Success = false,
                Message = $"Customer '{customer.Name}' does not have an email address.",
                ErrorCode = "NO_EMAIL"
            };
        }

        try
        {
            // Render the HTML email content
            var html = _htmlRenderer.RenderInvoice(invoice, template, companyData, currencySymbol);
            var plainText = _htmlRenderer.RenderPlainText(invoice, template, companyData, currencySymbol);

            // Build the subject line
            var subject = BuildSubject(emailSettings.SubjectTemplate, invoice, companyData.Settings);

            // Build the request
            var request = new InvoiceEmailRequest
            {
                To = customer.Email,
                ToName = customer.Name,
                From = emailSettings.FromEmail,
                FromName = !string.IsNullOrWhiteSpace(emailSettings.FromName)
                    ? emailSettings.FromName
                    : companyData.Settings.Company.Name,
                ReplyTo = !string.IsNullOrWhiteSpace(emailSettings.ReplyToEmail)
                    ? emailSettings.ReplyToEmail
                    : null,
                Bcc = !string.IsNullOrWhiteSpace(emailSettings.BccEmail)
                    ? emailSettings.BccEmail
                    : null,
                Subject = subject,
                Html = html,
                Text = plainText,
                InvoiceId = invoice.Id
            };

            // Send the request
            return await SendEmailRequestAsync(request, emailSettings, cancellationToken);
        }
        catch (TaskCanceledException)
        {
            return new InvoiceEmailResponse
            {
                Success = false,
                Message = "The request timed out. Please check your internet connection and try again.",
                ErrorCode = "TIMEOUT"
            };
        }
        catch (HttpRequestException ex)
        {
            return new InvoiceEmailResponse
            {
                Success = false,
                Message = $"Network error: {ex.Message}",
                ErrorCode = "NETWORK_ERROR"
            };
        }
        catch (Exception ex)
        {
            return new InvoiceEmailResponse
            {
                Success = false,
                Message = $"An error occurred: {ex.Message}",
                ErrorCode = "UNKNOWN_ERROR"
            };
        }
    }

    /// <summary>
    /// Sends a custom email request to the API.
    /// </summary>
    public async Task<InvoiceEmailResponse> SendEmailRequestAsync(
        InvoiceEmailRequest request,
        InvoiceEmailSettings emailSettings,
        CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, InvoiceEmailSettings.ApiEndpoint);
        httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

        // Add API key authentication (from .env file)
        var apiKey = InvoiceEmailSettings.ApiKey;
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        httpRequest.Headers.Add("X-Api-Key", apiKey);

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            try
            {
                var result = JsonSerializer.Deserialize<InvoiceEmailResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result ?? new InvoiceEmailResponse
                {
                    Success = true,
                    Message = "Email sent successfully.",
                    Timestamp = DateTime.UtcNow
                };
            }
            catch
            {
                // If we can't parse the response, assume success since HTTP status was OK
                return new InvoiceEmailResponse
                {
                    Success = true,
                    Message = "Email sent successfully.",
                    Timestamp = DateTime.UtcNow
                };
            }
        }

        // Try to parse error response
        try
        {
            var errorResult = JsonSerializer.Deserialize<InvoiceEmailResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return errorResult ?? new InvoiceEmailResponse
            {
                Success = false,
                Message = $"API error: {response.StatusCode} - {responseContent}",
                ErrorCode = ((int)response.StatusCode).ToString()
            };
        }
        catch
        {
            return new InvoiceEmailResponse
            {
                Success = false,
                Message = $"API error: {response.StatusCode} - {responseContent}",
                ErrorCode = ((int)response.StatusCode).ToString()
            };
        }
    }

    /// <summary>
    /// Tests the email API connection.
    /// </summary>
    public async Task<InvoiceEmailResponse> TestConnectionAsync(
        CancellationToken cancellationToken = default)
    {
        if (!InvoiceEmailSettings.IsConfigured)
        {
            return new InvoiceEmailResponse
            {
                Success = false,
                Message = $"Email API is not configured. Please add {InvoiceEmailSettings.ApiEndpointEnvVar} and {InvoiceEmailSettings.ApiKeyEnvVar} to your .env file.",
                ErrorCode = "NOT_CONFIGURED"
            };
        }

        try
        {
            // Try to reach the API endpoint with a simple HEAD or GET request
            var apiKey = InvoiceEmailSettings.ApiKey;
            using var request = new HttpRequestMessage(HttpMethod.Get, InvoiceEmailSettings.ApiEndpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Headers.Add("X-Api-Key", apiKey);

            using var response = await _httpClient.SendAsync(request, cancellationToken);

            // Any response means the endpoint is reachable
            return new InvoiceEmailResponse
            {
                Success = true,
                Message = $"Connection successful. API responded with status {(int)response.StatusCode}.",
                Timestamp = DateTime.UtcNow
            };
        }
        catch (TaskCanceledException)
        {
            return new InvoiceEmailResponse
            {
                Success = false,
                Message = "Connection timed out.",
                ErrorCode = "TIMEOUT"
            };
        }
        catch (HttpRequestException ex)
        {
            return new InvoiceEmailResponse
            {
                Success = false,
                Message = $"Connection failed: {ex.Message}",
                ErrorCode = "CONNECTION_ERROR"
            };
        }
    }

    /// <summary>
    /// Renders invoice HTML for preview purposes.
    /// </summary>
    public string RenderInvoiceHtml(
        Invoice invoice,
        InvoiceTemplate template,
        CompanyData companyData,
        string currencySymbol = "$")
    {
        return _htmlRenderer.RenderInvoice(invoice, template, companyData, currencySymbol);
    }

    /// <summary>
    /// Renders a preview with sample data.
    /// </summary>
    public string RenderTemplatePreview(InvoiceTemplate template, CompanySettings companySettings)
    {
        return _htmlRenderer.RenderPreview(template, companySettings);
    }

    private static string BuildSubject(string template, Invoice invoice, CompanySettings settings)
    {
        var subject = template;

        subject = subject.Replace("{InvoiceNumber}", invoice.InvoiceNumber);
        subject = subject.Replace("{InvoiceId}", invoice.Id);
        subject = subject.Replace("{CompanyName}", settings.Company.Name);
        subject = subject.Replace("{IssueDate}", invoice.IssueDate.ToString("yyyy-MM-dd"));
        subject = subject.Replace("{DueDate}", invoice.DueDate.ToString("yyyy-MM-dd"));
        subject = subject.Replace("{Total}", invoice.Total.ToString("N2"));

        return subject;
    }

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
