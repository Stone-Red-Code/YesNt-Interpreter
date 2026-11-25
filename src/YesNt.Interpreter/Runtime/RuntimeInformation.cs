using System;
using System.Collections.Generic;
using System.Threading;

using YesNt.Interpreter.Utilities;

namespace YesNt.Interpreter.Runtime;

/// <summary>
/// Holds all mutable runtime state for a single script execution, including variables, lists,
/// labels, functions, the call stack, the line counter, and stop flags.
/// Each background task spawned by the <c>task</c> statement owns its own
/// <see cref="RuntimeInformation"/> whose <see cref="ParentRuntimeInformation"/> points back
/// to the main execution context.
/// </summary>
internal sealed class RuntimeInformation : IStatementContext
{
    public event Action<string> OnDebugOutput;

    public event Action<DebugEventArgs> OnLineExecuted;

    private event Action<string, bool> OnExit;

    private static int internalTaskId = 0;
    private readonly Dictionary<string, string> topVariables = [];
    private readonly Dictionary<string, List<string>> topLists = [];
    private readonly Dictionary<string, Dictionary<string, string>> topMaps = [];

    public Dictionary<string, string> GlobalVariables { get; set; } = [];
    public Dictionary<string, int> Functions { get; } = [];
    public Dictionary<string, List<string>> FunctionParameters { get; } = [];
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
    public Dictionary<string, Dictionary<string, string>> Maps => FunctionCallStack.Count == 0 ? topMaps : FunctionCallStack.Peek().Maps;

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

    /// <summary>
    /// Gets or sets the script's console and files. Kept by <see cref="Reset"/>: it belongs to
    /// the interpreter, not to one run.
    /// </summary>
    public IYesNtHost Host { get; set; } = new ConsoleHost();

    /// <summary>
    /// Gets or sets how the interpreter starts a task from the given lines at the given line
    /// (set by the interpreter, which knows its statements, host and task mode).
    /// </summary>
    internal Action<List<Line>, int> StartTaskAction { get; set; }

    /// <summary>
    /// Gets what the script waits for before it goes on (<see cref="WaitUntil"/>,
    /// <see cref="RetryLine"/>); <see langword="null"/> when it does not wait.
    /// </summary>
    public Func<bool> WaitCondition { get; private set; }

    /// <summary>
    /// Gets whether the script waits to run the current line again (<see cref="RetryLine"/>).
    /// </summary>
    public bool IsRetryingLine { get; private set; }

    /// <summary>
    /// Gets the text the current line runs again from, set by <see cref="RetryLine"/>.
    /// </summary>
    public string RetryText { get; private set; }

    public void SetOut(params string[] values)
    {
        OutParametersStack.Clear();

        foreach (string value in values)
        {
            OutParametersStack.Push((value ?? string.Empty).ToSafeString());
        }
    }

    public void WaitUntil(Func<bool> ready)
    {
        ArgumentNullException.ThrowIfNull(ready);
        WaitCondition = ready;
    }

    public void RetryLine(Func<bool> ready = null)
    {
        WaitCondition = ready ?? (() => true);
        IsRetryingLine = true;
        RetryText = CurrentLine;
        CurrentLine = string.Empty;
    }

    /// <summary>
    /// Says whether the script may go on: no wait, or one that is over (which ends it). A stopped
    /// script never waits.
    /// </summary>
    public bool TryEndWait()
    {
        if (WaitCondition is null || Stop || WaitCondition())
        {
            WaitCondition = null;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Takes the text the line runs again from, after <see cref="RetryLine"/>; <see langword="null"/>
    /// if the line is not run again.
    /// </summary>
    public string TakeRetryText()
    {
        string text = RetryText;
        RetryText = null;
        return text;
    }

    /// <summary>
    /// Ends a retry once the line was left as it is, to run again (<see cref="RetryLine"/>).
    /// </summary>
    public bool TakeRetry()
    {
        bool retrying = IsRetryingLine;
        IsRetryingLine = false;
        return retrying;
    }

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
            Host.Write(output.FromSafeString() + Environment.NewLine);
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
            Host.Write(output.FromSafeString());
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
        topMaps.Clear();
        Lines.Clear();
        GlobalVariables.Clear();
        Labels.Clear();
        Functions.Clear();
        FunctionParameters.Clear();
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
        WaitCondition = null;
        IsRetryingLine = false;
        RetryText = null;
#pragma warning disable S2696 // internalTaskId is a shared counter intentionally incremented by each Reset call
        TaskId = Interlocked.Increment(ref internalTaskId);
#pragma warning restore S2696
    }

    private void ParentRuntimeInformation_OnExit(string exitMessage, bool stopAllTasks)
    {
        Exit(ExitMessages.TerminatedByParentTask, stopAllTasks);
    }
}