using System;
using System.Text;

namespace YesNt.CodeEditor
{
    internal class InputHandler
    {
        private readonly TextEditor textEditor;

        public InputHandler(TextEditor textEditor)
        {
            this.textEditor = textEditor;
        }

        public bool HandleInput()
        {
            while (Console.KeyAvailable)
            {
                Console.ReadKey(true);
            }
            if (textEditor.EditMode == Mode.Edit)
            {
                ConsoleKeyInfo keyInfo = Console.ReadKey(true);

                if ((ConsoleModifiers.Alt & keyInfo.Modifiers) == ConsoleModifiers.Alt)
                {
                    switch (keyInfo.Key)
                    {
                        case ConsoleKey.C:
                            textEditor.EditMode = Mode.Command;
                            return true;

                        case ConsoleKey.B:
                            int position = textEditor.Lines.Count;
                            textEditor.CursorPosition.Y = position - 1;
                            textEditor.LineOffset = Math.Max(position - Console.WindowHeight + 3, 0);

                            textEditor.Display(true);
                            return true;

                        case ConsoleKey.T:
                            textEditor.CursorPosition.Y = 0;
                            textEditor.LineOffset = 0;

                            textEditor.Display(true);
                            return true;

                        case ConsoleKey.S:
                            textEditor.CursorPosition.X = 0;
                            return true;

                        case ConsoleKey.E:
                            if (textEditor.Lines.Count > textEditor.CursorPosition.Y)
                            {
                                textEditor.CursorPosition.X = textEditor.Lines[textEditor.CursorPosition.Y].TrimEnd().Length;
                            }
                            else
                            {
                                textEditor.CursorPosition.X = 0;
                            }
                            return true;
                    }
                }
                if (keyInfo.Key == ConsoleKey.DownArrow)
                {
                    textEditor.CursorPosition.Y++;
                    if (textEditor.CursorPosition.Y - textEditor.LineOffset >= Console.WindowHeight - 3)
                    {
                        textEditor.LineOffset++;
                        textEditor.Display(true);
                    }
                }
                else if (keyInfo.Key == ConsoleKey.UpArrow)
                {
                    if (textEditor.CursorPosition.Y > 0)
                    {
                        textEditor.CursorPosition.Y--;
                        if (textEditor.CursorPosition.Y - textEditor.LineOffset < 0 && textEditor.LineOffset > 0)
                        {
                            textEditor.LineOffset--;
                            textEditor.Display(true);
                        }
                    }
                }
                else if (keyInfo.Key == ConsoleKey.LeftArrow)
                {
                    if (textEditor.CursorPosition.X > 0)
                    {
                        textEditor.CursorPosition.X--;
                    }
                }
                else if (keyInfo.Key == ConsoleKey.RightArrow)
                {
                    if (textEditor.CursorPosition.X + textEditor.GetSpacing() + 3 < Console.WindowWidth)
                    {
                        textEditor.CursorPosition.X++;
                    }
                }
                else if (keyInfo.Key == ConsoleKey.Enter)
                {
                    while (textEditor.Lines.Count <= textEditor.CursorPosition.Y)
                    {
                        textEditor.Lines.Add("");
                    }
                    textEditor.Lines.Insert(textEditor.CursorPosition.Y, "");

                    string line = textEditor.Lines[textEditor.CursorPosition.Y + 1];

                    textEditor.Lines[textEditor.CursorPosition.Y] = line[..Math.Min(textEditor.CursorPosition.X, line.Length)];
                    textEditor.Lines[textEditor.CursorPosition.Y + 1] = line[Math.Min(textEditor.CursorPosition.X, line.Length)..];

                    textEditor.CursorPosition.X = 0;
                    textEditor.CursorPosition.Y++;

                    if (textEditor.CursorPosition.Y - textEditor.LineOffset >= Console.WindowHeight - 3)
                    {
                        textEditor.LineOffset++;
                    }
                    textEditor.Display(true);
                }
                else
                {
                    while (textEditor.Lines.Count <= textEditor.CursorPosition.Y)
                    {
                        textEditor.Lines.Add("");
                    }

                    StringBuilder lineBuilder = new StringBuilder(textEditor.Lines[textEditor.CursorPosition.Y]);
                    while (lineBuilder.Length <= textEditor.CursorPosition.X)
                    {
                        lineBuilder.Append(' ');
                    }

                    textEditor.Lines[textEditor.CursorPosition.Y] = lineBuilder.ToString();

                    if (keyInfo.Key == ConsoleKey.Backspace)
                    {
                        if (textEditor.CursorPosition.X > 0)
                        {
                            if (textEditor.Lines[textEditor.CursorPosition.Y][textEditor.CursorPosition.X - 1] == ' ' && textEditor.CursorPosition.X > textEditor.Lines[textEditor.CursorPosition.Y].TrimEnd().Length)
                            {
                                textEditor.Lines[textEditor.CursorPosition.Y] = textEditor.Lines[textEditor.CursorPosition.Y].TrimEnd();
                                textEditor.CursorPosition.X = textEditor.Lines[textEditor.CursorPosition.Y].Length;
                            }
                            else
                            {
                                textEditor.Lines[textEditor.CursorPosition.Y] = textEditor.Lines[textEditor.CursorPosition.Y].Remove(textEditor.CursorPosition.X - 1, 1);
                                textEditor.CursorPosition.X--;
                            }
                        }
                        else
                        {
                            if (textEditor.CursorPosition.Y > 0)
                            {
                                if (string.IsNullOrWhiteSpace(textEditor.Lines[textEditor.CursorPosition.Y - 1]))
                                {
                                    textEditor.Lines.RemoveAt(--textEditor.CursorPosition.Y);
                                }
                                else
                                {
                                    textEditor.CursorPosition.Y--;
                                    textEditor.CursorPosition.X = textEditor.Lines[textEditor.CursorPosition.Y].TrimEnd().Length;
                                    textEditor.Lines[textEditor.CursorPosition.Y] += textEditor.Lines[textEditor.CursorPosition.Y + 1];
                                    textEditor.Lines.RemoveAt(textEditor.CursorPosition.Y + 1);
                                }
                                if (textEditor.CursorPosition.Y - textEditor.LineOffset < 0 && textEditor.LineOffset > 0)
                                {
                                    textEditor.LineOffset--;
                                }
                                textEditor.Display(true);
                            }
                        }

                        while (textEditor.Lines.Count > 0 && string.IsNullOrWhiteSpace(textEditor.Lines[^1]))
                        {
                            textEditor.Lines.RemoveAt(textEditor.Lines.Count - 1);
                        }
                    }
                    else if (textEditor.CursorPosition.X + textEditor.GetSpacing() + 3 < Console.WindowWidth)
                    {
                        char input = keyInfo.KeyChar;
                        if (!char.IsControl(input))
                        {
                            textEditor.Lines[textEditor.CursorPosition.Y] = textEditor.Lines[textEditor.CursorPosition.Y].Insert(textEditor.CursorPosition.X, input.ToString());
                            textEditor.CursorPosition.X++;
                        }
                    }
                }
            }
            else if (textEditor.EditMode == Mode.Command)
            {
                Console.SetCursorPosition(3, Console.WindowHeight - 2);

                string input = Console.ReadLine() ?? string.Empty;
                string command = input.Split(' ')[0].Trim();
                string path;

                Console.CursorVisible = false;

                switch (command)
                {
                    case "edit":
                        WriteStatus(string.Empty);
                        textEditor.EditMode = Mode.Edit;
                        break;

                    case "line":
                        WriteStatus(string.Empty);

                        bool success = false;
                        int lineNumber = 0;
                        if (input.Split(' ').Length == 2)
                        {
                            success = int.TryParse(input.Split(' ')[1], out lineNumber);
                        }

                        if (success && lineNumber > 0)
                        {
                            textEditor.CursorPosition.Y = lineNumber - 1;
                            textEditor.CursorPosition.X = 0;
                            textEditor.LineOffset = lineNumber - 1;
                            textEditor.EditMode = Mode.Edit;
                        }
                        else
                        {
                            WriteStatus("Invalid line number!");
                        }
                        break;

                    case "save":
                        textEditor.Save(input, false);
                        break;

                    case "run":
                        if (textEditor.Save(input, true))
                        {
                            textEditor.EditMode = Mode.Debug;
                            Console.Clear();
                            Console.CursorVisible = true;
                            textEditor.YesNtInterpreter.Execute(textEditor.CurrentPath);
                            while (Console.KeyAvailable)
                            {
                                Console.ReadKey(true);
                            }
                            Console.ReadKey();
                            WriteStatus(string.Empty);
                            textEditor.EditMode = Mode.Command;
                        }
                        break;

                    case "debug":
                        if (textEditor.Save(input, true))
                        {
                            textEditor.EditMode = Mode.Debug;
                            Console.Clear();
                            Console.CursorVisible = true;
                            textEditor.YesNtInterpreter.Execute(textEditor.CurrentPath, true);
                            while (Console.KeyAvailable)
                            {
                                Console.ReadKey(true);
                            }
                            Console.ReadKey();
                            WriteStatus(string.Empty);
                            textEditor.EditMode = Mode.Command;
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

                        try
                        {
                            textEditor.Load(path);
                        }
                        catch (Exception ex)
                        {
                            WriteStatus(ex.Message);
                        }
                        textEditor.LineOffset = 0;
                        textEditor.CursorPosition.X = 0;
                        textEditor.CursorPosition.Y = 0;
                        break;

                    case "new":

                        textEditor.LineOffset = 0;
                        textEditor.CursorPosition.X = 0;
                        textEditor.CursorPosition.Y = 0;
                        textEditor.CurrentPath = string.Empty;
                        textEditor.Lines.Clear();
                        WriteStatus(string.Empty);
                        break;

                    case "exit":
                        return false;

                    default:
                        WriteStatus("Command not found!");
                        break;
                }
                textEditor.Display(true);
            }
            return true;
        }

        internal static void WriteStatus(string input)
        {
            Console.SetCursorPosition(0, Console.WindowHeight - 1);
            Console.Write(input + new string(' ', Console.WindowWidth - input.Length - 1));
        }
    }
}