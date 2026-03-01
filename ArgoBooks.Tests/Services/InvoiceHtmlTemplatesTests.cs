using ArgoBooks.Core.Services.InvoiceTemplates;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// Tests for the InvoiceHtmlTemplates static class.
/// </summary>
public class InvoiceHtmlTemplatesTests
{
    #region Professional Template Tests

    [Fact]
    public void Professional_IsNotNullOrEmpty()
    {
        Assert.False(string.IsNullOrEmpty(InvoiceHtmlTemplates.Professional));
    }

    [Fact]
    public void Professional_ContainsHtmlDocType()
    {
        Assert.Contains("<!DOCTYPE html>", InvoiceHtmlTemplates.Professional);
    }

    [Fact]
    public void Professional_ContainsInvoiceNumberPlaceholder()
    {
        Assert.Contains("{{InvoiceNumber}}", InvoiceHtmlTemplates.Professional);
    }

    [Fact]
    public void Professional_ContainsCompanyNamePlaceholder()
    {
        Assert.Contains("{{CompanyName}}", InvoiceHtmlTemplates.Professional);
    }

    [Fact]
    public void Professional_ContainsCustomerNamePlaceholder()
    {
        Assert.Contains("{{CustomerName}}", InvoiceHtmlTemplates.Professional);
    }

    [Fact]
    public void Professional_ContainsPrimaryColorPlaceholder()
    {
        Assert.Contains("{{PrimaryColor}}", InvoiceHtmlTemplates.Professional);
    }

    [Fact]
    public void Professional_ContainsFontFamilyPlaceholder()
    {
        Assert.Contains("{{FontFamily}}", InvoiceHtmlTemplates.Professional);
    }

    [Fact]
    public void Professional_ContainsHeaderTextPlaceholder()
    {
        Assert.Contains("{{HeaderText}}", InvoiceHtmlTemplates.Professional);
    }

    [Fact]
    public void Professional_ContainsDatePlaceholders()
    {
        Assert.Contains("{{IssueDate}}", InvoiceHtmlTemplates.Professional);
        Assert.Contains("{{DueDate}}", InvoiceHtmlTemplates.Professional);
    }

    [Fact]
    public void Professional_IsValidHtml()
    {
        Assert.Contains("<html", InvoiceHtmlTemplates.Professional);
        Assert.Contains("</html>", InvoiceHtmlTemplates.Professional);
        Assert.Contains("<body", InvoiceHtmlTemplates.Professional);
        Assert.Contains("</body>", InvoiceHtmlTemplates.Professional);
    }

    #endregion
}
