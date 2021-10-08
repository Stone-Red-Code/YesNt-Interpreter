using System;
using System.Collections.Generic;
using System.IO;

using YesNt.Interpreter;

namespace YesNt.CodeEditor
{
    internal class TextEditor
    {
        private readonly YesNtInterpreter yesNtInterpreter = new();
        private readonly SyntaxHighlighter syntaxHighlighter;
        private int lineOffset = 0;
        private readonly List<string> lines = new();
        private readonly List<string> debugOutput = new();
        private readonly Point cursorPosition = new(0, 0);
        private Mode mode = Mode.Command;

        private string currentPath = string.Empty;

        public TextEditor(string path) : this()
        {
            if (File.Exists(path))
            {
                Load(path);
            }
        }

        public TextEditor()
        {
            yesNtInterpreter.Initialize();
            yesNtInterpreter.OnDebugOutput += YesNtInterpreter_OnDebugOutput;
            yesNtInterpreter.OnLineExecuted += YesNtInterpreter_OnLineExecuted;
            syntaxHighlighter = new(yesNtInterpreter.StatementInformation);
        }

        public void Run()
        {
            Console.Clear();

            Display(true);
            do
            {
                Display(false);
            } while (HandleInput());
        }

        private void Display(bool drawAll)
        {
            Console.CursorVisible = false;

            Console.SetCursorPosition(0, 0);
            int padding = (Console.WindowHeight + lineOffset - 3).ToString().Length;
            padding = Math.Max(padding, lines.Count.ToString().Length);
            padding += 1;
            for (int i = lineOffset; i < Console.WindowHeight + lineOffset - 2; i++)
            {
                Console.SetCursorPosition(0, i - lineOffset);

                string lineCountString = $"{i + 1}".PadRight(padding, ' ') + "| ";
                if (i < lines.Count)
                {
                    if (cursorPosition.Y == i || drawAll)
                    {
                        Console.Write(lineCountString);
                        string printLine = $"{lines[i].Substring(0, Math.Min(lines[i].Length, Console.WindowWidth))}".TrimEnd();
                        syntaxHighlighter.Write(printLine);
                        Console.Write(new string(' ', Math.Max(Console.WindowWidth - lineCountString.Length - printLine.Length, 0)));
                    }
                }
                else if (drawAll || cursorPosition.Y == i)
                {
                    Console.Write(lineCountString + new string(' ', Console.WindowWidth - lineCountString.Length));
                }
            }

            Console.SetCursorPosition(0, Console.WindowHeight - 3);
            Console.Write(new string('-', Console.WindowWidth));
            Console.SetCursorPosition(0, Console.WindowHeight - 2);
            Console.Write(">>>" + new string(' ', Console.WindowWidth - 3));

            Console.SetCursorPosition(Math.Min(cursorPosition.X + padding + 2, Console.WindowWidth - 1), cursorPosition.Y - lineOffset);
            Console.CursorVisible = true;
        }

