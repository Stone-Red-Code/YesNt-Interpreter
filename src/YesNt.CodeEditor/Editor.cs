using System;
using System.Collections.Generic;
using System.IO;
using System.Timers;

using YesNt.Interpreter.Runtime;

namespace YesNt.CodeEditor;

internal class TextEditor
{
    private readonly InputHandler inputHandler;
    private readonly SyntaxHighlighter syntaxHighlighter;
    private readonly List<string> debugOutput = [];

    private readonly Point oldSize = new Point(0, 0);
    public YesNtInterpreter YesNtInterpreter { get; } = new();
    public int LineOffset { get; set; } = 0;
    public List<string> Lines { get; } = [];
    public Point CursorPosition { get; } = new(0, 0);
    public Mode EditMode { get; set; } = Mode.Command;
    public string CurrentPath { get; set; } = string.Empty;
    public bool IsStepDebugMode { get; set; }

    public TextEditor(string path) : this()
    {
        if (File.Exists(path))
        {
            _ = Load(path);
        }
    }

    public TextEditor()
    {
        YesNtInterpreter.OnDebugOutput += YesNtInterpreter_OnDebugOutput;
        YesNtInterpreter.OnLineExecuted += YesNtInterpreter_OnLineExecuted;
        syntaxHighlighter = new(YesNtInterpreter.StatementInformation);
        inputHandler = new InputHandler(this);
        Console.CancelKeyPress += Console_CancelKeyPress;

        Timer timer = new Timer(100);
        timer.Elapsed += (s, e) =>
        {
            if (SizeChanged() && EditMode != Mode.Debug)
            {
                Display(true);

                if (EditMode == Mode.Command)
                {
                    InputHandler.WriteStatus(string.Empty);
                    Console.SetCursorPosition(3, Console.WindowHeight - 2);
                }
            }
        };
        timer.Start();
    }

    public void Run()
    {
        Console.Clear();

        Display(true);
        do
        {
            Display(false);
        } while (inputHandler.HandleInput());

        Console.Clear();
    }

    public void Display(bool drawAll)
    {
        Console.CursorVisible = false;
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.BackgroundColor = ConsoleColor.Black;

        Console.SetCursorPosition(0, 0);

        if (SizeChanged())
        {
            drawAll = true;
            InputHandler.WriteStatus(string.Empty);
        }

        if (LineOffset < 0 || CursorPosition.Y < 0 || CursorPosition.Y < 0)
        {
            LineOffset = 0;
            CursorPosition.Y = 0;
            CursorPosition.X = 0;
        }

        for (int i = LineOffset; i < Console.WindowHeight + LineOffset - 2; i++)
        {
            Console.SetCursorPosition(0, i - LineOffset);

            string lineCountString = $"{i + 1}".PadRight(GetSpacing(), ' ') + "| ";
            if (i < Lines.Count)
            {
                if (CursorPosition.Y == i || drawAll)
                {
                    Console.Write(lineCountString);
                    string printLine = $"{Lines[i][..Math.Min(Lines[i].Length, Console.WindowWidth)]}".TrimEnd();
                    syntaxHighlighter.Write(printLine);
                    Console.Write(new string(' ', Math.Max(Console.WindowWidth - lineCountString.Length - printLine.Length, 0)));
                }
            }
            else if (drawAll || CursorPosition.Y == i)
            {
                Console.Write(lineCountString + new string(' ', Console.WindowWidth - lineCountString.Length));
            }
        }

        Console.SetCursorPosition(0, Console.WindowHeight - 3);
        Console.Write(new string('-', Console.WindowWidth));
        Console.SetCursorPosition(0, Console.WindowHeight - 2);
        Console.Write(">>>" + new string(' ', Console.WindowWidth - 3));

        Console.SetCursorPosition(Math.Min(CursorPosition.X + GetSpacing() + 2, Console.WindowWidth - 1), Math.Min(CursorPosition.Y - LineOffset, Console.WindowHeight - 4));

        Console.CursorVisible = true;
    }

    public bool Load(string path)
    {
        if (string.IsNullOrEmpty(Path.GetExtension(path)))
        {
            path = Path.ChangeExtension(path, "ynt");
        }

        if (!File.Exists(path))
        {
            InputHandler.WriteStatus("File does not exist!");
            return false;
        }

        Lines.Clear();
        Lines.AddRange(File.ReadAllLines(path));
        CurrentPath = path;
        InputHandler.WriteStatus("File Loaded!");
        return true;
    }

    public bool Save(string input, bool loadIfExists)
    {
        string path;
        if (input.Split(' ').Length == 2)
        {
            path = input.Split(' ')[1];

            if (CurrentPath.Trim() != path.Trim() && loadIfExists)
            {
                return Load(path);
            }
            if (string.IsNullOrEmpty(Path.GetExtension(path)))
            {
                path = Path.ChangeExtension(path, "ynt");
            }
            CurrentPath = path;
        }
        else if (!string.IsNullOrWhiteSpace(CurrentPath))
        {
            path = CurrentPath;
        }
        else if (input.Split(' ').Length > 2)
        {
            InputHandler.WriteStatus("Invalid arguments!");
            return false;
        }
        else
        {
            InputHandler.WriteStatus("File path is empty! (Save the file before you can use this command)");
            return false;
        }

        if (string.IsNullOrEmpty(Path.GetExtension(path)))
        {
            path = Path.ChangeExtension(path, "ynt");
        }

        while (Lines.Count > 0 && string.IsNullOrWhiteSpace(Lines[^1]))
        {
            Lines.RemoveAt(Lines.Count - 1);
        }

        try
        {
            File.WriteAllLines(path, Lines);
            InputHandler.WriteStatus("File Saved!");
            return true;
        }
        catch (Exception ex)
        {
            InputHandler.WriteStatus(ex.Message);
            return false;
        }
    }

