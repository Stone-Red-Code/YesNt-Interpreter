using System.Text.RegularExpressions;

namespace YesNt.Interpreter.Utilities
{
    internal static class Evaluator
    {
        public static bool? EvaluateCondition(string input)
        {
            string[] parts = input.Split("==");
            if (parts.Length == 2)
            {
                string part1 = parts[0].FromSaveString().Trim();
                string part2 = parts[1].FromSaveString().Trim();
                return part1 == part2;
            }

            parts = input.Split("!=");
            if (parts.Length == 2)
            {
                string part1 = parts[0].FromSaveString().Trim();
                string part2 = parts[1].FromSaveString().Trim();
                return part1 != part2;
            }

            parts = input.Split(">");
            if (parts.Length == 2)
            {
                bool succ1 = parts[0].ToStandardizedNumber(out double part1);
                bool succ2 = parts[1].ToStandardizedNumber(out double part2);
                if (!succ1 || !succ2)
                {
                    return false;
                }

                return part1 > part2;
            }

            parts = input.Split("<");
            if (parts.Length == 2)
            {
                bool succ1 = parts[0].ToStandardizedNumber(out double part1);
                bool succ2 = parts[1].ToStandardizedNumber(out double part2);
                if (!succ1 || !succ2)
                {
                    return false;
                }

                return part1 < part2;
            }

            parts = input.Split(">=");
            if (parts.Length == 2)
            {
                bool succ1 = parts[0].ToStandardizedNumber(out double part1);
                bool succ2 = parts[1].ToStandardizedNumber(out double part2);
                if (!succ1 || !succ2)
                {
                    return false;
                }

                return part1 >= part2;
            }

            parts = input.Split("<=");
            if (parts.Length == 2)
            {
                bool succ1 = parts[0].ToStandardizedNumber(out double part1);
                bool succ2 = parts[1].ToStandardizedNumber(out double part2);
                if (!succ1 || !succ2)
                {
                    return false;
                }

                return part1 <= part2;
            }

            return null;
        }

        public static string Calculate(string input, char op = '+')
        {
            if (input is null)
            {
                return null;
            }

            MatchCollection matches = Regex.Matches(input, @"\(([^()]+)\)");
            while (matches.Count > 0)
            {
                for (int i = 0; i < matches.Count; i++)
                {
                    string calc = matches[i].Value.Substring(1, matches[i].Length - 2);
                    string ret = Calculate(calc);
                    input = input.Replace(matches[i].Value, ret);
                }
                matches = Regex.Matches(input, @"\(([^()]+)\)");
            }

            string[] parts = input.Split(op);

            double number = double.NaN;

            foreach (string p in parts)
            {
                string part = p;

                switch (op)
                {
                    case '+':
                        part = Calculate(part, '-');
                        break;

                    case '-':
                        part = Calculate(part, '*');
                        break;

                    case '*':
                        part = Calculate(part, '/');
                        break;
                }

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
                        }
                    }
                }
                else
                {
                    return null;
                }
            }

            return number.ToString();
        }
    }
}