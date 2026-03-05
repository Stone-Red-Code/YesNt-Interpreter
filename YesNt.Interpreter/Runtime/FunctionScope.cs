using System.Collections.Generic;

namespace YesNt.Interpreter.Runtime;

/// <summary>
/// Represents one frame on the function call stack. Created when a <c>call</c> statement is
/// executed and popped when the matching <c>return</c> is reached.
/// </summary>
internal class FunctionScope(int callerLine, Stack<string> arguments)
{
    /// <summary>Gets the zero-based line index to return to after this function completes.</summary>
    public int CallerLine { get; } = callerLine;

    /// <summary>Gets the local variable table for this function invocation.</summary>
    public Dictionary<string, string> Variables { get; } = [];

    /// <summary>Gets the local list table for this function invocation.</summary>
    public Dictionary<string, List<string>> Lists { get; } = [];

    /// <summary>Gets the local label table for this function invocation.</summary>
    public Dictionary<string, int> Labels { get; } = [];

    /// <summary>Gets the stack of input arguments passed to this function via <c>push_in</c>.</summary>
    public Stack<string> Arguments { get; } = arguments;

    /// <summary>Gets the stack of output values pushed via <c>push_out</c>, consumed by the caller via <c>%out</c>.</summary>
    public Stack<string> Results { get; } = new();
}