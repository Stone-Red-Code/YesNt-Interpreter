namespace YesNt.Interpreter.Enums;

/// <summary>
/// Controls the execution order of statements. Lower values run first.
/// </summary>
public enum Priority
{
    /// <summary>Runs before all other statements. Used for syntax pre-processing such as string literals.</summary>
    PreProcessing,

    /// <summary>Runs very early. Used for inline substitutions such as variable reads and parameter pops.</summary>
    Highest,

    /// <summary>Runs early.</summary>
    VeryHigh,

    /// <summary>Runs above normal order.</summary>
    High,

    /// <summary>Default execution order.</summary>
    Normal,

    /// <summary>Runs below normal order.</summary>
    Low,

    /// <summary>Runs last. Used for control-flow and variable definitions that depend on substitutions being complete.</summary>
    VeryLow
}