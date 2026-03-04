using System.Text.RegularExpressions;

using YesNt.Interpreter.Attributes;
using YesNt.Interpreter.Enums;
using YesNt.Interpreter.Runtime;

namespace YesNt.Interpreter.Statements;

internal partial class VariableStatements : StatementRuntimeInformation
{
    [Statement("var", SearchMode.StartOfLine, SpaceAround.End, System.ConsoleColor.DarkBlue, Priority = Priority.VeryLow, Separator = "=")]
    public void DefineVariable(string args)
    {
        string[] parts = args.Split('=');
        if (parts.Length == 2)
        {
            string key = parts[0].Trim();
            if (key.Contains(' '))
            {
                RuntimeInfo.Exit(ExitMessages.InvalidSyntax, true);
            }

            if (RuntimeInfo.Variables.ContainsKey(key))
            {
                RuntimeInfo.Variables[key] = parts[1].Trim();
            }
            else
            {
                RuntimeInfo.Variables.Add(key, parts[1].Trim());
            }
        }
        else
        {
            RuntimeInfo.Exit(ExitMessages.InvalidSyntax, true);
        }
    }

    [Statement("global", SearchMode.StartOfLine, SpaceAround.End, System.ConsoleColor.DarkBlue, Priority = Priority.VeryLow, Separator = "=")]
    public void DefineGlobalVariable(string args)
    {
        string[] parts = args.Split('=');
        if (parts.Length == 2)
        {
            string key = parts[0].Trim();
            if (key.Contains(' '))
            {
                RuntimeInfo.Exit(ExitMessages.InvalidSyntax, true);
            }

            if (RuntimeInfo.GlobalVariables.ContainsKey(key))
            {
                RuntimeInfo.GlobalVariables[key] = parts[1].Trim();
            }
            else
            {
                RuntimeInfo.GlobalVariables.Add(key, parts[1].Trim());
            }
        }
        else
        {
            RuntimeInfo.Exit(ExitMessages.InvalidSyntax, true);
        }
    }

    [Statement("delete", SearchMode.StartOfLine, SpaceAround.End, System.ConsoleColor.Red, Priority = Priority.VeryLow)]
    public void DeleteVariable(string args)
    {
        string key = args.Trim();

        if (RuntimeInfo.Variables.ContainsKey(key))
        {
            _ = RuntimeInfo.Variables.Remove(key);
        }
        else if (RuntimeInfo.GlobalVariables.ContainsKey(key))
        {
            _ = RuntimeInfo.GlobalVariables.Remove(key);
        }
        else
        {
            RuntimeInfo.Exit(ExitMessages.VariableNotFound(key), true);
        }
    }

    [Statement("${", SearchMode.Contains, SpaceAround.None, Priority = Priority.Highest, Separator = "}")]
    public void ReadVariable(string _)
    {
        if (!RuntimeInfo.CurrentLine.Contains("${"))
        {
            return;
        }

        MatchCollection matches = VariableStatementRegex().Matches(RuntimeInfo.CurrentLine);

        if (matches.Count <= 0)
        {
            return;
        }

        for (int i = 0; i < matches.Count; i++)
        {
            string varName = matches[i].Groups[1].Value;
            if (RuntimeInfo.Variables.TryGetValue(varName, out string value))
            {
                RuntimeInfo.CurrentLine = RuntimeInfo.CurrentLine.Replace(matches[i].Value, value);
            }
            else if (RuntimeInfo.GlobalVariables.TryGetValue(varName, out value))
            {
                RuntimeInfo.CurrentLine = RuntimeInfo.CurrentLine.Replace(matches[i].Value, value);
            }
            else if (!RuntimeInfo.IsSearching)
            {
                RuntimeInfo.Exit(ExitMessages.VariableNotFound(varName), true);
                return;
            }
        }
    }

    [GeneratedRegex("\\$\\{([a-zA-Z0-9]+)\\}")]
    private static partial Regex VariableStatementRegex();
}

