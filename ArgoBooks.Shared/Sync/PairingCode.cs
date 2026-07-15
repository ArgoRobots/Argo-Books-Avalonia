using System.Text;

namespace ArgoBooks.Shared.Sync;

/// <summary>
/// Helpers for the human-typed short pairing code: an 8-character code drawn from an alphabet
/// that omits visually-ambiguous characters (0/1/I/L/O/U), displayed as "XXXX-XXXX".
/// </summary>
public static class PairingCode
{
    public const string Alphabet = "23456789ABCDEFGHJKMNPQRSTVWXYZ";

    /// <summary>Uppercases the input and drops any character not in <see cref="Alphabet"/> (spaces, dashes, ambiguous letters/digits).</summary>
    public static string Normalize(string input)
    {
        var sb = new StringBuilder(input.Length);
        foreach (var c in input.ToUpperInvariant())
        {
            if (Alphabet.IndexOf(c) >= 0)
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }

    /// <summary>Best-effort display formatting: inserts a dash after the 4th character when the code is exactly 8 characters long; otherwise returns the input unchanged.</summary>
    public static string Format(string code)
        => code.Length == 8 ? $"{code[..4]}-{code[4..]}" : code;
}
