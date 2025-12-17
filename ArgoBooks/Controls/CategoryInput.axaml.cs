using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.Controls;

/// <summary>
/// Represents a category item for selection.
/// </summary>
public class CategoryItem
{
    public string? Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// A searchable category input control.
/// </summary>
public partial class CategoryInput : UserControl, INotifyPropertyChanged
{
    public new event PropertyChangedEventHandler? PropertyChanged;
    private TextBox? _categorySearchBox;
    private ListBox? _categoryListBox;
    private bool _isUpdatingText;

    protected void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #region Styled Properties

    public static readonly StyledProperty<string?> SelectedCategoryIdProperty =
        AvaloniaProperty.Register<CategoryInput, string?>(nameof(SelectedCategoryId), null, defaultBindingMode: BindingMode.TwoWay);

    /// <summary>
    /// Gets or sets the selected category ID.
    /// </summary>
    public string? SelectedCategoryId
    {
        get => GetValue(SelectedCategoryIdProperty);
        set => SetValue(SelectedCategoryIdProperty, value);
    }

    public static readonly StyledProperty<string?> SelectedCategoryNameProperty =
        AvaloniaProperty.Register<CategoryInput, string?>(nameof(SelectedCategoryName), null, defaultBindingMode: BindingMode.TwoWay);

    /// <summary>
    /// Gets or sets the selected category name.
    /// </summary>
    public string? SelectedCategoryName
    {
        get => GetValue(SelectedCategoryNameProperty);
        set => SetValue(SelectedCategoryNameProperty, value);
    }

    public static readonly StyledProperty<ObservableCollection<CategoryItem>> CategoriesProperty =
        AvaloniaProperty.Register<CategoryInput, ObservableCollection<CategoryItem>>(nameof(Categories), new ObservableCollection<CategoryItem>());

    /// <summary>
    /// Gets or sets the available categories.
    /// </summary>
    public ObservableCollection<CategoryItem> Categories
    {
        get => GetValue(CategoriesProperty);
        set => SetValue(CategoriesProperty, value);
    }

    public static readonly StyledProperty<ICommand?> OpenCategoriesPageCommandProperty =
        AvaloniaProperty.Register<CategoryInput, ICommand?>(nameof(OpenCategoriesPageCommand));

    /// <summary>
    /// Gets or sets the command to open the categories page.
    /// </summary>
    public ICommand? OpenCategoriesPageCommand
    {
        get => GetValue(OpenCategoriesPageCommandProperty);
        set => SetValue(OpenCategoriesPageCommandProperty, value);
    }

    #endregion

    #region Properties

    private CategoryItem? _selectedCategory;
    /// <summary>
    /// Gets the selected category.
    /// </summary>
    public CategoryItem? SelectedCategory
    {
        get => _selectedCategory;
        private set
        {
            if (_selectedCategory != value)
            {
                _selectedCategory = value;
                RaisePropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets whether there are any categories available.
    /// </summary>
    public bool HasCategories => Categories.Count > 0;

    private string _searchText = string.Empty;
    /// <summary>
    /// Gets or sets the search text.
    /// </summary>
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText != value)
            {
                _searchText = value;
                RaisePropertyChanged();
                if (!_isUpdatingText)
                {
                    UpdateFilteredCategories();
                    if (!string.IsNullOrEmpty(value) && value != SelectedCategory?.Name)
                    {
                        IsDropdownOpen = true;
                    }
                }
            }
        }
    }

    private bool _isDropdownOpen;
    /// <summary>
    /// Gets or sets whether the dropdown is open.
    /// </summary>
    public bool IsDropdownOpen
    {
        get => _isDropdownOpen;
        set
        {
            if (_isDropdownOpen != value)
            {
                _isDropdownOpen = value;
                RaisePropertyChanged();

                if (value)
                {
                    SelectedIndex = 0;
                    UpdateFilteredCategories();
                }
            }
        }
    }

    private bool _hasFilteredCategories;
    /// <summary>
    /// Gets whether there are filtered categories.
    /// </summary>
    public bool HasFilteredCategories
    {
        get => _hasFilteredCategories;
        private set
        {
            if (_hasFilteredCategories != value)
            {
                _hasFilteredCategories = value;
                RaisePropertyChanged();
            }
        }
    }

