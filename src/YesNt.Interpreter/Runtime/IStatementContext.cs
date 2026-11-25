using System.Collections.Generic;

namespace YesNt.Interpreter.Runtime;

/// <summary>
/// Exposes the script runtime state accessible to custom statement handlers registered
/// via <see cref="YesNtInterpreter.AddStatement(YesNt.Interpreter.Runtime.StatementInformation, System.Action{string, YesNt.Interpreter.Runtime.IStatementContext})"/>.
/// </summary>
public interface IStatementContext
{
    /// <summary>Gets the local variable table for the current scope.</summary>
    Dictionary<string, string> Variables { get; }

    /// <summary>Gets or sets the global variable table shared across all scopes.</summary>
    Dictionary<string, string> GlobalVariables { get; set; }

    /// <summary>Gets or sets the text of the line currently being processed.
    /// Inline-substitution handlers (e.g. <c>%read_line</c>) write their result here.</summary>
    string CurrentLine { get; set; }

    /// <summary>Gets or sets the zero-based index of the next line to execute.
    /// Set this to implement control-flow jumps inside a custom statement.</summary>
    int LineNumber { get; set; }

    /// <summary>
    /// Gives the script a statement's result the way built-in statements such as <c>length</c>
    /// and <c>list get</c> do: the output stack is emptied, then <paramref name="values"/> are
    /// pushed in order, so the last is on top and the script's first <c>%out</c> reads it.
    /// Values are plain text: spaces and keywords in them stay part of the value when it is
    /// put into a line.
    /// </summary>
    /// <param name="values">The results, the last one on top.</param>
    void SetOut(params string[] values);

    /// <summary>Terminates execution with the given message.</summary>
    /// <param name="message">The message written to debug output.</param>
    /// <param name="stopAllTasks">
    /// If <see langword="true"/>, also terminates all tasks spawned by the <c>task</c> statement.
    /// If <see langword="false"/>, only terminates the current execution context (main script or individual task).
    /// </param>
    void Exit(string message, bool stopAllTasks);
}
