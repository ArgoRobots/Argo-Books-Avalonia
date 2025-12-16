using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.Controls;

/// <summary>
/// A phone number input control with country code selector and auto-formatting.
/// </summary>
public partial class PhoneInput : UserControl, INotifyPropertyChanged
{
    public new event PropertyChangedEventHandler? PropertyChanged;
    private TextBox? _phoneNumberBox;
    private TextBox? _countrySearchBox;
    private bool _isUpdatingText;

    protected void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #region Styled Properties

    public static readonly StyledProperty<CountryDialCode?> SelectedCountryProperty =
        AvaloniaProperty.Register<PhoneInput, CountryDialCode?>(nameof(SelectedCountry), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<string> PhoneNumberProperty =
        AvaloniaProperty.Register<PhoneInput, string>(nameof(PhoneNumber), string.Empty, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<string> FullPhoneNumberProperty =
        AvaloniaProperty.Register<PhoneInput, string>(nameof(FullPhoneNumber), string.Empty, defaultBindingMode: BindingMode.TwoWay);

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the selected country dial code.
    /// </summary>
    public CountryDialCode? SelectedCountry
    {
        get => GetValue(SelectedCountryProperty);
        set => SetValue(SelectedCountryProperty, value);
    }

    /// <summary>
    /// Gets or sets the phone number (without country code).
    /// </summary>
    public string PhoneNumber
    {
        get => GetValue(PhoneNumberProperty);
        set => SetValue(PhoneNumberProperty, value);
    }

    /// <summary>
    /// Gets or sets the full phone number including country code.
    /// </summary>
    public string FullPhoneNumber
    {
        get => GetValue(FullPhoneNumberProperty);
        set => SetValue(FullPhoneNumberProperty, value);
    }

    private string _countrySearchText = string.Empty;
    /// <summary>
    /// Gets or sets the country search text.
    /// </summary>
    public string CountrySearchText
    {
        get => _countrySearchText;
        set
        {
            if (_countrySearchText != value)
            {
                _countrySearchText = value;
                RaisePropertyChanged();
                if (!_isUpdatingText)
                {
                    UpdateFilteredCountries();
                    if (!string.IsNullOrEmpty(value))
                    {
                        IsCountryDropdownOpen = true;
                    }
                }
            }
        }
    }

    private string _formattedPhoneNumber = string.Empty;
    /// <summary>
    /// Gets or sets the formatted phone number display.
    /// </summary>
    public string FormattedPhoneNumber
    {
        get => _formattedPhoneNumber;
        set
        {
            if (_formattedPhoneNumber != value && !_isUpdatingText)
            {
                _isUpdatingText = true;
                var rawDigits = ExtractDigits(value);
                var formatted = FormatPhoneNumber(rawDigits);
                _formattedPhoneNumber = formatted;
                PhoneNumber = rawDigits;
                UpdateFullPhoneNumber();
                RaisePropertyChanged();
                _isUpdatingText = false;
            }
        }
    }

    private bool _isCountryDropdownOpen;
    /// <summary>
    /// Gets or sets whether the country dropdown is open.
    /// </summary>
    public bool IsCountryDropdownOpen
    {
        get => _isCountryDropdownOpen;
        set
        {
            if (_isCountryDropdownOpen != value)
            {
                _isCountryDropdownOpen = value;
                RaisePropertyChanged();

                // Refresh the list when opening
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
    /// Gets the filtered dial codes based on search.
    /// </summary>
    public ObservableCollection<CountryDialCode> FilteredDialCodes { get; } = new();

    #endregion

    #region Commands

    public ICommand ToggleCountryDropdownCommand { get; }
    public ICommand SelectCountryCommand { get; }

    #endregion

    #region Static Data

    /// <summary>
    /// Complete list of country dial codes.
    /// </summary>
    public static readonly List<CountryDialCode> AllDialCodes =
    [
        new("US", "United States", "+1", "United States of America"),
        new("CA", "Canada", "+1", "Canada"),
        new("GB", "United Kingdom", "+44", "United Kingdom of Great Britain and Northern Ireland"),
        new("DE", "Germany", "+49", "Germany"),
        new("FR", "France", "+33", "France"),
        new("AU", "Australia", "+61", "Australia"),
        new("JP", "Japan", "+81", "Japan"),
        new("CN", "China", "+86", "China"),
        new("IN", "India", "+91", "India"),
        new("MX", "Mexico", "+52", "Mexico"),
        new("BR", "Brazil", "+55", "Brazil"),
        new("IT", "Italy", "+39", "Italy"),
        new("ES", "Spain", "+34", "Spain"),
        new("NL", "Netherlands", "+31", "Netherlands"),
        new("KR", "South Korea", "+82", "South Korea"),
        new("SG", "Singapore", "+65", "Singapore"),
        new("RU", "Russia", "+7", "Russia"),
        new("SA", "Saudi Arabia", "+966", "Saudi Arabia"),
        new("AE", "UAE", "+971", "United Arab Emirates"),
        new("CH", "Switzerland", "+41", "Switzerland"),
        new("SE", "Sweden", "+46", "Sweden"),
        new("NO", "Norway", "+47", "Norway"),
        new("DK", "Denmark", "+45", "Denmark"),
        new("FI", "Finland", "+358", "Finland"),
        new("PL", "Poland", "+48", "Poland"),
        new("AT", "Austria", "+43", "Austria"),
        new("BE", "Belgium", "+32", "Belgium"),
        new("IE", "Ireland", "+353", "Ireland"),
        new("PT", "Portugal", "+351", "Portugal"),
        new("NZ", "New Zealand", "+64", "New Zealand"),
        new("ZA", "South Africa", "+27", "South Africa"),
        new("IL", "Israel", "+972", "Israel"),
        new("TH", "Thailand", "+66", "Thailand"),
        new("MY", "Malaysia", "+60", "Malaysia"),
        new("PH", "Philippines", "+63", "Philippines"),
        new("ID", "Indonesia", "+62", "Indonesia"),
        new("VN", "Vietnam", "+84", "Vietnam"),
        new("TW", "Taiwan", "+886", "Taiwan"),
        new("HK", "Hong Kong", "+852", "Hong Kong"),
        new("AR", "Argentina", "+54", "Argentina"),
        new("CL", "Chile", "+56", "Chile"),
        new("CO", "Colombia", "+57", "Colombia"),
        new("PE", "Peru", "+51", "Peru"),
        new("EG", "Egypt", "+20", "Egypt"),
        new("NG", "Nigeria", "+234", "Nigeria"),
        new("KE", "Kenya", "+254", "Kenya"),
        new("TR", "Turkey", "+90", "Turkey"),
        new("GR", "Greece", "+30", "Greece"),
        new("CZ", "Czech Republic", "+420", "Czechia"),
        new("HU", "Hungary", "+36", "Hungary"),
        new("RO", "Romania", "+40", "Romania"),
        new("UA", "Ukraine", "+380", "Ukraine"),
        new("PK", "Pakistan", "+92", "Pakistan"),
        new("BD", "Bangladesh", "+880", "Bangladesh"),
        new("VE", "Venezuela", "+58", "Venezuela"),
        new("EC", "Ecuador", "+593", "Ecuador"),
        new("GT", "Guatemala", "+502", "Guatemala"),
        new("CU", "Cuba", "+53", "Cuba"),
        new("DO", "Dominican Republic", "+1", "Dominican Republic"),
        new("PR", "Puerto Rico", "+1", "Puerto Rico"),
        new("PA", "Panama", "+507", "Panama"),
        new("CR", "Costa Rica", "+506", "Costa Rica"),
        new("UY", "Uruguay", "+598", "Uruguay"),
        new("PY", "Paraguay", "+595", "Paraguay"),
        new("BO", "Bolivia", "+591", "Bolivia"),
        new("HN", "Honduras", "+504", "Honduras"),
        new("SV", "El Salvador", "+503", "El Salvador"),
        new("NI", "Nicaragua", "+505", "Nicaragua"),
        new("JM", "Jamaica", "+1", "Jamaica"),
        new("TT", "Trinidad and Tobago", "+1", "Trinidad and Tobago"),
        new("BS", "Bahamas", "+1", "Bahamas"),
        new("BB", "Barbados", "+1", "Barbados"),
        new("MA", "Morocco", "+212", "Morocco"),
        new("DZ", "Algeria", "+213", "Algeria"),
        new("TN", "Tunisia", "+216", "Tunisia"),
        new("LY", "Libya", "+218", "Libya"),
        new("GH", "Ghana", "+233", "Ghana"),
        new("CI", "Ivory Coast", "+225", "Ivory Coast"),
        new("SN", "Senegal", "+221", "Senegal"),
        new("CM", "Cameroon", "+237", "Cameroon"),
        new("TZ", "Tanzania", "+255", "Tanzania"),
        new("UG", "Uganda", "+256", "Uganda"),
        new("ET", "Ethiopia", "+251", "Ethiopia"),
        new("SD", "Sudan", "+249", "Sudan"),
        new("AO", "Angola", "+244", "Angola"),
        new("ZW", "Zimbabwe", "+263", "Zimbabwe"),
        new("ZM", "Zambia", "+260", "Zambia"),
        new("BW", "Botswana", "+267", "Botswana"),
        new("NA", "Namibia", "+264", "Namibia"),
        new("MZ", "Mozambique", "+258", "Mozambique"),
        new("MG", "Madagascar", "+261", "Madagascar"),
        new("MU", "Mauritius", "+230", "Mauritius"),
        new("RW", "Rwanda", "+250", "Rwanda"),
        new("IQ", "Iraq", "+964", "Iraq"),
        new("IR", "Iran", "+98", "Iran"),
        new("AF", "Afghanistan", "+93", "Afghanistan"),
        new("SY", "Syria", "+963", "Syria"),
        new("JO", "Jordan", "+962", "Jordan"),
        new("LB", "Lebanon", "+961", "Lebanon"),
        new("KW", "Kuwait", "+965", "Kuwait"),
        new("QA", "Qatar", "+974", "Qatar"),
        new("BH", "Bahrain", "+973", "Bahrain"),
        new("OM", "Oman", "+968", "Oman"),
        new("YE", "Yemen", "+967", "Yemen"),
        new("NP", "Nepal", "+977", "Nepal"),
        new("LK", "Sri Lanka", "+94", "Sri Lanka"),
        new("MM", "Myanmar", "+95", "Myanmar"),
        new("KH", "Cambodia", "+855", "Cambodia"),
        new("LA", "Laos", "+856", "Lao"),
        new("BN", "Brunei", "+673", "Brunei"),
        new("MN", "Mongolia", "+976", "Mongolia"),
        new("KZ", "Kazakhstan", "+7", "Kazakhstan"),
        new("UZ", "Uzbekistan", "+998", "Uzbekistan"),
        new("AZ", "Azerbaijan", "+994", "Azerbaijan"),
        new("GE", "Georgia", "+995", "Georgia"),
        new("AM", "Armenia", "+374", "Armenia"),
        new("BY", "Belarus", "+375", "Belarus"),
        new("MD", "Moldova", "+373", "Moldova"),
        new("LT", "Lithuania", "+370", "Lithuania"),
        new("LV", "Latvia", "+371", "Latvia"),
        new("EE", "Estonia", "+372", "Estonia"),
        new("SK", "Slovakia", "+421", "Slovakia"),
        new("SI", "Slovenia", "+386", "Slovenia"),
        new("HR", "Croatia", "+385", "Croatia"),
        new("BA", "Bosnia and Herzegovina", "+387", "Bosnia and Herzegovina"),
        new("RS", "Serbia", "+381", "Serbia"),
        new("ME", "Montenegro", "+382", "Montenegro"),
        new("MK", "North Macedonia", "+389", "North Macedonia"),
        new("AL", "Albania", "+355", "Albania"),
        new("BG", "Bulgaria", "+359", "Bulgaria"),
        new("CY", "Cyprus", "+357", "Cyprus"),
        new("MT", "Malta", "+356", "Malta"),
        new("IS", "Iceland", "+354", "Iceland"),
        new("LU", "Luxembourg", "+352", "Luxembourg"),
        new("MC", "Monaco", "+377", "Monaco"),
        new("LI", "Liechtenstein", "+423", "Liechtenstein"),
        new("AD", "Andorra", "+376", "Andorra"),
        new("SM", "San Marino", "+378", "San Marino"),
        new("VA", "Vatican City", "+379", "Vatican City"),
        new("FJ", "Fiji", "+679", "Fiji"),
        new("PG", "Papua New Guinea", "+675", "Papua New Guinea"),
        new("NC", "New Caledonia", "+687", "New Caledonia"),
        new("PF", "French Polynesia", "+689", "French Polynesia"),
        new("GU", "Guam", "+1", "Guam"),
        new("VI", "US Virgin Islands", "+1", "US Virgin Islands"),
        new("AS", "American Samoa", "+1", "American Samoa"),
        new("MP", "Northern Mariana Islands", "+1", "Northern Mariana Islands")
    ];

    #endregion

    public PhoneInput()
    {
        ToggleCountryDropdownCommand = new RelayCommand(ToggleCountryDropdown);
        SelectCountryCommand = new RelayCommand<CountryDialCode>(SelectCountry);

        InitializeComponent();

        // Set DataContext to self for simpler bindings
        DataContext = this;

        // Initialize with US as default
        SelectedCountry = AllDialCodes.FirstOrDefault(c => c.Code == "US");
        UpdateCountrySearchText();
        UpdateFilteredCountries();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SelectedCountryProperty)
        {
            UpdateCountrySearchText();
            UpdateFullPhoneNumber();
        }
        else if (change.Property == PhoneNumberProperty && !_isUpdatingText)
        {
            _isUpdatingText = true;
            _formattedPhoneNumber = FormatPhoneNumber(PhoneNumber);
            RaisePropertyChanged(nameof(FormattedPhoneNumber));
            UpdateFullPhoneNumber();
            _isUpdatingText = false;
        }
        else if (change.Property == FullPhoneNumberProperty && !_isUpdatingText)
        {
            ParseFullPhoneNumber(change.NewValue as string ?? string.Empty);
        }
    }

    protected override void OnLoaded(Avalonia.Interactivity.RoutedEventArgs e)
    {
        base.OnLoaded(e);

        _phoneNumberBox = this.FindControl<TextBox>("PhoneNumberBox");
        _countrySearchBox = this.FindControl<TextBox>("CountrySearchBox");

        if (_phoneNumberBox != null)
        {
            _phoneNumberBox.TextChanged += OnPhoneNumberTextChanged;
            _phoneNumberBox.KeyDown += OnPhoneNumberKeyDown;
        }

        if (_countrySearchBox != null)
        {
            _countrySearchBox.GotFocus += OnCountrySearchBoxGotFocus;
            _countrySearchBox.KeyDown += OnCountrySearchBoxKeyDown;
        }
    }

    private void OnPhoneNumberTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_isUpdatingText || _phoneNumberBox == null)
            return;
    }

    private void OnPhoneNumberKeyDown(object? sender, KeyEventArgs e)
    {
        // Allow navigation and deletion keys
        if (e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.Home ||
            e.Key == Key.End || e.Key == Key.Delete || e.Key == Key.Back ||
            e.Key == Key.Tab)
        {
            return;
        }

        // Only allow digits
        if (!IsDigitKey(e.Key))
        {
            e.Handled = true;
        }
    }

    private bool IsDigitKey(Key key)
    {
        return key >= Key.D0 && key <= Key.D9 ||
               key >= Key.NumPad0 && key <= Key.NumPad9;
    }

    private void OnCountrySearchBoxGotFocus(object? sender, GotFocusEventArgs e)
    {
        IsCountryDropdownOpen = true;
        _countrySearchBox?.SelectAll();
    }

    private void OnCountrySearchBoxKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Down:
                IsCountryDropdownOpen = true;
                e.Handled = true;
                break;
            case Key.Escape:
                IsCountryDropdownOpen = false;
                UpdateCountrySearchText();
                e.Handled = true;
                break;
            case Key.Enter:
                if (FilteredDialCodes.Count > 0)
                {
                    SelectCountry(FilteredDialCodes[0]);
                }
                e.Handled = true;
                break;
        }
    }

