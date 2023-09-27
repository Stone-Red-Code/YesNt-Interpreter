using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;

using YesNt.Interpreter.Enums;
using YesNt.Interpreter.Runtime;
using YesNt.Interpreter.Utilities;

namespace YesNt.CodeEditor;

internal partial class SyntaxHighlighter
{
    private readonly ReadOnlyCollection<StatementInformation> statementInformation;
    private readonly string[] replacementValues;

    public SyntaxHighlighter(ReadOnlyCollection<StatementInformation> statementInformation)
    {
        this.statementInformation = statementInformation;
        replacementValues = StringExtentions.ReplacementRules.Values.ToArray();
    }

    public static string Base64Encode(string plainText)
    {
        byte[] plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
        return System.Convert.ToBase64String(plainTextBytes);
    }

    public static string Base64Decode(string base64EncodedData)
    {
        byte[] base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
        return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
    }

    public void Write(string input)
    {
        input = input.Replace("\0", string.Empty);

        if (input.StartsWith('#'))
        {
            input = AddColorInformation(input, input, ConsoleColor.Gray, SearchMode.Exact);
        }
        else
        {
            for (int i = 0; i < replacementValues.Length; i++)
            {
                input = AddColorInformation(input, replacementValues[i], ConsoleColor.Blue, SearchMode.Contains);
            }

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
                    _ => statement.Name.Trim()
                };
                input = input.TrimEnd(' ');
                if (statement.SearchMode == SearchMode.StartOfLine && input.StartsWith(name))
                {
                    if (statement.Seperator is not null && input.Contains(statement.Seperator))
                    {
                        input = AddColorInformation(input, statement.Seperator, statement.Color, SearchMode.StartOfLine);
                    }
                    else if (statement.Seperator is not null)
                    {
                        continue;
                    }
                    input = AddColorInformation(input, input[..name.Length], statement.Color, statement.SearchMode);
                }
                else if (statement.SearchMode == SearchMode.Contains && input.Contains(name))
                {
                    if (statement.Seperator is not null && input.Contains(statement.Seperator))
                    {
                        input = AddColorInformation(input, statement.Seperator, statement.Color, SearchMode.StartOfLine);
                    }
                    else if (statement.Seperator is not null)
                    {
                        continue;
                    }
                    input = AddColorInformation(input, input.Substring(input.IndexOf(name), name.Length), statement.Color, statement.SearchMode);
                }
                else if (statement.SearchMode == SearchMode.EndOfLine && input.EndsWith(name))
                {
                    if (statement.Seperator is not null && input.Contains(statement.Seperator))
                    {
                        input = AddColorInformation(input, statement.Seperator, statement.Color, SearchMode.StartOfLine);
                    }
                    else if (statement.Seperator is not null)
                    {
                        continue;
                    }
                    input = AddColorInformation(input, input[^name.Length..], statement.Color, statement.SearchMode);
                }
                else if (statement.SearchMode == SearchMode.Exact && input.Equals(name))
                {
                    if (statement.Seperator is not null && input.Contains(statement.Seperator))
                    {
                        input = AddColorInformation(input, statement.Seperator, statement.Color, SearchMode.StartOfLine);
                    }
                    else if (statement.Seperator is not null)
                    {
                        continue;
                    }
                    input = AddColorInformation(input, input, statement.Color, statement.SearchMode);
                }
            }

            MatchCollection matches = VariableRegex().Matches(input);
            for (int i = 0; i < matches.Count; i++)
            {
                input = AddColorInformation(input, matches[i].Value, ConsoleColor.Cyan, SearchMode.Contains);
            }

            matches = VariableDeclarationRegex().Matches(input);
            for (int i = 0; i < matches.Count; i++)
            {
                input = AddColorInformation(input, matches[i].Value, ConsoleColor.DarkCyan, SearchMode.StartOfLine);
            }

            matches = GlobalVariableDeclarationRegex().Matches(input);
            for (int i = 0; i < matches.Count; i++)
            {
                input = AddColorInformation(input, matches[i].Value, ConsoleColor.Blue, SearchMode.StartOfLine);
            }
        }

        foreach (string part in input.Split("\0"))
        {
            ConsoleColor consoleColor;
            string messagePart = part;

            string stringColor = StringColorRegex().Match(messagePart).Value;
            bool succ = int.TryParse(stringColor, out int colorIndex);
            if (succ && colorIndex >= 0 && colorIndex < 16)
            {
                messagePart = Base64Decode(messagePart.Replace($"\r{stringColor}\r", string.Empty));
                consoleColor = (ConsoleColor)colorIndex;
            }
            else
            {
                consoleColor = ConsoleColor.White;
            }

            if (consoleColor == Console.BackgroundColor)
            {
                consoleColor = ConsoleColor.White;
            }
            Console.ForegroundColor = consoleColor;
            Console.Write(messagePart);
        }
        Console.ForegroundColor = ConsoleColor.Gray;
    }

    private static string AddColorInformation(string originalString, string value, ConsoleColor color, SearchMode searchMode)
    {
        int spacesAtEnd = value.WhiteSpaceAtEnd();

        string base64Value = Base64Encode(value.TrimEnd());

        string reult = searchMode switch
        {
            SearchMode.StartOfLine => originalString.ReplaceFirstOccurrence(value, $"\0\r{(int)color}\r{base64Value}\0" + new string(' ', spacesAtEnd)),
            SearchMode.EndOfLine => originalString.ReplaceLastOccurrence(value, $"\0\r{(int)color}\r{base64Value}\0" + new string(' ', spacesAtEnd)),
            _ => originalString.Replace(value, $"\0\r{(int)color}\r{base64Value}\0" + new string(' ', spacesAtEnd))
        };
        return reult;
    }

    [GeneratedRegex("^<[a-zA-Z0-9]+")]
    private static partial Regex VariableDeclarationRegex();

    [GeneratedRegex("^!<[a-zA-Z0-9]+")]
    private static partial Regex GlobalVariableDeclarationRegex();

    [GeneratedRegex(">[a-zA-Z0-9]+")]
    private static partial Regex VariableRegex();

    [GeneratedRegex("(?<=(\\r))(.*)(?=\\r)")]
    private static partial Regex StringColorRegex();
}