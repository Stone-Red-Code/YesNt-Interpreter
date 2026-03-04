using System;
using System.Collections.Generic;

using YesNt.Interpreter.Attributes;
using YesNt.Interpreter.Enums;
using YesNt.Interpreter.Runtime;
using YesNt.Interpreter.Utilities;

namespace YesNt.Interpreter.Statements;

internal class CodeFlowStatements : StatementRuntimeInformation
{
    [Statement("goto", SearchMode.StartOfLine, SpaceAround.End, ConsoleColor.Green, Priority = Priority.VeryLow)]
    public void Jump(string args)
    {
        string key = NormalizeBlockName(args);

        if (RuntimeInfo.Labels.TryGetValue(key, out int value))
        {
            RuntimeInfo.LineNumber = value;
        }
        else
        {
            RuntimeInfo.SearchLabel = key;
            RuntimeInfo.IsLocalSearch = RuntimeInfo.IsInFunction;
        }
    }

    [Statement("if", SearchMode.StartOfLine, SpaceAround.End, ConsoleColor.Green, Priority = Priority.VeryLow, Separator = " goto ")]
    public void JumpIf(string args)
    {
        string[] parts = args.Split(" goto ", 2, StringSplitOptions.None);
        if (parts.Length != 2)
        {
            RuntimeInfo.Exit("Invalid syntax", true);
            return;
        }

        string condition = parts[0].Trim();
        string key = NormalizeBlockName(parts[1]);

        bool? result = Evaluator.EvaluateCondition(condition);

        if (result is null)
        {
            RuntimeInfo.Exit("Invalid operation", true);
            return;
        }

        if (result == false)
        {
            return;
        }

        if (RuntimeInfo.Labels.TryGetValue(key, out int value))
        {
            RuntimeInfo.LineNumber = value;
        }
        else
        {
            RuntimeInfo.SearchLabel = key;
            RuntimeInfo.IsLocalSearch = RuntimeInfo.IsInFunction;
        }
    }

    [Statement("label", SearchMode.StartOfLine, SpaceAround.End, ConsoleColor.Green, ExecuteInSearchMode = true)]
    public void FindLabel(string args)
    {
        string key = NormalizeBlockName(args);
        if (RuntimeInfo.Labels.ContainsKey(key))
        {
            RuntimeInfo.Labels[key] = RuntimeInfo.LineNumber;
        }
        else
        {
            RuntimeInfo.Labels.Add(key, RuntimeInfo.LineNumber);
        }

        if (!string.IsNullOrWhiteSpace(RuntimeInfo.SearchLabel) && RuntimeInfo.SearchLabel == key)
        {
            RuntimeInfo.SearchLabel = string.Empty;
            RuntimeInfo.IsLocalSearch = false;
        }
    }

    [Statement("call", SearchMode.StartOfLine, SpaceAround.End, ConsoleColor.DarkYellow, Priority = Priority.VeryLow)]
    public void Call(string args)
    {
        string key = NormalizeBlockName(args);

        RuntimeInfo.FunctionCallStack.Push(new FunctionScope(RuntimeInfo.LineNumber, new Stack<string>(RuntimeInfo.InParametersStack)));
        RuntimeInfo.InParametersStack.Clear();

        if (RuntimeInfo.Functions.TryGetValue(key, out int value))
        {
            RuntimeInfo.LineNumber = value;
        }
        else
        {
            RuntimeInfo.SearchFunction = key;
        }
    }

    [Statement("if", SearchMode.StartOfLine, SpaceAround.End, ConsoleColor.DarkYellow, Priority = Priority.VeryLow, Separator = " call ")]
    public void CallIf(string args)
    {
        string[] parts = args.Split(" call ", 2, StringSplitOptions.None);
        if (parts.Length != 2)
        {
            RuntimeInfo.Exit("Invalid syntax", true);
            return;
        }

        string condition = parts[0].Trim();
        string key = NormalizeBlockName(parts[1]);

        bool? result = Evaluator.EvaluateCondition(condition);

        if (result is null)
        {
            RuntimeInfo.Exit("Invalid operation", true);
            return;
        }

        if (result == false)
        {
            return;
        }

        RuntimeInfo.FunctionCallStack.Push(new FunctionScope(RuntimeInfo.LineNumber, new Stack<string>(RuntimeInfo.InParametersStack)));
        RuntimeInfo.InParametersStack.Clear();

        if (RuntimeInfo.Functions.TryGetValue(key, out int value))
        {
            RuntimeInfo.LineNumber = value;
        }
        else
        {
            RuntimeInfo.SearchFunction = key;
        }
    }

    [Statement("exit", SearchMode.Exact, SpaceAround.None, ConsoleColor.Red, ExecuteInSearchMode = true)]
    public void End(string _)
    {
        if (RuntimeInfo.IsSearching)
        {
            RuntimeInfo.IsInFunction = false;
            if (RuntimeInfo.IsLocalSearch)
            {
                RuntimeInfo.Exit($"Label \"{RuntimeInfo.SearchLabel}\" not found", true);
            }

            return;
        }
        else
        {
            RuntimeInfo.IsInFunction = false;
        }

        RuntimeInfo.Exit("Planned termination by code", false);
    }

    [Statement("abort_all", SearchMode.Exact, SpaceAround.None, ConsoleColor.Red, ExecuteInSearchMode = true)]
    public void Terminate(string _)
    {
        if (RuntimeInfo.IsSearching)
        {
            RuntimeInfo.IsInFunction = false;
            if (RuntimeInfo.IsLocalSearch)
            {
                RuntimeInfo.Exit($"Label \"{RuntimeInfo.SearchLabel}\" not found", true);
            }

            return;
        }
        else
        {
            RuntimeInfo.IsInFunction = false;
        }

        RuntimeInfo.Exit("Planned termination by code. Canceling all tasks", true);
    }

    [Statement("throw", SearchMode.StartOfLine, SpaceAround.End, ConsoleColor.Red)]
    public void Throw(string message)
    {
        RuntimeInfo.Exit(message, true);
    }

    [Statement("error", SearchMode.StartOfLine, SpaceAround.End, ConsoleColor.Red)]
    public void Error(string message)
    {
        RuntimeInfo.Exit(message, false);
    }

    private static string NormalizeBlockName(string value)
    {
        return value.Trim().TrimEnd(':').Trim();
    }
}
