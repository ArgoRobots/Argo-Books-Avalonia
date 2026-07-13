using ArgoBooks.Core.Services.Sync;

namespace ArgoBooks.Mobile.ViewModels;

/// <summary>
/// Read-only detail for a single row (customer, invoice, expense, product, etc.). The phone
/// only ever shows Title/Subtitle/Amount: the snapshot is a lightweight read model, not the
/// full record, so there is nothing else to show. Editing happens on the desktop.
/// </summary>
public partial class ItemDetailViewModel : ViewModelBase
{
    public string Title { get; }
    public string Subtitle { get; }
    public string Amount { get; }

    public ItemDetailViewModel(RowDto row)
    {
        Title = row.Title;
        Subtitle = row.Subtitle;
        Amount = row.Amount;
    }
}
