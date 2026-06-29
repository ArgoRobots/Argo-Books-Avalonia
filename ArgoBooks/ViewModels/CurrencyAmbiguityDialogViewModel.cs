using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ArgoBooks.Core.Models.Common;
using ArgoBooks.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>Result of the currency-ambiguity resolution dialog.</summary>
public enum CurrencyAmbiguityDialogResult
{
    Cancel,
    Confirm
}

/// <summary>One selectable currency in a symbol's dropdown (e.g. "USD - US Dollar ($)").</summary>
public sealed class CurrencyOptionItem
{
    public required string Code { get; init; }
    public required string Display { get; init; }
    public override string ToString() => Display;
}

/// <summary>
/// One ambiguous symbol the user must resolve (e.g. "$" could be USD/CAD/AUD), with the candidate
/// currencies and the smart default pre-selected.
/// </summary>
public partial class SymbolChoiceViewModel : ObservableObject
{
    public string Symbol { get; }

    /// <summary>e.g. "Amounts shown with \"$\" (appears in 12 rows)".</summary>
    public string Header { get; }

    public ObservableCollection<CurrencyOptionItem> Options { get; } = [];

    [ObservableProperty]
    private CurrencyOptionItem? _selectedOption;

    public string? SelectedCode => SelectedOption?.Code;

    public SymbolChoiceViewModel(AmbiguousSymbolPrompt prompt, string companyCurrency)
    {
        Symbol = prompt.Symbol;
        var rowText = prompt.RowCount == 1 ? "1 row" : $"{prompt.RowCount} rows";
        Header = $"Amounts shown with \"{prompt.Symbol}\" (appears in {rowText})";

        foreach (var code in prompt.Candidates)
            Options.Add(new CurrencyOptionItem { Code = code, Display = CurrencyInfo.GetByCode(code).DisplayString });

        // Smart default: the company currency if it shares this symbol, otherwise the first
        // (priority-ordered) candidate.
        var defaultCode = prompt.Candidates.FirstOrDefault(c =>
                              string.Equals(c, companyCurrency, StringComparison.OrdinalIgnoreCase))
                          ?? prompt.Candidates.FirstOrDefault();
        SelectedOption = Options.FirstOrDefault(o => o.Code == defaultCode) ?? Options.FirstOrDefault();
    }
}

/// <summary>
/// Modal that asks the user to resolve each ambiguous currency symbol found in an import's amount
/// cells. Only shown when at least one genuinely ambiguous symbol is present. The chosen codes are
/// applied to every row using that symbol.
/// </summary>
public partial class CurrencyAmbiguityDialogViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isOpen;

    public ObservableCollection<SymbolChoiceViewModel> Symbols { get; } = [];

    private TaskCompletionSource<CurrencyAmbiguityDialogResult>? _completionSource;

    /// <summary>Shows the dialog for the given ambiguous symbols and returns the user's choice.</summary>
    public Task<CurrencyAmbiguityDialogResult> ShowAsync(
        IReadOnlyList<AmbiguousSymbolPrompt> ambiguities, string companyCurrency)
    {
        Symbols.Clear();
        foreach (var a in ambiguities)
            Symbols.Add(new SymbolChoiceViewModel(a, companyCurrency));

        IsOpen = true;
        _completionSource = new TaskCompletionSource<CurrencyAmbiguityDialogResult>();
        return _completionSource.Task;
    }

    /// <summary>The user's resolution: symbol -> chosen ISO code.</summary>
    public IReadOnlyDictionary<string, string> Resolution =>
        Symbols.Where(s => s.SelectedCode != null)
               .ToDictionary(s => s.Symbol, s => s.SelectedCode!, StringComparer.Ordinal);

    [RelayCommand]
    private void Confirm()
    {
        IsOpen = false;
        _completionSource?.TrySetResult(CurrencyAmbiguityDialogResult.Confirm);
    }

    [RelayCommand]
    private void Cancel()
    {
        IsOpen = false;
        _completionSource?.TrySetResult(CurrencyAmbiguityDialogResult.Cancel);
    }
}
