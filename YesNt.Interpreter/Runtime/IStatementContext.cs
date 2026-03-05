using System.Collections.Generic;

namespace YesNt.Interpreter.Runtime;

/// <summary>
/// Exposes the script runtime state accessible to custom statement handlers registered
/// via <see cref="YesNtInterpreter.AddStatement"/>.
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

    /// <summary>Terminates execution with the given message.</summary>
    /// <param name="message">The message written to debug output.</param>
    /// <param name="isError">
    /// <see langword="true"/> to signal an error termination;
    /// <see langword="false"/> for a planned, non-error termination.
    /// </param>
    void Exit(string message, bool isError);
}
