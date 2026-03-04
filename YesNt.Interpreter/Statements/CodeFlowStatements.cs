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

    [Statement("label", SearchMode.StartOfLine, SpaceAround.End, ConsoleColor.Green, ExecuteInSearchMode = true, Separator = ":")]
    public void FindLabel(string args)
    {
        string labelDeclaration = args.Trim();
        if (!labelDeclaration.EndsWith(':'))
        {
            RuntimeInfo.Exit("Invalid syntax. Statement must end with ':'", true);
            return;
        }

        string key = NormalizeBlockName(labelDeclaration);
        if (string.IsNullOrWhiteSpace(key))
        {
            RuntimeInfo.Exit("Invalid syntax", true);
            return;
        }

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

    [Statement("if", SearchMode.StartOfLine, SpaceAround.End, ConsoleColor.Green, Priority = Priority.VeryLow, Separator = ":")]
    public void IfBlock(string args)
    {
        args = args.Trim();
        if (!args.EndsWith(':'))
        {
            RuntimeInfo.Exit("Invalid syntax. Statement must end with ':'", true);
            return;
        }

        string condition = args[..^1].Trim();
        bool? result = Evaluator.EvaluateCondition(condition);

        if (result is null)
        {
            RuntimeInfo.Exit("Invalid operation", true);
            return;
        }

        if (result == true)
        {
            return;
        }

        (int targetLine, _) = FindElseOrEndIf(RuntimeInfo.LineNumber);
        if (targetLine < 0)
        {
            RuntimeInfo.Exit("No matching end_if found", true);
            return;
        }

        RuntimeInfo.LineNumber = targetLine;
    }

    [Statement("else:", SearchMode.Exact, SpaceAround.None, ConsoleColor.Green)]
    public void Else(string _)
    {
        int targetLine = FindEndIf(RuntimeInfo.LineNumber);
        if (targetLine < 0)
        {
            RuntimeInfo.Exit("No matching end_if found", true);
            return;
        }

        RuntimeInfo.LineNumber = targetLine;
    }

    [Statement("end_if", SearchMode.Exact, SpaceAround.None, ConsoleColor.Green)]
    public void EndIf(string _)
    {
    }

    [Statement("while", SearchMode.StartOfLine, SpaceAround.End, ConsoleColor.Green, Priority = Priority.VeryLow, Separator = ":")]
    public void While(string args)
    {
        args = args.Trim();
        if (!args.EndsWith(':'))
        {
            RuntimeInfo.Exit("Invalid syntax. Statement must end with ':'", true);
            return;
        }

        string condition = args[..^1].Trim();
        bool? result = Evaluator.EvaluateCondition(condition);

        if (result is null)
        {
            RuntimeInfo.Exit("Invalid operation", true);
            return;
        }

        if (result == true)
        {
            return;
        }

        int endWhileLine = FindEndWhile(RuntimeInfo.LineNumber);
        if (endWhileLine < 0)
        {
            RuntimeInfo.Exit("No matching end_while found", true);
            return;
        }

        RuntimeInfo.LineNumber = endWhileLine;
    }

    [Statement("end_while", SearchMode.Exact, SpaceAround.None, ConsoleColor.Green)]
    public void EndWhile(string _)
    {
        int whileLine = FindWhile(RuntimeInfo.LineNumber);
        if (whileLine < 0)
        {
            RuntimeInfo.Exit("No matching while found", true);
            return;
        }

        RuntimeInfo.LineNumber = whileLine - 1;
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

    private (int TargetLine, bool IsElse) FindElseOrEndIf(int currentLine)
    {
        int depth = 0;

        for (int i = currentLine + 1; i < RuntimeInfo.Lines.Count; i++)
        {
            string line = RuntimeInfo.Lines[i].Content.Trim().Replace("\r", string.Empty);

            if (IsIfStart(line))
            {
                depth++;
                continue;
            }

            if (line == "end_if")
            {
                if (depth == 0)
                {
                    return (i, false);
                }

                depth--;
                continue;
            }

            if (line == "else:" && depth == 0)
            {
                return (i, true);
            }
        }

        return (-1, false);
    }

    private int FindEndIf(int currentLine)
    {
        int depth = 0;

        for (int i = currentLine + 1; i < RuntimeInfo.Lines.Count; i++)
        {
            string line = RuntimeInfo.Lines[i].Content.Trim().Replace("\r", string.Empty);

            if (IsIfStart(line))
            {
                depth++;
                continue;
            }

            if (line == "end_if")
            {
                if (depth == 0)
                {
                    return i;
                }

                depth--;
            }
        }

        return -1;
    }

    private static bool IsIfStart(string line)
    {
        return line.StartsWith("if ", StringComparison.Ordinal) && line.EndsWith(':');
    }

    private int FindEndWhile(int currentLine)
    {
        int depth = 0;

        for (int i = currentLine + 1; i < RuntimeInfo.Lines.Count; i++)
        {
            string line = RuntimeInfo.Lines[i].Content.Trim().Replace("\r", string.Empty);

            if (IsWhileStart(line))
            {
                depth++;
                continue;
            }

            if (line == "end_while")
            {
                if (depth == 0)
                {
                    return i;
                }

                depth--;
            }
        }

        return -1;
    }

    private int FindWhile(int currentLine)
    {
        int depth = 0;

        for (int i = currentLine - 1; i >= 0; i--)
        {
            string line = RuntimeInfo.Lines[i].Content.Trim().Replace("\r", string.Empty);

            if (line == "end_while")
            {
                depth++;
                continue;
            }

            if (IsWhileStart(line))
            {
                if (depth == 0)
                {
                    return i;
                }

                depth--;
            }
        }

        return -1;
    }

    private static bool IsWhileStart(string line)
    {
        return line.StartsWith("while ", StringComparison.Ordinal) && line.EndsWith(':');
    }
}
