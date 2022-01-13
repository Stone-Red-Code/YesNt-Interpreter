namespace YesNt.Interpreter.Runtime
{
    internal class Line
    {
        public Line(string content, string fileName, int lineNumber)
        {
            Content = content;
            FileName = fileName;
            LineNumber = lineNumber;
        }

        public string Content { get; set; }
        public string FileName { get; set; }
        public int LineNumber { get; set; }
    }
}