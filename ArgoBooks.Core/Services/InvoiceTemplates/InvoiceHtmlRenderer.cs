using System.Text;
using System.Text.RegularExpressions;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Models;
using ArgoBooks.Core.Models.Invoices;
using ArgoBooks.Core.Models.Transactions;

namespace ArgoBooks.Core.Services.InvoiceTemplates;

/// <summary>
/// Renders invoice HTML from templates and data.
/// Uses a simple mustache-like template syntax.
/// </summary>
public partial class InvoiceHtmlRenderer
{
    /// <summary>
    /// Renders an invoice to HTML using the specified template.
    /// </summary>
    /// <param name="invoice">The invoice to render.</param>
    /// <param name="template">The template to use for rendering.</param>
    /// <param name="companyData">Company data for company info and customer lookup.</param>
    /// <param name="currencySymbol">Currency symbol to use (defaults to $).</param>
    /// <returns>The rendered HTML string.</returns>
    public string RenderInvoice(
        Invoice invoice,
        InvoiceTemplate template,
        CompanyData companyData,
        string currencySymbol = "$")
    {
        var customer = companyData.GetCustomer(invoice.CustomerId);
        var companySettings = companyData.Settings;

        // Get the base HTML template
        var html = InvoiceHtmlTemplates.GetTemplate(template.BaseTemplate);

        // Build the data context for template rendering
        var context = BuildContext(invoice, template, customer, companySettings, currencySymbol);

        // Process the template
        html = ProcessTemplate(html, context);

        return html;
    }

    /// <summary>
    /// Renders a preview invoice (for template designer) with sample data.
    /// </summary>
    public string RenderPreview(InvoiceTemplate template, CompanySettings companySettings)
    {
        // Create sample invoice data for preview
        var sampleInvoice = new Invoice
        {
            Id = "INV-2024-00001",
            InvoiceNumber = "INV-2024-00001",
            IssueDate = DateTime.Today,
            DueDate = DateTime.Today.AddDays(30),
            Subtotal = 1250.00m,
            TaxRate = 10m,
            TaxAmount = 125.00m,
            Total = 1375.00m,
            AmountPaid = 0m,
            Balance = 1375.00m,
            Notes = "Thank you for your business. Please remit payment within 30 days.",
            LineItems =
            [
                new() { Description = "Website Design Services", Quantity = 1, UnitPrice = 800.00m },
                new() { Description = "Logo Design", Quantity = 1, UnitPrice = 250.00m },
                new() { Description = "Business Card Design", Quantity = 2, UnitPrice = 100.00m }
            ]
        };

        // Create sample customer
        var sampleCustomer = new Models.Entities.Customer
        {
            Name = "Acme Corporation",
            Email = "billing@acme.com",
            Address = new Models.Common.Address
            {
                Street = "123 Business Ave, Suite 100",
                City = "New York",
                State = "NY",
                ZipCode = "10001",
                Country = "USA"
            }
        };

        var html = InvoiceHtmlTemplates.GetTemplate(template.BaseTemplate);
        var context = BuildContext(sampleInvoice, template, sampleCustomer, companySettings, "$");
        return ProcessTemplate(html, context);
    }

    /// <summary>
    /// Generates plain text version of the invoice for email fallback.
    /// </summary>
    public string RenderPlainText(
        Invoice invoice,
        InvoiceTemplate template,
        CompanyData companyData,
        string currencySymbol = "$")
    {
        var customer = companyData.GetCustomer(invoice.CustomerId);
        var companySettings = companyData.Settings;
        var sb = new StringBuilder();

        // Header
        sb.AppendLine($"{template.HeaderText}");
        sb.AppendLine(new string('=', 50));
        sb.AppendLine();

        // Company info
        sb.AppendLine(companySettings.Company.Name);
        if (!string.IsNullOrWhiteSpace(companySettings.Company.Address))
            sb.AppendLine(companySettings.Company.Address);
        if (!string.IsNullOrWhiteSpace(companySettings.Company.Email))
            sb.AppendLine(companySettings.Company.Email);
        sb.AppendLine();

        // Invoice details
        sb.AppendLine($"Invoice #: {invoice.InvoiceNumber}");
        sb.AppendLine($"Date: {invoice.IssueDate:MMMM d, yyyy}");
        sb.AppendLine($"Due Date: {invoice.DueDate:MMMM d, yyyy}");
        sb.AppendLine();

        // Bill to
        sb.AppendLine("BILL TO:");
        sb.AppendLine(customer?.Name ?? "Unknown Customer");
        var customerAddress = customer?.Address?.ToString();
        if (!string.IsNullOrWhiteSpace(customerAddress))
            sb.AppendLine(customerAddress);
        if (!string.IsNullOrWhiteSpace(customer?.Email))
            sb.AppendLine(customer.Email);
        sb.AppendLine();

        // Line items
        sb.AppendLine(new string('-', 50));
        sb.AppendLine("ITEMS:");
        sb.AppendLine(new string('-', 50));

        foreach (var item in invoice.LineItems)
        {
            var amount = item.Quantity * item.UnitPrice;
            sb.AppendLine($"{item.Description}");
            sb.AppendLine($"  {item.Quantity} x {currencySymbol}{item.UnitPrice:N2} = {currencySymbol}{amount:N2}");
        }

        sb.AppendLine(new string('-', 50));

        // Totals
        sb.AppendLine($"Subtotal: {currencySymbol}{invoice.Subtotal:N2}");
        if (invoice.TaxRate > 0)
            sb.AppendLine($"Tax ({invoice.TaxRate}%): {currencySymbol}{invoice.TaxAmount:N2}");
        sb.AppendLine($"TOTAL: {currencySymbol}{invoice.Total:N2}");

        if (invoice.AmountPaid > 0)
        {
            sb.AppendLine($"Amount Paid: -{currencySymbol}{invoice.AmountPaid:N2}");
            sb.AppendLine($"Balance Due: {currencySymbol}{invoice.Balance:N2}");
        }

        sb.AppendLine();

        // Notes
        if (template.ShowNotes && !string.IsNullOrWhiteSpace(invoice.Notes))
        {
            sb.AppendLine("NOTES:");
            sb.AppendLine(invoice.Notes);
            sb.AppendLine();
        }

        // Payment instructions
        if (template.ShowPaymentInstructions && !string.IsNullOrWhiteSpace(template.PaymentInstructions))
        {
            sb.AppendLine("PAYMENT INSTRUCTIONS:");
            sb.AppendLine(template.PaymentInstructions);
            sb.AppendLine();
        }

        // Footer
        sb.AppendLine(new string('=', 50));
        sb.AppendLine(template.FooterText);

        return sb.ToString();
    }

