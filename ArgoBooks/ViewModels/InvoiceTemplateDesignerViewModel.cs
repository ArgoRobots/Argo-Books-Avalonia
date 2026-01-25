using System.Collections.ObjectModel;
using ArgoBooks.Core.Models.Invoices;
using ArgoBooks.Core.Services.InvoiceTemplates;
using ArgoBooks.Localization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the Invoice Template Designer modal.
/// Allows users to customize invoice templates.
/// </summary>
public partial class InvoiceTemplateDesignerViewModel : ViewModelBase
{
    #region Events

    public event EventHandler? TemplateSaved;
    public event EventHandler? ModalClosed;
    public event EventHandler? BrowseLogoRequested;

    #endregion

    #region Modal State

    [ObservableProperty]
    private bool _isOpen;

    [ObservableProperty]
    private bool _isEditMode;

    [ObservableProperty]
    private string _modalTitle = "Create Invoice Template";

    [ObservableProperty]
    private string _validationMessage = string.Empty;

    [ObservableProperty]
    private bool _hasValidationMessage;

    #endregion

    #region Template Properties

    private string _editingTemplateId = string.Empty;

    [ObservableProperty]
    private string _templateName = string.Empty;

    [ObservableProperty]
    private InvoiceTemplateType _selectedBaseTemplate = InvoiceTemplateType.Professional;

    [ObservableProperty]
    private bool _isDefault;

    // Colors
    [ObservableProperty]
    private string _primaryColor = "#2563eb";

    [ObservableProperty]
    private string _secondaryColor = "#e5e7eb";

    [ObservableProperty]
    private string _accentColor = "#059669";

    [ObservableProperty]
    private string _textColor = "#1f2937";

    [ObservableProperty]
    private string _backgroundColor = "#ffffff";

    // Font
    [ObservableProperty]
    private string _selectedFontFamily = "Arial, Helvetica, sans-serif";

    // Logo
    [ObservableProperty]
    private bool _showLogo = true;

    [ObservableProperty]
    private string? _logoBase64;

    [ObservableProperty]
    private int _logoWidth = 150;

    [ObservableProperty]
    private bool _hasLogo;

    // Text content
    [ObservableProperty]
    private string _headerText = "INVOICE";

    [ObservableProperty]
    private string _footerText = "Thank you for your business!";

    [ObservableProperty]
    private string _paymentInstructions = string.Empty;

    // Display options
    [ObservableProperty]
    private bool _showCompanyAddress = true;

    [ObservableProperty]
    private bool _showTaxBreakdown = true;

    [ObservableProperty]
    private bool _showItemDescriptions = true;

    [ObservableProperty]
    private bool _showNotes = true;

    [ObservableProperty]
    private bool _showPaymentInstructions = true;

    [ObservableProperty]
    private bool _showDueDateProminent = true;

    // Preview HTML
    [ObservableProperty]
    private string _previewHtml = string.Empty;

    #endregion

    #region Collections

    public ObservableCollection<InvoiceTemplateType> BaseTemplateOptions { get; } =
    [
        InvoiceTemplateType.Professional,
        InvoiceTemplateType.Modern,
        InvoiceTemplateType.Classic,
        InvoiceTemplateType.Minimal
    ];

    public ObservableCollection<string> FontFamilyOptions { get; } =
    [
        "Arial, Helvetica, sans-serif",
        "'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif",
        "Georgia, 'Times New Roman', Times, serif",
        "system-ui, -apple-system, sans-serif",
        "'Courier New', Courier, monospace"
    ];

    #endregion

    #region Constructor

    public InvoiceTemplateDesignerViewModel()
    {
    }

    #endregion

    #region Open Modal

    public void OpenCreateModal()
    {
        ResetForm();
        IsEditMode = false;
        ModalTitle = "Create Invoice Template".Translate();
        UpdatePreview();
        IsOpen = true;
    }

    public void OpenEditModal(InvoiceTemplate template)
    {
        LoadTemplate(template);
        IsEditMode = true;
        ModalTitle = $"Edit Template: {template.Name}".Translate();
        UpdatePreview();
        IsOpen = true;
    }