        private bool HandleInput()
        {
            if (mode == Mode.Edit)
            {
                ConsoleKeyInfo keyInfo = Console.ReadKey(true);

                if ((ConsoleModifiers.Alt & keyInfo.Modifiers) != 0 && keyInfo.Key == ConsoleKey.C)
                {
                    mode = Mode.Command;
                }
                else if (keyInfo.Key == ConsoleKey.DownArrow)
                {
                    cursorPosition.Y++;
                    if (cursorPosition.Y - lineOffset >= Console.WindowHeight - 3)
                    {
                        lineOffset++;
                        Display(true);
                    }
                }
                else if (keyInfo.Key == ConsoleKey.UpArrow)
                {
                    if (cursorPosition.Y > 0)
                    {
                        cursorPosition.Y--;
                        if (cursorPosition.Y - lineOffset < 0 && lineOffset > 0)
                        {
                            lineOffset--;
                            Display(true);
                        }
                    }
                }
                else if (keyInfo.Key == ConsoleKey.LeftArrow)
                {
                    if (cursorPosition.X > 0)
                    {
                        cursorPosition.X--;
                    }
                }
                else if (keyInfo.Key == ConsoleKey.RightArrow)
                {
                    if (cursorPosition.X + 6 < Console.WindowWidth - 1)
                    {
                        cursorPosition.X++;
                    }
                }
                else if (keyInfo.Key == ConsoleKey.Enter)
                {
                    while (lines.Count <= cursorPosition.Y)
                    {
                        lines.Add("");
                    }
                    lines.Insert(cursorPosition.Y, "");

                    string line = lines[cursorPosition.Y + 1];

                    lines[cursorPosition.Y] = line.Substring(0, Math.Min(cursorPosition.X, line.Length));
                    lines[cursorPosition.Y + 1] = line.Substring(Math.Min(cursorPosition.X, line.Length));

                    cursorPosition.X = 0;
                    cursorPosition.Y++;

                    if (cursorPosition.Y - lineOffset >= Console.WindowHeight - 3)
                    {
                        lineOffset++;
                    }
                    Display(true);
                }
                else
                {
                    while (lines.Count <= cursorPosition.Y)
                    {
                        lines.Add("");
                    }

                    while (lines[cursorPosition.Y].Length <= cursorPosition.X)
                    {
                        lines[cursorPosition.Y] += " ";
                    }

                    if (keyInfo.Key == ConsoleKey.Backspace)
                    {
                        if (cursorPosition.X > 0)
                        {
                            lines[cursorPosition.Y] = lines[cursorPosition.Y].Remove(cursorPosition.X - 1, 1);
                            cursorPosition.X--;
                        }
                        else
                        {
                            if (cursorPosition.Y > 0)
                            {
                                if (string.IsNullOrWhiteSpace(lines[cursorPosition.Y - 1]))
                                {
                                    lines.RemoveAt(--cursorPosition.Y);
                                }
                                else
                                {
                                    cursorPosition.Y--;
                                    cursorPosition.X = lines[cursorPosition.Y].TrimEnd().Length;
                                    lines[cursorPosition.Y] += lines[cursorPosition.Y + 1];
                                    lines.RemoveAt(cursorPosition.Y + 1);
                                }
                                if (cursorPosition.Y - lineOffset < 0 && lineOffset > 0)
                                {
                                    lineOffset--;
                                }
                                Display(true);
                            }
                        }

                        while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[lines.Count - 1]))
                        {
                            lines.RemoveAt(lines.Count - 1);
                        }
                    }
                    else
                    {
                        char input = keyInfo.KeyChar;
                        if (!char.IsControl(input))
                        {
                            lines[cursorPosition.Y] = lines[cursorPosition.Y].Insert(cursorPosition.X, input.ToString());
                            cursorPosition.X++;
                        }
                    }
                }
            }
            else if (mode == Mode.Command)
            {
                Console.SetCursorPosition(3, Console.WindowHeight - 2);

                string input = Console.ReadLine() ?? string.Empty;
                string command = input.Split(' ')[0].Trim();
                string path;

                switch (command)
                {
                    case "edit":
                        WriteStatus(string.Empty);
                        mode = Mode.Edit;
                        break;

                    case "save":
                        Save(input, false);
                        break;

                    case "run":
                        if (Save(input, true))
                        {
                            Console.Clear();
                            yesNtInterpreter.Execute(currentPath);
                            Console.ReadKey();
                        }
                        break;

                    case "debug":
                        if (Save(input, true))
                        {
                            Console.Clear();
                            yesNtInterpreter.Execute(currentPath, true);
                            Console.ReadKey();
                            WriteStatus(string.Empty);
                        }
                        break;

                    case "load":
                        if (input.Split(' ').Length == 2)
                        {
                            path = input.Split(' ')[1];
                        }
                        else
                        {
                            WriteStatus("Invalid arguments!");
                            break;
                        }

                        if (!File.Exists(path))
                        {
                            WriteStatus("File does not exist!");
                            break;
                        }

                        try
                        {
                            Load(path);
                        }
                        catch (Exception ex)
                        {
                            WriteStatus(ex.Message);
                        }
                        lineOffset = 0;
                        cursorPosition.X = 0;
                        cursorPosition.Y = 0;
                        break;

                    case "new":
                        if (Save(input, false))
                        {
                            lineOffset = 0;
                            cursorPosition.X = 0;
                            cursorPosition.Y = 0;
                            currentPath = string.Empty;
                            lines.Clear();
                            WriteStatus(string.Empty);
                        }
                        break;

                    case "exit":
                        return false;

                    default:
                        WriteStatus("Command not found!");
                        break;
                }
                Display(true);
            }
            return true;
        }

        private bool Load(string path)
        {
            if (!File.Exists(path))
            {
                WriteStatus("File does not exist!");
                return false;
            }

            lines.Clear();
            foreach (string line in File.ReadAllLines(path))
            {
                lines.Add(line.Replace("\n", ""));
            }
            currentPath = path;
            WriteStatus("File Loaded!");
            return true;
        }

        private bool Save(string input, bool loadIfExists)
        {
            string text = "";
            string path = "";

            foreach (string line in lines)
            {
                text += line + "\n";
            }
            text = text.TrimEnd('\n');

            if (input.Split(' ').Length == 2)
            {
                path = input.Split(' ')[1];

                if (currentPath.Trim() != path.Trim() && loadIfExists)
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
                currentPath = path;
            }
            else if (!string.IsNullOrWhiteSpace(currentPath))
            {
                path = currentPath;
            }
            else if (input.Split(' ').Length > 2)
            {
                WriteStatus("Invalid arguments!");
                return false;
            }
            else
            {
                WriteStatus("File path is empty!");
                return false;
            }

            try
            {
                File.WriteAllText(path, text);
                WriteStatus("File Saved!");
                return true;
            }
            catch (Exception ex)
            {
                WriteStatus(ex.Message);
                return false;
            }
        }

        private void WriteStatus(string input)
        {
            Console.SetCursorPosition(0, Console.WindowHeight - 1);
            Console.Write(input + new string(' ', Console.WindowWidth - input.Length - 1));
        }

        private void YesNtInterpreter_OnDebugOutput(string output)
        {
            debugOutput.Add(output);
        }

        private void YesNtInterpreter_OnLineExecuted(Interpreter.Runtime.DebugEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            if (e.OriginalLine == e.CurrentLine)
            {
                Console.WriteLine($"{Environment.NewLine}[{e.LineNumber}] [{e.CurrentLine}] ==>");
            }
            else
            {
                Console.WriteLine($"{Environment.NewLine}[{e.LineNumber}] [{e.OriginalLine}] => [{e.CurrentLine}] ==>");
            }
            Console.ResetColor();

            foreach (string output in debugOutput)
            {
                Console.Write(output);
            }
            debugOutput.Clear();
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
        Command
    }
}