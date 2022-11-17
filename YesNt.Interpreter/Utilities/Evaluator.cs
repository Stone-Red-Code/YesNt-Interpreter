using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace YesNt.Interpreter.Utilities;

internal static partial class Evaluator
{
    public static bool? EvaluateCondition(string input)
    {
        if (input.ToLower().FromSafeString().Trim() == "true")
        {
            return true;
        }
        else if (input.ToLower().FromSafeString().Trim() == "false")
        {
            return false;
        }

        string[] parts = input.Split("==");
        if (parts.Length == 2)
        {
            string part1 = parts[0].FromSafeString().Trim();
            string part2 = parts[1].FromSafeString().Trim();
            return part1 == part2;
        }

        parts = input.Split("!=");
        if (parts.Length == 2)
        {
            string part1 = parts[0].FromSafeString().Trim();
            string part2 = parts[1].FromSafeString().Trim();
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

    public static string Calculate(string input)
    {
        input = PlusPlusRegex().Replace(input, "+");
        input = MinusMinusRegex().Replace(input, "+");
        input = MinusPlusRegex().Replace(input, "-");
        input = PlusMinusRegex().Replace(input, "-");

        string yes = Calculate(input, '+');
        return yes;
    }

    private static string Calculate(string input, char op)
    {
        if (input is null)
        {
            return null;
        }

        input = input.FromSafeString();

        MatchCollection matches = ParenthesesRegex().Matches(input);
        while (matches.Count > 0)
        {
            for (int i = 0; i < matches.Count; i++)
            {
                string calc = matches[i].Value.Substring(1, matches[i].Length - 2);
                string ret = Calculate(calc);
                input = input.Replace(matches[i].Value, ret);
            }
            matches = ParenthesesRegex().Matches(input);
        }

        string[] parts = input.Split(op);

        //Weird fix
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
                '+' => Calculate(part, '-'),
                '-' => Calculate(part, '*'),
                '*' => Calculate(part, '/'),
                '/' => Calculate(part, '%'),
                '%' => Calculate(part, '^'),
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