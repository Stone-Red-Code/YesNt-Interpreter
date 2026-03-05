using System;

using YesNt.Interpreter.Enums;

namespace YesNt.Interpreter.Attributes;

/// <summary>
/// Marks a parameterless method as a YesNt static statement handler.
/// Static statements are invoked once per line before regular statement matching begins,
/// regardless of whether the line matches any keyword. They are typically used for
/// pre-processing tasks such as transforming the current line before other statements run.
/// </summary>
/// <remarks>
/// Methods decorated with this attribute must be instance methods on a class that inherits
/// <see cref="Runtime.StatementRuntimeInformation"/> and must have no parameters.
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class StaticStatementAttribute : Attribute
{
    /// <summary>
    /// Gets or sets a value indicating whether this handler is still invoked while the interpreter
    /// is in search mode (scanning for a label or function definition). Defaults to <see langword="false"/>.
    /// </summary>
    public bool ExecuteInSearchMode { get; set; }

    /// <summary>
    /// Gets or sets the execution priority relative to other static statements.
    /// Defaults to <see cref="Priority.Normal"/>.
    /// </summary>
    public Priority Priority { get; set; } = Priority.Normal;
}