using System;

using YesNt.Interpreter.Enums;

namespace YesNt.Interpreter.Attributes;

/// <summary>
/// Marks a method as a YesNt statement handler.
/// The interpreter matches source lines against the <see cref="Name"/> keyword according to
/// <see cref="SearchMode"/> and <see cref="SpaceAround"/> rules, then invokes the decorated method
/// with the remaining argument text.
/// </summary>
/// <remarks>
/// Methods decorated with this attribute must be instance methods on a class that inherits
/// <see cref="Runtime.StatementRuntimeInformation"/> and must accept a single <see cref="string"/> parameter.
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class StatementAttribute : Attribute
{
    /// <summary>Gets the keyword that identifies this statement in source code.</summary>
    public string Name { get; }

    /// <summary>Gets where in the line the keyword is searched for.</summary>
    public SearchMode SearchMode { get; }

    /// <summary>Gets which sides of the keyword must be padded with a space.</summary>
    public SpaceAround SpaceAround { get; }

    /// <summary>Gets or sets the syntax-highlight color used by the code editor.</summary>
    public ConsoleColor Color { get; set; }

    /// <summary>
    /// Gets or sets the execution priority. Statements with a lower <see cref="Priority"/> value
    /// run before those with a higher value. Defaults to <see cref="Priority.Normal"/>.
    /// </summary>
    public Priority Priority { get; set; } = Priority.Normal;

    /// <summary>
    /// Gets or sets a value indicating whether this statement is still invoked while the interpreter
    /// is in search mode (scanning for a label or function definition). Defaults to <see langword="false"/>.
    /// </summary>
    public bool ExecuteInSearchMode { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the full current line (including the keyword itself)
    /// is passed as the argument, rather than stripping the keyword prefix/suffix first.
    /// Defaults to <see langword="false"/>.
    /// </summary>
    public bool KeepStatementInArgs { get; set; }

    /// <summary>
    /// Gets a value indicating whether this statement should be excluded from syntax highlighting.
    /// Set to <see langword="true"/> when no <see cref="Color"/> is provided.
    /// </summary>
    public bool IgnoreSyntaxHighlighting { get; }

    /// <summary>
    /// Gets or sets an optional sub-string that must also be present in the line for this statement
    /// to match. Used to differentiate overloaded keywords (e.g. <c>call</c> vs <c>call … with …</c>).
    /// </summary>
    public string Separator { get; set; }

    /// <summary>
    /// Gets or sets the name of the statement that marks the end of this block.
    /// Used for block boundary caching (e.g., "while" has BlockPair = "end_while").
    /// </summary>
    public string BlockPair { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this statement is the end of a block.
    /// Used for block boundary caching (e.g., "end_while" has IsBlockEnd = true).
    /// </summary>
    public bool IsBlockEnd { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this statement is an intermediate part of a block
    /// (e.g., "else:" between "if" and "end_if").
    /// </summary>
    public bool IsBlockIntermediate { get; set; }

    /// <summary>
    /// Initializes a new <see cref="StatementAttribute"/> with a syntax-highlight color.
    /// </summary>
    /// <param name="name">The keyword that identifies this statement.</param>
    /// <param name="searchMode">Where in the line the keyword is matched.</param>
    /// <param name="spaceAround">Which sides of the keyword require a surrounding space.</param>
    /// <param name="color">The color used for syntax highlighting in the code editor.</param>
    public StatementAttribute(string name, SearchMode searchMode, SpaceAround spaceAround, ConsoleColor color)
    {
        Name = name;
        SearchMode = searchMode;
        SpaceAround = spaceAround;
        Color = color;
    }

    /// <summary>
    /// Initializes a new <see cref="StatementAttribute"/> without a syntax-highlight color.
    /// The statement will be excluded from syntax highlighting.
    /// </summary>
    /// <param name="name">The keyword that identifies this statement.</param>
    /// <param name="searchMode">Where in the line the keyword is matched.</param>
    /// <param name="spaceAround">Which sides of the keyword require a surrounding space.</param>
    public StatementAttribute(string name, SearchMode searchMode, SpaceAround spaceAround)
    {
        Name = name;
        SearchMode = searchMode;
        SpaceAround = spaceAround;
        IgnoreSyntaxHighlighting = true;
    }
}
