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
            RuntimeInfo.Exit(ExitMessages.InvalidSyntax, true);
            return;
        }

        string condition = parts[0].Trim();
        string key = NormalizeBlockName(parts[1]);

        bool? result = Evaluator.EvaluateCondition(condition);

        if (result is null)
        {
            RuntimeInfo.Exit(ExitMessages.InvalidOperation, true);
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

    [Statement("label", SearchMode.StartOfLine, SpaceAround.End, ConsoleColor.Green, ExecuteInSearchMode = true, Separator = ":")]
    public void FindLabel(string args)
    {
        string labelDeclaration = args.Trim();
        if (!labelDeclaration.EndsWith(':'))
        {
            RuntimeInfo.Exit(ExitMessages.InvalidSyntaxColonRequired, true);
            return;
        }

        string key = NormalizeBlockName(labelDeclaration);
        if (string.IsNullOrWhiteSpace(key))
        {
            RuntimeInfo.Exit(ExitMessages.InvalidSyntax, true);
            return;
        }

        RuntimeInfo.Labels[key] = RuntimeInfo.LineNumber;

        if (!string.IsNullOrWhiteSpace(RuntimeInfo.SearchLabel) && RuntimeInfo.SearchLabel == key)
        {
            RuntimeInfo.SearchLabel = string.Empty;
            RuntimeInfo.IsLocalSearch = false;
        }
    }

    [Statement("call", SearchMode.StartOfLine, SpaceAround.End, ConsoleColor.DarkYellow, Priority = Priority.VeryLow)]
    public void Call(string args)
    {
        CallFunction(NormalizeBlockName(args));
    }

    [Statement("if", SearchMode.StartOfLine, SpaceAround.End, ConsoleColor.DarkYellow, Priority = Priority.VeryLow, Separator = " call ")]
    public void CallIf(string args)
    {
        string[] parts = args.Split(" call ", 2, StringSplitOptions.None);
        if (parts.Length != 2)
        {
            RuntimeInfo.Exit(ExitMessages.InvalidSyntax, true);
            return;
        }

        string condition = parts[0].Trim();
        bool? result = Evaluator.EvaluateCondition(condition);

        if (result is null)
        {
            RuntimeInfo.Exit(ExitMessages.InvalidOperation, true);
            return;
        }

        if (result == true)
        {
            CallFunction(NormalizeBlockName(parts[1]));
        }
    }

    [Statement("if", SearchMode.StartOfLine, SpaceAround.End, ConsoleColor.Green, Priority = Priority.VeryLow, Separator = ":", BlockPair = "end_if")]
    public void IfBlock(string args)
    {
        args = args.Trim();
        if (!args.EndsWith(':'))
        {
            RuntimeInfo.Exit(ExitMessages.InvalidSyntaxColonRequired, true);
            return;
        }

        string condition = args[..^1].Trim();
        bool? result = Evaluator.EvaluateCondition(condition);

        if (result is null)
        {
            RuntimeInfo.Exit(ExitMessages.InvalidOperation, true);
            return;
        }

        if (result == true)
        {
            return;
        }

        int targetLine = FindBlockBoundary(RuntimeInfo.LineNumber);
        if (targetLine < 0)
        {
            RuntimeInfo.Exit(ExitMessages.NoMatchingEndIf, true);
            return;
        }

        RuntimeInfo.LineNumber = targetLine;
    }

    [Statement("else:", SearchMode.Exact, SpaceAround.None, ConsoleColor.Green, IsBlockIntermediate = true, BlockPair = "end_if")]
    public void Else(string _)
    {
        int targetLine = FindBlockBoundary(RuntimeInfo.LineNumber);
        if (targetLine < 0)
        {
            RuntimeInfo.Exit(ExitMessages.NoMatchingEndIf, true);
            return;
        }

        RuntimeInfo.LineNumber = targetLine;
    }

    [Statement("end_if", SearchMode.Exact, SpaceAround.None, ConsoleColor.Green, IsBlockEnd = true)]
    public void EndIf(string _)
    {
        // Intentionally empty: end_if is a block-boundary marker only; no runtime action needed.
    }

    [Statement("while", SearchMode.StartOfLine, SpaceAround.End, ConsoleColor.Green, Priority = Priority.VeryLow, Separator = ":", BlockPair = "end_while")]
    public void While(string args)
    {
        args = args.Trim();
        if (!args.EndsWith(':'))
        {
            RuntimeInfo.Exit(ExitMessages.InvalidSyntaxColonRequired, true);
            return;
        }

        string condition = args[..^1].Trim();
        bool? result = Evaluator.EvaluateCondition(condition);

        if (result is null)
        {
            RuntimeInfo.Exit(ExitMessages.InvalidOperation, true);
            return;
        }

        if (result == true)
        {
            return;
        }

        int endWhileLine = FindBlockBoundary(RuntimeInfo.LineNumber);
        if (endWhileLine < 0)
        {
            RuntimeInfo.Exit(ExitMessages.NoMatchingEndWhile, true);
            return;
        }

        RuntimeInfo.LineNumber = endWhileLine;
    }

    [Statement("end_while", SearchMode.Exact, SpaceAround.None, ConsoleColor.Green, IsBlockEnd = true)]
    public void EndWhile(string _)
    {
        int whileLine = FindBlockBoundary(RuntimeInfo.LineNumber);
        if (whileLine < 0)
        {
            RuntimeInfo.Exit(ExitMessages.NoMatchingWhile, true);
            return;
        }

        RuntimeInfo.LineNumber = whileLine - 1;
    }

    [Statement("exit", SearchMode.Exact, SpaceAround.None, ConsoleColor.Red, ExecuteInSearchMode = true)]
    public void End(string _)
    {
        HandleExit(ExitMessages.PlannedTermination, false);
    }

    [Statement("abort_all", SearchMode.Exact, SpaceAround.None, ConsoleColor.Red, ExecuteInSearchMode = true)]
    public void Terminate(string _)
    {
        HandleExit(ExitMessages.PlannedTerminationCancelingTasks, true);
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

    private void CallFunction(string key)
    {
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

    private void HandleExit(string exitMessage, bool isError)
    {
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
        RuntimeInfo.Exit(exitMessage, isError);
    }

    private int FindBlockBoundary(int currentLine)
    {
        return RuntimeInfo.BlockBoundaries.TryGetValue(currentLine, out int cached) ? cached : -1;
    }
}