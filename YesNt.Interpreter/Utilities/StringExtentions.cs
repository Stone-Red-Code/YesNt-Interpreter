using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace YesNt.Interpreter.Utilities;

/// <summary>
/// Extension methods for string manipulation used throughout the interpreter.
/// </summary>
/// <remarks>
/// <para>
/// YesNt uses a "safe string" encoding to pass values through the interpreter pipeline without
/// accidentally triggering keyword matching. Special characters (spaces, operators, punctuation,
/// control characters) are replaced with SOH-delimited three-letter codes
/// (e.g. space → <c>\x01spc\x01</c>, newline → <c>\x01nli\x01</c>). These codes use the
/// non-printable SOH character (U+0001) as a sentinel. Written via string concatenation
/// (<c>"\x01" + "spc" + "\x01"</c>) to avoid C#'s greedy <c>\x</c> hex escape absorbing
/// following hex-digit letters. U+0001 cannot appear in normal user source, preventing raw
/// source text from being accidentally decoded.
/// The mapping is defined in <see cref="ReplacementRules"/>. Use <see cref="ToSafeString"/>
/// to encode and <see cref="FromSafeString"/> to decode.
/// </para>
/// </remarks>
public static class StringExtensions
{
    private static readonly Dictionary<string, string> reverseReplacementRules;

    /// <summary>
    /// Gets the table that maps special characters to their safe-string escape codes.
    /// Keys are the original characters; values are the three-letter tilde codes.
    /// </summary>
    public static Dictionary<string, string> ReplacementRules { get; } = new()
    {
        {"~",  "\x01" + "til" + "\x01" },
        {" ",  "\x01" + "spc" + "\x01" },
        {"%",  "\x01" + "per" + "\x01" },
        {"<",  "\x01" + "let" + "\x01" },
        {">",  "\x01" + "grt" + "\x01" },
        {",",  "\x01" + "com" + "\x01" },
        {"!",  "\x01" + "exm" + "\x01" },
        {"|",  "\x01" + "pip" + "\x01" },
        {"\n", "\x01" + "nli" + "\x01" },
        {"\r", "\x01" + "ret" + "\x01" },
        {"\t", "\x01" + "tab" + "\x01" },
        {"\b", "\x01" + "bac" + "\x01" },
        {"\f", "\x01" + "for" + "\x01" },
        {"\a", "\x01" + "ale" + "\x01" },
        {"",   "\x01" + "emp" + "\x01" },
    };

    static StringExtensions()
    {
        reverseReplacementRules = ReplacementRules.ToDictionary(x => x.Value, x => x.Key);
    }

    /// <summary>
    /// Encodes a string into safe-string format so that special characters cannot accidentally
    /// trigger interpreter keyword matching. Each character is wrapped with vertical-tab sentinels
    /// before rule substitution so that multi-character replacements do not overlap.
    /// </summary>
    /// <param name="input">The plain string to encode.</param>
    /// <returns>The safe-string encoded representation.</returns>
    public static string ToSafeString(this string input)
    {
        StringBuilder output = new StringBuilder();
        foreach (char c in input)
        {
            _ = output.Append($"\v{c}\v");
        }

        return ReplaceOnce(output.ToString(), ReplacementRules);
    }

    /// <summary>
    /// Decodes a safe-string back to its original plain-text form.
    /// </summary>
    /// <param name="input">A safe-string encoded string.</param>
    /// <returns>The decoded plain string.</returns>
    public static string FromSafeString(this string input)
    {
        return ReplaceOnce(input.Replace("\v", string.Empty), reverseReplacementRules);
    }

    /// <summary>
    /// Tries to parse the string as a <see cref="double"/>, first decoding safe-string encoding
    /// and normalising decimal separators (comma → period).
    /// </summary>
    /// <param name="input">The string to parse (may be safe-string encoded).</param>
    /// <param name="result">When this method returns, contains the parsed value if successful.</param>
    /// <returns><see langword="true"/> if parsing succeeded; otherwise <see langword="false"/>.</returns>
    public static bool ToStandardizedNumber(this string input, out double result)
    {
        return double.TryParse(input.FromSafeString().Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out result);
    }

    /// <summary>Replaces only the first occurrence of <paramref name="oldValue"/> in the string.</summary>
    /// <param name="input">The source string.</param>
    /// <param name="oldValue">The substring to find.</param>
    /// <param name="newValue">The replacement value.</param>
    /// <returns>A new string with the first occurrence replaced.</returns>
    public static string ReplaceFirstOccurrence(this string input, string oldValue, string newValue)
    {
        int place = input.IndexOf(oldValue);
        return input.Remove(place, oldValue.Length).Insert(place, newValue);
    }

    /// <summary>Replaces only the last occurrence of <paramref name="oldValue"/> in the string.</summary>
    /// <param name="input">The source string.</param>
    /// <param name="oldValue">The substring to find.</param>
    /// <param name="newValue">The replacement value.</param>
    /// <returns>A new string with the last occurrence replaced.</returns>
    public static string ReplaceLastOccurrence(this string input, string oldValue, string newValue)
    {
        int place = input.LastIndexOf(oldValue);
        return input.Remove(place, Math.Min(oldValue.Length, input.Length - place)).Insert(place, newValue);
    }

    /// <summary>Counts the number of trailing whitespace characters in the string.</summary>
    /// <param name="input">The source string.</param>
    /// <returns>The number of whitespace characters at the end of the string.</returns>
    public static int WhiteSpaceAtEnd(this string input)
    {
        int count = 0;
        int index = input.Length - 1;
        while (index >= 0 && char.IsWhiteSpace(input[index--]))
        {
            count++;
        }

        return count;
    }

    private static string ReplaceOnce(string input, Dictionary<string, string> replacementRules)
    {
        // \x01emp\x01/string.Empty is a special case, it is used to represent empty strings and won't work with the normal rules because an empty string always matches and causes an infinite loop 3 letter abbreviation
        IEnumerable<KeyValuePair<string, string>> matches = replacementRules.Where(rule => rule.Key != string.Empty && input.Contains(rule.Key, StringComparison.Ordinal));
        if (!matches.Any())
        {
            return input;
        }

        KeyValuePair<string, string> match = matches.First();
        int startIndex = input.IndexOf(match.Key, StringComparison.Ordinal);
        int endIndex = startIndex + match.Key.Length;

        string before = ReplaceOnce(input[..startIndex], replacementRules);
        string replaced = match.Value;
        string after = ReplaceOnce(input[endIndex..], replacementRules);

        return before + replaced + after;
    }
}