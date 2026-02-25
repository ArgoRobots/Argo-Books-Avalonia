using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ArgoBooks.Data;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.Controls;

/// <summary>
/// A searchable country input control with flag display.
/// </summary>
public partial class CountryInput : UserControl, INotifyPropertyChanged
{
    public new event PropertyChangedEventHandler? PropertyChanged;
    private TextBox? _countrySearchBox;
    private ListBox? _countryListBox;
    private bool _isUpdatingText;

    protected void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #region Styled Properties

    public static readonly StyledProperty<string> SelectedCountryNameProperty =
        AvaloniaProperty.Register<CountryInput, string>(nameof(SelectedCountryName), string.Empty, defaultBindingMode: BindingMode.TwoWay);

    /// <summary>
    /// Gets or sets the selected country name.
    /// </summary>
    public string SelectedCountryName
    {
        get => GetValue(SelectedCountryNameProperty);
        set => SetValue(SelectedCountryNameProperty, value);
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets the selected country.
    /// </summary>
    public CountryDialCode? SelectedCountry
    {
        get;
        private set
        {
            if (field != value)
            {
                field = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(SelectedCountryFlag));
            }
        }
    }

    /// <summary>
    /// Gets the selected country's flag image.
    /// </summary>
    public IImage? SelectedCountryFlag => SelectedCountry?.FlagImage;

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
                    UpdateFilteredCountries();
                    if (!string.IsNullOrEmpty(value) && value != SelectedCountry?.Name)
                    {
                        IsDropdownOpen = true;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Gets or sets whether the dropdown is open.
    /// </summary>
    public bool IsDropdownOpen
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                RaisePropertyChanged();

                if (value)
                {
                    SelectedIndex = 0;
                    UpdateFilteredCountries();
                }
            }
        }
    }

    /// <summary>
    /// Gets whether there are filtered countries.
    /// </summary>
    public bool HasFilteredCountries
    {
        get;
        private set
        {
            if (field != value)
            {
                field = value;
                RaisePropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the currently highlighted index in the dropdown.
    /// </summary>
    public int SelectedIndex
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                RaisePropertyChanged();
                ScrollToSelectedItem();
            }
        }
    } = -1;

    /// <summary>
    /// Gets the filtered countries based on search.
    /// </summary>
    public ObservableCollection<CountryDialCode> FilteredCountries { get; } = [];

    #endregion

    #region Commands

    public ICommand ToggleDropdownCommand { get; }
    public ICommand SelectCountryCommand { get; }

    #endregion

    public CountryInput()
    {
        ToggleDropdownCommand = new RelayCommand(ToggleDropdown);
        SelectCountryCommand = new RelayCommand<CountryDialCode>(SelectCountry);

        InitializeComponent();

        UpdateFilteredCountries();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SelectedCountryNameProperty && !_isUpdatingText)
        {
            var name = change.NewValue as string ?? string.Empty;
            // First try exact match
            var country = PhoneInput.AllDialCodes.FirstOrDefault(c =>
                c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            // If no match, try normalizing the country name (handles USA, UK, etc.)
            if (country == null && !string.IsNullOrWhiteSpace(name))
            {
                var normalizedName = Countries.NormalizeCountry(name);
                if (normalizedName != null)
                {
                    country = PhoneInput.AllDialCodes.FirstOrDefault(c =>
                        c.Name.Equals(normalizedName, StringComparison.OrdinalIgnoreCase));
                }
            }
            SelectedCountry = country;
            UpdateSearchText();
        }
    }

    protected override void OnLoaded(Avalonia.Interactivity.RoutedEventArgs e)
    {
        base.OnLoaded(e);

        _countrySearchBox = this.FindControl<TextBox>("CountrySearchBox");

        if (_countrySearchBox != null)
        {
            _countrySearchBox.GotFocus += OnCountrySearchBoxGotFocus;
            _countrySearchBox.KeyDown += OnCountrySearchBoxKeyDown;
        }

        _countryListBox = this.FindControl<ListBox>("CountryListBox");
        if (_countryListBox != null)
        {
            _countryListBox.DoubleTapped += OnCountryListBoxDoubleTapped;
            _countryListBox.PointerWheelChanged += OnCountryListBoxPointerWheelChanged;
            _countryListBox.PointerReleased += OnCountryListBoxPointerReleased;
        }
    }

    private void OnCountryListBoxPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        e.Handled = true;
    }

    private void OnCountryListBoxDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (_countryListBox?.SelectedItem is CountryDialCode country)
        {
            SelectCountry(country);
        }
    }

    private void OnCountryListBoxPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_countryListBox?.SelectedItem is CountryDialCode country)
        {
            SelectCountry(country);
        }
    }

    private void OnCountrySearchBoxGotFocus(object? sender, GotFocusEventArgs e)
    {
        IsDropdownOpen = true;
        _countrySearchBox?.SelectAll();
    }

    private void OnCountrySearchBoxKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Down:
                if (!IsDropdownOpen)
                {
                    IsDropdownOpen = true;
                    SelectedIndex = 0;
                }
                else if (SelectedIndex < FilteredCountries.Count - 1)
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
                if (IsDropdownOpen && SelectedIndex >= 0 && SelectedIndex < FilteredCountries.Count)
                {
                    SelectCountry(FilteredCountries[SelectedIndex]);
                }
                else if (FilteredCountries.Count > 0)
                {
                    SelectCountry(FilteredCountries[0]);
                }
                if (e.Key == Key.Enter)
                    e.Handled = true;
                break;
        }
    }

    private void ScrollToSelectedItem()
    {
        if (_countryListBox == null || SelectedIndex < 0 || SelectedIndex >= FilteredCountries.Count)
            return;

        _countryListBox.ScrollIntoView(FilteredCountries[SelectedIndex]);
    }

    private void ToggleDropdown()
    {
        IsDropdownOpen = !IsDropdownOpen;
        if (IsDropdownOpen)
        {
            _countrySearchBox?.Focus();
            _countrySearchBox?.SelectAll();
        }
    }

    private void SelectCountry(CountryDialCode? country)
    {
        if (country == null)
            return;

        _isUpdatingText = true;
        SelectedCountry = country;
        SelectedCountryName = country.Name;
        UpdateSearchText();
        IsDropdownOpen = false;
        _isUpdatingText = false;
    }

    private void UpdateSearchText()
    {
        _isUpdatingText = true;
        SearchText = SelectedCountry?.Name ?? string.Empty;
        _isUpdatingText = false;
    }

    private void UpdateFilteredCountries()
    {
        FilteredCountries.Clear();

        var searchText = _searchText.Trim().ToLowerInvariant();

        bool showingFullList = string.IsNullOrEmpty(searchText) || searchText == SelectedCountry?.Name.ToLowerInvariant();

        IEnumerable<CountryDialCode> filtered;

        if (showingFullList)
        {
            filtered = PhoneInput.AllDialCodes;
        }
        else
        {
            filtered = PhoneInput.AllDialCodes.Where(c =>
                c.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                c.Code.Equals(searchText, StringComparison.OrdinalIgnoreCase));
        }

        // Get the last priority country code (Canada = "CA")
        var lastPriorityCode = Countries.Priority.LastOrDefault()?.Code;

        foreach (var item in filtered.Take(50))
        {
            // Only show separator when displaying the full list, after the last priority country
            item.ShowSeparatorAfter = showingFullList && item.Code == lastPriorityCode;
            FilteredCountries.Add(item);
        }

        HasFilteredCountries = FilteredCountries.Count > 0;
    }
}
