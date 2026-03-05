namespace YesNt.Interpreter.Enums;

/// <summary>
/// Determines where in a source line the interpreter searches for a statement keyword.
/// </summary>
public enum SearchMode
{
    /// <summary>The keyword must appear at the beginning of the line.</summary>
    StartOfLine,

    /// <summary>The keyword must appear at the end of the line.</summary>
    EndOfLine,

    /// <summary>The keyword may appear anywhere in the line.</summary>
    Contains,

    /// <summary>The entire line must exactly match the keyword.</summary>
    Exact
}