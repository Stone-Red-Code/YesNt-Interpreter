using System;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

using YesNt.Interpreter.Enums;
using YesNt.Interpreter.Runtime;
using YesNt.Interpreter.Utilities;

namespace YesNt.CodeEditor
{
    internal class SyntaxHighlighter
    {
        private ReadOnlyCollection<StatementInformation> statementInformation;

        public SyntaxHighlighter(ReadOnlyCollection<StatementInformation> statementInformation)
        {
            this.statementInformation = statementInformation;
        }

        public void Write(string input)
        {
            input = input.Replace("\0", string.Empty);
            foreach (StatementInformation statement in statementInformation)
            {
                if (statement.IgnoreSyntaxHighlighting)
                {
                    continue;
                }

                string name = statement.SpaceAround switch
                {
                    SpaceAround.StartEnd => $" {statement.Name.Trim()} ",
                    SpaceAround.Start => $" {statement.Name.Trim()}",
                    SpaceAround.End => $"{statement.Name.Trim()} ",
                    _ => statement.Name
                };
                input = input.TrimEnd();
                if (statement.SearchMode == SearchMode.StartOfLine && input.StartsWith(name))
                {
                    input = AddColorInformation(input, input.Substring(0, name.Length), ConsoleColor.DarkGreen, statement.SearchMode);
                }
                if (statement.SearchMode == SearchMode.Contains && $"{input} ".Contains(name))
                {
                    input = AddColorInformation($"{input} ", $"{input} ".Substring($"{input} ".IndexOf(name), name.Length), ConsoleColor.Green, statement.SearchMode);
                }
                if (statement.SearchMode == SearchMode.EndOfLine && input.EndsWith(name))
                {
                    input = AddColorInformation(input, input.Substring(input.Length - name.Length), ConsoleColor.DarkYellow, statement.SearchMode);
                }
                if (statement.SearchMode == SearchMode.Exact && input.Equals(name))
                {
                    input = AddColorInformation(input, input, ConsoleColor.Red, statement.SearchMode);
                }
            }

            MatchCollection matches = Regex.Matches(input, @">[a-zA-Z0-9]+");
            for (int i = 0; i < matches.Count; i++)
            {
                input = AddColorInformation(input, matches[i].Value, ConsoleColor.Cyan, SearchMode.Contains);
            }

            matches = Regex.Matches(input, @"^<[a-zA-Z0-9]+");
            for (int i = 0; i < matches.Count; i++)
            {
                input = AddColorInformation(input, matches[i].Value, ConsoleColor.DarkCyan, SearchMode.StartOfLine);
            }

            foreach (string part in input.Split("\0"))
            {
                ConsoleColor consoleColor;
                string messagePart = part;

                string stringColor = Regex.Match(messagePart, "(?<=(\\r))(.*)(?=\\r)").Value;
                bool succ = int.TryParse(stringColor, out int colorIndex);
                if (succ && colorIndex >= 0 && colorIndex < 16)
                {
                    messagePart = messagePart.Replace($"\r{stringColor}\r", string.Empty);
                    consoleColor = (ConsoleColor)colorIndex;
                }
                else
                {
                    consoleColor = ConsoleColor.White;
                }

                if (consoleColor == Console.BackgroundColor || consoleColor == ConsoleColor.DarkGray)
                {
                    consoleColor = ConsoleColor.White;
                }
                Console.ForegroundColor = consoleColor;
                Console.Write(messagePart, consoleColor);
                Console.ResetColor();
            }
        }

        private string AddColorInformation(string originalString, string value, ConsoleColor color, SearchMode searchMode)
        {
            int spacesAtEnd = value.WhiteSpaceAtEnd();
            string reult = searchMode switch
            {
                SearchMode.StartOfLine => originalString.ReplaceFirstOccurrence(value, $"\0\r{(int)color}\r{value.TrimEnd()}\0" + new string(' ', spacesAtEnd)),
                SearchMode.EndOfLine => originalString.ReplaceLastOccurrence(value, $"\0\r{(int)color}\r{value.TrimEnd()}\0" + new string(' ', spacesAtEnd)),
                _ => originalString.Replace(value, $"\0\r{(int)color}\r{value.TrimEnd()}\0" + new string(' ', spacesAtEnd))
            };
            return reult;
        }
    }
}