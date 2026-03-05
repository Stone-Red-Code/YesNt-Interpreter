using System;

using YesNt.Interpreter.Enums;

namespace YesNt.Interpreter.Runtime;

/// <summary>
/// A read-only snapshot of a registered statement's metadata, used for tooling such as
/// syntax highlighters. Instances are obtained from <see cref="YesNtInterpreter.StatementInformation"/>.
/// </summary>
public class StatementInformation
{
    /// <summary>Gets the keyword that identifies this statement in source code.</summary>
    public string Name { get; internal set; }

    /// <summary>Gets where in the line the keyword is searched for.</summary>
    public SearchMode SearchMode { get; internal set; }

    /// <summary>Gets which sides of the keyword must be padded with a space.</summary>
    public SpaceAround SpaceAround { get; internal set; }

    /// <summary>Gets the syntax-highlight color for this statement.</summary>
    public ConsoleColor Color { get; internal set; }

    /// <summary>
    /// Gets a value indicating whether this statement is excluded from syntax highlighting.
    /// </summary>
    public bool IgnoreSyntaxHighlighting { get; internal set; }

    /// <summary>
    /// Gets the optional sub-string that must be present in the line for this statement to match,
    /// or <see langword="null"/> if no separator is required.
    /// </summary>
    public string Separator { get; set; }
}