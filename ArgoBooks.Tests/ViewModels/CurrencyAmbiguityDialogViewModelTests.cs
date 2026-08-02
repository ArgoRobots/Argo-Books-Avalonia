using ArgoBooks.Core.Models.Common;
using ArgoBooks.Core.Services;
using ArgoBooks.ViewModels;
using Xunit;

namespace ArgoBooks.Tests.ViewModels;

/// <summary>
/// Tests for <see cref="CurrencyAmbiguityDialogViewModel"/>: the smart default selection and the
/// resolution map it produces.
/// </summary>
public class CurrencyAmbiguityDialogViewModelTests
{
    private static AmbiguousSymbolPrompt DollarPrompt() => new()
    {
        Symbol = "$",
        Candidates = CurrencyInfo.CandidatesForSymbol("$"), // [USD, CAD, AUD]
        RowCount = 5
    };

    [Fact]
    public void SmartDefault_CompanyCurrencySharesSymbol_IsPreselected()
    {
        var vm = new CurrencyAmbiguityDialogViewModel();
        vm.ShowAsync([DollarPrompt()], companyCurrency: "CAD");

        Assert.Equal("CAD", Assert.Single(vm.Symbols).SelectedCode);
    }

    [Fact]
    public void SmartDefault_CompanyCurrencyDoesNotShareSymbol_UsesTopPriority()
    {
        var vm = new CurrencyAmbiguityDialogViewModel();
        vm.ShowAsync([DollarPrompt()], companyCurrency: "EUR"); // EUR doesn't use "$"

        Assert.Equal("USD", Assert.Single(vm.Symbols).SelectedCode); // priority-ordered default
    }

    [Fact]
    public void Resolution_AfterConfirm_MapsSymbolToChosenCode()
    {
        var vm = new CurrencyAmbiguityDialogViewModel();
        vm.ShowAsync([DollarPrompt()], companyCurrency: "USD");

        // Change the selection to CAD, then confirm.
        var choice = vm.Symbols[0];
        choice.SelectedOption = System.Linq.Enumerable.First(choice.Options, o => o.Code == "CAD");
        vm.ConfirmCommand.Execute(null);

        Assert.Equal(new Dictionary<string, string> { ["$"] = "CAD" }, vm.Resolution);
        Assert.False(vm.IsOpen);
    }
}
