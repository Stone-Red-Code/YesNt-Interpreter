namespace YesNt.CodeEditor
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            TextEditor textEditor = args.Length > 0 ? new TextEditor(args[0]) : new TextEditor();
            textEditor.Run();
        }
    }
}