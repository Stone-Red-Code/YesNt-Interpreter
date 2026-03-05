using YesNt.CodeEditor;

TextEditor textEditor = args.Length > 0 ? new TextEditor(args[0]) : new TextEditor();
textEditor.Run();