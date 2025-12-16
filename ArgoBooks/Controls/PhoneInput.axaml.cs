using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
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
    private int _previousCaretPosition;

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
                UpdateFilteredCountries();
                if (!_isUpdatingText && !string.IsNullOrEmpty(value))
                {
                    IsCountryDropdownOpen = true;
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
            }
        }
    }

    /// <summary>
    /// Gets whether there are filtered countries.
    /// </summary>
    public bool HasFilteredCountries => FilteredDialCodes.Count > 0;

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
        new("US", "United States", "+1"),
        new("CA", "Canada", "+1"),
        new("GB", "United Kingdom", "+44"),
        new("DE", "Germany", "+49"),
        new("FR", "France", "+33"),
        new("AU", "Australia", "+61"),
        new("JP", "Japan", "+81"),
        new("CN", "China", "+86"),
        new("IN", "India", "+91"),
        new("MX", "Mexico", "+52"),
        new("BR", "Brazil", "+55"),
        new("IT", "Italy", "+39"),
        new("ES", "Spain", "+34"),
        new("NL", "Netherlands", "+31"),
        new("KR", "South Korea", "+82"),
        new("SG", "Singapore", "+65"),
        new("RU", "Russia", "+7"),
        new("SA", "Saudi Arabia", "+966"),
        new("AE", "UAE", "+971"),
        new("CH", "Switzerland", "+41"),
        new("SE", "Sweden", "+46"),
        new("NO", "Norway", "+47"),
        new("DK", "Denmark", "+45"),
        new("FI", "Finland", "+358"),
        new("PL", "Poland", "+48"),
        new("AT", "Austria", "+43"),
        new("BE", "Belgium", "+32"),
        new("IE", "Ireland", "+353"),
        new("PT", "Portugal", "+351"),
        new("NZ", "New Zealand", "+64"),
        new("ZA", "South Africa", "+27"),
        new("IL", "Israel", "+972"),
        new("TH", "Thailand", "+66"),
        new("MY", "Malaysia", "+60"),
        new("PH", "Philippines", "+63"),
        new("ID", "Indonesia", "+62"),
        new("VN", "Vietnam", "+84"),
        new("TW", "Taiwan", "+886"),
        new("HK", "Hong Kong", "+852"),
        new("AR", "Argentina", "+54"),
        new("CL", "Chile", "+56"),
        new("CO", "Colombia", "+57"),
        new("PE", "Peru", "+51"),
        new("EG", "Egypt", "+20"),
        new("NG", "Nigeria", "+234"),
        new("KE", "Kenya", "+254"),
        new("TR", "Turkey", "+90"),
        new("GR", "Greece", "+30"),
        new("CZ", "Czech Republic", "+420"),
        new("HU", "Hungary", "+36"),
        new("RO", "Romania", "+40"),
        new("UA", "Ukraine", "+380"),
        new("PK", "Pakistan", "+92"),
        new("BD", "Bangladesh", "+880"),
        new("VE", "Venezuela", "+58"),
        new("EC", "Ecuador", "+593"),
        new("GT", "Guatemala", "+502"),
        new("CU", "Cuba", "+53"),
        new("DO", "Dominican Republic", "+1"),
        new("PR", "Puerto Rico", "+1"),
        new("PA", "Panama", "+507"),
        new("CR", "Costa Rica", "+506"),
        new("UY", "Uruguay", "+598"),
        new("PY", "Paraguay", "+595"),
        new("BO", "Bolivia", "+591"),
        new("HN", "Honduras", "+504"),
        new("SV", "El Salvador", "+503"),
        new("NI", "Nicaragua", "+505"),
        new("JM", "Jamaica", "+1"),
        new("TT", "Trinidad and Tobago", "+1"),
        new("BS", "Bahamas", "+1"),
        new("BB", "Barbados", "+1"),
        new("MA", "Morocco", "+212"),
        new("DZ", "Algeria", "+213"),
        new("TN", "Tunisia", "+216"),
        new("LY", "Libya", "+218"),
        new("GH", "Ghana", "+233"),
        new("CI", "Ivory Coast", "+225"),
        new("SN", "Senegal", "+221"),
        new("CM", "Cameroon", "+237"),
        new("TZ", "Tanzania", "+255"),
        new("UG", "Uganda", "+256"),
        new("ET", "Ethiopia", "+251"),
        new("SD", "Sudan", "+249"),
        new("AO", "Angola", "+244"),
        new("ZW", "Zimbabwe", "+263"),
        new("ZM", "Zambia", "+260"),
        new("BW", "Botswana", "+267"),
        new("NA", "Namibia", "+264"),
        new("MZ", "Mozambique", "+258"),
        new("MG", "Madagascar", "+261"),
        new("MU", "Mauritius", "+230"),
        new("RW", "Rwanda", "+250"),
        new("IQ", "Iraq", "+964"),
        new("IR", "Iran", "+98"),
        new("AF", "Afghanistan", "+93"),
        new("SY", "Syria", "+963"),
        new("JO", "Jordan", "+962"),
        new("LB", "Lebanon", "+961"),
        new("KW", "Kuwait", "+965"),
        new("QA", "Qatar", "+974"),
        new("BH", "Bahrain", "+973"),
        new("OM", "Oman", "+968"),
        new("YE", "Yemen", "+967"),
        new("NP", "Nepal", "+977"),
        new("LK", "Sri Lanka", "+94"),
        new("MM", "Myanmar", "+95"),
        new("KH", "Cambodia", "+855"),
        new("LA", "Laos", "+856"),
        new("BN", "Brunei", "+673"),
        new("MN", "Mongolia", "+976"),
        new("KZ", "Kazakhstan", "+7"),
        new("UZ", "Uzbekistan", "+998"),
        new("AZ", "Azerbaijan", "+994"),
        new("GE", "Georgia", "+995"),
        new("AM", "Armenia", "+374"),
        new("BY", "Belarus", "+375"),
        new("MD", "Moldova", "+373"),
        new("LT", "Lithuania", "+370"),
        new("LV", "Latvia", "+371"),
        new("EE", "Estonia", "+372"),
        new("SK", "Slovakia", "+421"),
        new("SI", "Slovenia", "+386"),
        new("HR", "Croatia", "+385"),
        new("BA", "Bosnia and Herzegovina", "+387"),
        new("RS", "Serbia", "+381"),
        new("ME", "Montenegro", "+382"),
        new("MK", "North Macedonia", "+389"),
        new("AL", "Albania", "+355"),
        new("BG", "Bulgaria", "+359"),
        new("CY", "Cyprus", "+357"),
        new("MT", "Malta", "+356"),
        new("IS", "Iceland", "+354"),
        new("LU", "Luxembourg", "+352"),
        new("MC", "Monaco", "+377"),
        new("LI", "Liechtenstein", "+423"),
        new("AD", "Andorra", "+376"),
        new("SM", "San Marino", "+378"),
        new("VA", "Vatican City", "+379"),
        new("FJ", "Fiji", "+679"),
        new("PG", "Papua New Guinea", "+675"),
        new("NC", "New Caledonia", "+687"),
        new("PF", "French Polynesia", "+689"),
        new("GU", "Guam", "+1"),
        new("VI", "US Virgin Islands", "+1"),
        new("AS", "American Samoa", "+1"),
        new("MP", "Northern Mariana Islands", "+1")
    ];

    #endregion

    public PhoneInput()
    {
        ToggleCountryDropdownCommand = new RelayCommand(ToggleCountryDropdown);
        SelectCountryCommand = new RelayCommand<CountryDialCode>(SelectCountry);

        InitializeComponent();

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

        _previousCaretPosition = _phoneNumberBox.CaretIndex;
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
        CountrySearchText = SelectedCountry?.DialCode ?? "+1";
        _isUpdatingText = false;
    }

    private void UpdateFilteredCountries()
    {
        FilteredDialCodes.Clear();

        var searchText = _countrySearchText?.Trim().ToLowerInvariant() ?? string.Empty;

        IEnumerable<CountryDialCode> filtered;

        if (string.IsNullOrEmpty(searchText))
        {
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

        foreach (var item in filtered.Take(50)) // Limit for performance
        {
            FilteredDialCodes.Add(item);
        }

        RaisePropertyChanged(nameof(HasFilteredCountries));
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
    /// Country name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Phone dial code (e.g., +1, +44).
    /// </summary>
    public string DialCode { get; }

    /// <summary>
    /// Display format for the dropdown.
    /// </summary>
    public string DisplayName => $"{DialCode} {Name}";

    /// <summary>
    /// Short display for the selected item.
    /// </summary>
    public string ShortDisplay => DialCode;

    public CountryDialCode(string code, string name, string dialCode)
    {
        Code = code;
        Name = name;
        DialCode = dialCode;
    }

    public override string ToString() => DisplayName;
}
