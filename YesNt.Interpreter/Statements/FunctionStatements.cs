using System;
using System.Collections.Generic;

using YesNt.Interpreter.Attributes;
using YesNt.Interpreter.Enums;
using YesNt.Interpreter.Runtime;
using YesNt.Interpreter.Utilities;

namespace YesNt.Interpreter.Statements;

internal class FunctionStatements : StatementRuntimeInformation
{
    [Statement("func", SearchMode.StartOfLine, SpaceAround.End, ConsoleColor.DarkYellow, ExecuteInSearchMode = true, Separator = ":")]
    public void FindFunction(string args)
    {
        if (RuntimeInfo.InternalIsInFunction)
        {
            RuntimeInfo.Exit("Nested functions are not allowed", true);
            return;
        }

        string functionDeclaration = args.Trim();
        if (!functionDeclaration.EndsWith(':'))
        {
            RuntimeInfo.Exit("Invalid syntax. Statement must end with ':'", true);
            return;
        }

        string key = NormalizeBlockName(functionDeclaration);
        if (string.IsNullOrWhiteSpace(key))
        {
            RuntimeInfo.Exit("Invalid syntax", true);
            return;
        }

        if (RuntimeInfo.Functions.ContainsKey(key))
        {
            RuntimeInfo.Functions[key] = RuntimeInfo.LineNumber;
        }
        else
        {
            RuntimeInfo.Functions.Add(key, RuntimeInfo.LineNumber);
        }

        if (!string.IsNullOrWhiteSpace(RuntimeInfo.SearchFunction) && RuntimeInfo.SearchFunction == key)
        {
            RuntimeInfo.SearchFunction = string.Empty;
        }

        RuntimeInfo.IsInFunction = true;
    }

    [Statement("push_in", SearchMode.StartOfLine, SpaceAround.End, ConsoleColor.Yellow)]
    public void AddInParameter(string args)
    {
        RuntimeInfo.InParametersStack.Push(args);
    }

    [Statement("%out", SearchMode.Contains, SpaceAround.None, ConsoleColor.Yellow, KeepStatementInArgs = true, Priority = Priority.Highest)]
    public void GetOutParameter(string args)
    {
        while (args.Contains("%out"))
        {
            if (RuntimeInfo.OutParametersStack.Count == 0)
            {
                RuntimeInfo.Exit("No out argument in stack", true);
                return;
            }

            args = args.ReplaceFirstOccurrence("%out", RuntimeInfo.OutParametersStack.Pop());
        }

        RuntimeInfo.CurrentLine = args.TrimEnd();
    }

    [Statement("%has_out", SearchMode.Contains, SpaceAround.None, ConsoleColor.Yellow, KeepStatementInArgs = true, Priority = Priority.Highest)]
    public void CheckIfOutParameterAvailable(string args)
    {
        args = args.Replace("%has_out", (RuntimeInfo.OutParametersStack.Count > 0).ToString());

        RuntimeInfo.CurrentLine = args.TrimEnd();
    }

    [Statement("call", SearchMode.StartOfLine, SpaceAround.End, ConsoleColor.DarkYellow, Priority = Priority.Low, Separator = " with ")]
    public void Call(string args)
    {
        string[] parts = args.Split(" with ", 2, StringSplitOptions.None);
        if (parts.Length != 2)
        {
            RuntimeInfo.Exit("Invalid syntax", true);
            return;
        }

        string key = NormalizeBlockName(parts[0]);
        string[] functionArguments = parts[1].Split(',');

        foreach (string argument in functionArguments)
        {
            RuntimeInfo.InParametersStack.Push(argument.Trim());
        }

        RuntimeInfo.FunctionCallStack.Push(new FunctionScope(RuntimeInfo.LineNumber, new Stack<string>(RuntimeInfo.InParametersStack)));
        RuntimeInfo.InParametersStack.Clear();
        RuntimeInfo.CurrentLine = string.Empty;

        if (RuntimeInfo.Functions.TryGetValue(key, out int value))
        {
            RuntimeInfo.LineNumber = value;
        }
        else
        {
            RuntimeInfo.SearchFunction = key;
        }
    }

    [Statement("%in", SearchMode.Contains, SpaceAround.None, ConsoleColor.Yellow, KeepStatementInArgs = true, Priority = Priority.Highest)]
    public void GetInParameter(string args)
    {
        if (!RuntimeInfo.IsInFunction)
        {
            RuntimeInfo.Exit("Statement not allowed outside of function", true);
            return;
        }

        while (args.Contains("%in"))
        {
            if (RuntimeInfo.FunctionCallStack.Peek().Arguments.Count == 0)
            {
                RuntimeInfo.Exit("No in argument in stack", true);
                return;
            }

            args = args.ReplaceFirstOccurrence("%in", RuntimeInfo.FunctionCallStack.Peek().Arguments.Pop());
        }

        RuntimeInfo.CurrentLine = args.TrimEnd();
    }

    [Statement("%has_in", SearchMode.Contains, SpaceAround.None, ConsoleColor.Yellow, KeepStatementInArgs = true, Priority = Priority.Highest)]
    public void CheckIfInParameterAvailable(string args)
    {
        if (!RuntimeInfo.IsInFunction)
        {
            RuntimeInfo.Exit("Statement not allowed outside of function", true);
            return;
        }

        args = args.Replace("%has_in", (RuntimeInfo.FunctionCallStack.Peek().Arguments.Count > 0).ToString());

        RuntimeInfo.CurrentLine = args.TrimEnd();
    }

    [Statement("push_out", SearchMode.StartOfLine, SpaceAround.End, ConsoleColor.Yellow)]
    public void AddOutParameter(string args)
    {
        if (!RuntimeInfo.IsInFunction)
        {
            RuntimeInfo.Exit("Statement not allowed outside of function", true);
            return;
        }

        RuntimeInfo.FunctionCallStack.Peek().Results.Push(args);
    }

    [Statement("return", SearchMode.Exact, SpaceAround.None, ConsoleColor.DarkYellow, ExecuteInSearchMode = true)]
    public void Return(string _)
    {
        if (!RuntimeInfo.IsInFunction)
        {
            RuntimeInfo.Exit("Statement not allowed outside of function", true);
            return;
        }

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

        if (RuntimeInfo.FunctionCallStack.Count > 0)
        {
            FunctionScope functionScope = RuntimeInfo.FunctionCallStack.Pop();

            RuntimeInfo.OutParametersStack = new Stack<string>(functionScope.Results);
            RuntimeInfo.LineNumber = functionScope.CallerLine;
        }
        else
        {
            RuntimeInfo.Exit("No function in stack", true);
        }
    }

    [Statement("clear_call_stack", SearchMode.Exact, SpaceAround.None, ConsoleColor.Red)]
    public void ClearCallStack(string _)
    {
        RuntimeInfo.FunctionCallStack.Clear();
    }

    private static string NormalizeBlockName(string value)
    {
        return value.Trim().TrimEnd(':').Trim();
    }
}
