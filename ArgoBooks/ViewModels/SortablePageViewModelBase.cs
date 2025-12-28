using System.Collections.ObjectModel;
using ArgoBooks.Core.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// Base class for page ViewModels that support sorting and pagination.
/// Provides common sorting, pagination properties and navigation commands.
/// </summary>
public abstract partial class SortablePageViewModelBase : ViewModelBase
{
    #region Sorting

    [ObservableProperty]
    private string _sortColumn = "Name";

    [ObservableProperty]
    private SortDirection _sortDirection = SortDirection.None;

    /// <summary>
    /// Sorts by the specified column. Toggles direction if same column, otherwise starts ascending.
    /// </summary>
    [RelayCommand]
    protected void SortBy(string column)
    {
        if (SortColumn == column)
        {
            SortDirection = SortDirection switch
            {
                SortDirection.None => SortDirection.Ascending,
                SortDirection.Ascending => SortDirection.Descending,
                SortDirection.Descending => SortDirection.None,
                _ => SortDirection.Ascending
            };
        }
        else
        {
            SortColumn = column;
            SortDirection = SortDirection.Ascending;
        }

        OnSortOrPageChanged();
    }

    /// <summary>
    /// Gets whether the specified column is sorted ascending.
    /// </summary>
    public bool IsSortedAscending(string column) => SortColumn == column && SortDirection == SortDirection.Ascending;

    /// <summary>
    /// Gets whether the specified column is sorted descending.
    /// </summary>
    public bool IsSortedDescending(string column) => SortColumn == column && SortDirection == SortDirection.Descending;

    #endregion

    #region Pagination

    [ObservableProperty]
    private int _currentPage = 1;

    [ObservableProperty]
    private int _totalPages = 1;

    [ObservableProperty]
    private int _pageSize = 10;

    /// <summary>
    /// Available page size options.
    /// </summary>
    public ObservableCollection<int> PageSizeOptions { get; } = [10, 25, 50, 100];

    /// <summary>
    /// Page numbers for pagination display.
    /// </summary>
    public ObservableCollection<int> PageNumbers { get; } = [];

    /// <summary>
    /// Gets whether we can navigate to the previous page.
    /// </summary>
    public bool CanGoToPreviousPage => CurrentPage > 1;

    /// <summary>
    /// Gets whether we can navigate to the next page.
    /// </summary>
    public bool CanGoToNextPage => CurrentPage < TotalPages;

    partial void OnCurrentPageChanged(int value)
    {
        NotifyPaginationChanged();
        OnSortOrPageChanged();
    }

    partial void OnPageSizeChanged(int value)
    {
        CurrentPage = 1;
        NotifyPaginationChanged();
        OnSortOrPageChanged();
    }

    /// <summary>
    /// Navigates to the previous page.
    /// </summary>
    [RelayCommand]
    protected void GoToPreviousPage()
    {
        if (CanGoToPreviousPage)
            CurrentPage--;
    }

    /// <summary>
    /// Navigates to the next page.
    /// </summary>
    [RelayCommand]
    protected void GoToNextPage()
    {
        if (CanGoToNextPage)
            CurrentPage++;
    }

    /// <summary>
    /// Navigates to a specific page.
    /// </summary>
    [RelayCommand]
    protected void GoToPage(int page)
    {
        if (page >= 1 && page <= TotalPages && page != CurrentPage)
            CurrentPage = page;
    }

    /// <summary>
    /// Notifies that pagination-related properties have changed.
    /// </summary>
    protected void NotifyPaginationChanged()
    {
        OnPropertyChanged(nameof(CanGoToPreviousPage));
        OnPropertyChanged(nameof(CanGoToNextPage));
    }

    /// <summary>
    /// Updates the PageNumbers collection based on TotalPages.
    /// Override for custom pagination display (e.g., sliding window).
    /// </summary>
    protected virtual void UpdatePageNumbers()
    {
        PageNumbers.Clear();
        for (int i = 1; i <= TotalPages; i++)
        {
            PageNumbers.Add(i);
        }
    }

    #endregion

    /// <summary>
    /// Called when sorting or pagination changes. Override to refresh data.
    /// </summary>
    protected abstract void OnSortOrPageChanged();
}
