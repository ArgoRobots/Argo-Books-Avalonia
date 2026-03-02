using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace ArgoBooks.Helpers;

/// <summary>
/// An ObservableCollection that supports bulk operations with a single notification,
/// avoiding per-item UI updates that cause slow table rendering.
/// </summary>
public class BatchObservableCollection<T> : ObservableCollection<T>
{
    /// <summary>
    /// Replaces all items in the collection with the specified items,
    /// raising a single Reset notification instead of per-item notifications.
    /// </summary>
    public void ReplaceAll(IEnumerable<T> items)
    {
        Items.Clear();
        foreach (var item in items)
            Items.Add(item);

        OnPropertyChanged(new PropertyChangedEventArgs("Count"));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }
}
