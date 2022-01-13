using System;
using System.Collections.Generic;
using System.IO;

using YesNt.Interpreter;

namespace YesNt.CodeEditor
{
    internal class TextEditor
    {
        private readonly InputHandler inputHandler;
        private readonly SyntaxHighlighter syntaxHighlighter;
        private readonly List<string> debugOutput = new();

        public YesNtInterpreter YesNtInterpreter { get; } = new();
        public int LineOffset { get; set; } = 0;
        public List<string> Lines { get; } = new();
        public Point CursorPosition { get; } = new(0, 0);
        public Mode EditMode { get; set; } = Mode.Command;
        public string CurrentPath { get; set; } = string.Empty;

        public TextEditor(string path) : this()
        {
            if (File.Exists(path))
            {
                Load(path);
            }
        }

        public TextEditor()
        {
            YesNtInterpreter.Initialize();
            YesNtInterpreter.OnDebugOutput += YesNtInterpreter_OnDebugOutput;
            YesNtInterpreter.OnLineExecuted += YesNtInterpreter_OnLineExecuted;
            syntaxHighlighter = new(YesNtInterpreter.StatementInformation);
            inputHandler = new InputHandler(this);
            Console.CancelKeyPress += Console_CancelKeyPress;
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
                inputHandler.WriteStatus(string.Empty);
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

            Console.SetCursorPosition(Math.Min(CursorPosition.X + GetSpacing() + 2, Console.WindowWidth - 1), CursorPosition.Y - LineOffset);

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
                inputHandler.WriteStatus("File does not exist!");
                return false;
            }

            Lines.Clear();
            Lines.AddRange(File.ReadAllLines(path));
            CurrentPath = path;
            inputHandler.WriteStatus("File Loaded!");
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
                    if (Load(path))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
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
                inputHandler.WriteStatus("Invalid arguments!");
                return false;
            }
            else
            {
                inputHandler.WriteStatus("File path is empty! (Save the file before you can use this command)");
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
                inputHandler.WriteStatus("File Saved!");
                return true;
            }
            catch (Exception ex)
            {
                inputHandler.WriteStatus(ex.Message);
                return false;
            }
        }

        private void YesNtInterpreter_OnDebugOutput(string output)
        {
            debugOutput.Add(output);
        }

        private void YesNtInterpreter_OnLineExecuted(Interpreter.Runtime.DebugEventArgs e)
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
                        Console.WriteLine($"{sharedString}[{e.CurrentLine}] ==>");
                    }
                    else
                    {
                        Console.WriteLine($"{sharedString}[{e.OriginalLine}] => [{e.CurrentLine}] ==>");
                    }
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
                string[] outputs = debugOutput.ToArray();
                debugOutput.Clear();

                foreach (string output in outputs)
                {
                    Console.Write(output);
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

        public int GetSpacing()
        {
            int padding = (Console.WindowHeight + LineOffset - 3).ToString().Length;
            padding = Math.Max(padding, Lines.Count.ToString().Length);
            padding += 1;
            return padding;
        }

        private readonly Point oldSize = new Point(0, 0);

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

    internal class Point
    {
        public int X { get; set; }
        public int Y { get; set; }

        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    internal enum Mode
    {
        Edit,
        Command,
        Debug
    }
}