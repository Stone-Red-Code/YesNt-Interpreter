using System;
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

    /// <summary>
    /// Holds the script before its next line until <paramref name="ready"/> returns
    /// <see langword="true"/>, without blocking: <see cref="YesNtInterpreter.Step"/> and
    /// <see cref="YesNtInterpreter.RunFor"/> return <see cref="StepResult.Waiting"/> meanwhile,
    /// so a host's loop can do other work. Use it for a statement that takes time, such as
    /// <c>sleep</c>. A stopped script stops waiting.
    /// </summary>
    /// <param name="ready">Asked each time the script is stepped; called on the stepping thread.</param>
    void WaitUntil(Func<bool> ready);

    /// <summary>
    /// Runs the current line again once <paramref name="ready"/> returns <see langword="true"/>
    /// (or at the next step, when it is <see langword="null"/>), starting from
    /// <see cref="CurrentLine"/> as it is now. Use it when something the line needs is not
    /// there yet, such as input the user has not entered: replace what is already known in
    /// <see cref="CurrentLine"/> and ask again for the rest when the line runs again. Statements
    /// after this one do not run on the line meanwhile.
    /// </summary>
    /// <param name="ready">Asked each time the script is stepped, or <see langword="null"/>.</param>
    void RetryLine(Func<bool> ready = null);

    /// <summary>Terminates execution with the given message.</summary>
    /// <param name="message">The message written to debug output.</param>
    /// <param name="stopAllTasks">
    /// If <see langword="true"/>, also terminates all tasks spawned by the <c>task</c> statement.
    /// If <see langword="false"/>, only terminates the current execution context (main script or individual task).
    /// </param>
    void Exit(string message, bool stopAllTasks);
}
