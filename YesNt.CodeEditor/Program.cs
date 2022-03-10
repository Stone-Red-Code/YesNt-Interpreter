namespace YesNt.CodeEditor
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            TextEditor textEditor;

            if (args.Length > 0)
            {
                textEditor = new TextEditor(args[0]);
            }
            else
            {
                textEditor = new TextEditor();
            }

            textEditor.Run();
        }
    }
}