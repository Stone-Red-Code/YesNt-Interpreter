using System.Text.RegularExpressions;

using YesNt.Interpreter.Attributes;
using YesNt.Interpreter.Enums;
using YesNt.Interpreter.Runtime;

namespace YesNt.Interpreter.Statements;

internal partial class VariableStatements : StatementRuntimeInformation
{
    [Statement("<", SearchMode.StartOfLine, SpaceAround.None, Priority = Priority.VeryLow)]
    public void DefineVariable(string args)
    {
        string[] parts = args.Split('=');
        if (parts.Length == 2)
        {
            string key = parts[0].Trim();
            if (key.Contains(' '))
            {
                RuntimeInfo.Exit("Invalid Syntax", true);
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
            RuntimeInfo.Exit("Invalid syntax", true);
        }
    }

    [Statement("!<", SearchMode.StartOfLine, SpaceAround.None, Priority = Priority.VeryLow)]
    public void DefineGlobalVariable(string args)
    {
        string[] parts = args.Split('=');
        if (parts.Length == 2)
        {
            string key = parts[0].Trim();
            if (key.Contains(' '))
            {
                RuntimeInfo.Exit("Invalid Syntax", true);
            }

            if (RuntimeInfo.GloablVariables.ContainsKey(key))
            {
                RuntimeInfo.GloablVariables[key] = parts[1].Trim();
            }
            else
            {
                RuntimeInfo.GloablVariables.Add(key, parts[1].Trim());
            }
        }
        else
        {
            RuntimeInfo.Exit("Invalid syntax", true);
        }
    }

    [Statement("del", SearchMode.StartOfLine, SpaceAround.End, System.ConsoleColor.Red, Priority = Priority.VeryLow)]
    public void DeleteVariable(string args)
    {
        string key = args.Trim();

        if (RuntimeInfo.Variables.ContainsKey(key))
        {
            _ = RuntimeInfo.Variables.Remove(key);
        }
        else if (RuntimeInfo.GloablVariables.ContainsKey(key))
        {
            _ = RuntimeInfo.GloablVariables.Remove(key);
        }
        else
        {
            RuntimeInfo.Exit($"Variable \"{key}\" not found", true);
        }
    }

    [Statement(">", SearchMode.Contains, SpaceAround.None, Priority = Priority.Highest)]
    public void ReadVariable(string _)
    {
        if (!RuntimeInfo.CurrentLine.Contains('>'))
        {
            return;
        }

        MatchCollection matches = VariableStatementRegex().Matches(RuntimeInfo.CurrentLine);

        if (matches.Count <= 0)
        {
            RuntimeInfo.Exit("Invalid syntax", true);
        }

        for (int i = 0; i < matches.Count; i++)
        {
            string varName = matches[i].Value.Replace(">", string.Empty);
            if (RuntimeInfo.Variables.TryGetValue(varName, out string value))
            {
                RuntimeInfo.CurrentLine = RuntimeInfo.CurrentLine.Replace($">{varName}", value);
            }
            else if (RuntimeInfo.GloablVariables.TryGetValue(varName, out value))
            {
                RuntimeInfo.CurrentLine = RuntimeInfo.CurrentLine.Replace($">{varName}", value);
            }
            else if (!RuntimeInfo.IsSearching)
            {
                RuntimeInfo.Exit($"Variable \"{varName}\" not found", true);
                return;
            }
        }
    }

    [GeneratedRegex(">[a-zA-Z0-9]+")]
    private static partial Regex VariableStatementRegex();
}