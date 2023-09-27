using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;

namespace YesNt.Interpreter.Utilities;

public static class StringExtentions
{
    private static readonly Dictionary<string, string> reverseReplacementRules;

    public static Dictionary<string, string> ReplacementRules { get; } = new()
    {
        {"~", "~til" },
        {" ", "~spc" },
        {"%", "~per" },
        {"<", "~let" },
        {">", "~grt" },
        {",", "~com" },
        {"!", "~exm" },
        {"|", "~pip" }
    };

    [SuppressMessage("Minor Code Smell", "S3963:\"static\" fields should be initialized inline", Justification = "Doesn't work because it throws a TypeInitializationException")]
    static StringExtentions()
    {
        reverseReplacementRules = ReplacementRules.ToDictionary(x => x.Value, x => x.Key);
    }

    public static string ToSafeString(this string input)
    {
        StringBuilder output = new StringBuilder();
        foreach (char c in input)
        {
            _ = output.Append($"\v{c}\v");
        }

        return ReplaceOnce(output.ToString(), ReplacementRules);
    }

    public static string FromSafeString(this string input)
    {
        return ReplaceOnce(input.Replace("\v", ""), reverseReplacementRules);
    }

    public static string ReplaceOnce(string input, Dictionary<string, string> replacementRules)
    {
        IEnumerable<KeyValuePair<string, string>> matches = replacementRules.Where(rule => input.Contains(rule.Key));
        if (!matches.Any())
        {
            return input;
        }

        KeyValuePair<string, string> match = matches.First();
        int startIndex = input.IndexOf(match.Key);
        int endIndex = startIndex + match.Key.Length;

        string before = ReplaceOnce(input[..startIndex], replacementRules);
        string replaced = match.Value;
        string after = ReplaceOnce(input[endIndex..], replacementRules);

        return before + replaced + after;
    }

    public static bool ToStandardizedNumber(this string input, out double result)
    {
        return double.TryParse(input.FromSafeString().Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out result);
    }

    public static string ReplaceFirstOccurrence(this string input, string oldValue, string newValue)
    {
        int place = input.IndexOf(oldValue);
        return input.Remove(place, oldValue.Length).Insert(place, newValue);
    }

    public static string ReplaceLastOccurrence(this string input, string oldValue, string newValue)
    {
        int place = input.LastIndexOf(oldValue);
        return input.Remove(place, Math.Min(oldValue.Length, input.Length - place)).Insert(place, newValue);
    }

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
}