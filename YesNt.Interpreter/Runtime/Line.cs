namespace YesNt.Interpreter.Runtime;

internal class Line(string content, string fileName, int lineNumber)
{
    public string Content { get; set; } = content;

    public string FileName { get; set; } = fileName;

    public int LineNumber { get; set; } = lineNumber;
}