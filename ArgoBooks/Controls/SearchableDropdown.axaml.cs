using System.Collections;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.Controls;

/// <summary>
/// A searchable dropdown control with filtering, keyboard navigation, and "Add new" support.
/// </summary>
public partial class SearchableDropdown : UserControl
{
    private TextBox? _searchTextBox;
    private int _highlightedIndex = -1;

    #region Styled Properties

    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<SearchableDropdown, IEnumerable?>(nameof(ItemsSource));

    public static readonly StyledProperty<object?> SelectedItemProperty =
        AvaloniaProperty.Register<SearchableDropdown, object?>(nameof(SelectedItem), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<string?> SearchTextProperty =
        AvaloniaProperty.Register<SearchableDropdown, string?>(nameof(SearchText), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<string?> DisplayMemberPathProperty =
        AvaloniaProperty.Register<SearchableDropdown, string?>(nameof(DisplayMemberPath));

    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<SearchableDropdown, string?>(nameof(Label));

    public static readonly StyledProperty<string?> PlaceholderProperty =
        AvaloniaProperty.Register<SearchableDropdown, string?>(nameof(Placeholder), "Search...");

    public static readonly StyledProperty<string?> HelperTextProperty =
        AvaloniaProperty.Register<SearchableDropdown, string?>(nameof(HelperText));

    public static readonly StyledProperty<string?> ErrorMessageProperty =
        AvaloniaProperty.Register<SearchableDropdown, string?>(nameof(ErrorMessage));

    public static readonly StyledProperty<bool> HasErrorProperty =
        AvaloniaProperty.Register<SearchableDropdown, bool>(nameof(HasError));

    public static readonly StyledProperty<bool> IsRequiredProperty =
        AvaloniaProperty.Register<SearchableDropdown, bool>(nameof(IsRequired));

    public static readonly StyledProperty<bool> IsDropdownOpenProperty =
        AvaloniaProperty.Register<SearchableDropdown, bool>(nameof(IsDropdownOpen));

    public static readonly StyledProperty<bool> ShowAddNewProperty =
        AvaloniaProperty.Register<SearchableDropdown, bool>(nameof(ShowAddNew));

    public static readonly StyledProperty<string> AddNewTextProperty =
        AvaloniaProperty.Register<SearchableDropdown, string>(nameof(AddNewText), "Add new...");

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
    /// Gets or sets the selected item.
    /// </summary>
    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    /// <summary>
    /// Gets or sets the search/filter text.
    /// </summary>
    public string? SearchText
    {
        get => GetValue(SearchTextProperty);
        set => SetValue(SearchTextProperty, value);
    }

    /// <summary>
    /// Gets or sets the property path to display.
    /// </summary>
    public string? DisplayMemberPath
    {
        get => GetValue(DisplayMemberPathProperty);
        set => SetValue(DisplayMemberPathProperty, value);
    }

    /// <summary>
    /// Gets or sets the label text.
    /// </summary>
    public string? Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    /// <summary>
    /// Gets or sets the placeholder text.
    /// </summary>
    public string? Placeholder
    {
        get => GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    /// <summary>
    /// Gets or sets the helper text.
    /// </summary>
    public string? HelperText
    {
        get => GetValue(HelperTextProperty);
        set => SetValue(HelperTextProperty, value);
    }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string? ErrorMessage
    {
        get => GetValue(ErrorMessageProperty);
        set => SetValue(ErrorMessageProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the control has a validation error.
    /// </summary>
    public bool HasError
    {
        get => GetValue(HasErrorProperty);
        set => SetValue(HasErrorProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the field is required.
    /// </summary>
    public bool IsRequired
    {
        get => GetValue(IsRequiredProperty);
        set => SetValue(IsRequiredProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the dropdown is open.
    /// </summary>
    public bool IsDropdownOpen
    {
        get => GetValue(IsDropdownOpenProperty);
        set => SetValue(IsDropdownOpenProperty, value);
    }

    /// <summary>
    /// Gets or sets whether to show the "Add new" button.
    /// </summary>
    public bool ShowAddNew
    {
        get => GetValue(ShowAddNewProperty);
        set => SetValue(ShowAddNewProperty, value);
    }

    /// <summary>
    /// Gets or sets the "Add new" button text.
    /// </summary>
    public string AddNewText
    {
        get => GetValue(AddNewTextProperty);
        set => SetValue(AddNewTextProperty, value);
    }

    /// <summary>
    /// Gets the filtered items based on search text.
    /// </summary>
    public ObservableCollection<object> FilteredItems { get; } = new();

    /// <summary>
    /// Gets whether there are filtered items to display.
    /// </summary>
    public bool HasFilteredItems => FilteredItems.Count > 0;

    #endregion

    #region Commands

    /// <summary>
    /// Command to toggle the dropdown.
    /// </summary>
    public ICommand ToggleDropdownCommand { get; }

    /// <summary>
    /// Command to select an item.
    /// </summary>
    public ICommand SelectItemCommand { get; }

    /// <summary>
    /// Command executed when "Add new" is clicked.
    /// </summary>
    public ICommand? AddNewCommand
    {
        get => _addNewCommand;
        set => _addNewCommand = value;
    }
    private ICommand? _addNewCommand;

    #endregion

    #region Events

    /// <summary>
    /// Event raised when the selected item changes.
    /// </summary>
    public event EventHandler<object?>? SelectionChanged;

    /// <summary>
    /// Event raised when "Add new" is clicked.
    /// </summary>
    public event EventHandler? AddNewRequested;

    #endregion

    public SearchableDropdown()
    {
        ToggleDropdownCommand = new RelayCommand(ToggleDropdown);
        SelectItemCommand = new RelayCommand<object>(SelectItem);

        InitializeComponent();

        // Subscribe to property changes
        this.GetObservable(ItemsSourceProperty).Subscribe(_ => UpdateFilteredItems());
        this.GetObservable(SearchTextProperty).Subscribe(_ => OnSearchTextChanged());
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _searchTextBox = this.FindControl<TextBox>("SearchTextBox");
        if (_searchTextBox != null)
        {
            _searchTextBox.GotFocus += OnSearchTextBoxGotFocus;
            _searchTextBox.KeyDown += OnSearchTextBoxKeyDown;
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        HandleKeyDown(e);
    }

    private void OnSearchTextBoxGotFocus(object? sender, GotFocusEventArgs e)
    {
        IsDropdownOpen = true;
    }

    private void OnSearchTextBoxKeyDown(object? sender, KeyEventArgs e)
    {
        HandleKeyDown(e);
    }

    private void HandleKeyDown(KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Down:
                if (!IsDropdownOpen)
                {
                    IsDropdownOpen = true;
                }
                else
                {
                    MoveHighlight(1);
                }
                e.Handled = true;
                break;

            case Key.Up:
                MoveHighlight(-1);
                e.Handled = true;
                break;

            case Key.Enter:
                if (IsDropdownOpen && _highlightedIndex >= 0 && _highlightedIndex < FilteredItems.Count)
                {
                    SelectItem(FilteredItems[_highlightedIndex]);
                }
                e.Handled = true;
                break;

            case Key.Escape:
                IsDropdownOpen = false;
                e.Handled = true;
                break;

            case Key.Tab:
                IsDropdownOpen = false;
                break;
        }
    }

    private void MoveHighlight(int direction)
    {
        if (FilteredItems.Count == 0)
            return;

        _highlightedIndex += direction;

        if (_highlightedIndex < 0)
            _highlightedIndex = FilteredItems.Count - 1;
        else if (_highlightedIndex >= FilteredItems.Count)
            _highlightedIndex = 0;
    }

    private void ToggleDropdown()
    {
        IsDropdownOpen = !IsDropdownOpen;
        if (IsDropdownOpen)
        {
            _searchTextBox?.Focus();
        }
    }

    private void SelectItem(object? item)
    {
        if (item == null)
            return;

        SelectedItem = item;
        SearchText = GetDisplayText(item);
        IsDropdownOpen = false;
        _highlightedIndex = -1;

        SelectionChanged?.Invoke(this, item);
    }

    private void OnSearchTextChanged()
    {
        UpdateFilteredItems();

        // Open dropdown when typing
        if (!string.IsNullOrEmpty(SearchText) && !IsDropdownOpen)
        {
            IsDropdownOpen = true;
        }

        _highlightedIndex = FilteredItems.Count > 0 ? 0 : -1;
    }

    private void UpdateFilteredItems()
    {
        FilteredItems.Clear();

        if (ItemsSource == null)
            return;

        var searchText = SearchText?.ToLowerInvariant() ?? string.Empty;

        foreach (var item in ItemsSource)
        {
            if (item == null)
                continue;

            var displayText = GetDisplayText(item).ToLowerInvariant();

            if (string.IsNullOrEmpty(searchText) || displayText.Contains(searchText))
            {
                FilteredItems.Add(item);
            }
        }

        // Notify property changed for HasFilteredItems
        RaisePropertyChanged(nameof(HasFilteredItems));
    }

    private string GetDisplayText(object item)
    {
        if (string.IsNullOrEmpty(DisplayMemberPath))
            return item.ToString() ?? string.Empty;

        var property = item.GetType().GetProperty(DisplayMemberPath);
        return property?.GetValue(item)?.ToString() ?? item.ToString() ?? string.Empty;
    }

    private void OnAddNewClicked()
    {
        AddNewRequested?.Invoke(this, EventArgs.Empty);
        IsDropdownOpen = false;
    }
}
