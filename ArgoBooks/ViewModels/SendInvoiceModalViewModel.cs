using System.Collections.ObjectModel;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Invoices;
using ArgoBooks.Core.Models.Transactions;
using ArgoBooks.Core.Services.InvoiceTemplates;
using ArgoBooks.Localization;
using ArgoBooks.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the Send Invoice modal.
/// Handles invoice email sending with template selection and preview.
/// </summary>
public partial class SendInvoiceModalViewModel : ViewModelBase
{
    #region Events

    public event EventHandler? InvoiceSent;
    public event EventHandler? ModalClosed;

    #endregion

    #region Modal State

    [ObservableProperty]
    private bool _isOpen;

    [ObservableProperty]
    private bool _isSending;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _hasStatusMessage;

    [ObservableProperty]
    private bool _isError;

    #endregion

    #region Invoice Data

    private Invoice? _invoice;

    [ObservableProperty]
    private string _invoiceId = string.Empty;

    [ObservableProperty]
    private string _invoiceNumber = string.Empty;

    [ObservableProperty]
    private string _customerName = string.Empty;

    [ObservableProperty]
    private string _customerEmail = string.Empty;

    [ObservableProperty]
    private string _invoiceTotal = string.Empty;

    [ObservableProperty]
    private string _invoiceBalance = string.Empty;

    [ObservableProperty]
    private string _dueDate = string.Empty;

    [ObservableProperty]
    private bool _hasCustomerEmail;

    #endregion

    #region Template Selection

    [ObservableProperty]
    private InvoiceTemplate? _selectedTemplate;

    [ObservableProperty]
    private string _previewHtml = string.Empty;

    public ObservableCollection<InvoiceTemplateOption> TemplateOptions { get; } = [];

    #endregion

    #region Email Settings

    [ObservableProperty]
    private bool _isEmailConfigured;

    [ObservableProperty]
    private string _fromEmail = string.Empty;

    [ObservableProperty]
    private string _subject = string.Empty;

    #endregion

    #region Services

    private readonly InvoiceEmailService _emailService;

    #endregion

    #region Constructor

    public SendInvoiceModalViewModel()
    {
        _emailService = new InvoiceEmailService();
    }

    #endregion

    #region Open Modal

    public void OpenForInvoice(Invoice invoice)
    {
        _invoice = invoice;
        LoadInvoiceData();
        LoadTemplates();
        LoadEmailSettings();
        UpdatePreview();
        IsOpen = true;
    }

    public void OpenForInvoice(string invoiceId)
    {
        var invoice = App.CompanyManager?.CompanyData?.GetInvoice(invoiceId);
        if (invoice != null)
        {
            OpenForInvoice(invoice);
        }
    }

    #endregion

    #region Commands

    [RelayCommand]
    private void Close()
    {
        IsOpen = false;
        ModalClosed?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private async Task Send()
    {
        if (_invoice == null || SelectedTemplate == null)
        {
            ShowError("Please select an invoice template.");
            return;
        }

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null)
        {
            ShowError("Company data not available.");
            return;
        }

        if (!InvoiceEmailSettings.IsConfigured)
        {
            ShowError($"Email API is not configured. Please add {InvoiceEmailSettings.ApiEndpointEnvVar} and {InvoiceEmailSettings.ApiKeyEnvVar} to your .env file.");
            return;
        }

        var emailSettings = companyData.Settings.InvoiceEmail;

        if (!HasCustomerEmail)
        {
            ShowError("Customer does not have an email address.");
            return;
        }

        IsSending = true;
        StatusMessage = "Sending invoice...".Translate();
        HasStatusMessage = true;
        IsError = false;

        try
        {
            var currencySymbol = CurrencyService.GetCurrencySymbol(
                companyData.Settings.Localization.Currency);

            var response = await _emailService.SendInvoiceAsync(
                _invoice,
                SelectedTemplate,
                companyData,
                emailSettings,
                currencySymbol);

            if (response.Success)
            {
                // Update invoice status to Sent if it was Draft or Pending
                if (_invoice.Status is InvoiceStatus.Draft or InvoiceStatus.Pending)
                {
                    _invoice.Status = InvoiceStatus.Sent;
                    _invoice.UpdatedAt = DateTime.UtcNow;

                    // Add history entry
                    _invoice.History.Add(new InvoiceHistoryEntry
                    {
                        Action = "Sent",
                        Details = $"Invoice sent to {CustomerEmail}",
                        Timestamp = DateTime.UtcNow
                    });

                    App.CompanyManager?.MarkAsChanged();
                }

                StatusMessage = "Invoice sent successfully!".Translate();
                IsError = false;

                // Close modal after short delay
                await Task.Delay(1500);
                InvoiceSent?.Invoke(this, EventArgs.Empty);
                Close();
            }
            else
            {
                ShowError(response.Message);
            }
        }
        catch (Exception ex)
        {
            ShowError($"Failed to send invoice: {ex.Message}");
        }
        finally
        {
            IsSending = false;
        }
    }

