using ArgoBooks.Core.Models.Payroll;
using ArgoBooks.ViewModels;
using Xunit;

namespace ArgoBooks.Tests.ViewModels;

/// <summary>
/// The employees table sorts when a header is clicked.
///
/// All six headers were wired to the sort command and drew their arrow, but the list was built
/// with a hard-coded order-by-name that never read the sort state. The arrow moved and the rows
/// did not, which is the one failure mode a screenshot of the header cannot show.
/// </summary>
public class EmployeesPageSortTests : ModalViewModelTestBase
{
    private void Seed()
    {
        Company.Employees.Add(new Employee
        {
            Id = "EMP-001",
            Name = "Chris Okafor",
            Province = "ON",
            PayType = PayType.Hourly,
            PayRate = 28m,
            PayFrequency = PayFrequency.Weekly,
        });

        Company.Employees.Add(new Employee
        {
            Id = "EMP-002",
            Name = "Alex Reyes",
            Province = "BC",
            PayType = PayType.Salary,
            PayRate = 82_000m,
            PayFrequency = PayFrequency.Monthly,
            IsArchived = true,
        });

        Company.Employees.Add(new Employee
        {
            Id = "EMP-003",
            Name = "Dana Smith",
            Province = "AB",
            PayType = PayType.Salary,
            PayRate = 62_400m,
            PayFrequency = PayFrequency.Biweekly,
        });
    }

    private List<string> Names(EmployeesPageViewModel vm) => [.. vm.Employees.Select(e => e.Name)];

    private EmployeesPageViewModel Loaded()
    {
        Seed();
        return new EmployeesPageViewModel();
    }

    [Fact]
    public void WithNoSortChosen_TheListIsByName()
    {
        EmployeesPageViewModel vm = Loaded();

        Assert.Equal(["Alex Reyes", "Chris Okafor", "Dana Smith"], Names(vm));
    }

    /// <summary>
    /// Every sortable header on the page. A column wired to a key the sort map does not carry
    /// falls back to the default order, which looks exactly like the bug this covers: the arrow
    /// flips and the rows sit still.
    ///
    /// Asserts the order changes rather than that it mirrors exactly. Pay type and status have
    /// only two values across three employees, so the tied rows keep their relative places and
    /// descending is legitimately not the reverse of ascending.
    /// </summary>
    [Theory]
    [InlineData("Employee")]
    [InlineData("Province")]
    [InlineData("PayType")]
    [InlineData("PayRate")]
    [InlineData("Frequency")]
    [InlineData("Status")]
    public void ClickingAHeader_ActuallyReordersTheRows(string column)
    {
        EmployeesPageViewModel vm = Loaded();
        List<string> unsorted = Names(vm);

        vm.SortByCommand.Execute(column);
        List<string> ascending = Names(vm);

        vm.SortByCommand.Execute(column);
        List<string> descending = Names(vm);

        Assert.Equal(3, ascending.Count);
        Assert.NotEqual(ascending, descending);
        Assert.True(ascending != unsorted || descending != unsorted,
            $"clicking {column} left the rows in their original order both ways");
    }

    /// <summary>The two columns whose values repeat, checked on the row that leads each way.</summary>
    [Theory]
    [InlineData("PayType", "Alex Reyes", "Chris Okafor")]
    [InlineData("Status", "Chris Okafor", "Alex Reyes")]
    public void ARepeatedValueColumn_LeadsWithEachEndInTurn(string column, string ascendingFirst, string descendingFirst)
    {
        EmployeesPageViewModel vm = Loaded();

        vm.SortByCommand.Execute(column);
        Assert.Equal(ascendingFirst, vm.Employees[0].Name);

        vm.SortByCommand.Execute(column);
        Assert.Equal(descendingFirst, vm.Employees[0].Name);
    }

    [Fact]
    public void SortingByProvince_OrdersByTheProvince()
    {
        EmployeesPageViewModel vm = Loaded();

        vm.SortByCommand.Execute("Province");

        Assert.Equal(["AB", "BC", "ON"], vm.Employees.Select(e => e.Province));
    }

    /// <summary>
    /// Pay rate sorts as money, not as text. An 82,000 sorting below a 28 is the classic
    /// symptom of a numeric column ordered by its formatted string.
    /// </summary>
    [Fact]
    public void SortingByPayRate_OrdersByTheNumber()
    {
        EmployeesPageViewModel vm = Loaded();

        vm.SortByCommand.Execute("PayRate");

        Assert.Equal(["Chris Okafor", "Dana Smith", "Alex Reyes"], Names(vm));
    }

    /// <summary>A third click clears the sort and puts the default order back.</summary>
    [Fact]
    public void AThirdClick_GoesBackToTheDefaultOrder()
    {
        EmployeesPageViewModel vm = Loaded();

        vm.SortByCommand.Execute("Province");
        vm.SortByCommand.Execute("Province");
        vm.SortByCommand.Execute("Province");

        Assert.Equal(["Alex Reyes", "Chris Okafor", "Dana Smith"], Names(vm));
    }
}
