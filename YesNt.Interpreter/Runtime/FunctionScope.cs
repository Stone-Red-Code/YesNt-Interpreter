using System.Collections.Generic;

namespace YesNt.Interpreter.Runtime;

internal class FunctionScope(int callerLine, Stack<string> arguments)
{
    public int CallerLine { get; } = callerLine;
    public Dictionary<string, string> Variables { get; } = [];
    public Dictionary<string, List<string>> Lists { get; } = [];
    public Dictionary<string, int> Labels { get; } = [];
    public Stack<string> Arguments { get; } = arguments;
    public Stack<string> Results { get; } = new();
}