    [RelayCommand]
    private async Task PreviewInBrowser()
    {
        UpdatePreview();
        await InvoicePreviewService.PreviewInBrowserAsync(PreviewHtml, _invoice?.Id);
    }

    [RelayCommand]
    private void OpenEmailSettings()
    {
        // Navigate to settings - this would be handled by the view
        Close();
        App.NavigationService?.NavigateTo("Settings", new Dictionary<string, object?>
        {
            { "openTab", "invoiceEmail" }
        });
    }

    [RelayCommand]
    private void OpenTemplateDesigner()
    {
        // This would open the template designer modal
        // The view should handle this by opening InvoiceTemplateDesignerModal
    }

    [RelayCommand]
    private void SelectTemplate(InvoiceTemplate? template)
    {
        if (template != null)
        {
            SelectedTemplate = template;
        }
    }

    #endregion

    #region Property Change Handlers

    partial void OnSelectedTemplateChanged(InvoiceTemplate? value)
    {
        if (value != null)
        {
            UpdatePreview();
        }
    }

    #endregion

    #region Helper Methods

    private void LoadInvoiceData()
    {
        if (_invoice == null) return;

        var companyData = App.CompanyManager?.CompanyData;
        var customer = companyData?.GetCustomer(_invoice.CustomerId);
        var currencySymbol = CurrencyService.GetCurrencySymbol(
            companyData?.Settings.Localization.Currency ?? "USD");

        InvoiceId = _invoice.Id;
        InvoiceNumber = _invoice.InvoiceNumber;
        CustomerName = customer?.Name ?? "Unknown Customer";
        CustomerEmail = customer?.Email ?? string.Empty;
        HasCustomerEmail = !string.IsNullOrWhiteSpace(customer?.Email);
        InvoiceTotal = $"{currencySymbol}{_invoice.Total:N2}";
        InvoiceBalance = $"{currencySymbol}{_invoice.Balance:N2}";
        DueDate = _invoice.DueDate.ToString("MMMM d, yyyy");

        // Reset status
        StatusMessage = string.Empty;
        HasStatusMessage = false;
        IsError = false;
    }

    private void LoadTemplates()
    {
        TemplateOptions.Clear();

        var companyData = App.CompanyManager?.CompanyData;
        var templates = companyData?.InvoiceTemplates ?? [];

        // If no templates exist, create defaults
        if (templates.Count == 0)
        {
            templates = InvoiceTemplateFactory.CreateDefaultTemplates();
            foreach (var t in templates)
            {
                companyData?.InvoiceTemplates.Add(t);
            }
            App.CompanyManager?.MarkAsChanged();
        }

        foreach (var template in templates)
        {
            TemplateOptions.Add(new InvoiceTemplateOption
            {
                Id = template.Id,
                Name = template.Name,
                BaseType = template.BaseTemplate.ToString(),
                IsDefault = template.IsDefault,
                Template = template
            });
        }

        // Select the default template or the first one
        var defaultTemplate = templates.FirstOrDefault(t => t.IsDefault) ?? templates.FirstOrDefault();
        if (defaultTemplate != null)
        {
            SelectedTemplate = defaultTemplate;
        }
    }

    private void LoadEmailSettings()
    {
        // Check if API credentials are configured in .env file
        IsEmailConfigured = InvoiceEmailSettings.IsConfigured;

        var settings = App.CompanyManager?.CompanyData?.Settings.InvoiceEmail;
        if (settings != null)
        {
            FromEmail = settings.FromEmail;

            // Build default subject
            if (_invoice != null)
            {
                var companyName = App.CompanyManager?.CompanyData?.Settings.Company.Name ?? "Company";
                Subject = settings.SubjectTemplate
                    .Replace("{InvoiceNumber}", _invoice.InvoiceNumber)
                    .Replace("{CompanyName}", companyName);
            }
        }
    }

    private void UpdatePreview()
    {
        if (_invoice == null || SelectedTemplate == null) return;

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null) return;

        var currencySymbol = CurrencyService.GetCurrencySymbol(
            companyData.Settings.Localization.Currency);

        PreviewHtml = _emailService.RenderInvoiceHtml(
            _invoice,
            SelectedTemplate,
            companyData,
            currencySymbol);
    }

    private void ShowError(string message)
    {
        StatusMessage = message;
        HasStatusMessage = true;
        IsError = true;
    }

    #endregion
}

/// <summary>
/// Display model for template selection.
/// </summary>
public partial class InvoiceTemplateOption : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _baseType = string.Empty;

    [ObservableProperty]
    private bool _isDefault;

    public InvoiceTemplate? Template { get; set; }
}
