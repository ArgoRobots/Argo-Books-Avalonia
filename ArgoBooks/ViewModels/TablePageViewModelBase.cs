using System.Collections.ObjectModel;
using ArgoBooks.Controls.ColumnWidths;
using ArgoBooks.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// Extended base class for table page ViewModels with column visibility,
/// column menu, and responsive header support.
/// </summary>
public abstract partial class TablePageViewModelBase : SortablePageViewModelBase, IColumnMenuViewModel
{
    #region Responsive Header

    /// <summary>
    /// Helper for responsive header layout.
    /// </summary>
    public ResponsiveHeaderHelper ResponsiveHeader { get; } = new();

    #endregion

    #region Column Visibility

    /// <summary>
    /// Column visibility settings. Key is column name, value is whether visible.
    /// </summary>
    public Dictionary<string, bool> ColumnVisibility { get; } = new();

    /// <summary>
    /// Gets or sets whether the column menu is open.
    /// </summary>
    [ObservableProperty]
    private bool _isColumnMenuOpen;

    /// <summary>
    /// Gets or sets the X position of the column menu.
    /// </summary>
    [ObservableProperty]
    private double _columnMenuX;

    /// <summary>
    /// Gets or sets the Y position of the column menu.
    /// </summary>
    [ObservableProperty]
    private double _columnMenuY;

    /// <summary>
    /// Gets the column widths manager for this page.
    /// </summary>
    public abstract ITableColumnWidths ColumnWidths { get; }

    /// <summary>
    /// Toggles the column visibility menu.
    /// </summary>
    [RelayCommand]
    protected virtual void ToggleColumnMenu()
    {
        IsColumnMenuOpen = !IsColumnMenuOpen;
    }

    /// <summary>
    /// Closes the column visibility menu.
    /// </summary>
    [RelayCommand]
    protected virtual void CloseColumnMenu()
    {
        IsColumnMenuOpen = false;
    }

    /// <summary>
    /// Sets visibility for a column and updates the column widths manager.
    /// </summary>
    protected void SetColumnVisible(string columnName, bool isVisible)
    {
        ColumnVisibility[columnName] = isVisible;
        ColumnWidths.SetColumnVisibility(columnName, isVisible);
    }

    /// <summary>
    /// Gets visibility for a column.
    /// </summary>
    protected bool GetColumnVisible(string columnName)
    {
        return ColumnVisibility.TryGetValue(columnName, out var visible) ? visible : true;
    }

    #endregion

    #region Modal State

    /// <summary>
    /// Gets or sets whether the Add modal is open.
    /// </summary>
    [ObservableProperty]
    private bool _isAddModalOpen;

    /// <summary>
    /// Gets or sets whether the Edit modal is open.
    /// </summary>
    [ObservableProperty]
    private bool _isEditModalOpen;

    /// <summary>
    /// Gets or sets whether the Delete confirmation is open.
    /// </summary>
    [ObservableProperty]
    private bool _isDeleteConfirmOpen;

    /// <summary>
    /// Gets or sets whether the Filter modal is open.
    /// </summary>
    [ObservableProperty]
    private bool _isFilterModalOpen;

    #endregion

    #region Pagination

    /// <summary>
    /// Gets or sets the pagination text (e.g., "Showing 1-10 of 50").
    /// </summary>
    [ObservableProperty]
    private string _paginationText = "0 items";

    /// <summary>
    /// Updates the page numbers collection based on current page and total pages.
    /// Uses a sliding window of 5 pages.
    /// </summary>
    protected override void UpdatePageNumbers()
    {
        PageNumbers.Clear();
        var startPage = Math.Max(1, CurrentPage - 2);
        var endPage = Math.Min(TotalPages, startPage + 4);
        startPage = Math.Max(1, endPage - 4);

        for (var i = startPage; i <= endPage; i++)
        {
            PageNumbers.Add(i);
        }
    }

    /// <summary>
    /// Updates the pagination text based on total count and page info.
    /// </summary>
    protected void UpdatePaginationText(int totalCount, string itemName)
    {
        PaginationText = Utilities.PaginationTextHelper.FormatPaginationText(
            totalCount, CurrentPage, PageSize, TotalPages, itemName);
    }

    #endregion

    #region Filter and Search

    /// <summary>
    /// Gets or sets the search query.
    /// </summary>
    [ObservableProperty]
    private string? _searchQuery;

    /// <summary>
    /// Called when search query changes.
    /// </summary>
    partial void OnSearchQueryChanged(string? value)
    {
        CurrentPage = 1;
        OnSortOrPageChanged();
    }

    /// <summary>
    /// Clears the search query.
    /// </summary>
    [RelayCommand]
    protected virtual void ClearSearch()
    {
        SearchQuery = null;
    }

    #endregion

    #region Abstract Members

    /// <summary>
    /// Opens the Add modal. Override to implement.
    /// </summary>
    [RelayCommand]
    protected abstract void OpenAddModal();

    /// <summary>
    /// Opens the Filter modal. Override to implement.
    /// </summary>
    [RelayCommand]
    protected abstract void OpenFilterModal();

    #endregion
}

/// <summary>
/// Generic base class for table pages with a typed display item collection.
/// </summary>
/// <typeparam name="TDisplayItem">The type of display item shown in the table.</typeparam>
public abstract partial class TablePageViewModelBase<TDisplayItem> : TablePageViewModelBase
    where TDisplayItem : class
{
    /// <summary>
    /// Display items for the table.
    /// </summary>
    public ObservableCollection<TDisplayItem> Items { get; } = [];

    /// <summary>
    /// Applies search filtering to a list of items using Levenshtein scoring.
    /// </summary>
    /// <typeparam name="TSource">The source item type.</typeparam>
    /// <param name="items">The items to filter.</param>
    /// <param name="fieldSelector">Function to extract searchable fields from an item.</param>
    /// <returns>The filtered and scored items.</returns>
    protected List<TSource> ApplySearch<TSource>(
        List<TSource> items,
        Func<TSource, IEnumerable<string?>> fieldSelector)
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
            return items;

        return items
            .Select(item => new
            {
                Item = item,
                Score = fieldSelector(item)
                    .Where(f => !string.IsNullOrEmpty(f))
                    .Select(f => Utilities.LevenshteinDistance.ComputeSearchScore(SearchQuery, f!))
                    .DefaultIfEmpty(-1)
                    .Max()
            })
            .Where(x => x.Score >= 0)
            .OrderByDescending(x => x.Score)
            .Select(x => x.Item)
            .ToList();
    }

    /// <summary>
    /// Calculates pagination values after filtering and sorting.
    /// </summary>
    /// <param name="totalCount">Total number of items after filtering.</param>
    protected void CalculatePagination(int totalCount)
    {
        TotalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / PageSize));
        if (CurrentPage > TotalPages)
            CurrentPage = TotalPages;

        UpdatePageNumbers();
    }

    /// <summary>
    /// Gets a page of items from a list.
    /// </summary>
    protected IEnumerable<T> GetPagedItems<T>(IEnumerable<T> items)
    {
        return items
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize);
    }
}
