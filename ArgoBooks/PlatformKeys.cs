using Avalonia.Input;

namespace ArgoBooks;

/// <summary>
/// Platform-correct shortcut modifiers.
///
/// Avalonia maps the Mac's Command key to <see cref="KeyModifiers.Meta"/>, never to
/// <see cref="KeyModifiers.Control"/>. A shortcut written against Control alone is
/// therefore unreachable on macOS: no one holds the physical Control key to save a
/// file, so the binding is simply dead there.
/// </summary>
public static class PlatformKeys
{
    /// <summary>Command on macOS, Ctrl on Windows and Linux.</summary>
    public static KeyModifiers Command { get; } =
        OperatingSystem.IsMacOS() ? KeyModifiers.Meta : KeyModifiers.Control;

    /// <summary>
    /// How the modifier is written in the UI: "Cmd+" on macOS, "Ctrl+" elsewhere.
    ///
    /// Spelled out rather than the conventional U+2318 glyph on purpose. The app loads
    /// Inter as its default font, Inter has no glyph there, and the fallback on macOS is
    /// what turned the sample banner's dismiss button into stacked bars. Three letters
    /// always render.
    /// </summary>
    public static string CommandLabel { get; } = OperatingSystem.IsMacOS() ? "Cmd+" : "Ctrl+";

    /// <summary>True while the platform's shortcut modifier is held.</summary>
    public static bool HasCommand(this KeyModifiers modifiers) => modifiers.HasFlag(Command);

    /// <summary>
    /// True while a wheel gesture should zoom rather than scroll.
    ///
    /// Control counts on every platform, not only where it is <see cref="Command"/>: a
    /// trackpad pinch on macOS arrives as a wheel event carrying Control, so ignoring it
    /// there would trade working pinch-to-zoom for nothing.
    /// </summary>
    public static bool HasZoomModifier(this KeyModifiers modifiers) =>
        modifiers.HasFlag(Command) || modifiers.HasFlag(KeyModifiers.Control);
}
