using ArgoBooks.Core.Data;
using ArgoBooks.Core.Models;
using ArgoBooks.Core.Models.Transactions;
using ArgoBooks.Core.Services.InvoiceTemplates;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// Tests for the InvoiceHtmlRenderer class.
/// </summary>
public class InvoiceHtmlRendererTests
{
    private readonly InvoiceHtmlRenderer _renderer = new();

    #region RenderPlainText Tests

    [Fact]
    public void RenderPlainText_WithInvoice_ReturnsNonEmptyString()
    {
        var invoice = new Invoice
        {
            Id = "INV-001",
            InvoiceNumber = "INV-001",
            CustomerId = "CUS-001",
            Total = 100.00m
        };
        var template = InvoiceTemplateFactory.CreateProfessionalTemplate();
        var companyData = new CompanyData();

        var result = _renderer.RenderPlainText(invoice, template, companyData);

        Assert.False(string.IsNullOrEmpty(result));
    }

    [Fact]
    public void RenderPlainText_ContainsInvoiceNumber()
    {
        var invoice = new Invoice
        {
            Id = "INV-001",
            InvoiceNumber = "INV-001",
            CustomerId = "CUS-001",
            Total = 50.00m
        };
        var template = InvoiceTemplateFactory.CreateProfessionalTemplate();
        var companyData = new CompanyData();

        var result = _renderer.RenderPlainText(invoice, template, companyData);

        Assert.Contains("INV-001", result);
    }

    #endregion

    #region RenderInvoice Tests

    [Fact]
    public void RenderInvoice_ReturnsHtml()
    {
        var invoice = new Invoice
        {
            Id = "INV-001",
            InvoiceNumber = "INV-001",
            CustomerId = "CUS-001",
            Total = 100.00m
        };
        var template = InvoiceTemplateFactory.CreateProfessionalTemplate();
        var companyData = new CompanyData();

        var result = _renderer.RenderInvoice(invoice, template, companyData);

        Assert.False(string.IsNullOrEmpty(result));
        Assert.Contains("<", result); // Should contain HTML tags
    }

    #endregion

    #region RenderPreview Tests

    [Fact]
    public void RenderPreview_ReturnsHtml()
    {
        var template = InvoiceTemplateFactory.CreateProfessionalTemplate();
        var companySettings = new CompanySettings();

        var result = _renderer.RenderPreview(template, companySettings, false);

        Assert.False(string.IsNullOrEmpty(result));
        Assert.Contains("<", result);
    }

    #endregion
}