    public int GetSpacing()
    {
        int padding = (Console.WindowHeight + LineOffset - 3).ToString().Length;
        padding = Math.Max(padding, Lines.Count.ToString().Length);
        padding += 1;
        return padding;
    }

    public void FormatLines()
    {
        const int indentationSize = 4;
        List<string> blockStack = [];

        for (int i = 0; i < Lines.Count; i++)
        {
            string trimmed = Lines[i].Trim(' ');
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                Lines[i] = string.Empty;
                continue;
            }

            if (trimmed.StartsWith('#'))
            {
                Lines[i] = trimmed;
                continue;
            }

            if (trimmed == "else:")
            {
                for (int j = blockStack.Count - 1; j >= 0; j--)
                {
                    if (blockStack[j] is "if" or "else")
                    {
                        blockStack.RemoveAt(j);
                        break;
                    }
                }
            }
            bool isTerminatingStatement = trimmed == "exit"
                || trimmed.StartsWith("throw ", StringComparison.Ordinal)
                || trimmed.StartsWith("error ", StringComparison.Ordinal);
            bool closesFunctionBlock = isTerminatingStatement && blockStack.Count > 0 && blockStack[^1] == "func";

            if (trimmed == "end_if")
            {
                for (int j = blockStack.Count - 1; j >= 0; j--)
                {
                    if (blockStack[j] is "if" or "else")
                    {
                        blockStack.RemoveAt(j);
                        break;
                    }
                }
            }
            else if (trimmed == "end_while")
            {
                for (int j = blockStack.Count - 1; j >= 0; j--)
                {
                    if (blockStack[j] == "while")
                    {
                        blockStack.RemoveAt(j);
                        break;
                    }
                }
            }
            else if (trimmed == "return")
            {
                for (int j = blockStack.Count - 1; j >= 0; j--)
                {
                    if (blockStack[j] == "func")
                    {
                        blockStack.RemoveAt(j);
                        break;
                    }
                }
            }

            int lineIndentation = closesFunctionBlock ? Math.Max(0, blockStack.Count - 1) : blockStack.Count;
            Lines[i] = new string(' ', lineIndentation * indentationSize) + trimmed;

            if (!isTerminatingStatement && (
                (trimmed.StartsWith("if ", StringComparison.Ordinal) && trimmed.EndsWith(':'))
                || (trimmed.StartsWith("while ", StringComparison.Ordinal) && trimmed.EndsWith(':'))
                || (trimmed.StartsWith("func ", StringComparison.Ordinal) && trimmed.EndsWith(':'))
                || trimmed == "else:"))
            {
                if (trimmed.StartsWith("if ", StringComparison.Ordinal))
                {
                    blockStack.Add("if");
                }
                else if (trimmed.StartsWith("while ", StringComparison.Ordinal))
                {
                    blockStack.Add("while");
                }
                else if (trimmed.StartsWith("func ", StringComparison.Ordinal))
                {
                    blockStack.Add("func");
                }
                else
                {
                    blockStack.Add("else");
                }
            }

            if (isTerminatingStatement)
            {
                while (blockStack.Count > 0 && blockStack[^1] != "func")
                {
                    blockStack.RemoveAt(blockStack.Count - 1);
                }

                if (closesFunctionBlock && blockStack.Count > 0 && blockStack[^1] == "func")
                {
                    blockStack.RemoveAt(blockStack.Count - 1);
                }
            }
        }
    }

    private static string ToLiteral(string input)
    {
        return Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(input, false);
    }

    private void YesNtInterpreter_OnDebugOutput(string output)
    {
        debugOutput.Add(output);
    }

    private void YesNtInterpreter_OnLineExecuted(DebugEventArgs e)
    {
        lock (Console.Out)
        {
            if (e is not null)
            {
                Console.ForegroundColor = ConsoleColor.Magenta;

                string sharedString = (Console.CursorLeft != 0) ? Environment.NewLine : string.Empty;
                sharedString += (e.IsTask ? $"[Task: {e.TaskId}]" : string.Empty) + $"[{e.LineNumber}]";

                if (e.OriginalLine == e.CurrentLine)
                {
                    Console.WriteLine($"{sharedString}[{ToLiteral(e.CurrentLine)}] ==>");
                }
                else
                {
                    Console.WriteLine($"{sharedString}[{ToLiteral(e.OriginalLine)}] => [{ToLiteral(e.CurrentLine)}] ==>");
                }
                Console.ForegroundColor = ConsoleColor.Gray;
            }
            string[] outputs = debugOutput.ToArray();
            debugOutput.Clear();

            foreach (string output in outputs)
            {
                Console.Write(output);
            }

            if (IsStepDebugMode && e is not null && !e.IsTask)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("[Step] Press any key for next line (Ctrl+C to stop)...");
                Console.ForegroundColor = ConsoleColor.Gray;
                _ = Console.ReadKey(true);
            }
        }
    }

    private void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
    {
        e.Cancel = true;
        switch (EditMode)
        {
            case Mode.Debug:
                YesNtInterpreter.Stop();
                EditMode = Mode.Command;
                break;
        }
    }

    private bool SizeChanged()
    {
        if (oldSize.X != Console.WindowWidth || oldSize.Y != Console.WindowHeight)
        {
            oldSize.X = Console.WindowWidth;
            oldSize.Y = Console.WindowHeight;
            return true;
        }
        return false;
    }
}

internal class Point(int x, int y)
{
    public int X { get; set; } = x;
    public int Y { get; set; } = y;
}

internal enum Mode
{
    Edit,
    Command,
    Debug
}