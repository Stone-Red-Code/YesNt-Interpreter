using System.Collections.Generic;

namespace YesNt.Interpreter.Runtime
{
    internal class FunctionScope
    {
        public int CallerLine { get; }
        public Dictionary<string, string> Variables { get; } = new();

        public FunctionScope(int callerLine)
        {
            CallerLine = callerLine;
        }
    }
}