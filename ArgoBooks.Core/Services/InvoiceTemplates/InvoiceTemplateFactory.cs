using ArgoBooks.Core.Models.Invoices;

namespace ArgoBooks.Core.Services.InvoiceTemplates;

/// <summary>
/// Factory for creating default invoice templates.
/// </summary>
public static class InvoiceTemplateFactory
{
    /// <summary>
    /// Creates the default set of invoice templates.
    /// </summary>
    public static List<InvoiceTemplate> CreateDefaultTemplates()
    {
        return
        [
            CreateProfessionalTemplate(),
            CreateModernTemplate(),
            CreateClassicTemplate(),
            CreateElegantTemplate(),
            CreateRibbonTemplate()
        ];
    }

    /// <summary>
    /// Creates the Professional template with default settings.
    /// </summary>
    public static InvoiceTemplate CreateProfessionalTemplate()
    {
        return new InvoiceTemplate
        {
            Id = "default-professional",
            Name = "Professional",
            BaseTemplate = InvoiceTemplateType.Professional,
            IsDefault = true,
            PrimaryColor = "#2563eb",
            SecondaryColor = "#e5e7eb",
            AccentColor = "#059669",
            TextColor = "#1f2937",
            BackgroundColor = "#ffffff",
            FontFamily = "Arial, Helvetica, sans-serif",
            HeaderText = "INVOICE",
            FooterText = "Thank you for your business!",
            ShowLogo = true,
            ShowCompanyAddress = true,
            ShowTaxBreakdown = true,
            ShowItemDescriptions = true,
            ShowNotes = true,
            ShowPaymentInstructions = true,
            ShowDueDateProminent = true
        };
    }

    /// <summary>
    /// Creates the Modern template with default settings.
    /// </summary>
    public static InvoiceTemplate CreateModernTemplate()
    {
        return new InvoiceTemplate
        {
            Id = "default-modern",
            Name = "Modern",
            BaseTemplate = InvoiceTemplateType.Modern,
            IsDefault = false,
            PrimaryColor = "#0f172a",
            SecondaryColor = "#f1f5f9",
            AccentColor = "#0ea5e9",
            TextColor = "#0f172a",
            BackgroundColor = "#ffffff",
            FontFamily = "'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif",
            HeaderText = "Invoice",
            FooterText = "Thank you for your business.",
            ShowLogo = true,
            ShowCompanyAddress = true,
            ShowTaxBreakdown = true,
            ShowItemDescriptions = true,
            ShowNotes = true,
            ShowPaymentInstructions = true,
            ShowDueDateProminent = false
        };
    }

    /// <summary>
    /// Creates the Classic template with default settings.
    /// </summary>
    public static InvoiceTemplate CreateClassicTemplate()
    {
        return new InvoiceTemplate
        {
            Id = "default-classic",
            Name = "Classic",
            BaseTemplate = InvoiceTemplateType.Classic,
            IsDefault = false,
            PrimaryColor = "#1e3a5f",
            SecondaryColor = "#d1d5db",
            AccentColor = "#047857",
            TextColor = "#111827",
            BackgroundColor = "#ffffff",
            FontFamily = "Georgia, 'Times New Roman', Times, serif",
            HeaderText = "INVOICE",
            FooterText = "Thank you for your business!",
            ShowLogo = true,
            ShowCompanyAddress = true,
            ShowTaxBreakdown = true,
            ShowItemDescriptions = true,
            ShowNotes = true,
            ShowPaymentInstructions = true,
            ShowDueDateProminent = false
        };
    }

    /// <summary>
    /// Creates the Elegant template with default settings.
    /// </summary>
    public static InvoiceTemplate CreateElegantTemplate()
    {
        return new InvoiceTemplate
        {
            Id = "default-elegant",
            Name = "Elegant",
            BaseTemplate = InvoiceTemplateType.Elegant,
            IsDefault = false,
            PrimaryColor = "#4f46e5",
            SecondaryColor = "#f3f4f6",
            AccentColor = "#0d9488",
            TextColor = "#1f2937",
            BackgroundColor = "#ffffff",
            FontFamily = "Georgia, 'Times New Roman', Times, serif",
            HeaderText = "Invoice",
            FooterText = "Thank you for your business!",
            ShowLogo = true,
            ShowCompanyAddress = true,
            ShowTaxBreakdown = true,
            ShowItemDescriptions = true,
            ShowNotes = true,
            ShowPaymentInstructions = true,
            ShowDueDateProminent = true
        };
    }

    /// <summary>
    /// Creates the Ribbon template with default settings.
    /// </summary>
    public static InvoiceTemplate CreateRibbonTemplate()
    {
        return new InvoiceTemplate
        {
            Id = "default-ribbon",
            Name = "Ribbon",
            BaseTemplate = InvoiceTemplateType.Ribbon,
            IsDefault = false,
            PrimaryColor = "#1a5276",
            SecondaryColor = "#ffee58",
            AccentColor = "#7cb342",
            TextColor = "#333333",
            BackgroundColor = "#ffffff",
            FontFamily = "'Open Sans', 'Segoe UI', Arial, sans-serif",
            HeaderText = "SALES RECEIPT",
            FooterText = "Thank you for your business!",
            ShowLogo = true,
            ShowCompanyAddress = true,
            ShowTaxBreakdown = true,
            ShowItemDescriptions = true,
            ShowNotes = true,
            ShowPaymentInstructions = true,
            ShowDueDateProminent = false
        };
    }

    /// <summary>
    /// Creates a custom template with the specified base type.
    /// </summary>
    public static InvoiceTemplate CreateCustomTemplate(string id, string name, InvoiceTemplateType baseType)
    {
        var baseTemplate = baseType switch
        {
            InvoiceTemplateType.Professional => CreateProfessionalTemplate(),
            InvoiceTemplateType.Modern => CreateModernTemplate(),
            InvoiceTemplateType.Classic => CreateClassicTemplate(),
            InvoiceTemplateType.Elegant => CreateElegantTemplate(),
            InvoiceTemplateType.Ribbon => CreateRibbonTemplate(),
            _ => CreateProfessionalTemplate()
        };

        baseTemplate.Id = id;
        baseTemplate.Name = name;
        baseTemplate.IsDefault = false;
        baseTemplate.CreatedAt = DateTime.UtcNow;
        baseTemplate.UpdatedAt = DateTime.UtcNow;

        return baseTemplate;
    }
}
