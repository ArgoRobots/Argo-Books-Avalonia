using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Entities;
using ArgoBooks.Core.Models.Invoices;
using ArgoBooks.ViewModels;
using Xunit;

namespace ArgoBooks.Tests.ViewModels;

/// <summary>
/// Drives the real InvoiceModalsViewModel against an in-memory company. Guards the invoice-paper
/// editing flows where a per-invoice tweak must NOT leak into shared, persisted state: editing the
/// logo on the paper must not mutate the shared template, and a recurring schedule must inherit the
/// invoice's actual payment terms rather than a hardcoded default.
/// </summary>
public class InvoiceModalsViewModelTests : ModalViewModelTestBase
{
    [Fact]
    public void SetLogoFromPaper_AppliesTheLogoToEveryTemplate()
    {
        // The invoice logo is a single company-wide choice, so setting it on the paper (while one
        // template is selected) must land on every template, not just the selected one.
        Company.InvoiceTemplates.Add(new InvoiceTemplate { Id = "tmpl-a", Name = "AAA a", ShowLogo = false });
        Company.InvoiceTemplates.Add(new InvoiceTemplate { Id = "tmpl-b", Name = "AAB b", ShowLogo = false });
        var vm = new InvoiceModalsViewModel();
        vm.OpenCreateModal();
        vm.SelectedTemplate = vm.TemplateOptions.First(t => t.Id == "tmpl-a");

        vm.SetLogoFromPaper("LOGO-DATA");

        Assert.All(Company.InvoiceTemplates, t =>
        {
            Assert.Equal("LOGO-DATA", t.LogoBase64);
            Assert.True(t.ShowLogo);
        });
    }

    [Fact]
    public void DeleteLogoFromPaper_RemovesTheLogoFromEveryTemplate()
    {
        Company.InvoiceTemplates.Add(new InvoiceTemplate { Id = "tmpl-a", Name = "AAA a", LogoBase64 = "LOGO", ShowLogo = true });
        Company.InvoiceTemplates.Add(new InvoiceTemplate { Id = "tmpl-b", Name = "AAB b", LogoBase64 = "LOGO", ShowLogo = true });
        var vm = new InvoiceModalsViewModel();
        vm.OpenCreateModal();
        vm.SelectedTemplate = vm.TemplateOptions.First(t => t.Id == "tmpl-a");

        vm.DeleteLogoFromPaper();

        Assert.All(Company.InvoiceTemplates, t =>
        {
            Assert.Null(t.LogoBase64);
            Assert.False(t.ShowLogo);
        });
    }

    [Fact]
    public async Task CreateAndSendInvoice_LineItemWithoutAProduct_ShowsAProductError()
    {
        // Customer deliberately has no email so, before the fix, the send path stops at the email check
        // (never reaching the confirm dialog) with a non-product error; after the fix the product check
        // fires first. Either way this stays headless-safe.
        Company.Customers.Add(new Customer { Id = "CUST-1", Name = "Acme" });
        var vm = new InvoiceModalsViewModel();
        vm.OpenCreateModal();
        vm.SelectedCustomer = vm.CustomerOptions.First(c => c.Id == "CUST-1");
        // A line with amounts (so the total is positive) but no product selected.
        vm.LineItems[0].Quantity = 1;
        vm.LineItems[0].UnitPrice = 100;

        await vm.CreateAndSendInvoiceCommand.ExecuteAsync(null);

        Assert.True(vm.HasSendError);
        Assert.Contains("product", vm.SendErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveAsDraft_RecurringInvoice_DerivesPaymentTermsFromTheInvoiceDueDate()
    {
        Company.Customers.Add(new Customer { Id = "CUST-1", Name = "Acme" });
        var vm = new InvoiceModalsViewModel();
        vm.OpenCreateModal();
        vm.SelectedCustomer = vm.CustomerOptions.First(c => c.Id == "CUST-1");
        // Issue Jan 1, due Jan 16 -> a 15-day term, not the old hardcoded Net 30.
        vm.ModalIssueDate = new DateTimeOffset(new DateTime(2026, 1, 1), TimeSpan.Zero);
        vm.ModalDueDate = new DateTimeOffset(new DateTime(2026, 1, 16), TimeSpan.Zero);
        vm.IsRecurring = true;
        vm.RecurringFrequency = Frequency.Monthly;
        vm.RecurringStartDate = new DateTimeOffset(new DateTime(2026, 1, 1), TimeSpan.Zero);

        await vm.SaveAsDraftCommand.ExecuteAsync(null);

        var schedule = Assert.Single(Company.RecurringInvoices);
        Assert.Equal("Net 15", schedule.PaymentTerms);
    }
}
