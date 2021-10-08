using System;

namespace YesNt.Interpreter.Runtime
{
    public class DebugEventArgs : EventArgs
    {
        public int LineNumber { get; internal set; }
        public string CurrentLine { get; internal set; }
        public string OriginalLine { get; internal set; }
    }
}