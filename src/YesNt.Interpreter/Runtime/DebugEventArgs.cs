using System;

namespace YesNt.Interpreter.Runtime;

/// <summary>
/// Provides per-line execution data raised through <see cref="YesNtInterpreter.OnLineExecuted"/>.
/// </summary>
public class DebugEventArgs : EventArgs
{
    /// <summary>Gets the 1-based line number of the executed line within its source file.</summary>
    public int LineNumber { get; internal set; }

    /// <summary>
    /// Gets the line content after all statement transformations have been applied
    /// (e.g. after variable substitution). May differ from <see cref="OriginalLine"/>.
    /// </summary>
    public string CurrentLine { get; internal set; }

    /// <summary>Gets the raw line content as it appeared in the source file.</summary>
    public string OriginalLine { get; internal set; }

    /// <summary>
    /// Gets the task identifier of the task that executed this line, or <c>0</c> if the line
    /// was executed on the main thread.
    /// </summary>
    public int TaskId { get; internal set; }

    /// <summary>
    /// Gets a value indicating whether this line was executed inside a background task
    /// (spawned with the <c>task</c> statement).
    /// </summary>
    public bool IsTask { get; internal set; }
}