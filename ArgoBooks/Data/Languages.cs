namespace ArgoBooks.Data;

/// <summary>
/// Provides a shared list of supported languages.
/// </summary>
public static class Languages
{
    /// <summary>
    /// Priority/common languages shown at the top of dropdowns.
    /// </summary>
    public static readonly IReadOnlyList<string> Priority =
    [
        "English",
        "French",
        "German",
        "Italian"
    ];

    /// <summary>
    /// Complete list of supported languages.
    /// </summary>
    public static readonly IReadOnlyList<string> All =
    [
        "Albanian",
        "Arabic",
        "Basque",
        "Belarusian",
        "Bengali",
        "Bosnian",
        "Bulgarian",
        "Catalan",
        "Chinese (Simplified)",
        "Chinese (Traditional)",
        "Croatian",
        "Czech",
        "Danish",
        "Dutch",
        "English",
        "Estonian",
        "Filipino",
        "Finnish",
        "French",
        "Galician",
        "German",
        "Greek",
        "Hebrew",
        "Hindi",
        "Hungarian",
        "Icelandic",
        "Indonesian",
        "Irish",
        "Italian",
        "Japanese",
        "Korean",
        "Latvian",
        "Lithuanian",
        "Luxembourgish",
        "Macedonian",
        "Malay",
        "Maltese",
        "Norwegian",
        "Persian",
        "Polish",
        "Portuguese",
        "Romanian",
        "Russian",
        "Serbian",
        "Slovak",
        "Slovenian",
        "Spanish",
        "Swahili",
        "Swedish",
        "Thai",
        "Turkish",
        "Ukrainian",
        "Urdu",
        "Vietnamese"
    ];
}
