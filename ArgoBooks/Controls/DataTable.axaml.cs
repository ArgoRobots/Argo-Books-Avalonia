using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.Controls;

/// <summary>
/// Selection mode for data table rows.
/// </summary>
public enum DataTableSelectionMode
{
    None,
    Single,
    Multiple
}

/// <summary>
/// A data table control with sorting, pagination, search, and row selection.
/// </summary>
public partial class DataTable : UserControl
{
    private readonly List<object> _allItems = new();
    private readonly List<object> _filteredItems = new();
    private DataTableColumn? _currentSortColumn;

    #region Styled Properties

    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<DataTable, IEnumerable?>(nameof(ItemsSource));

    public static readonly StyledProperty<ObservableCollection<DataTableColumn>> ColumnsProperty =
        AvaloniaProperty.Register<DataTable, ObservableCollection<DataTableColumn>>(
            nameof(Columns), new ObservableCollection<DataTableColumn>());

    public static readonly StyledProperty<DataTemplate?> RowTemplateProperty =
        AvaloniaProperty.Register<DataTable, DataTemplate?>(nameof(RowTemplate));

    public static readonly StyledProperty<object?> SelectedItemProperty =
        AvaloniaProperty.Register<DataTable, object?>(nameof(SelectedItem), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<IList?> SelectedItemsProperty =
        AvaloniaProperty.Register<DataTable, IList?>(nameof(SelectedItems));

    public static readonly StyledProperty<DataTableSelectionMode> SelectionModeProperty =
        AvaloniaProperty.Register<DataTable, DataTableSelectionMode>(nameof(SelectionMode), DataTableSelectionMode.Single);

    public static readonly StyledProperty<string?> SearchTextProperty =
        AvaloniaProperty.Register<DataTable, string?>(nameof(SearchText), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<string> SearchPlaceholderProperty =
        AvaloniaProperty.Register<DataTable, string>(nameof(SearchPlaceholder), "Search...");

    public static readonly StyledProperty<int> PageSizeProperty =
        AvaloniaProperty.Register<DataTable, int>(nameof(PageSize), 10);

    public static readonly StyledProperty<int> CurrentPageProperty =
        AvaloniaProperty.Register<DataTable, int>(nameof(CurrentPage), 1);

    public static readonly StyledProperty<bool> ShowHeaderProperty =
        AvaloniaProperty.Register<DataTable, bool>(nameof(ShowHeader), true);

    public static readonly StyledProperty<bool> ShowSearchProperty =
        AvaloniaProperty.Register<DataTable, bool>(nameof(ShowSearch), true);

    public static readonly StyledProperty<bool> ShowPaginationProperty =
        AvaloniaProperty.Register<DataTable, bool>(nameof(ShowPagination), true);

    public static readonly StyledProperty<bool> ShowAlternatingRowsProperty =
        AvaloniaProperty.Register<DataTable, bool>(nameof(ShowAlternatingRows), true);

    public static readonly StyledProperty<string> EmptyMessageProperty =
        AvaloniaProperty.Register<DataTable, string>(nameof(EmptyMessage), "No data to display");

    public static readonly StyledProperty<object?> HeaderActionsProperty =
        AvaloniaProperty.Register<DataTable, object?>(nameof(HeaderActions));

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the items source.
    /// </summary>
    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>
    /// Gets or sets the column definitions.
    /// </summary>
    public ObservableCollection<DataTableColumn> Columns
    {
        get => GetValue(ColumnsProperty);
        set => SetValue(ColumnsProperty, value);
    }

    /// <summary>
    /// Gets or sets the row template.
    /// </summary>
    public DataTemplate? RowTemplate
    {
        get => GetValue(RowTemplateProperty);
        set => SetValue(RowTemplateProperty, value);
    }

    /// <summary>
    /// Gets or sets the selected item.
    /// </summary>
    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    /// <summary>
    /// Gets or sets the selected items (for multiple selection).
    /// </summary>
    public IList? SelectedItems
    {
        get => GetValue(SelectedItemsProperty);
        set => SetValue(SelectedItemsProperty, value);
    }

    /// <summary>
    /// Gets or sets the selection mode.
    /// </summary>
    public DataTableSelectionMode SelectionMode
    {
        get => GetValue(SelectionModeProperty);
        set => SetValue(SelectionModeProperty, value);
    }

    /// <summary>
    /// Gets or sets the search text.
    /// </summary>
    public string? SearchText
    {
        get => GetValue(SearchTextProperty);
        set => SetValue(SearchTextProperty, value);
    }

    /// <summary>
    /// Gets or sets the search placeholder text.
    /// </summary>
    public string SearchPlaceholder
    {
        get => GetValue(SearchPlaceholderProperty);
        set => SetValue(SearchPlaceholderProperty, value);
    }

    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    public int PageSize
    {
        get => GetValue(PageSizeProperty);
        set => SetValue(PageSizeProperty, value);
    }

    /// <summary>
    /// Gets or sets the current page (1-based).
    /// </summary>
    public int CurrentPage
    {
        get => GetValue(CurrentPageProperty);
        set => SetValue(CurrentPageProperty, value);
    }

    /// <summary>
    /// Gets or sets whether to show the header with search.
    /// </summary>
    public bool ShowHeader
    {
        get => GetValue(ShowHeaderProperty);
        set => SetValue(ShowHeaderProperty, value);
    }

    /// <summary>
    /// Gets or sets whether to show the search box.
    /// </summary>
    public bool ShowSearch
    {
        get => GetValue(ShowSearchProperty);
        set => SetValue(ShowSearchProperty, value);
    }

    /// <summary>
    /// Gets or sets whether to show pagination.
    /// </summary>
    public bool ShowPagination
    {
        get => GetValue(ShowPaginationProperty);
        set => SetValue(ShowPaginationProperty, value);
    }

    /// <summary>
    /// Gets or sets whether to show alternating row colors.
    /// </summary>
    public bool ShowAlternatingRows
    {
        get => GetValue(ShowAlternatingRowsProperty);
        set => SetValue(ShowAlternatingRowsProperty, value);
    }

    /// <summary>
    /// Gets or sets the empty state message.
    /// </summary>
    public string EmptyMessage
    {
        get => GetValue(EmptyMessageProperty);
        set => SetValue(EmptyMessageProperty, value);
    }

    /// <summary>
    /// Gets or sets the header actions content.
    /// </summary>
    public object? HeaderActions
    {
        get => GetValue(HeaderActionsProperty);
        set => SetValue(HeaderActionsProperty, value);
    }

    /// <summary>
    /// Gets the items currently displayed on the current page.
    /// </summary>
    public ObservableCollection<object> DisplayedItems { get; } = new();

    /// <summary>
    /// Gets the available page size options.
    /// </summary>
    public int[] PageSizeOptions { get; } = { 10, 25, 50, 100 };

    /// <summary>
    /// Gets the total number of items (after filtering).
    /// </summary>
    public int TotalItemCount => _filteredItems.Count;

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)_filteredItems.Count / PageSize));

    /// <summary>
    /// Gets the start index of displayed items (1-based).
    /// </summary>
    public int DisplayedItemsStart => _filteredItems.Count == 0 ? 0 : (CurrentPage - 1) * PageSize + 1;

    /// <summary>
    /// Gets the end index of displayed items.
    /// </summary>
    public int DisplayedItemsEnd => Math.Min(CurrentPage * PageSize, _filteredItems.Count);

    /// <summary>
    /// Gets whether there are items to display.
    /// </summary>
    public bool HasItems => DisplayedItems.Count > 0;

    /// <summary>
    /// Gets whether navigation to previous page is possible.
    /// </summary>
    public bool CanGoToPreviousPage => CurrentPage > 1;

    /// <summary>
    /// Gets whether navigation to next page is possible.
    /// </summary>
    public bool CanGoToNextPage => CurrentPage < TotalPages;

    #endregion

    #region Commands

    public ICommand SortCommand { get; }
    public ICommand ClearSearchCommand { get; }
    public ICommand FirstPageCommand { get; }
    public ICommand PreviousPageCommand { get; }
    public ICommand NextPageCommand { get; }
    public ICommand LastPageCommand { get; }
    public ICommand SelectRowCommand { get; }

    #endregion

    #region Events

    /// <summary>
    /// Event raised when selection changes.
    /// </summary>
    public event EventHandler<object?>? SelectionChanged;

    /// <summary>
    /// Event raised when a row is double-clicked.
    /// </summary>
    public event EventHandler<object?>? RowDoubleClicked;

    #endregion

    public DataTable()
    {
        SortCommand = new RelayCommand<DataTableColumn>(SortByColumn);
        ClearSearchCommand = new RelayCommand(ClearSearch);
        FirstPageCommand = new RelayCommand(GoToFirstPage, () => CanGoToPreviousPage);
        PreviousPageCommand = new RelayCommand(GoToPreviousPage, () => CanGoToPreviousPage);
        NextPageCommand = new RelayCommand(GoToNextPage, () => CanGoToNextPage);
        LastPageCommand = new RelayCommand(GoToLastPage, () => CanGoToNextPage);
        SelectRowCommand = new RelayCommand<object>(SelectRow);

        InitializeComponent();

        // Subscribe to property changes
        this.GetObservable(ItemsSourceProperty).Subscribe(_ => OnItemsSourceChanged());
        this.GetObservable(SearchTextProperty).Subscribe(_ => OnSearchTextChanged());
        this.GetObservable(PageSizeProperty).Subscribe(_ => OnPagingChanged());
        this.GetObservable(CurrentPageProperty).Subscribe(_ => UpdateDisplayedItems());
    }

    private void OnItemsSourceChanged()
    {
        _allItems.Clear();

        if (ItemsSource != null)
        {
            foreach (var item in ItemsSource)
            {
                if (item != null)
                    _allItems.Add(item);
            }
        }

        // Subscribe to collection changes if observable
        if (ItemsSource is INotifyCollectionChanged observable)
        {
            observable.CollectionChanged += OnSourceCollectionChanged;
        }

        ApplyFilterAndSort();
    }

    private void OnSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _allItems.Clear();

        if (ItemsSource != null)
        {
            foreach (var item in ItemsSource)
            {
                if (item != null)
                    _allItems.Add(item);
            }
        }

        ApplyFilterAndSort();
    }

    private void OnSearchTextChanged()
    {
        CurrentPage = 1;
        ApplyFilterAndSort();
    }

    private void OnPagingChanged()
    {
        if (CurrentPage > TotalPages)
            CurrentPage = TotalPages;

        UpdateDisplayedItems();
    }

    private void ApplyFilterAndSort()
    {
        _filteredItems.Clear();

        // Apply search filter
        var searchText = SearchText?.ToLowerInvariant() ?? string.Empty;

        foreach (var item in _allItems)
        {
            if (string.IsNullOrEmpty(searchText) || MatchesSearch(item, searchText))
            {
                _filteredItems.Add(item);
            }
        }

        // Apply sort
        if (_currentSortColumn != null && _currentSortColumn.SortDirection != SortDirection.None)
        {
            var comparer = _currentSortColumn.SortDirection == SortDirection.Ascending
                ? Comparer<object>.Create((x, y) => _currentSortColumn.Compare(x, y))
                : Comparer<object>.Create((x, y) => _currentSortColumn.Compare(y, x));

            _filteredItems.Sort(comparer);
        }

        UpdateDisplayedItems();
        NotifyPagingPropertiesChanged();
    }

    private bool MatchesSearch(object item, string searchText)
    {
        // Search across all columns
        foreach (var column in Columns)
        {
            var value = column.GetValue(item)?.ToString()?.ToLowerInvariant();
            if (value?.Contains(searchText) == true)
                return true;
        }

        return false;
    }

    private void UpdateDisplayedItems()
    {
        DisplayedItems.Clear();

        var skip = (CurrentPage - 1) * PageSize;
        var items = _filteredItems.Skip(skip).Take(PageSize);

        foreach (var item in items)
        {
            DisplayedItems.Add(item);
        }

        RaisePropertyChanged(nameof(HasItems));
        RaisePropertyChanged(nameof(DisplayedItemsStart));
        RaisePropertyChanged(nameof(DisplayedItemsEnd));
    }

    private void SortByColumn(DataTableColumn? column)
    {
        if (column == null || !column.IsSortable)
            return;

        // Clear sort on other columns
        foreach (var col in Columns)
        {
            if (col != column)
                col.ClearSort();
        }

        column.ToggleSort();
        _currentSortColumn = column.SortDirection != SortDirection.None ? column : null;

        ApplyFilterAndSort();
    }

    private void ClearSearch()
    {
        SearchText = string.Empty;
    }

    private void GoToFirstPage()
    {
        CurrentPage = 1;
    }

    private void GoToPreviousPage()
    {
        if (CurrentPage > 1)
            CurrentPage--;
    }

    private void GoToNextPage()
    {
        if (CurrentPage < TotalPages)
            CurrentPage++;
    }

    private void GoToLastPage()
    {
        CurrentPage = TotalPages;
    }

    private void SelectRow(object? item)
    {
        if (SelectionMode == DataTableSelectionMode.None || item == null)
            return;

        if (SelectionMode == DataTableSelectionMode.Single)
        {
            SelectedItem = item;
            SelectionChanged?.Invoke(this, item);
        }
        else if (SelectionMode == DataTableSelectionMode.Multiple)
        {
            SelectedItems ??= new ObservableCollection<object>();

            if (SelectedItems.Contains(item))
                SelectedItems.Remove(item);
            else
                SelectedItems.Add(item);

            SelectionChanged?.Invoke(this, SelectedItems);
        }
    }

    private void NotifyPagingPropertiesChanged()
    {
        RaisePropertyChanged(nameof(TotalItemCount));
        RaisePropertyChanged(nameof(TotalPages));
        RaisePropertyChanged(nameof(CanGoToPreviousPage));
        RaisePropertyChanged(nameof(CanGoToNextPage));
    }
}
