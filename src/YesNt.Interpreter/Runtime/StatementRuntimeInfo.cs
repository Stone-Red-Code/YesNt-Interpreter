namespace YesNt.Interpreter.Runtime;

/// <summary>
/// Base class for all classes that host statement handler methods.
/// Subclasses declare methods decorated with <see cref="Attributes.StatementAttribute"/> or
/// <see cref="Attributes.StaticStatementAttribute"/>; the source generator
/// (<c>GeneratedStatementRegistry</c>) discovers these at compile time and wires them up.
/// </summary>
internal abstract class StatementRuntimeInformation
{
    /// <summary>
    /// Gets or sets the runtime state for the current execution context.
    /// Injected by the generated registry before any handler is invoked.
    /// </summary>
    public RuntimeInformation RuntimeInfo { get; set; }

    /// <summary>
    /// Trims surrounding whitespace and a trailing colon from a block or function name.
    /// </summary>
    protected static string NormalizeBlockName(string value)
    {
        return value.Trim().TrimEnd(':').Trim();
    }
}