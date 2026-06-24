using System.Collections.ObjectModel;
using ArgoBooks.Core.Models.BankMatching;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

public partial class PdfStatementReviewModalViewModel : ViewModelBase
{
    [ObservableProperty] private bool _isOpen;
    public ObservableCollection<PdfRowVm> Rows { get; } = [];

    private TaskCompletionSource<List<BankStatementLine>?>? _tcs;

    public Task<List<BankStatementLine>?> ReviewAsync(List<BankStatementLine> rows)
    {
        Rows.Clear();
        foreach (var r in rows) Rows.Add(new PdfRowVm(r));
        _tcs = new TaskCompletionSource<List<BankStatementLine>?>();
        IsOpen = true;
        return _tcs.Task;
    }

    [RelayCommand]
    private void DeleteRow(PdfRowVm? row) { if (row != null) Rows.Remove(row); }

    [RelayCommand]
    private void Confirm()
    {
        IsOpen = false;
        _tcs?.TrySetResult(Rows.Select(r => r.ToLine()).ToList());
    }

    [RelayCommand]
    private void Cancel()
    {
        IsOpen = false;
        _tcs?.TrySetResult(null);
    }
}

public partial class PdfRowVm(BankStatementLine line) : ObservableObject
{
    [ObservableProperty] private DateTimeOffset _date = line.Date == default ? DateTimeOffset.Now : new DateTimeOffset(line.Date);
    [ObservableProperty] private string _description = line.Description;
    [ObservableProperty] private decimal _amount = line.Amount;

    public BankStatementLine ToLine() => new()
    {
        Id = string.IsNullOrEmpty(line.Id) ? Guid.NewGuid().ToString("N") : line.Id,
        Date = Date.DateTime, Description = Description, Amount = Amount
    };
}
