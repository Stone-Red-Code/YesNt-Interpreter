namespace YesNt.Interpreter.Enums;

/// <summary>
/// Specifies which sides of a statement keyword must be surrounded by a space when matching.
/// </summary>
public enum SpaceAround
{
    /// <summary>A space is required both before and after the keyword.</summary>
    StartEnd,

    /// <summary>A space is required before the keyword only.</summary>
    Start,

    /// <summary>A space is required after the keyword only.</summary>
    End,

    /// <summary>No surrounding spaces are required.</summary>
    None
}