    public void OpenEditModal(string templateId)
    {
        var template = App.CompanyManager?.CompanyData?.GetInvoiceTemplate(templateId);
        if (template != null)
        {
            OpenEditModal(template);
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
    private void Save()
    {
        // Validation
        if (string.IsNullOrWhiteSpace(TemplateName))
        {
            ValidationMessage = "Please enter a template name".Translate();
            HasValidationMessage = true;
            return;
        }

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null) return;

        if (IsEditMode)
        {
            // Update existing template
            var template = companyData.GetInvoiceTemplate(_editingTemplateId);
            if (template != null)
            {
                UpdateTemplateFromForm(template);

                // If this is being set as default, unset others
                if (template.IsDefault)
                {
                    foreach (var t in companyData.InvoiceTemplates.Where(t => t.Id != template.Id))
                    {
                        t.IsDefault = false;
                    }
                }
            }
        }
        else
        {
            // Create new template
            var id = $"template-{++companyData.IdCounters.InvoiceTemplate}";
            var template = new InvoiceTemplate { Id = id };
            UpdateTemplateFromForm(template);
            template.CreatedAt = DateTime.UtcNow;

            // If this is being set as default or is the first template, handle defaults
            if (template.IsDefault || companyData.InvoiceTemplates.Count == 0)
            {
                template.IsDefault = true;
                foreach (var t in companyData.InvoiceTemplates)
                {
                    t.IsDefault = false;
                }
            }

            companyData.InvoiceTemplates.Add(template);
        }

        App.CompanyManager?.MarkAsChanged();
        TemplateSaved?.Invoke(this, EventArgs.Empty);
        Close();
    }

    [RelayCommand]
    private void SelectLogo()
    {
        // Raise event to open file picker in App.axaml.cs
        BrowseLogoRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Sets the logo from a file path.
    /// </summary>
    /// <param name="filePath">Path to the image file.</param>
    public void SetLogoFromFile(string filePath)
    {
        try
        {
            var bytes = System.IO.File.ReadAllBytes(filePath);
            LogoBase64 = Convert.ToBase64String(bytes);
            HasLogo = true;
            UpdatePreview();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load logo: {ex.Message}");
        }
    }

    [RelayCommand]
    private void RemoveLogo()
    {
        LogoBase64 = null;
        HasLogo = false;
        UpdatePreview();
    }

    [RelayCommand]
    private async Task PreviewInBrowser()
    {
        UpdatePreview();
        await InvoicePreviewService.PreviewInBrowserAsync(PreviewHtml, "template-preview");
    }

    #endregion

    #region Property Change Handlers

    partial void OnSelectedBaseTemplateChanged(InvoiceTemplateType value)
    {
        // Load default colors for the selected base template
        var defaults = value switch
        {
            InvoiceTemplateType.Professional => InvoiceTemplateFactory.CreateProfessionalTemplate(),
            InvoiceTemplateType.Modern => InvoiceTemplateFactory.CreateModernTemplate(),
            InvoiceTemplateType.Classic => InvoiceTemplateFactory.CreateClassicTemplate(),
            InvoiceTemplateType.Minimal => InvoiceTemplateFactory.CreateMinimalTemplate(),
            _ => InvoiceTemplateFactory.CreateProfessionalTemplate()
        };

        // Only update colors if not in edit mode or if this is a new template
        if (!IsEditMode)
        {
            PrimaryColor = defaults.PrimaryColor;
            SecondaryColor = defaults.SecondaryColor;
            AccentColor = defaults.AccentColor;
            TextColor = defaults.TextColor;
            SelectedFontFamily = defaults.FontFamily;
        }

        UpdatePreview();
    }

    partial void OnPrimaryColorChanged(string value) => UpdatePreview();
    partial void OnSecondaryColorChanged(string value) => UpdatePreview();
    partial void OnAccentColorChanged(string value) => UpdatePreview();
    partial void OnTextColorChanged(string value) => UpdatePreview();
    partial void OnBackgroundColorChanged(string value) => UpdatePreview();
    partial void OnSelectedFontFamilyChanged(string value) => UpdatePreview();
    partial void OnHeaderTextChanged(string value) => UpdatePreview();
    partial void OnFooterTextChanged(string value) => UpdatePreview();
    partial void OnPaymentInstructionsChanged(string value) => UpdatePreview();
    partial void OnShowLogoChanged(bool value) => UpdatePreview();
    partial void OnShowCompanyAddressChanged(bool value) => UpdatePreview();
    partial void OnShowTaxBreakdownChanged(bool value) => UpdatePreview();
    partial void OnShowItemDescriptionsChanged(bool value) => UpdatePreview();
    partial void OnShowNotesChanged(bool value) => UpdatePreview();
    partial void OnShowPaymentInstructionsChanged(bool value) => UpdatePreview();
    partial void OnShowDueDateProminentChanged(bool value) => UpdatePreview();
    partial void OnLogoBase64Changed(string? value)
    {
        HasLogo = !string.IsNullOrEmpty(value);
        UpdatePreview();
    }

    #endregion

    #region Helper Methods

    private void UpdatePreview()
    {
        var template = BuildTemplateFromForm();
        var companySettings = App.CompanyManager?.CompanyData?.Settings ?? new();
        var emailService = new InvoiceEmailService();
        PreviewHtml = emailService.RenderTemplatePreview(template, companySettings);
    }

    private InvoiceTemplate BuildTemplateFromForm()
    {
        return new InvoiceTemplate
        {
            Id = _editingTemplateId,
            Name = TemplateName,
            BaseTemplate = SelectedBaseTemplate,
            IsDefault = IsDefault,
            PrimaryColor = PrimaryColor,
            SecondaryColor = SecondaryColor,
            AccentColor = AccentColor,
            TextColor = TextColor,
            BackgroundColor = BackgroundColor,
            FontFamily = SelectedFontFamily,
            LogoBase64 = LogoBase64,
            LogoWidth = LogoWidth,
            HeaderText = HeaderText,
            FooterText = FooterText,
            PaymentInstructions = PaymentInstructions,
            ShowLogo = ShowLogo,
            ShowCompanyAddress = ShowCompanyAddress,
            ShowTaxBreakdown = ShowTaxBreakdown,
            ShowItemDescriptions = ShowItemDescriptions,
            ShowNotes = ShowNotes,
            ShowPaymentInstructions = ShowPaymentInstructions,
            ShowDueDateProminent = ShowDueDateProminent
        };
    }

    private void UpdateTemplateFromForm(InvoiceTemplate template)
    {
        template.Name = TemplateName;
        template.BaseTemplate = SelectedBaseTemplate;
        template.IsDefault = IsDefault;
        template.PrimaryColor = PrimaryColor;
        template.SecondaryColor = SecondaryColor;
        template.AccentColor = AccentColor;
        template.TextColor = TextColor;
        template.BackgroundColor = BackgroundColor;
        template.FontFamily = SelectedFontFamily;
        template.LogoBase64 = LogoBase64;
        template.LogoWidth = LogoWidth;
        template.HeaderText = HeaderText;
        template.FooterText = FooterText;
        template.PaymentInstructions = PaymentInstructions;
        template.ShowLogo = ShowLogo;
        template.ShowCompanyAddress = ShowCompanyAddress;
        template.ShowTaxBreakdown = ShowTaxBreakdown;
        template.ShowItemDescriptions = ShowItemDescriptions;
        template.ShowNotes = ShowNotes;
        template.ShowPaymentInstructions = ShowPaymentInstructions;
        template.ShowDueDateProminent = ShowDueDateProminent;
        template.UpdatedAt = DateTime.UtcNow;
    }

    private void LoadTemplate(InvoiceTemplate template)
    {
        _editingTemplateId = template.Id;
        TemplateName = template.Name;
        SelectedBaseTemplate = template.BaseTemplate;
        IsDefault = template.IsDefault;
        PrimaryColor = template.PrimaryColor;
        SecondaryColor = template.SecondaryColor;
        AccentColor = template.AccentColor;
        TextColor = template.TextColor;
        BackgroundColor = template.BackgroundColor;
        SelectedFontFamily = template.FontFamily;
        LogoBase64 = template.LogoBase64;
        LogoWidth = template.LogoWidth;
        HeaderText = template.HeaderText;
        FooterText = template.FooterText;
        PaymentInstructions = template.PaymentInstructions;
        ShowLogo = template.ShowLogo;
        ShowCompanyAddress = template.ShowCompanyAddress;
        ShowTaxBreakdown = template.ShowTaxBreakdown;
        ShowItemDescriptions = template.ShowItemDescriptions;
        ShowNotes = template.ShowNotes;
        ShowPaymentInstructions = template.ShowPaymentInstructions;
        ShowDueDateProminent = template.ShowDueDateProminent;
        HasLogo = !string.IsNullOrEmpty(template.LogoBase64);
    }

    private void ResetForm()
    {
        _editingTemplateId = string.Empty;
        TemplateName = string.Empty;
        SelectedBaseTemplate = InvoiceTemplateType.Professional;
        IsDefault = false;

        var defaults = InvoiceTemplateFactory.CreateProfessionalTemplate();
        PrimaryColor = defaults.PrimaryColor;
        SecondaryColor = defaults.SecondaryColor;
        AccentColor = defaults.AccentColor;
        TextColor = defaults.TextColor;
        BackgroundColor = defaults.BackgroundColor;
        SelectedFontFamily = defaults.FontFamily;
        LogoBase64 = null;
        LogoWidth = 150;
        HeaderText = defaults.HeaderText;
        FooterText = defaults.FooterText;
        PaymentInstructions = string.Empty;
        ShowLogo = true;
        ShowCompanyAddress = true;
        ShowTaxBreakdown = true;
        ShowItemDescriptions = true;
        ShowNotes = true;
        ShowPaymentInstructions = true;
        ShowDueDateProminent = true;
        HasLogo = false;
        ValidationMessage = string.Empty;
        HasValidationMessage = false;
    }

    /// <summary>
    /// Sets the logo from a file (called from view code-behind after file selection).
    /// </summary>
    public async Task SetLogoFromFileAsync(string filePath)
    {
        try
        {
            var bytes = await File.ReadAllBytesAsync(filePath);
            LogoBase64 = Convert.ToBase64String(bytes);
            HasLogo = true;
            UpdatePreview();
        }
        catch
        {
            // Handle error
        }
    }

    #endregion
}
