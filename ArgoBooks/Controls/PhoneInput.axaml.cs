using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;
using ArgoBooks.Data;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
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
    /// Generated from the shared Countries data.
    /// </summary>
    public static readonly List<CountryDialCode> AllDialCodes =
        Countries.All.Select(c => new CountryDialCode(c.Code, c.Name, c.DialCode, c.PhoneFormat, c.FlagFileName)).ToList();

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
            ReformatPhoneNumberForNewCountry();
            UpdatePhoneNumberPlaceholder();
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
            UpdatePhoneNumberPlaceholder();
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

    private void ReformatPhoneNumberForNewCountry()
    {
        if (_phoneNumberBox == null || _isFormattingPhone)
            return;

        _isFormattingPhone = true;

        // Get current digits and reformat with new country's format
        var currentText = _phoneNumberBox.Text ?? string.Empty;
        var rawDigits = ExtractDigits(currentText);

        if (!string.IsNullOrEmpty(rawDigits))
        {
            var country = SelectedCountry ?? AllDialCodes.FirstOrDefault(c => c.Code == "US");
            var maxDigits = country?.PhoneFormat.Count(c => c == 'X') ?? 10;

            // Limit digits to new format's max
            if (rawDigits.Length > maxDigits)
            {
                rawDigits = rawDigits[..maxDigits];
            }

            var formatted = FormatPhoneNumber(rawDigits);
            _formattedPhoneNumber = formatted;
            _phoneNumberBox.Text = formatted;
            PhoneNumber = rawDigits;
            RaisePropertyChanged(nameof(FormattedPhoneNumber));
        }

        _isFormattingPhone = false;
    }

    private void UpdatePhoneNumberPlaceholder()
    {
        if (_phoneNumberBox == null)
            return;

        var country = SelectedCountry ?? AllDialCodes.FirstOrDefault(c => c.Code == "US");
        if (country != null)
        {
            // Generate example placeholder by replacing X with sample digits
            var placeholder = country.PhoneFormat;
            var digitIndex = 1;
            var result = new StringBuilder();

            foreach (var ch in placeholder)
            {
                if (ch == 'X')
                {
                    result.Append((digitIndex % 10).ToString());
                    digitIndex++;
                }
                else
                {
                    result.Append(ch);
                }
            }

            _phoneNumberBox.Watermark = result.ToString();
        }
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

        var searchText = _countrySearchText.Trim().ToLowerInvariant();

        IEnumerable<CountryDialCode> filtered;

        if (string.IsNullOrEmpty(searchText) || searchText.StartsWith('+'))
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
