using ArgoBooks.ViewModels;
using Xunit;

namespace ArgoBooks.Tests.ViewModels;

/// <summary>
/// Tests for the employee form's validation.
///
/// Only the validation is exercised. Saving needs an open company, and the form deliberately
/// sets its error messages and returns before it touches company data, so the rules can be
/// checked on their own.
/// </summary>
public class PayrollModalsViewModelTests
{
    private static PayrollModalsViewModel FormWith(DateTimeOffset? start, DateTimeOffset? end)
    {
        var vm = new PayrollModalsViewModel
        {
            Name = "Test Person",
            PayRate = "50000",
            StartDate = start,
            EndDate = end,
        };

        vm.SaveEmployeeCommand.Execute(null);
        return vm;
    }

    [Fact]
    public void AnEndDateBeforeTheStartDate_IsRejected()
    {
        // It would put pay periods outside the employment and make a record of employment
        // nonsense.
        PayrollModalsViewModel vm = FormWith(
            new DateTimeOffset(new DateTime(2026, 6, 1)),
            new DateTimeOffset(new DateTime(2026, 5, 1)));

        Assert.NotEmpty(vm.EndDateError);
        Assert.True(vm.IsEmployeeModalOpen == false || vm.EndDateError.Length > 0);
    }

    [Fact]
    public void AnEndDateAfterTheStartDate_IsAccepted()
    {
        PayrollModalsViewModel vm = FormWith(
            new DateTimeOffset(new DateTime(2026, 1, 5)),
            new DateTimeOffset(new DateTime(2026, 9, 30)));

        Assert.Empty(vm.EndDateError);
    }

    [Fact]
    public void AnEndDateOnTheStartDate_IsAccepted()
    {
        // Someone who worked a single day still has to be payable.
        var day = new DateTimeOffset(new DateTime(2026, 3, 2));
        PayrollModalsViewModel vm = FormWith(day, day);

        Assert.Empty(vm.EndDateError);
    }

    [Fact]
    public void AnEmployeeWhoHasNotLeft_NeedsNoEndDate()
    {
        PayrollModalsViewModel vm = FormWith(new DateTimeOffset(new DateTime(2026, 1, 5)), null);

        Assert.Empty(vm.EndDateError);
    }

    [Fact]
    public void AnEndDateWithNoStartDate_IsAccepted()
    {
        // Start date is optional, so an end date on its own cannot be contradicted by anything
        // and must not be rejected.
        PayrollModalsViewModel vm = FormWith(null, new DateTimeOffset(new DateTime(2026, 9, 30)));

        Assert.Empty(vm.EndDateError);
    }

    [Fact]
    public void OpeningTheAddForm_ClearsAPreviousEmployeesDates()
    {
        // The form is shell-hosted and reused, so a leaver's end date must not follow the next
        // person who gets added.
        var vm = new PayrollModalsViewModel
        {
            StartDate = new DateTimeOffset(new DateTime(2026, 1, 5)),
            EndDate = new DateTimeOffset(new DateTime(2026, 9, 30)),
        };

        vm.OpenAddEmployeeModal();

        Assert.Null(vm.StartDate);
        Assert.Null(vm.EndDate);
        Assert.Empty(vm.EndDateError);
    }

    [Fact]
    public void AShortProvinceList_ExplainsItself()
    {
        // Only the provinces with a loaded rate table are offered. Without a reason on screen
        // an employer whose staff work elsewhere just sees a dropdown that is missing.
        var vm = new PayrollModalsViewModel();

        if (vm.SupportedProvinces.Count < 13)
        {
            Assert.NotEmpty(vm.ProvinceSupportNote);
            Assert.Contains(vm.SupportedProvinces[0], vm.ProvinceSupportNote);
        }
        else
        {
            Assert.Empty(vm.ProvinceSupportNote);
        }
    }

    [Fact]
    public void OnlyProvincesThatCanBeCalculated_AreOffered()
    {
        // An employee that no pay run could ever include must not be creatable.
        var vm = new PayrollModalsViewModel();

        Assert.NotEmpty(vm.SupportedProvinces);
        Assert.Contains(vm.Province, vm.SupportedProvinces);
    }
}
