using System;
using System.Collections.Generic;

using YesNt.Interpreter.Attributes;
using YesNt.Interpreter.Enums;
using YesNt.Interpreter.Runtime;
using YesNt.Interpreter.Utilities;

namespace YesNt.Interpreter.Statements;

internal partial class VariableStatements : StatementRuntimeInformation
{
    [Statement("var", SearchMode.StartOfLine, SpaceAround.End, System.ConsoleColor.DarkBlue, Priority = Priority.VeryLow, Separator = "=")]
    public void DefineVariable(string args)
    {
        DefineVariableIn(RuntimeInfo.Variables, args);
    }

    [Statement("global", SearchMode.StartOfLine, SpaceAround.End, System.ConsoleColor.DarkBlue, Priority = Priority.VeryLow, Separator = "=")]
    public void DefineGlobalVariable(string args)
    {
        DefineVariableIn(RuntimeInfo.GlobalVariables, args);
    }

    private void DefineVariableIn(Dictionary<string, string> dict, string args)
    {
        string[] parts = args.Split('=');
        if (parts.Length == 2)
        {
            string key = parts[0].Trim();
            if (key.Contains(' '))
            {
                RuntimeInfo.Exit(ExitMessages.InvalidSyntax, true);
            }

            dict[key] = parts[1].Trim();
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
        RuntimeInfo.CurrentLine = TemplateProcessor.ProcessVariables(RuntimeInfo.CurrentLine, RuntimeInfo);
    }
}
