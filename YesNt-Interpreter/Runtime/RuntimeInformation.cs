using System;
using System.Collections.Generic;

using YesNt.Interpreter.Utilities;

namespace YesNt.Interpreter
{
    internal class RuntimeInformation
    {
        public Dictionary<string, string> Variables { get; } = new();
        public Dictionary<string, int> Labels { get; } = new();
        public List<string> Lines { get; set; } = new();
        public string CurrentLine { get; set; } = string.Empty;
        public Stack<int> LabelStack { get; set; } = new();
        public string SearchLabel { get; set; } = string.Empty;
        public int LineNumber { get; set; } = 0;
        public bool Stop { get; private set; } = false;
        public bool IsDebugMode { get; set; }

        public event Action<string> OnDebugOutput;

        public void WriteLine(string output)
        {
            if (IsDebugMode)
            {
                OnDebugOutput?.Invoke(output.FromSaveString() + Environment.NewLine);
            }
            else
            {
                Console.WriteLine(output.FromSaveString());
            }
        }

        public void Write(string output)
        {
            if (IsDebugMode)
            {
                OnDebugOutput?.Invoke(output.FromSaveString());
            }
            else
            {
                Console.Write(output.FromSaveString());
            }
        }

        public void Exit(string message)
        {
            WriteLine($"{Environment.NewLine}[The process was terminated at line {LineNumber + 1} with the message: {message}]");
            Stop = true;
        }

        public void Reset()
        {
            CurrentLine = string.Empty;
            Lines.Clear();
            Variables.Clear();
            Labels.Clear();
            LabelStack.Clear();
            SearchLabel = string.Empty;
            Stop = false;
            IsDebugMode = false;
            LineNumber = 0;
        }
    }
}