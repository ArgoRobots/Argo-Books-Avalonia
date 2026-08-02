using ArgoBooks.Shared.Sync;
using Avalonia.Controls;

namespace ArgoBooks.Mobile.Views;

public partial class PairingView : UserControl
{
    private bool _formattingCode;
    private int _prevDisplayLen;

    public PairingView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Live-formats the pairing code to "XXXX-XXXX" as the user types: the dash appears as soon as
    /// the first four characters are entered, and backspacing through it removes it cleanly.
    /// Done in the view because Avalonia's TextBox does not reliably re-render when its bound
    /// property is rewritten from inside its own change notification.
    /// </summary>
    private void OnCodeTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_formattingCode || sender is not TextBox tb)
        {
            return;
        }

        var raw = tb.Text ?? string.Empty;
        // A shorter raw string than what we last displayed means the user is deleting; used so the
        // trailing dash after the 4th character is only added while typing forward.
        var deleting = raw.Length < _prevDisplayLen;

        var code = PairingCode.Normalize(raw);
        if (code.Length > 8)
        {
            code = code[..8];
        }

        var formatted = FormatLive(code, deleting);
        _prevDisplayLen = formatted.Length;

        if (formatted != raw)
        {
            _formattingCode = true;
            tb.Text = formatted;
            tb.CaretIndex = formatted.Length;
            _formattingCode = false;
        }
    }

    /// <summary>Groups the (already-normalized, max-8) code as "XXXX-XXXX", showing the dash once
    /// four characters exist. The trailing dash is only added while typing forward, so backspacing
    /// past it removes it instead of immediately re-adding it.</summary>
    private static string FormatLive(string code, bool deleting)
    {
        if (code.Length < 4)
        {
            return code;
        }
        if (code.Length == 4)
        {
            return deleting ? code : code + "-";
        }
        return code[..4] + "-" + code[4..];
    }
}
