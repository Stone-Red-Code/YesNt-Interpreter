using System;

namespace YesNt.CodeEditor
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            TextEditor textEditor;

            Console.CancelKeyPress += Console_CancelKeyPress;
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

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
        }
    }
}