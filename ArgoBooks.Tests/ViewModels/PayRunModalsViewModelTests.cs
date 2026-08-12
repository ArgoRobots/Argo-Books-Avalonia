using ArgoBooks.Core.Models.Payroll;
using ArgoBooks.ViewModels;
using Xunit;

namespace ArgoBooks.Tests.ViewModels;

/// <summary>
/// Tests for the run payroll modal.
///
/// The empty cases are the ones worth pinning. An employer opening this for the first time, and
/// one who has archived their last employee, both saw an empty panel that read like a failure to
/// load rather than an answer, and the two are fixed differently.
/// </summary>
public class PayRunModalsViewModelTests : ModalViewModelTestBase
{
    private static Employee Person(string id = "EMP-001", string name = "Dana Smith", bool archived = false) => new()
    {
        Id = id,
        Name = name,
        Province = "AB",
        PayType = PayType.Salary,
        PayRate = 52000m,
        PayFrequency = PayFrequency.Biweekly,
        IsArchived = archived,
    };

    [Fact]
    public void WithNoEmployeesAtAll_ItSaysToAddOne()
    {
        var vm = new PayRunModalsViewModel();
        vm.OpenRunModal();

        Assert.Empty(vm.SelectableEmployees);
        Assert.True(vm.HasNobodyToPay);
        Assert.Contains("no employees yet", vm.NoEmployeesMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WithEveryEmployeeArchived_ItSaysToRestoreOne()
    {
        // Not "no employees": they have staff, and sending them off to add someone they already
        // have is the wrong instruction.
        Company.Employees.Add(Person(archived: true));
        Company.Employees.Add(Person("EMP-002", "Alex Jones", archived: true));

        var vm = new PayRunModalsViewModel();
        vm.OpenRunModal();

        Assert.Empty(vm.SelectableEmployees);
        Assert.True(vm.HasNobodyToPay);
        Assert.Contains("archived", vm.NoEmployeesMessage, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("no employees yet", vm.NoEmployeesMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WithSomebodyToPay_ThereIsNoMessage()
    {
        Company.Employees.Add(Person());

        var vm = new PayRunModalsViewModel();
        vm.OpenRunModal();

        Assert.Single(vm.SelectableEmployees);
        Assert.False(vm.HasNobodyToPay);
        Assert.Empty(vm.NoEmployeesMessage);
    }

    [Fact]
    public void ArchivedStaffAreExcluded_ButDoNotSuppressTheActiveOnes()
    {
        Company.Employees.Add(Person("EMP-001", "Dana Smith"));
        Company.Employees.Add(Person("EMP-002", "Alex Jones", archived: true));

        var vm = new PayRunModalsViewModel();
        vm.OpenRunModal();

        Assert.Single(vm.SelectableEmployees);
        Assert.Equal("EMP-001", vm.SelectableEmployees[0].Id);
        Assert.False(vm.HasNobodyToPay);
    }

    [Fact]
    public void ReopeningAfterAnEmployeeIsAdded_ClearsTheMessage()
    {
        var vm = new PayRunModalsViewModel();
        vm.OpenRunModal();
        Assert.True(vm.HasNobodyToPay);

        Company.Employees.Add(Person());
        vm.OpenRunModal();

        Assert.False(vm.HasNobodyToPay);
        Assert.Empty(vm.NoEmployeesMessage);
    }
}
