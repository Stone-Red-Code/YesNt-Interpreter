using System;
using System.Globalization;
using System.Text;

namespace YesNt.Interpreter.Utilities;

public static class StringExtentions
{
    public static string ToSafeString(this string input)
    {
        StringBuilder output = new StringBuilder();
        foreach (char c in input)
        {
            _ = output.Append($"\v{c}\v");
        }
        return output.ToString();
    }

    public static string FromSafeString(this string input)
    {
        return input.Replace("\v", "");
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