    private int _selectedIndex = -1;
    /// <summary>
    /// Gets or sets the currently highlighted index in the dropdown.
    /// </summary>
    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            if (_selectedIndex != value)
            {
                _selectedIndex = value;
                RaisePropertyChanged();
                ScrollToSelectedItem();
            }
        }
    }

    /// <summary>
    /// Gets the filtered categories based on search.
    /// </summary>
    public ObservableCollection<CategoryItem> FilteredCategories { get; } = new();

    #endregion

    #region Commands

    public ICommand ToggleDropdownCommand { get; }

    #endregion

    public CategoryInput()
    {
        ToggleDropdownCommand = new RelayCommand(ToggleDropdown);

        InitializeComponent();

        DataContext = this;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == CategoriesProperty)
        {
            RaisePropertyChanged(nameof(HasCategories));
            UpdateFilteredCategories();

            // Subscribe to collection changes
            if (change.OldValue is ObservableCollection<CategoryItem> oldCollection)
            {
                oldCollection.CollectionChanged -= OnCategoriesCollectionChanged;
            }
            if (change.NewValue is ObservableCollection<CategoryItem> newCollection)
            {
                newCollection.CollectionChanged += OnCategoriesCollectionChanged;
            }
        }
        else if (change.Property == SelectedCategoryIdProperty && !_isUpdatingText)
        {
            var id = change.NewValue as string;
            var category = Categories.FirstOrDefault(c => c.Id == id);
            SelectedCategory = category;
            UpdateSearchText();
        }
        else if (change.Property == SelectedCategoryNameProperty && !_isUpdatingText)
        {
            var name = change.NewValue as string;
            var category = Categories.FirstOrDefault(c =>
                c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            SelectedCategory = category;
            UpdateSearchText();
        }
    }

    private void OnCategoriesCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        RaisePropertyChanged(nameof(HasCategories));
        UpdateFilteredCategories();
    }

    protected override void OnLoaded(Avalonia.Interactivity.RoutedEventArgs e)
    {
        base.OnLoaded(e);

        _categorySearchBox = this.FindControl<TextBox>("CategorySearchBox");

        if (_categorySearchBox != null)
        {
            _categorySearchBox.GotFocus += OnCategorySearchBoxGotFocus;
            _categorySearchBox.KeyDown += OnCategorySearchBoxKeyDown;
        }

        _categoryListBox = this.FindControl<ListBox>("CategoryListBox");
        if (_categoryListBox != null)
        {
            _categoryListBox.DoubleTapped += OnCategoryListBoxDoubleTapped;
            _categoryListBox.PointerWheelChanged += OnCategoryListBoxPointerWheelChanged;
            _categoryListBox.PointerReleased += OnCategoryListBoxPointerReleased;
        }

        // Initialize with current selection
        UpdateFilteredCategories();
        UpdateSearchText();
    }

    private void OnCategoryListBoxPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        e.Handled = true;
    }

    private void OnCategoryListBoxDoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (_categoryListBox?.SelectedItem is CategoryItem category)
        {
            SelectCategory(category);
        }
    }

    private void OnCategoryListBoxPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_categoryListBox?.SelectedItem is CategoryItem category)
        {
            SelectCategory(category);
        }
    }

    private void OnCategorySearchBoxGotFocus(object? sender, GotFocusEventArgs e)
    {
        IsDropdownOpen = true;
        _categorySearchBox?.SelectAll();
    }

    private void OnCategorySearchBoxKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Down:
                if (!IsDropdownOpen)
                {
                    IsDropdownOpen = true;
                    SelectedIndex = 0;
                }
                else if (SelectedIndex < FilteredCategories.Count - 1)
                {
                    SelectedIndex++;
                }
                e.Handled = true;
                break;
            case Key.Up:
                if (IsDropdownOpen && SelectedIndex > 0)
                {
                    SelectedIndex--;
                }
                e.Handled = true;
                break;
            case Key.Escape:
                IsDropdownOpen = false;
                UpdateSearchText();
                e.Handled = true;
                break;
            case Key.Enter:
            case Key.Tab:
                if (IsDropdownOpen && SelectedIndex >= 0 && SelectedIndex < FilteredCategories.Count)
                {
                    SelectCategory(FilteredCategories[SelectedIndex]);
                }
                else if (FilteredCategories.Count > 0)
                {
                    SelectCategory(FilteredCategories[0]);
                }
                if (e.Key == Key.Enter)
                    e.Handled = true;
                break;
        }
    }

    private void ScrollToSelectedItem()
    {
        if (_categoryListBox == null || SelectedIndex < 0 || SelectedIndex >= FilteredCategories.Count)
            return;

        _categoryListBox.ScrollIntoView(FilteredCategories[SelectedIndex]);
    }

    private void ToggleDropdown()
    {
        IsDropdownOpen = !IsDropdownOpen;
        if (IsDropdownOpen)
        {
            _categorySearchBox?.Focus();
            _categorySearchBox?.SelectAll();
        }
    }

    private void SelectCategory(CategoryItem? category)
    {
        if (category == null)
            return;

        _isUpdatingText = true;
        SelectedCategory = category;
        SelectedCategoryId = category.Id;
        SelectedCategoryName = category.Name;
        UpdateSearchText();
        IsDropdownOpen = false;
        _isUpdatingText = false;
    }

    private void UpdateSearchText()
    {
        _isUpdatingText = true;
        _searchText = SelectedCategory?.Name ?? string.Empty;
        RaisePropertyChanged(nameof(SearchText));
        _isUpdatingText = false;
    }

    private void UpdateFilteredCategories()
    {
        FilteredCategories.Clear();

        var searchText = _searchText?.Trim().ToLowerInvariant() ?? string.Empty;

        IEnumerable<CategoryItem> filtered;

        if (string.IsNullOrEmpty(searchText) || searchText == SelectedCategory?.Name?.ToLowerInvariant())
        {
            filtered = Categories;
        }
        else
        {
            filtered = Categories.Where(c =>
                c.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var item in filtered.Take(50))
        {
            FilteredCategories.Add(item);
        }

        HasFilteredCategories = FilteredCategories.Count > 0;
    }

    /// <summary>
    /// Clears the selection.
    /// </summary>
    public void ClearSelection()
    {
        _isUpdatingText = true;
        SelectedCategory = null;
        SelectedCategoryId = null;
        SelectedCategoryName = null;
        _searchText = string.Empty;
        RaisePropertyChanged(nameof(SearchText));
        _isUpdatingText = false;
    }

    /// <summary>
    /// Handles the click event for the "Create one" button.
    /// </summary>
    private void OnCreateCategoryClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        OpenCategoriesPageCommand?.Execute(null);
    }
}
