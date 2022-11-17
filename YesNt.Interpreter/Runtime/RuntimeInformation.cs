using System;
using System.Collections.Generic;
using System.Linq;

using YesNt.Interpreter.Utilities;

namespace YesNt.Interpreter.Runtime
{
    internal sealed class RuntimeInformation
    {
        private RuntimeInformation parentRuntimeInformation;
        private static int internalTaskId = 0;
        private int taskId = 0;

        private readonly Dictionary<string, string> topVariables = new();
        private readonly Dictionary<string, int> topLabels = new();

        public Dictionary<string, string> GloablVariables { get; set; } = new();
        public Dictionary<string, int> Functions { get; } = new();
        public Stack<FunctionScope> FunctionCallStack { get; } = new();
        public Stack<string> InParametersStack { get; } = new();
        public Stack<string> OutParametersStack { get; set; } = new();
        public List<Line> Lines { get; set; } = new();
        public string CurrentLine { get; set; } = string.Empty;
        public string SearchLabel { get; set; } = string.Empty;
        public string SearchFunction { get; set; } = string.Empty;
        public int LineNumber { get; set; } = 0;
        public bool Stop { get; private set; } = false;
        public bool StopAllTasks { get; private set; } = false;
        public bool IsDebugMode { get; set; } = false;
        public string WorkingDirectory { get; set; } = string.Empty;
        public bool IsTask => ParentRuntimeInformation is not null;
        public int TaskId => IsTask ? taskId : 0;
        public bool InternalIsInFunction { get; set; }

        public bool IsInFunction
        {
            get => InternalIsInFunction || FunctionCallStack.Count > 0;
            set => InternalIsInFunction = value;
        }

        public Dictionary<string, string> Variables => FunctionCallStack.Count == 0 ? topVariables : FunctionCallStack.Peek().Variables;

        public Dictionary<string, int> Labels => FunctionCallStack.Count == 0 ? topLabels : FunctionCallStack.Peek().Labels;

        public RuntimeInformation ParentRuntimeInformation
        {
            get => parentRuntimeInformation;
            set
            {
                parentRuntimeInformation = value;
                if (parentRuntimeInformation is not null)
                {
                    parentRuntimeInformation.OnExit += ParentRuntimeInformation_OnExit;
                }
            }
        }

        public bool IsSearching => !string.IsNullOrWhiteSpace(SearchLabel + SearchFunction) || (IsInFunction && FunctionCallStack.Count == 0);
        public bool IsLocalSearch { get; set; }

        private event Action<string, bool> OnExit;

        public event Action<string> OnDebugOutput;

        public event Action<DebugEventArgs> OnLineExecuted;

        private void ParentRuntimeInformation_OnExit(string exitMessage, bool stopAllTasks)
        {
            Exit($"Terminated by parent task", stopAllTasks);
        }

        public void WriteLine(string output, bool forceWrite = false)
        {
            if ((Stop && !forceWrite) || (parentRuntimeInformation?.StopAllTasks == true && !forceWrite))
            {
                return;
            }

            if (IsDebugMode)
            {
                if (IsTask)
                {
                    parentRuntimeInformation!.WriteLine(output.FromSaveString(), forceWrite);
                }
                else
                {
                    OnDebugOutput?.Invoke(output.FromSaveString() + Environment.NewLine);
                }
            }
            else
            {
                Console.WriteLine(output.FromSaveString());
            }
        }

        public void Write(string output, bool forceWrite = false)
        {
            if ((Stop && !forceWrite) || (parentRuntimeInformation?.StopAllTasks == true && !forceWrite))
            {
                return;
            }

            if (IsDebugMode)
            {
                if (IsTask)
                {
                    parentRuntimeInformation!.Write(output.FromSaveString(), forceWrite);
                }
                else
                {
                    OnDebugOutput?.Invoke(output.FromSaveString());
                }
            }
            else
            {
                Console.Write(output.FromSaveString());
            }
        }

        public void Exit(string message, bool stopAllTasks)
        {
            if (!Stop)
            {
                Line line = Lines[Math.Min(LineNumber, Lines.Count - 1)];
                WriteLine($"{Environment.NewLine}[{(IsTask ? $"Task {TaskId}" : "The process")} was terminated at line {line.LineNumber + 1} in the file \"{line.FileName}\" with the message: {message}]", true);
                while (FunctionCallStack.Count > 0)
                {
                    int stackLineNumber = FunctionCallStack.Pop().CallerLine;
                    Line stackLine = (ParentRuntimeInformation?.Lines ?? Lines).ElementAt(stackLineNumber);
                    WriteLine($"    at line {stackLine.LineNumber + 1} in the file \"{stackLine.FileName}\"", true);
                }

                Stop = true;
            }
            if (stopAllTasks && !StopAllTasks)
            {
                StopAllTasks = true;
                OnExit?.Invoke(message, StopAllTasks);
                parentRuntimeInformation?.Exit("Terminated by child task", true);
            }
        }

        public void LineExecuted(DebugEventArgs debugEventArgs)
        {
            if (IsTask)
            {
                parentRuntimeInformation.LineExecuted(debugEventArgs);
            }
            else
            {
                OnLineExecuted?.Invoke(debugEventArgs);
            }
        }

        public void Reset()
        {
            topVariables.Clear();
            Lines.Clear();
            GloablVariables.Clear();
            Labels.Clear();
            Functions.Clear();
            FunctionCallStack.Clear();
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
            taskId = internalTaskId + 1;
#pragma warning disable S2696 // Instance members should not write to "static" fields
            internalTaskId++;
#pragma warning restore S2696 // Instance members should not write to "static" fields
        }
    }
}