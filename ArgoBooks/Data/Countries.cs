namespace ArgoBooks.Data;

/// <summary>
/// Provides a shared list of countries with their dial codes and phone formats.
/// </summary>
public static class Countries
{
    /// <summary>
    /// ISO codes for priority/common countries shown at the top of country lists.
    /// </summary>
    private static readonly string[] PriorityCodes =
    [
        "US", // United States
        "CN", // China
        "DE", // Germany
        "JP", // Japan
        "GB", // United Kingdom
        "FR", // France
        "IT", // Italy
        "CA"  // Canada
    ];

    /// <summary>
    /// Complete list of country data with dial codes and phone format patterns.
    /// </summary>
    public static readonly IReadOnlyList<CountryInfo> All =
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

    /// <summary>
    /// Gets just the country names for simple dropdowns.
    /// </summary>
    public static IReadOnlyList<string> Names { get; } = All.Select(c => c.Name).ToList();

    /// <summary>
    /// Gets the priority/common countries that should appear at the top of country lists.
    /// </summary>
    public static IReadOnlyList<CountryInfo> Priority { get; } =
        PriorityCodes
            .Select(code => All.First(c => c.Code == code))
            .ToList();

    /// <summary>
    /// Gets all countries with priority countries listed first, followed by all countries alphabetically.
    /// </summary>
    public static IReadOnlyList<CountryInfo> AllWithPriorityFirst { get; } =
        Priority.Concat(All).ToList();

    /// <summary>
    /// Checks if a country is in the priority list.
    /// </summary>
    public static bool IsPriority(string code) =>
        PriorityCodes.Contains(code, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Finds a country by name.
    /// </summary>
    public static CountryInfo? FindByName(string name) =>
        All.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Finds a country by ISO code.
    /// </summary>
    public static CountryInfo? FindByCode(string code) =>
        All.FirstOrDefault(c => c.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
}

/// <summary>
/// Represents country information including dial code and phone format.
/// </summary>
public class CountryInfo
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
    /// Phone number format pattern using X for digits.
    /// </summary>
    public string PhoneFormat { get; }

    /// <summary>
    /// Flag file name (defaults to Name if not specified).
    /// </summary>
    public string FlagFileName { get; }

    public CountryInfo(string code, string name, string dialCode, string phoneFormat, string? flagFileName = null)
    {
        Code = code;
        Name = name;
        DialCode = dialCode;
        PhoneFormat = phoneFormat;
        FlagFileName = flagFileName ?? name;
    }

    public override string ToString() => Name;
}
