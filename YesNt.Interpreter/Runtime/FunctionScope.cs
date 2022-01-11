using System.Collections.Generic;

namespace YesNt.Interpreter.Runtime
{
    internal class FunctionScope
    {
        public int CallerLine { get; }
        public Dictionary<string, string> Variables { get; } = new();
        public Stack<string> Arguemtns { get; } = new();
        public Stack<string> Results { get; } = new();

        public FunctionScope(int callerLine, Stack<string> arguemtns)
        {
            CallerLine = callerLine;
            Arguemtns = arguemtns;
        }
    }
}