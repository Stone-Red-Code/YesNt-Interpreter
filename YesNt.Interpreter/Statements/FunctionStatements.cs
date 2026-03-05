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
            RuntimeInfo.Exit(ExitMessages.NestedFunctionsNotAllowed, true);
            return;
        }

        string functionDeclaration = args.Trim();
        if (!functionDeclaration.EndsWith(':'))
        {
            RuntimeInfo.Exit(ExitMessages.InvalidSyntaxColonRequired, true);
            return;
        }

        string key = NormalizeBlockName(functionDeclaration);
        if (string.IsNullOrWhiteSpace(key))
        {
            RuntimeInfo.Exit(ExitMessages.InvalidSyntax, true);
            return;
        }

        RuntimeInfo.Functions[key] = RuntimeInfo.LineNumber;

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
        RuntimeInfo.CurrentLine = TemplateProcessor.ProcessStackParameters(args, "%out", RuntimeInfo.OutParametersStack, RuntimeInfo, ExitMessages.NoOutArgumentInStack);
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
            RuntimeInfo.Exit(ExitMessages.InvalidSyntax, true);
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
            RuntimeInfo.Exit(ExitMessages.StatementNotAllowedOutsideFunction, true);
            return;
        }

        RuntimeInfo.CurrentLine = TemplateProcessor.ProcessStackParameters(args, "%in", RuntimeInfo.FunctionCallStack.Peek().Arguments, RuntimeInfo, ExitMessages.NoInArgumentInStack);
    }

    [Statement("%has_in", SearchMode.Contains, SpaceAround.None, ConsoleColor.Yellow, KeepStatementInArgs = true, Priority = Priority.Highest)]
    public void CheckIfInParameterAvailable(string args)
    {
        if (!RuntimeInfo.IsInFunction)
        {
            RuntimeInfo.Exit(ExitMessages.StatementNotAllowedOutsideFunction, true);
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
            RuntimeInfo.Exit(ExitMessages.StatementNotAllowedOutsideFunction, true);
            return;
        }

        RuntimeInfo.FunctionCallStack.Peek().Results.Push(args);
    }

    [Statement("return", SearchMode.Exact, SpaceAround.None, ConsoleColor.DarkYellow, ExecuteInSearchMode = true)]
    public void Return(string _)
    {
        if (!RuntimeInfo.IsInFunction)
        {
            RuntimeInfo.Exit(ExitMessages.StatementNotAllowedOutsideFunction, true);
            return;
        }

        if (RuntimeInfo.IsSearching)
        {
            RuntimeInfo.IsInFunction = false;

            if (RuntimeInfo.IsLocalSearch)
            {
                RuntimeInfo.Exit(ExitMessages.LabelNotFound(RuntimeInfo.SearchLabel), true);
            }
            return;
        }

        RuntimeInfo.IsInFunction = false;

        if (RuntimeInfo.FunctionCallStack.Count > 0)
        {
            FunctionScope functionScope = RuntimeInfo.FunctionCallStack.Pop();

            RuntimeInfo.OutParametersStack = new Stack<string>(functionScope.Results);
            RuntimeInfo.LineNumber = functionScope.CallerLine;
        }
        else
        {
            RuntimeInfo.Exit(ExitMessages.NoFunctionInStack, true);
        }
    }

    [Statement("clear_call_stack", SearchMode.Exact, SpaceAround.None, ConsoleColor.Red)]
    public void ClearCallStack(string _)
    {
        RuntimeInfo.FunctionCallStack.Clear();
    }
}
