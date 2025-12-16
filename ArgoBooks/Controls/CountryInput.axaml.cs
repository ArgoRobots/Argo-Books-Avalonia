using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
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
    private ScrollViewer? _countryScrollViewer;
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

    private CountryDialCode? _selectedCountry;
    /// <summary>
    /// Gets the selected country.
    /// </summary>
    public CountryDialCode? SelectedCountry
    {
        get => _selectedCountry;
        private set
        {
            if (_selectedCountry != value)
            {
                _selectedCountry = value;
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
                    UpdateFilteredCountries();
                }
            }
        }
    }

    private bool _hasFilteredCountries;
    /// <summary>
    /// Gets whether there are filtered countries.
    /// </summary>
    public bool HasFilteredCountries
    {
        get => _hasFilteredCountries;
        private set
        {
            if (_hasFilteredCountries != value)
            {
                _hasFilteredCountries = value;
                RaisePropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets the filtered countries based on search.
    /// </summary>
    public ObservableCollection<CountryDialCode> FilteredCountries { get; } = new();

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

        DataContext = this;

        UpdateFilteredCountries();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SelectedCountryNameProperty && !_isUpdatingText)
        {
            var name = change.NewValue as string ?? string.Empty;
            var country = PhoneInput.AllDialCodes.FirstOrDefault(c =>
                c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
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

        _countryScrollViewer = this.FindControl<ScrollViewer>("CountryScrollViewer");
        if (_countryScrollViewer != null)
        {
            _countryScrollViewer.PointerWheelChanged += OnCountryScrollViewerPointerWheelChanged;
        }
    }

    private void OnCountryScrollViewerPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        e.Handled = true;
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
                IsDropdownOpen = true;
                e.Handled = true;
                break;
            case Key.Escape:
                IsDropdownOpen = false;
                UpdateSearchText();
                e.Handled = true;
                break;
            case Key.Enter:
                if (FilteredCountries.Count > 0)
                {
                    SelectCountry(FilteredCountries[0]);
                }
                e.Handled = true;
                break;
        }
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
        _searchText = SelectedCountry?.Name ?? string.Empty;
        RaisePropertyChanged(nameof(SearchText));
        _isUpdatingText = false;
    }

    private void UpdateFilteredCountries()
    {
        FilteredCountries.Clear();

        var searchText = _searchText?.Trim().ToLowerInvariant() ?? string.Empty;

        IEnumerable<CountryDialCode> filtered;

        if (string.IsNullOrEmpty(searchText) || searchText == SelectedCountry?.Name.ToLowerInvariant())
        {
            filtered = PhoneInput.AllDialCodes;
        }
        else
        {
            filtered = PhoneInput.AllDialCodes.Where(c =>
                c.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                c.Code.Equals(searchText, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var item in filtered.Take(50))
        {
            FilteredCountries.Add(item);
        }

        HasFilteredCountries = FilteredCountries.Count > 0;
    }
}
