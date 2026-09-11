using ArgoBooks.Core.Models.Entities;
using ArgoBooks.ViewModels;
using Xunit;

namespace ArgoBooks.Tests.ViewModels;

public class CustomerModalsViewModelTests : ModalViewModelTestBase
{
    private static async Task AddAsync(CustomerModalsViewModel vm, string first, string last, string id = "")
    {
        vm.ModalId = id;
        vm.ModalFirstName = first;
        vm.ModalLastName = last;
        await vm.SaveNewCustomerAsync();
    }

    // A typed ID doesn't move the counter, so the counter can reach an ID that's already taken.
    // Two customers sharing an ID show each other's invoices and can't be deleted.
    [Fact]
    public async Task SaveNew_BlankId_SkipsAnIdTypedEarlier()
    {
        Company.IdCounters.Customer = 3;
        var vm = new CustomerModalsViewModel();

        await AddAsync(vm, "Jane", "Doe", "CUS-005");
        await AddAsync(vm, "Ann", "Lee");
        await AddAsync(vm, "Bob", "Ray");

        Assert.Equal(["CUS-005", "CUS-004", "CUS-006"], Company.Customers.Select(c => c.Id));
    }

    // Bank import creates one-word customers such as "Amazon"; they must stay editable.
    [Fact]
    public async Task SaveEdited_OneWordName_SavesWithoutALastName()
    {
        Company.Customers.Add(new Customer { Id = "CUS-001", Name = "Amazon" });
        var vm = new CustomerModalsViewModel();

        vm.OpenEditModal(new CustomerDisplayItem { Id = "CUS-001", Name = "Amazon" });
        vm.ModalNotes = "Online orders";
        await vm.SaveEditedCustomerAsync();

        var customer = Assert.Single(Company.Customers);
        Assert.Equal("Amazon", customer.Name);
        Assert.Equal("Online orders", customer.Notes);
    }
}
