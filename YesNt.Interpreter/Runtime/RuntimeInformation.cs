using System;
using System.Collections.Generic;

using YesNt.Interpreter.Runtime;
using YesNt.Interpreter.Utilities;

namespace YesNt.Interpreter
{
    internal class RuntimeInformation
    {
        private RuntimeInformation parentRuntimeInformation;
        private static int internalTaskId = 0;
        private int taskId = 0;
        private readonly Dictionary<string, string> topVariables = new();

        public Dictionary<string, string> GloablVariables { get; set; } = new();
        public Dictionary<string, int> Labels { get; } = new();
        public Dictionary<string, int> Functions { get; } = new();
        public Stack<FunctionScope> FunctionCallStack { get; } = new();
        public List<string> Lines { get; set; } = new();
        public string CurrentLine { get; set; } = string.Empty;
        public string SearchLabel { get; set; } = string.Empty;
        public string SearchFunction { get; set; } = string.Empty;
        public int LineNumber { get; set; } = 0;
        public bool Stop { get; private set; } = false;
        public bool StopAllTasks { get; private set; } = false;
        public bool IsDebugMode { get; set; } = false;
        public string CurrentFilePath { get; set; } = string.Empty;
        public bool IsTask => ParentRuntimeInformation is not null;
        public int TaskId => IsTask ? taskId : 0;

        public bool InternalIsInFunction { get; private set; } = false;

        public bool IsInFunction
        {
            get => InternalIsInFunction || FunctionCallStack.Count > 0;
            set => InternalIsInFunction = value;
        }

        public Dictionary<string, string> Variables
        {
            get
            {
                if (FunctionCallStack.Count == 0)
                {
                    return topVariables;
                }
                else
                {
                    return FunctionCallStack.Peek().Variables;
                }
            }
        }

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

        public bool IsSearching => !string.IsNullOrWhiteSpace(SearchLabel + SearchFunction) || IsInFunction && FunctionCallStack.Count == 0;

        private event Action<string, bool> OnExit;

        public event Action<string> OnDebugOutput;

        public event Action<DebugEventArgs> OnLineExecuted;

        private void ParentRuntimeInformation_OnExit(string exitMessage, bool stopChildTasks)
        {
            Exit($"Terminated by parent task", stopChildTasks);
        }

        public void WriteLine(string output, bool forceWrite = false)
        {
            if (Stop && !forceWrite || parentRuntimeInformation?.StopAllTasks == true && !forceWrite)
            {
                return;
            }

            if (IsDebugMode)
            {
                if (IsTask)
                {
                    parentRuntimeInformation.WriteLine(output.FromSaveString(), forceWrite);
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
            if (Stop && !forceWrite || parentRuntimeInformation?.StopAllTasks == true && !forceWrite)
            {
                return;
            }

            if (IsDebugMode)
            {
                if (IsTask)
                {
                    parentRuntimeInformation.Write(output.FromSaveString(), forceWrite);
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
                WriteLine($"{Environment.NewLine}[{(IsTask ? $"Task: {TaskId}" : "The process")} was terminated at line {LineNumber + 1} with the message: {message}]", true);
                Stop = true;
            }
            if (stopAllTasks == true && StopAllTasks == false)
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
            CurrentFilePath = string.Empty;
            CurrentLine = string.Empty;
            Stop = false;
            StopAllTasks = false;
            IsDebugMode = false;
            IsInFunction = false;
            InternalIsInFunction = false;
            LineNumber = 0;
            taskId = internalTaskId + 1;
            internalTaskId++;
        }
    }
}