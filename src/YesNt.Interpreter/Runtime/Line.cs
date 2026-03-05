namespace YesNt.Interpreter.Runtime;

/// <summary>
/// Represents a single source line together with its location metadata.
/// </summary>
internal class Line(string content, string fileName, int lineNumber)
{
    /// <summary>Gets or sets the raw text content of the line.</summary>
    public string Content { get; set; } = content;

    /// <summary>Gets or sets the name of the source file this line originated from.</summary>
    public string FileName { get; set; } = fileName;

    /// <summary>Gets or sets the zero-based line index within <see cref="FileName"/>.</summary>
    public int LineNumber { get; set; } = lineNumber;
}