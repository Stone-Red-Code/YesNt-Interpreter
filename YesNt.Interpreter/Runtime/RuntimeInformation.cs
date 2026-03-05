using System;
using System.Collections.Generic;

using YesNt.Interpreter.Utilities;

namespace YesNt.Interpreter.Runtime;

/// <summary>
/// Holds all mutable runtime state for a single script execution, including variables, lists,
/// labels, functions, the call stack, the line counter, and stop flags.
/// Each background task spawned by the <c>task</c> statement owns its own
/// <see cref="RuntimeInformation"/> whose <see cref="ParentRuntimeInformation"/> points back
/// to the main execution context.
/// </summary>
internal sealed class RuntimeInformation
{
    public event Action<string> OnDebugOutput;

    public event Action<DebugEventArgs> OnLineExecuted;

    private event Action<string, bool> OnExit;

    private static int internalTaskId = 0;
    private readonly Dictionary<string, string> topVariables = [];
    private readonly Dictionary<string, List<string>> topLists = [];

    public Dictionary<string, string> GlobalVariables { get; set; } = [];
    public Dictionary<string, int> Functions { get; } = [];
    public Dictionary<int, int> BlockBoundaries { get; } = [];
    internal Action PreScanLinesAction { get; set; }
    public Stack<FunctionScope> FunctionCallStack { get; } = new();
    public Stack<string> InParametersStack { get; } = new();
    public Stack<string> OutParametersStack { get; set; } = new();
    public List<Line> Lines { get; set; } = [];
    public string CurrentLine { get; set; } = string.Empty;
    public string SearchLabel { get; set; } = string.Empty;
    public string SearchFunction { get; set; } = string.Empty;
    public int LineNumber { get; set; } = 0;
    public bool Stop { get; private set; } = false;
    public bool StopAllTasks { get; private set; } = false;
    public bool IsDebugMode { get; set; } = false;
    public string WorkingDirectory { get; set; } = string.Empty;
    public bool IsTask => ParentRuntimeInformation is not null;
    public int TaskId { get => IsTask ? field : 0; private set; } = 0;
    public bool InternalIsInFunction { get; set; }

    public bool IsInFunction
    {
        get => InternalIsInFunction || FunctionCallStack.Count > 0;
        set => InternalIsInFunction = value;
    }

    public Dictionary<string, string> Variables => FunctionCallStack.Count == 0 ? topVariables : FunctionCallStack.Peek().Variables;
    public Dictionary<string, List<string>> Lists => FunctionCallStack.Count == 0 ? topLists : FunctionCallStack.Peek().Lists;

    public Dictionary<string, int> Labels { get => FunctionCallStack.Count == 0 ? field : FunctionCallStack.Peek().Labels; } = [];

    public RuntimeInformation ParentRuntimeInformation
    {
        get;
        set
        {
            field = value;
            field?.OnExit += ParentRuntimeInformation_OnExit;
        }
    }

    public bool IsSearching => !string.IsNullOrWhiteSpace(SearchLabel + SearchFunction) || (IsInFunction && FunctionCallStack.Count == 0);
    public bool IsLocalSearch { get; set; }

    public void WriteLine(string output, bool forceWrite = false)
    {
        if ((Stop && !forceWrite) || (ParentRuntimeInformation?.StopAllTasks == true && !forceWrite))
        {
            return;
        }

        if (IsDebugMode)
        {
            if (IsTask)
            {
                ParentRuntimeInformation!.WriteLine(output.FromSafeString(), forceWrite);
            }
            else
            {
                OnDebugOutput?.Invoke(output.FromSafeString() + Environment.NewLine);
            }
        }
        else
        {
            Console.WriteLine(output.FromSafeString());
        }
    }

    public void Write(string output, bool forceWrite = false)
    {
        if ((Stop && !forceWrite) || (ParentRuntimeInformation?.StopAllTasks == true && !forceWrite))
        {
            return;
        }

        if (IsDebugMode)
        {
            if (IsTask)
            {
                ParentRuntimeInformation!.Write(output.FromSafeString(), forceWrite);
            }
            else
            {
                OnDebugOutput?.Invoke(output.FromSafeString());
            }
        }
        else
        {
            Console.Write(output.FromSafeString());
        }
    }

    public void Exit(string message, bool stopAllTasks)
    {
        if (!Stop)
        {
            if (Lines.Count == 0)
            {
                WriteLine($"{Environment.NewLine}[{(IsTask ? $"Task {TaskId}" : "The process")} was terminated with the message: {message}]", true);
            }
            else
            {
                Line line = Lines[Math.Min(LineNumber, Lines.Count - 1)];
                WriteLine($"{Environment.NewLine}[{(IsTask ? $"Task {TaskId}" : "The process")} was terminated at line {line.LineNumber + 1} in the file \"{line.FileName}\" with the message: {message}]", true);
            }

            while (FunctionCallStack.Count > 0)
            {
                int stackLineNumber = FunctionCallStack.Pop().CallerLine;
                List<Line> targetLines = ParentRuntimeInformation?.Lines ?? Lines;
                if (stackLineNumber >= 0 && stackLineNumber < targetLines.Count)
                {
                    Line stackLine = targetLines[stackLineNumber];
                    WriteLine($"    at line {stackLine.LineNumber + 1} in the file \"{stackLine.FileName}\"", true);
                }
                else
                {
                    WriteLine("    at unknown location (source not available)", true);
                }
            }

            Stop = true;
        }
        if (stopAllTasks && !StopAllTasks)
        {
            StopAllTasks = true;
            OnExit?.Invoke(message, StopAllTasks);
            ParentRuntimeInformation?.Exit(ExitMessages.TerminatedByChildTask, true);
        }
    }

    public void LineExecuted(DebugEventArgs debugEventArgs)
    {
        if (IsTask)
        {
            ParentRuntimeInformation.LineExecuted(debugEventArgs);
        }
        else
        {
            OnLineExecuted?.Invoke(debugEventArgs);
        }
    }

    public void Reset()
    {
        topVariables.Clear();
        topLists.Clear();
        Lines.Clear();
        GlobalVariables.Clear();
        Labels.Clear();
        Functions.Clear();
        BlockBoundaries.Clear();
        FunctionCallStack.Clear();
        InParametersStack.Clear();
        OutParametersStack.Clear();
        ParentRuntimeInformation = null;
        SearchLabel = string.Empty;
        SearchFunction = string.Empty;
        WorkingDirectory = string.Empty;
        CurrentLine = string.Empty;
        Stop = false;
        StopAllTasks = false;
        IsDebugMode = false;
        IsInFunction = false;
        IsLocalSearch = false;
        LineNumber = 0;
        TaskId = ++internalTaskId;
    }

    private void ParentRuntimeInformation_OnExit(string exitMessage, bool stopAllTasks)
    {
        Exit(ExitMessages.TerminatedByParentTask, stopAllTasks);
    }
}