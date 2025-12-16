using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
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
    private ScrollViewer? _countryScrollViewer;
    private bool _isUpdatingText;
    private bool _isFormattingPhone;

    // Maximum number of extension digits allowed
    private const int MaxExtensionDigits = 5;

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
            if (_formattedPhoneNumber != value && !_isFormattingPhone)
            {
                _isFormattingPhone = true;
                var rawDigits = ExtractDigits(value);
                var formatted = FormatPhoneNumber(rawDigits);
                _formattedPhoneNumber = formatted;
                PhoneNumber = rawDigits;
                UpdateFullPhoneNumber();
                RaisePropertyChanged();
                _isFormattingPhone = false;
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
        new("AF", "Afghanistan", "+93", "Afghanistan"),
        new("AL", "Albania", "+355", "Albania"),
        new("DZ", "Algeria", "+213", "Algeria"),
        new("AD", "Andorra", "+376", "Andorra"),
        new("AO", "Angola", "+244", "Angola"),
        new("AG", "Antigua and Barbuda", "+1", "Antigua and Barbuda"),
        new("AR", "Argentina", "+54", "Argentina"),
        new("AM", "Armenia", "+374", "Armenia"),
        new("AU", "Australia", "+61", "Australia"),
        new("AT", "Austria", "+43", "Austria"),
        new("AZ", "Azerbaijan", "+994", "Azerbaijan"),
        new("BS", "Bahamas", "+1", "Bahamas"),
        new("BH", "Bahrain", "+973", "Bahrain"),
        new("BD", "Bangladesh", "+880", "Bangladesh"),
        new("BB", "Barbados", "+1", "Barbados"),
        new("BY", "Belarus", "+375", "Belarus"),
        new("BE", "Belgium", "+32", "Belgium"),
        new("BZ", "Belize", "+501", "Belize"),
        new("BJ", "Benin", "+229", "Benin"),
        new("BT", "Bhutan", "+975", "Bhutan"),
        new("BO", "Bolivia", "+591", "Bolivia"),
        new("BA", "Bosnia and Herzegovina", "+387", "Bosnia and Herzegovina"),
        new("BW", "Botswana", "+267", "Botswana"),
        new("BR", "Brazil", "+55", "Brazil"),
        new("BN", "Brunei", "+673", "Brunei"),
        new("BG", "Bulgaria", "+359", "Bulgaria"),
        new("BF", "Burkina Faso", "+226", "Burkina Faso"),
        new("BI", "Burundi", "+257", "Burundi"),
        new("CV", "Cabo Verde", "+238", "Cabo Verde"),
        new("KH", "Cambodia", "+855", "Cambodia"),
        new("CM", "Cameroon", "+237", "Cameroon"),
        new("CA", "Canada", "+1", "Canada"),
        new("CF", "Central African Republic", "+236", "Central African Republic"),
        new("TD", "Chad", "+235", "Chad"),
        new("CL", "Chile", "+56", "Chile"),
        new("CN", "China", "+86", "China"),
        new("CO", "Colombia", "+57", "Colombia"),
        new("KM", "Comoros", "+269", "Comoros"),
        new("CR", "Costa Rica", "+506", "Costa Rica"),
        new("HR", "Croatia", "+385", "Croatia"),
        new("CU", "Cuba", "+53", "Cuba"),
        new("CY", "Cyprus", "+357", "Cyprus"),
        new("CZ", "Czechia", "+420", "Czechia"),
        new("DK", "Denmark", "+45", "Denmark"),
        new("DJ", "Djibouti", "+253", "Djibouti"),
        new("DM", "Dominica", "+1", "Dominica"),
        new("DO", "Dominican Republic", "+1", "Dominican Republic"),
        new("EC", "Ecuador", "+593", "Ecuador"),
        new("EG", "Egypt", "+20", "Egypt"),
        new("SV", "El Salvador", "+503", "El Salvador"),
        new("GQ", "Equatorial Guinea", "+240", "Equatorial Guinea"),
        new("ER", "Eritrea", "+291", "Eritrea"),
        new("EE", "Estonia", "+372", "Estonia"),
        new("SZ", "Eswatini", "+268", "Eswatini"),
        new("ET", "Ethiopia", "+251", "Ethiopia"),
        new("FJ", "Fiji", "+679", "Fiji"),
        new("FI", "Finland", "+358", "Finland"),
        new("FR", "France", "+33", "France"),
        new("GA", "Gabon", "+241", "Gabon"),
        new("GM", "Gambia", "+220", "Gambia"),
        new("GE", "Georgia", "+995", "Georgia"),
        new("DE", "Germany", "+49", "Germany"),
        new("GH", "Ghana", "+233", "Ghana"),
        new("GR", "Greece", "+30", "Greece"),
        new("GD", "Grenada", "+1", "Grenada"),
        new("GT", "Guatemala", "+502", "Guatemala"),
        new("GN", "Guinea", "+224", "Guinea"),
        new("GW", "Guinea-Bissau", "+245", "Guinea-Bissau"),
        new("GY", "Guyana", "+592", "Guyana"),
        new("HT", "Haiti", "+509", "Haiti"),
        new("HN", "Honduras", "+504", "Honduras"),
        new("HU", "Hungary", "+36", "Hungary"),
        new("IS", "Iceland", "+354", "Iceland"),
        new("IN", "India", "+91", "India"),
        new("ID", "Indonesia", "+62", "Indonesia"),
        new("IR", "Iran", "+98", "Iran"),
        new("IQ", "Iraq", "+964", "Iraq"),
        new("IE", "Ireland", "+353", "Ireland"),
        new("IL", "Israel", "+972", "Israel"),
        new("IT", "Italy", "+39", "Italy"),
        new("CI", "Ivory Coast", "+225", "Ivory Coast"),
        new("JM", "Jamaica", "+1", "Jamaica"),
        new("JP", "Japan", "+81", "Japan"),
        new("JO", "Jordan", "+962", "Jordan"),
        new("KZ", "Kazakhstan", "+7", "Kazakhstan"),
        new("KE", "Kenya", "+254", "Kenya"),
        new("KI", "Kiribati", "+686", "Kiribati"),
        new("KW", "Kuwait", "+965", "Kuwait"),
        new("KG", "Kyrgyzstan", "+996", "Kyrgyzstan"),
        new("LA", "Lao", "+856", "Lao"),
        new("LV", "Latvia", "+371", "Latvia"),
        new("LB", "Lebanon", "+961", "Lebanon"),
        new("LS", "Lesotho", "+266", "Lesotho"),
        new("LR", "Liberia", "+231", "Liberia"),
        new("LY", "Libya", "+218", "Libya"),
        new("LI", "Liechtenstein", "+423", "Liechtenstein"),
        new("LT", "Lithuania", "+370", "Lithuania"),
        new("LU", "Luxembourg", "+352", "Luxembourg"),
        new("MG", "Madagascar", "+261", "Madagascar"),
        new("MW", "Malawi", "+265", "Malawi"),
        new("MY", "Malaysia", "+60", "Malaysia"),
        new("MV", "Maldives", "+960", "Maldives"),
        new("ML", "Mali", "+223", "Mali"),
        new("MT", "Malta", "+356", "Malta"),
        new("MH", "Marshall Islands", "+692", "Marshall Islands"),
        new("MR", "Mauritania", "+222", "Mauritania"),
        new("MU", "Mauritius", "+230", "Mauritius"),
        new("MX", "Mexico", "+52", "Mexico"),
        new("FM", "Micronesia", "+691", "Micronesia"),
        new("MD", "Moldova", "+373", "Moldova"),
        new("MC", "Monaco", "+377", "Monaco"),
        new("MN", "Mongolia", "+976", "Mongolia"),
        new("ME", "Montenegro", "+382", "Montenegro"),
        new("MA", "Morocco", "+212", "Morocco"),
        new("MZ", "Mozambique", "+258", "Mozambique"),
        new("MM", "Myanmar", "+95", "Myanmar"),
        new("NA", "Namibia", "+264", "Namibia"),
        new("NR", "Nauru", "+674", "Nauru"),
        new("NP", "Nepal", "+977", "Nepal"),
        new("NL", "Netherlands", "+31", "Netherlands"),
        new("NZ", "New Zealand", "+64", "New Zealand"),
        new("NI", "Nicaragua", "+505", "Nicaragua"),
        new("NE", "Niger", "+227", "Niger"),
        new("NG", "Nigeria", "+234", "Nigeria"),
        new("KP", "North Korea", "+850", "North Korea"),
        new("MK", "North Macedonia", "+389", "North Macedonia"),
        new("NO", "Norway", "+47", "Norway"),
        new("OM", "Oman", "+968", "Oman"),
        new("PK", "Pakistan", "+92", "Pakistan"),
        new("PW", "Palau", "+680", "Palau"),
        new("PA", "Panama", "+507", "Panama"),
        new("PG", "Papua New Guinea", "+675", "Papua New Guinea"),
        new("PY", "Paraguay", "+595", "Paraguay"),
        new("PE", "Peru", "+51", "Peru"),
        new("PH", "Philippines", "+63", "Philippines"),
        new("PL", "Poland", "+48", "Poland"),
        new("PT", "Portugal", "+351", "Portugal"),
        new("QA", "Qatar", "+974", "Qatar"),
        new("RO", "Romania", "+40", "Romania"),
        new("RU", "Russia", "+7", "Russia"),
        new("RW", "Rwanda", "+250", "Rwanda"),
        new("KN", "Saint Kitts and Nevis", "+1", "Saint Kitts and Nevis"),
        new("LC", "Saint Lucia", "+1", "Saint Lucia"),
        new("VC", "Saint Vincent and the Grenadines", "+1", "Saint Vincent and the Grenadines"),
        new("WS", "Samoa", "+685", "Samoa"),
        new("SM", "San Marino", "+378", "San Marino"),
        new("ST", "Sao Tome and Principe", "+239", "Sao Tome and Principe"),
        new("SA", "Saudi Arabia", "+966", "Saudi Arabia"),
        new("SN", "Senegal", "+221", "Senegal"),
        new("RS", "Serbia", "+381", "Serbia"),
        new("SC", "Seychelles", "+248", "Seychelles"),
        new("SL", "Sierra Leone", "+232", "Sierra Leone"),
        new("SG", "Singapore", "+65", "Singapore"),
        new("SK", "Slovakia", "+421", "Slovakia"),
        new("SI", "Slovenia", "+386", "Slovenia"),
        new("SB", "Solomon Islands", "+677", "Solomon Islands"),
        new("SO", "Somalia", "+252", "Somalia"),
        new("ZA", "South Africa", "+27", "South Africa"),
        new("KR", "South Korea", "+82", "South Korea"),
        new("SS", "South Sudan", "+211", "South Sudan"),
        new("ES", "Spain", "+34", "Spain"),
        new("LK", "Sri Lanka", "+94", "Sri Lanka"),
        new("SD", "Sudan", "+249", "Sudan"),
        new("SR", "Suriname", "+597", "Suriname"),
        new("SE", "Sweden", "+46", "Sweden"),
        new("CH", "Switzerland", "+41", "Switzerland"),
        new("SY", "Syria", "+963", "Syria"),
        new("TW", "Taiwan", "+886", "Taiwan"),
        new("TJ", "Tajikistan", "+992", "Tajikistan"),
        new("TZ", "Tanzania", "+255", "Tanzania"),
        new("TH", "Thailand", "+66", "Thailand"),
        new("CD", "The Democratic Republic of the Congo", "+243", "The Democratic Republic of the Congo"),
        new("CG", "The Republic of the Congo", "+242", "The Republic of the Congo"),
        new("TL", "Timor-Leste", "+670", "Timor-Leste"),
        new("TG", "Togo", "+228", "Togo"),
        new("TO", "Tonga", "+676", "Tonga"),
        new("TT", "Trinidad and Tobago", "+1", "Trinidad and Tobago"),
        new("TN", "Tunisia", "+216", "Tunisia"),
        new("TR", "Turkey", "+90", "Turkey"),
        new("TM", "Turkmenistan", "+993", "Turkmenistan"),
        new("TV", "Tuvalu", "+688", "Tuvalu"),
        new("UG", "Uganda", "+256", "Uganda"),
        new("UA", "Ukraine", "+380", "Ukraine"),
        new("AE", "United Arab Emirates", "+971", "United Arab Emirates"),
        new("GB", "United Kingdom", "+44", "United Kingdom of Great Britain and Northern Ireland"),
        new("US", "United States", "+1", "United States of America"),
        new("UY", "Uruguay", "+598", "Uruguay"),
        new("UZ", "Uzbekistan", "+998", "Uzbekistan"),
        new("VU", "Vanuatu", "+678", "Vanuatu"),
        new("VE", "Venezuela", "+58", "Venezuela"),
        new("VN", "Vietnam", "+84", "Vietnam"),
        new("EH", "Western Sahara", "+212", "Western Sahara"),
        new("YE", "Yemen", "+967", "Yemen"),
        new("ZM", "Zambia", "+260", "Zambia"),
        new("ZW", "Zimbabwe", "+263", "Zimbabwe")
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
        }

        if (_countrySearchBox != null)
        {
            _countrySearchBox.GotFocus += OnCountrySearchBoxGotFocus;
            _countrySearchBox.KeyDown += OnCountrySearchBoxKeyDown;
        }

        _countryScrollViewer = this.FindControl<ScrollViewer>("CountryScrollViewer");
        if (_countryScrollViewer != null)
        {
            // Prevent scroll events from bubbling up to parent (modal)
            _countryScrollViewer.PointerWheelChanged += OnCountryScrollViewerPointerWheelChanged;
        }
    }

    private void OnCountryScrollViewerPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        // Mark event as handled to prevent it from bubbling up to parent scroll viewers
        e.Handled = true;
    }

    private void OnPhoneNumberTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_isFormattingPhone || _phoneNumberBox == null)
            return;

        _isFormattingPhone = true;

        var currentText = _phoneNumberBox.Text ?? string.Empty;
        var caretIndex = _phoneNumberBox.CaretIndex;

        // Extract digits and limit to max allowed (10 phone + 5 extension)
        var rawDigits = ExtractDigits(currentText);
        if (rawDigits.Length > 10 + MaxExtensionDigits)
        {
            rawDigits = rawDigits[..(10 + MaxExtensionDigits)];
        }
        var formatted = FormatPhoneNumber(rawDigits);

        // Only update if different
        if (formatted != currentText)
        {
            // Calculate new caret position
            var digitsBeforeCaret = ExtractDigits(currentText[..Math.Min(caretIndex, currentText.Length)]).Length;

            _formattedPhoneNumber = formatted;
            _phoneNumberBox.Text = formatted;

            // Find position in formatted string that corresponds to same number of digits
            var newCaretPos = 0;
            var digitCount = 0;
            for (int i = 0; i < formatted.Length && digitCount < digitsBeforeCaret; i++)
            {
                if (char.IsDigit(formatted[i]))
                    digitCount++;
                newCaretPos = i + 1;
            }

            _phoneNumberBox.CaretIndex = Math.Min(newCaretPos, formatted.Length);
        }

        PhoneNumber = rawDigits;
        UpdateFullPhoneNumber();
        RaisePropertyChanged(nameof(FormattedPhoneNumber));

        _isFormattingPhone = false;
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

        // Handle numbers longer than 10 digits (extensions with limit)
        if (digits.Length > 10)
        {
            sb.Append(" x");
            var extensionEnd = Math.Min(digits.Length, 10 + MaxExtensionDigits);
            sb.Append(digits[10..extensionEnd]);
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
    private IImage? _flagImage;

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
    /// Gets the flag image loaded from resources.
    /// </summary>
    public IImage? FlagImage
    {
        get
        {
            if (_flagImage == null)
            {
                try
                {
                    var uri = new Uri(FlagPath);
                    var assets = Avalonia.Platform.AssetLoader.Open(uri);
                    _flagImage = new Bitmap(assets);
                }
                catch
                {
                    // Flag not found, return null
                }
            }
            return _flagImage;
        }
    }

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