    private Dictionary<string, object?> BuildContext(
        Invoice invoice,
        InvoiceTemplate template,
        Models.Entities.Customer? customer,
        CompanySettings companySettings,
        string currencySymbol)
    {
        var isOverdue = invoice.DueDate.Date < DateTime.Today &&
                        invoice.Balance > 0;

        var context = new Dictionary<string, object?>
        {
            // Template styling
            ["FontFamily"] = template.FontFamily,
            ["PrimaryColor"] = template.PrimaryColor,
            ["SecondaryColor"] = template.SecondaryColor,
            ["AccentColor"] = template.AccentColor,
            ["TextColor"] = template.TextColor,
            ["BackgroundColor"] = template.BackgroundColor,

            // Template settings
            ["HeaderText"] = template.HeaderText,
            ["FooterText"] = template.FooterText,
            ["PaymentInstructions"] = template.PaymentInstructions,
            ["ShowLogo"] = template.ShowLogo && !string.IsNullOrEmpty(template.LogoBase64),
            ["ShowCompanyAddress"] = template.ShowCompanyAddress,
            ["ShowTaxBreakdown"] = template.ShowTaxBreakdown && invoice.TaxAmount > 0,
            ["ShowItemDescriptions"] = template.ShowItemDescriptions,
            ["ShowNotes"] = template.ShowNotes,
            ["ShowPaymentInstructions"] = template.ShowPaymentInstructions && !string.IsNullOrWhiteSpace(template.PaymentInstructions),
            ["ShowDueDateProminent"] = template.ShowDueDateProminent,

            // Logo
            ["LogoSrc"] = template.ShowLogo && !string.IsNullOrEmpty(template.LogoBase64)
                ? $"data:image/png;base64,{template.LogoBase64}"
                : "",
            ["LogoWidth"] = template.LogoWidth.ToString(),

            // Company info
            ["CompanyName"] = companySettings.Company.Name,
            ["CompanyAddress"] = FormatAddress(companySettings.Company.Address),
            ["CompanyEmail"] = companySettings.Company.Email,
            ["CompanyPhone"] = companySettings.Company.Phone,

            // Customer info
            ["CustomerName"] = customer?.Name ?? "Unknown Customer",
            ["CustomerAddress"] = FormatAddress(customer?.Address),
            ["CustomerEmail"] = customer?.Email,

            // Invoice details
            ["InvoiceNumber"] = invoice.InvoiceNumber,
            ["IssueDate"] = invoice.IssueDate.ToString("MMMM d, yyyy"),
            ["DueDate"] = invoice.DueDate.ToString("MMMM d, yyyy"),
            ["IsOverdue"] = isOverdue,

            // Financial
            ["Subtotal"] = $"{currencySymbol}{invoice.Subtotal:N2}",
            ["TaxRate"] = invoice.TaxRate.ToString("0.##"),
            ["TaxAmount"] = $"{currencySymbol}{invoice.TaxAmount:N2}",
            ["Total"] = $"{currencySymbol}{invoice.Total:N2}",
            ["AmountPaid"] = invoice.AmountPaid > 0 ? $"{currencySymbol}{invoice.AmountPaid:N2}" : null,
            ["Balance"] = $"{currencySymbol}{invoice.Balance:N2}",

            // Notes
            ["Notes"] = invoice.Notes,

            // Line items (as a list of dictionaries)
            ["LineItems"] = invoice.LineItems.Select(item => new Dictionary<string, object?>
            {
                ["Description"] = item.Description,
                ["ItemDescription"] = null, // Can be extended for product descriptions
                ["Quantity"] = item.Quantity.ToString("0.##"),
                ["UnitPrice"] = $"{currencySymbol}{item.UnitPrice:N2}",
                ["Amount"] = $"{currencySymbol}{(item.Quantity * item.UnitPrice):N2}"
            }).ToList()
        };

        return context;
    }

