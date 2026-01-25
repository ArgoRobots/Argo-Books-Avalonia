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
            CreateMinimalTemplate()
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
            ShowCompanyAddress = false,
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
    /// Creates the Minimal template with default settings.
    /// </summary>
    public static InvoiceTemplate CreateMinimalTemplate()
    {
        return new InvoiceTemplate
        {
            Id = "default-minimal",
            Name = "Minimal",
            BaseTemplate = InvoiceTemplateType.Minimal,
            IsDefault = false,
            PrimaryColor = "#374151",
            SecondaryColor = "#e5e7eb",
            AccentColor = "#374151",
            TextColor = "#374151",
            BackgroundColor = "#ffffff",
            FontFamily = "system-ui, -apple-system, sans-serif",
            HeaderText = "Invoice",
            FooterText = "Thank you.",
            ShowLogo = true,
            ShowCompanyAddress = false,
            ShowTaxBreakdown = true,
            ShowItemDescriptions = false,
            ShowNotes = false,
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
            InvoiceTemplateType.Minimal => CreateMinimalTemplate(),
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
