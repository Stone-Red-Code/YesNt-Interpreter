using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace YesNt.Interpreter.Utilities;

/// <summary>
/// Provides expression evaluation used by conditional and arithmetic statements.
/// </summary>
internal static partial class Evaluator
{
    /// <summary>
    /// Evaluates a boolean condition string such as <c>a == b</c>, <c>x &gt; 3</c>, or <c>true</c>.
    /// </summary>
    /// <param name="input">The condition expression, which may contain safe-string encoded values.</param>
    /// <returns>
    /// <see langword="true"/> or <see langword="false"/> if the condition could be evaluated;
    /// <see langword="null"/> if the expression is not a recognized condition form (treated as an error by callers).
    /// </returns>
    public static bool? EvaluateCondition(string input)
    {
        input = input.FromSafeString();
        string lower = input.ToLower().Trim();
        if (lower == "true")
        {
            return true;
        }
        else if (lower == "false")
        {
            return false;
        }

        string[] parts = input.Split("==");
        if (parts.Length == 2)
        {
            string part1 = parts[0].Trim();
            string part2 = parts[1].Trim();
            return part1 == part2;
        }

        parts = input.Split("!=");
        if (parts.Length == 2)
        {
            string part1 = parts[0].Trim();
            string part2 = parts[1].Trim();
            return part1 != part2;
        }

        parts = input.Split(">=");
        if (parts.Length == 2)
        {
            bool succ1 = parts[0].ToStandardizedNumber(out double part1);
            bool succ2 = parts[1].ToStandardizedNumber(out double part2);
            return succ1 && succ2 && part1 >= part2;
        }

        parts = input.Split("<=");
        if (parts.Length == 2)
        {
            bool succ1 = parts[0].ToStandardizedNumber(out double part1);
            bool succ2 = parts[1].ToStandardizedNumber(out double part2);
            return succ1 && succ2 && part1 <= part2;
        }

        parts = input.Split(">");
        if (parts.Length == 2)
        {
            bool succ1 = parts[0].ToStandardizedNumber(out double part1);
            bool succ2 = parts[1].ToStandardizedNumber(out double part2);
            return succ1 && succ2 && part1 > part2;
        }

        parts = input.Split("<");
        if (parts.Length == 2)
        {
            bool succ1 = parts[0].ToStandardizedNumber(out double part1);
            bool succ2 = parts[1].ToStandardizedNumber(out double part2);
            return succ1 && succ2 && part1 < part2;
        }

        return null;
    }

    /// <summary>
    /// Evaluates a numeric arithmetic expression string and returns the result as a string.
    /// Supports <c>+</c>, <c>-</c>, <c>*</c>, <c>/</c>, <c>%</c> (modulo), and <c>^</c> (power) operators
    /// with standard precedence (<c>^</c> highest, <c>+</c>/<c>-</c> lowest) and parentheses.
    /// Adjacent sign characters (<c>++</c>, <c>--</c>, <c>-+</c>, <c>+-</c>) are normalized before evaluation.
    /// </summary>
    /// <param name="input">The arithmetic expression to evaluate.</param>
    /// <returns>The result as a culture-invariant numeric string, or <c>"NaN"</c> if evaluation failed.</returns>
    public static string Calculate(string input)
    {
        input = input.FromSafeString();
        input = PlusPlusRegex().Replace(input, "+");
        input = MinusMinusRegex().Replace(input, "+");
        input = MinusPlusRegex().Replace(input, "-");
        input = PlusMinusRegex().Replace(input, "-");

        return CalculateInternal(input, '+');
    }

    private static string CalculateInternal(string input, char op)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        if (input.ToStandardizedNumber(out double quickNum))
        {
            return quickNum.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        MatchCollection matches = ParenthesesRegex().Matches(input);
        while (matches.Count > 0)
        {
            for (int i = 0; i < matches.Count; i++)
            {
                string calc = matches[i].Value.Substring(1, matches[i].Length - 2);
                string ret = CalculateInternal(calc, '+');
                input = input.Replace(matches[i].Value, ret);
            }
            matches = ParenthesesRegex().Matches(input);
        }

        string[] parts = input.Split(op);

        // If the expression starts with the operator (e.g. "-3 + 5" split by '-' gives ["", "3 + 5"]),
        // prepend the operator back onto the first real part so it isn't lost.
        if (parts.Length >= 2 && string.IsNullOrWhiteSpace(parts[0]))
        {
            parts[1] = $"{op}{parts[1]}";
            parts = parts.Skip(1).ToArray();
        }

        double number = double.NaN;

        foreach (string p in parts)
        {
            string part = p;

            part = op switch
            {
                '+' => CalculateInternal(part, '-'),
                '-' => CalculateInternal(part, '*'),
                '*' => CalculateInternal(part, '/'),
                '/' => CalculateInternal(part, '%'),
                '%' => CalculateInternal(part, '^'),
                _ => part
            };

            if (part is null)
            {
                return null;
            }

            if (part.ToStandardizedNumber(out double num))
            {
                if (double.IsNaN(number))
                {
                    number = num;
                }
                else
                {
                    switch (op)
                    {
                        case '+':
                            number += num;
                            break;

                        case '-':
                            number -= num;
                            break;

                        case '*':
                            number *= num;
                            break;

                        case '/':
                            number /= num;
                            break;

                        case '%':
                            number %= num;
                            break;

                        case '^':
                            number = Math.Pow(number, num);
                            break;
                    }
                }
            }
            else
            {
                return null;
            }
        }

        return number.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    [GeneratedRegex("\\(([^()]+)\\)")]
    private static partial Regex ParenthesesRegex();

    [GeneratedRegex("(\\+ +\\+)+")]
    private static partial Regex PlusPlusRegex();

    [GeneratedRegex("(\\- +\\-)+")]
    private static partial Regex MinusMinusRegex();

    [GeneratedRegex("(\\- +\\+)+")]
    private static partial Regex MinusPlusRegex();

    [GeneratedRegex("(\\+ +\\-)+")]
    private static partial Regex PlusMinusRegex();
}