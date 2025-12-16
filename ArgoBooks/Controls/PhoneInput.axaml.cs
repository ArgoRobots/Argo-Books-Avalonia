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
    /// Complete list of country dial codes with phone format patterns.
    /// </summary>
    public static readonly List<CountryDialCode> AllDialCodes =
    [
        new("AF", "Afghanistan", "+93", "XX XXX XXXX"),
        new("AL", "Albania", "+355", "XX XXX XXXX"),
        new("DZ", "Algeria", "+213", "XXX XX XX XX"),
        new("AD", "Andorra", "+376", "XXX XXX"),
        new("AO", "Angola", "+244", "XXX XXX XXX"),
        new("AG", "Antigua and Barbuda", "+1", "XXX-XXXX"),
        new("AR", "Argentina", "+54", "XX XXXX-XXXX"),
        new("AM", "Armenia", "+374", "XX XXX XXX"),
        new("AU", "Australia", "+61", "XXXX XXX XXX"),
        new("AT", "Austria", "+43", "XXX XXXXXXX"),
        new("AZ", "Azerbaijan", "+994", "XX XXX XX XX"),
        new("BS", "Bahamas", "+1", "XXX-XXXX"),
        new("BH", "Bahrain", "+973", "XXXX XXXX"),
        new("BD", "Bangladesh", "+880", "XXXX-XXXXXX"),
        new("BB", "Barbados", "+1", "XXX-XXXX"),
        new("BY", "Belarus", "+375", "XX XXX-XX-XX"),
        new("BE", "Belgium", "+32", "XXX XX XX XX"),
        new("BZ", "Belize", "+501", "XXX-XXXX"),
        new("BJ", "Benin", "+229", "XX XX XX XX"),
        new("BT", "Bhutan", "+975", "XX XXX XXX"),
        new("BO", "Bolivia", "+591", "X XXX XXXX"),
        new("BA", "Bosnia and Herzegovina", "+387", "XX XXX-XXX"),
        new("BW", "Botswana", "+267", "XX XXX XXX"),
        new("BR", "Brazil", "+55", "(XX) XXXXX-XXXX"),
        new("BN", "Brunei", "+673", "XXX XXXX"),
        new("BG", "Bulgaria", "+359", "XX XXX XXXX"),
        new("BF", "Burkina Faso", "+226", "XX XX XX XX"),
        new("BI", "Burundi", "+257", "XX XX XXXX"),
        new("CV", "Cabo Verde", "+238", "XXX XX XX"),
        new("KH", "Cambodia", "+855", "XX XXX XXX"),
        new("CM", "Cameroon", "+237", "X XX XX XX XX"),
        new("CA", "Canada", "+1", "(XXX) XXX-XXXX"),
        new("CF", "Central African Republic", "+236", "XX XX XX XX"),
        new("TD", "Chad", "+235", "XX XX XX XX"),
        new("CL", "Chile", "+56", "X XXXX XXXX"),
        new("CN", "China", "+86", "XXX XXXX XXXX"),
        new("CO", "Colombia", "+57", "XXX XXX XXXX"),
        new("KM", "Comoros", "+269", "XXX XX XX"),
        new("CR", "Costa Rica", "+506", "XXXX XXXX"),
        new("HR", "Croatia", "+385", "XX XXX XXXX"),
        new("CU", "Cuba", "+53", "X XXX XXXX"),
        new("CY", "Cyprus", "+357", "XX XXXXXX"),
        new("CZ", "Czechia", "+420", "XXX XXX XXX"),
        new("DK", "Denmark", "+45", "XX XX XX XX"),
        new("DJ", "Djibouti", "+253", "XX XX XX XX"),
        new("DM", "Dominica", "+1", "XXX-XXXX"),
        new("DO", "Dominican Republic", "+1", "XXX-XXXX"),
        new("EC", "Ecuador", "+593", "XX XXX XXXX"),
        new("EG", "Egypt", "+20", "XXX XXX XXXX"),
        new("SV", "El Salvador", "+503", "XXXX XXXX"),
        new("GQ", "Equatorial Guinea", "+240", "XXX XXX XXX"),
        new("ER", "Eritrea", "+291", "X XXX XXX"),
        new("EE", "Estonia", "+372", "XXXX XXXX"),
        new("SZ", "Eswatini", "+268", "XXXX XXXX"),
        new("ET", "Ethiopia", "+251", "XX XXX XXXX"),
        new("FJ", "Fiji", "+679", "XXX XXXX"),
        new("FI", "Finland", "+358", "XX XXX XXXX"),
        new("FR", "France", "+33", "X XX XX XX XX"),
        new("GA", "Gabon", "+241", "X XX XX XX"),
        new("GM", "Gambia", "+220", "XXX XXXX"),
        new("GE", "Georgia", "+995", "XXX XXX XXX"),
        new("DE", "Germany", "+49", "XXX XXXXXXXX"),
        new("GH", "Ghana", "+233", "XX XXX XXXX"),
        new("GR", "Greece", "+30", "XXX XXX XXXX"),
        new("GD", "Grenada", "+1", "XXX-XXXX"),
        new("GT", "Guatemala", "+502", "XXXX XXXX"),
        new("GN", "Guinea", "+224", "XXX XX XX XX"),
        new("GW", "Guinea-Bissau", "+245", "XXX XXXX"),
        new("GY", "Guyana", "+592", "XXX XXXX"),
        new("HT", "Haiti", "+509", "XXXX XXXX"),
        new("HN", "Honduras", "+504", "XXXX XXXX"),
        new("HU", "Hungary", "+36", "XX XXX XXXX"),
        new("IS", "Iceland", "+354", "XXX XXXX"),
        new("IN", "India", "+91", "XXXXX XXXXX"),
        new("ID", "Indonesia", "+62", "XXX XXXX XXXX"),
        new("IR", "Iran", "+98", "XXX XXX XXXX"),
        new("IQ", "Iraq", "+964", "XXX XXX XXXX"),
        new("IE", "Ireland", "+353", "XX XXX XXXX"),
        new("IL", "Israel", "+972", "XX XXX XXXX"),
        new("IT", "Italy", "+39", "XXX XXX XXXX"),
        new("CI", "Ivory Coast", "+225", "XX XX XX XXXX"),
        new("JM", "Jamaica", "+1", "XXX-XXXX"),
        new("JP", "Japan", "+81", "XX XXXX XXXX"),
        new("JO", "Jordan", "+962", "X XXXX XXXX"),
        new("KZ", "Kazakhstan", "+7", "XXX XXX XX XX"),
        new("KE", "Kenya", "+254", "XXX XXXXXX"),
        new("KI", "Kiribati", "+686", "XXXX XXXX"),
        new("KW", "Kuwait", "+965", "XXXX XXXX"),
        new("KG", "Kyrgyzstan", "+996", "XXX XXXXXX"),
        new("LA", "Lao", "+856", "XX XX XXX XXX"),
        new("LV", "Latvia", "+371", "XX XXX XXX"),
        new("LB", "Lebanon", "+961", "XX XXX XXX"),
        new("LS", "Lesotho", "+266", "XX XXX XXX"),
        new("LR", "Liberia", "+231", "XX XXX XXXX"),
        new("LY", "Libya", "+218", "XX XXX XXXX"),
        new("LI", "Liechtenstein", "+423", "XXX XXXX"),
        new("LT", "Lithuania", "+370", "XXX XXXXX"),
        new("LU", "Luxembourg", "+352", "XXX XXX XXX"),
        new("MG", "Madagascar", "+261", "XX XX XXX XX"),
        new("MW", "Malawi", "+265", "X XXXX XXXX"),
        new("MY", "Malaysia", "+60", "XX XXXX XXXX"),
        new("MV", "Maldives", "+960", "XXX XXXX"),
        new("ML", "Mali", "+223", "XX XX XX XX"),
        new("MT", "Malta", "+356", "XXXX XXXX"),
        new("MH", "Marshall Islands", "+692", "XXX-XXXX"),
        new("MR", "Mauritania", "+222", "XX XX XX XX"),
        new("MU", "Mauritius", "+230", "XXXX XXXX"),
        new("MX", "Mexico", "+52", "XXX XXX XXXX"),
        new("FM", "Micronesia", "+691", "XXX XXXX"),
        new("MD", "Moldova", "+373", "XX XXX XXX"),
        new("MC", "Monaco", "+377", "XX XX XX XX"),
        new("MN", "Mongolia", "+976", "XX XX XXXX"),
        new("ME", "Montenegro", "+382", "XX XXX XXX"),
        new("MA", "Morocco", "+212", "XX XXX XXXX"),
        new("MZ", "Mozambique", "+258", "XX XXX XXXX"),
        new("MM", "Myanmar", "+95", "XX XXX XXXX"),
        new("NA", "Namibia", "+264", "XX XXX XXXX"),
        new("NR", "Nauru", "+674", "XXX XXXX"),
        new("NP", "Nepal", "+977", "XX XXX XXXX"),
        new("NL", "Netherlands", "+31", "XX XXXXXXXX"),
        new("NZ", "New Zealand", "+64", "XX XXX XXXX"),
        new("NI", "Nicaragua", "+505", "XXXX XXXX"),
        new("NE", "Niger", "+227", "XX XX XX XX"),
        new("NG", "Nigeria", "+234", "XXX XXX XXXX"),
        new("KP", "North Korea", "+850", "XXX XXXX XXXX"),
        new("MK", "North Macedonia", "+389", "XX XXX XXX"),
        new("NO", "Norway", "+47", "XXX XX XXX"),
        new("OM", "Oman", "+968", "XXXX XXXX"),
        new("PK", "Pakistan", "+92", "XXX XXX XXXX"),
        new("PW", "Palau", "+680", "XXX XXXX"),
        new("PA", "Panama", "+507", "XXXX XXXX"),
        new("PG", "Papua New Guinea", "+675", "XXX XXXX"),
        new("PY", "Paraguay", "+595", "XXX XXX XXX"),
        new("PE", "Peru", "+51", "XXX XXX XXX"),
        new("PH", "Philippines", "+63", "XXX XXX XXXX"),
        new("PL", "Poland", "+48", "XXX XXX XXX"),
        new("PT", "Portugal", "+351", "XXX XXX XXX"),
        new("QA", "Qatar", "+974", "XXXX XXXX"),
        new("RO", "Romania", "+40", "XXX XXX XXX"),
        new("RU", "Russia", "+7", "XXX XXX-XX-XX"),
        new("RW", "Rwanda", "+250", "XXX XXX XXX"),
        new("KN", "Saint Kitts and Nevis", "+1", "XXX-XXXX"),
        new("LC", "Saint Lucia", "+1", "XXX-XXXX"),
        new("VC", "Saint Vincent and the Grenadines", "+1", "XXX-XXXX"),
        new("WS", "Samoa", "+685", "XX XXXX"),
        new("SM", "San Marino", "+378", "XXXX XXXXXX"),
        new("ST", "Sao Tome and Principe", "+239", "XXX XXXX"),
        new("SA", "Saudi Arabia", "+966", "XX XXX XXXX"),
        new("SN", "Senegal", "+221", "XX XXX XX XX"),
        new("RS", "Serbia", "+381", "XX XXX XXXX"),
        new("SC", "Seychelles", "+248", "X XXX XXX"),
        new("SL", "Sierra Leone", "+232", "XX XXXXXX"),
        new("SG", "Singapore", "+65", "XXXX XXXX"),
        new("SK", "Slovakia", "+421", "XXX XXX XXX"),
        new("SI", "Slovenia", "+386", "XX XXX XXX"),
        new("SB", "Solomon Islands", "+677", "XXXXX"),
        new("SO", "Somalia", "+252", "XX XXX XXX"),
        new("ZA", "South Africa", "+27", "XX XXX XXXX"),
        new("KR", "South Korea", "+82", "XX XXXX XXXX"),
        new("SS", "South Sudan", "+211", "XX XXX XXXX"),
        new("ES", "Spain", "+34", "XXX XXX XXX"),
        new("LK", "Sri Lanka", "+94", "XX XXX XXXX"),
        new("SD", "Sudan", "+249", "XX XXX XXXX"),
        new("SR", "Suriname", "+597", "XXX XXXX"),
        new("SE", "Sweden", "+46", "XX XXX XX XX"),
        new("CH", "Switzerland", "+41", "XX XXX XX XX"),
        new("SY", "Syria", "+963", "XX XXXX XXXX"),
        new("TW", "Taiwan", "+886", "XXXX XXXX"),
        new("TJ", "Tajikistan", "+992", "XX XXX XXXX"),
        new("TZ", "Tanzania", "+255", "XXX XXX XXX"),
        new("TH", "Thailand", "+66", "XX XXX XXXX"),
        new("CD", "The Democratic Republic of the Congo", "+243", "XXX XXX XXX"),
        new("CG", "The Republic of the Congo", "+242", "XX XXX XXXX"),
        new("TL", "Timor-Leste", "+670", "XXX XXXX"),
        new("TG", "Togo", "+228", "XX XX XX XX"),
        new("TO", "Tonga", "+676", "XXXXX"),
        new("TT", "Trinidad and Tobago", "+1", "XXX-XXXX"),
        new("TN", "Tunisia", "+216", "XX XXX XXX"),
        new("TR", "Turkey", "+90", "XXX XXX XX XX"),
        new("TM", "Turkmenistan", "+993", "XX XXXXXX"),
        new("TV", "Tuvalu", "+688", "XXXXX"),
        new("UG", "Uganda", "+256", "XXX XXXXXX"),
        new("UA", "Ukraine", "+380", "XX XXX XX XX"),
        new("AE", "United Arab Emirates", "+971", "XX XXX XXXX"),
        new("GB", "United Kingdom", "+44", "XXXX XXX XXXX", "United Kingdom of Great Britain and Northern Ireland"),
        new("US", "United States", "+1", "(XXX) XXX-XXXX", "United States of America"),
        new("UY", "Uruguay", "+598", "X XXX XXXX"),
        new("UZ", "Uzbekistan", "+998", "XX XXX XX XX"),
        new("VU", "Vanuatu", "+678", "XXXXX"),
        new("VE", "Venezuela", "+58", "XXX XXX XXXX"),
        new("VN", "Vietnam", "+84", "XX XXXX XXXX"),
        new("EH", "Western Sahara", "+212", "XX XXX XXXX"),
        new("YE", "Yemen", "+967", "XXX XXX XXX"),
        new("ZM", "Zambia", "+260", "XX XXX XXXX"),
        new("ZW", "Zimbabwe", "+263", "XX XXX XXXX")
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

        // Extract digits and limit to max allowed by country format
        var rawDigits = ExtractDigits(currentText);
        var country = SelectedCountry ?? AllDialCodes.FirstOrDefault(c => c.Code == "US");
        var maxDigits = country?.PhoneFormat.Count(c => c == 'X') ?? 10;
        if (rawDigits.Length > maxDigits)
        {
            rawDigits = rawDigits[..maxDigits];
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
    /// Formats a phone number using the selected country's format pattern.
    /// </summary>
    private string FormatPhoneNumber(string digits)
    {
        if (string.IsNullOrEmpty(digits))
            return string.Empty;

        var country = SelectedCountry ?? AllDialCodes.FirstOrDefault(c => c.Code == "US");
        if (country == null)
            return digits;

        var maxDigits = country.PhoneFormat.Count(c => c == 'X');
        var limitedDigits = digits.Length > maxDigits ? digits[..maxDigits] : digits;

        return country.FormatPhoneNumber(limitedDigits);
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
    /// Phone number format pattern using X for digits (e.g., "(XXX) XXX-XXXX").
    /// </summary>
    public string PhoneFormat { get; }

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

    public CountryDialCode(string code, string name, string dialCode, string phoneFormat, string? flagFileName = null)
    {
        Code = code;
        Name = name;
        DialCode = dialCode;
        PhoneFormat = phoneFormat;
        FlagFileName = flagFileName ?? name;
    }

    /// <summary>
    /// Formats a phone number according to this country's format pattern.
    /// </summary>
    public string FormatPhoneNumber(string digits)
    {
        if (string.IsNullOrEmpty(digits))
            return string.Empty;

        var sb = new StringBuilder();
        var digitIndex = 0;
        var maxDigits = PhoneFormat.Count(c => c == 'X');

        foreach (var ch in PhoneFormat)
        {
            if (digitIndex >= digits.Length)
                break;

            if (ch == 'X')
            {
                sb.Append(digits[digitIndex]);
                digitIndex++;
            }
            else
            {
                sb.Append(ch);
            }
        }

        // If there are remaining digits beyond the format, append them
        if (digitIndex < digits.Length && digitIndex >= maxDigits)
        {
            // Remaining digits go beyond the format pattern
            for (int i = digitIndex; i < digits.Length; i++)
            {
                sb.Append(digits[i]);
            }
        }

        return sb.ToString();
    }

    public override string ToString() => DisplayName;
}