    private void ToggleCountryDropdown()
    {
        IsCountryDropdownOpen = !IsCountryDropdownOpen;
        if (IsCountryDropdownOpen)
        {
            _countrySearchBox?.Focus();
            _countrySearchBox?.SelectAll();
        }
    }

    private void SelectCountry(CountryDialCode? country)
    {
        if (country == null)
            return;

        SelectedCountry = country;
        UpdateCountrySearchText();
        IsCountryDropdownOpen = false;
        _phoneNumberBox?.Focus();
    }

    private void UpdateCountrySearchText()
    {
        _isUpdatingText = true;
        _countrySearchText = SelectedCountry?.DialCode ?? "+1";
        RaisePropertyChanged(nameof(CountrySearchText));
        _isUpdatingText = false;
    }

    private void UpdateFilteredCountries()
    {
        FilteredDialCodes.Clear();

        var searchText = _countrySearchText?.Trim().ToLowerInvariant() ?? string.Empty;

        IEnumerable<CountryDialCode> filtered;

        if (string.IsNullOrEmpty(searchText) || searchText.StartsWith("+"))
        {
            // Show all when empty or when showing dial code
            filtered = AllDialCodes;
        }
        else
        {
            // Search by dial code, country name, or country code
            filtered = AllDialCodes.Where(c =>
                c.DialCode.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                c.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                c.Code.Equals(searchText, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var item in filtered.Take(50))
        {
            FilteredDialCodes.Add(item);
        }

        HasFilteredCountries = FilteredDialCodes.Count > 0;
    }

    private void UpdateFullPhoneNumber()
    {
        if (_isUpdatingText)
            return;

        var dialCode = SelectedCountry?.DialCode ?? "+1";
        var digits = ExtractDigits(PhoneNumber);
        FullPhoneNumber = string.IsNullOrEmpty(digits) ? string.Empty : $"{dialCode} {digits}";
    }

    private void ParseFullPhoneNumber(string fullPhone)
    {
        if (string.IsNullOrWhiteSpace(fullPhone))
        {
            SelectedCountry = AllDialCodes.FirstOrDefault(c => c.Code == "US");
            PhoneNumber = string.Empty;
            return;
        }

        _isUpdatingText = true;

        // Try to match against known dial codes (longest match first)
        var sortedDialCodes = AllDialCodes.OrderByDescending(d => d.DialCode.Length).ToList();

        foreach (var dialCode in sortedDialCodes)
        {
            if (fullPhone.StartsWith(dialCode.DialCode))
            {
                SelectedCountry = dialCode;
                var remaining = fullPhone[dialCode.DialCode.Length..].Trim();
                PhoneNumber = ExtractDigits(remaining);
                _formattedPhoneNumber = FormatPhoneNumber(PhoneNumber);
                RaisePropertyChanged(nameof(FormattedPhoneNumber));
                UpdateCountrySearchText();
                _isUpdatingText = false;
                return;
            }
        }

        // No matching dial code found, use default
        SelectedCountry = AllDialCodes.FirstOrDefault(c => c.Code == "US");
        PhoneNumber = ExtractDigits(fullPhone);
        _formattedPhoneNumber = FormatPhoneNumber(PhoneNumber);
        RaisePropertyChanged(nameof(FormattedPhoneNumber));
        UpdateCountrySearchText();
        _isUpdatingText = false;
    }

    /// <summary>
    /// Formats a phone number with parentheses and dashes: (XXX) XXX-XXXX
    /// </summary>
    private static string FormatPhoneNumber(string digits)
    {
        if (string.IsNullOrEmpty(digits))
            return string.Empty;

        var sb = new StringBuilder();

        for (int i = 0; i < digits.Length && i < 10; i++)
        {
            if (i == 0) sb.Append('(');
            sb.Append(digits[i]);
            if (i == 2) sb.Append(") ");
            if (i == 5) sb.Append('-');
        }

        // Handle numbers longer than 10 digits (extensions, etc.)
        if (digits.Length > 10)
        {
            sb.Append(" x");
            sb.Append(digits[10..]);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Extracts only digits from a string.
    /// </summary>
    private static string ExtractDigits(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        return new string(input.Where(char.IsDigit).ToArray());
    }
}

/// <summary>
/// Represents a country with its dial code.
/// </summary>
public class CountryDialCode
{
    /// <summary>
    /// ISO country code (e.g., US, GB).
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Country name for display.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Phone dial code (e.g., +1, +44).
    /// </summary>
    public string DialCode { get; }

    /// <summary>
    /// Flag file name (matches the PNG file in Assets/CountryFlags).
    /// </summary>
    public string FlagFileName { get; }

    /// <summary>
    /// Path to the flag image asset.
    /// </summary>
    public string FlagPath => $"avares://ArgoBooks/Assets/CountryFlags/{FlagFileName}.png";

    /// <summary>
    /// Display format for the dropdown.
    /// </summary>
    public string DisplayName => $"{DialCode} {Name}";

    /// <summary>
    /// Short display for the selected item.
    /// </summary>
    public string ShortDisplay => DialCode;

    public CountryDialCode(string code, string name, string dialCode, string flagFileName)
    {
        Code = code;
        Name = name;
        DialCode = dialCode;
        FlagFileName = flagFileName;
    }

    public override string ToString() => DisplayName;
}