    private string ProcessTemplate(string template, Dictionary<string, object?> context)
    {
        var result = template;

        // Process sections ({{#Section}}...{{/Section}} and {{^Section}}...{{/Section}})
        result = ProcessSections(result, context);

        // Process loops ({{#ListName}}...{{/ListName}})
        result = ProcessLoops(result, context);

        // Process simple variables ({{VariableName}})
        result = ProcessVariables(result, context);

        return result;
    }

    private string ProcessSections(string template, Dictionary<string, object?> context)
    {
        // Process positive sections {{#Name}}content{{/Name}}
        var positiveSectionRegex = PositiveSectionRegex();
        template = positiveSectionRegex.Replace(template, match =>
        {
            var name = match.Groups[1].Value;
            var content = match.Groups[2].Value;

            if (context.TryGetValue(name, out var value))
            {
                // If it's a list, skip here (handled by ProcessLoops)
                if (value is IEnumerable<Dictionary<string, object?>>)
                    return match.Value;

                // If it's a truthy value, include the content
                if (IsTruthy(value))
                    return content;
            }

            return string.Empty;
        });

        // Process negative sections {{^Name}}content{{/Name}}
        var negativeSectionRegex = NegativeSectionRegex();
        template = negativeSectionRegex.Replace(template, match =>
        {
            var name = match.Groups[1].Value;
            var content = match.Groups[2].Value;

            if (context.TryGetValue(name, out var value))
            {
                // If it's a falsy value, include the content
                if (!IsTruthy(value))
                    return content;
            }
            else
            {
                // Variable not found, treat as falsy
                return content;
            }

            return string.Empty;
        });

        return template;
    }

    private string ProcessLoops(string template, Dictionary<string, object?> context)
    {
        var loopRegex = LoopRegex();

        return loopRegex.Replace(template, match =>
        {
            var name = match.Groups[1].Value;
            var itemTemplate = match.Groups[2].Value;

            if (context.TryGetValue(name, out var value) &&
                value is IEnumerable<Dictionary<string, object?>> items)
            {
                var sb = new StringBuilder();
                foreach (var item in items)
                {
                    // Merge item context with parent context
                    var itemContext = new Dictionary<string, object?>(context);
                    foreach (var kvp in item)
                    {
                        itemContext[kvp.Key] = kvp.Value;
                    }

                    var processedItem = ProcessSections(itemTemplate, itemContext);
                    processedItem = ProcessVariables(processedItem, itemContext);
                    sb.Append(processedItem);
                }
                return sb.ToString();
            }

            return string.Empty;
        });
    }

    private static string ProcessVariables(string template, Dictionary<string, object?> context)
    {
        var variableRegex = VariableRegex();

        return variableRegex.Replace(template, match =>
        {
            var name = match.Groups[1].Value;

            if (context.TryGetValue(name, out var value) && value != null)
            {
                return value.ToString() ?? string.Empty;
            }

            return string.Empty;
        });
    }

    private static bool IsTruthy(object? value)
    {
        return value switch
        {
            null => false,
            bool b => b,
            string s => !string.IsNullOrWhiteSpace(s),
            int i => i != 0,
            decimal d => d != 0,
            IEnumerable<object> list => list.Any(),
            _ => true
        };
    }

    private static string? FormatAddress(string? address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return null;

        // Replace newlines with <br> for HTML
        return address.Replace("\n", "<br>").Replace("\r", "");
    }

    private static string? FormatAddress(Models.Common.Address? address)
    {
        if (address == null)
            return null;

        var formatted = address.ToString();
        if (string.IsNullOrWhiteSpace(formatted))
            return null;

        // Replace commas with <br> for HTML display
        return formatted.Replace(", ", "<br>");
    }

    [GeneratedRegex(@"\{\{#(\w+)\}\}([\s\S]*?)\{\{/\1\}\}", RegexOptions.Compiled)]
    private static partial Regex PositiveSectionRegex();

    [GeneratedRegex(@"\{\{\^(\w+)\}\}([\s\S]*?)\{\{/\1\}\}", RegexOptions.Compiled)]
    private static partial Regex NegativeSectionRegex();

    [GeneratedRegex(@"\{\{#(\w+)\}\}([\s\S]*?)\{\{/\1\}\}", RegexOptions.Compiled)]
    private static partial Regex LoopRegex();

    [GeneratedRegex(@"\{\{(\w+)\}\}", RegexOptions.Compiled)]
    private static partial Regex VariableRegex();
}
