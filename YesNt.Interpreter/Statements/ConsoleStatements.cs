using System;
using System.Diagnostics.CodeAnalysis;

using YesNt.Interpreter.Attributes;
using YesNt.Interpreter.Enums;
using YesNt.Interpreter.Runtime;
using YesNt.Interpreter.Utilities;

namespace YesNt.Interpreter.Statements;

internal class ConsoleStatements : StatementRuntimeInformation
{
    [Statement("print_line", SearchMode.Exact, SpaceAround.None, ConsoleColor.DarkGreen, Priority = Priority.VeryLow)]
    public void WriteLineEmpty(string _)
    {
        RuntimeInfo.WriteLine(string.Empty);
    }

    [Statement("print_line", SearchMode.StartOfLine, SpaceAround.End, ConsoleColor.DarkGreen, Priority = Priority.VeryLow)]
    public void WriteLine(string args)
    {
        RuntimeInfo.WriteLine(args);
    }

    [Statement("print", SearchMode.StartOfLine, SpaceAround.End, ConsoleColor.DarkGreen, Priority = Priority.VeryLow)]
    public void Write(string args)
    {
        RuntimeInfo.Write(args);
    }

    [Statement("%read_line", SearchMode.Contains, SpaceAround.None, ConsoleColor.DarkGreen, KeepStatementInArgs = true, Priority = Priority.Highest)]
    public void ReadLine(string args)
    {
        args += " ";
        while (args.Contains("%read_line"))
        {
            string input = Console.ReadLine();
            if (input is null)
            {
                RuntimeInfo.Exit("Terminated by external process", true);
                return;
            }
            args = args.ReplaceFirstOccurrence("%read_line ", input.ToSafeString() + " ");
        }
        RuntimeInfo.CurrentLine = args.TrimEnd();
    }

    [Statement("%read_key", SearchMode.Contains, SpaceAround.None, ConsoleColor.DarkGreen, KeepStatementInArgs = true, Priority = Priority.Highest)]
    public void ReadKey(string args)
    {
        args += " ";
        while (args.Contains("%read_key"))
        {
            string input = ConsoleExtensions.ReadKey(RuntimeInfo).ToString();
            args = args.ReplaceFirstOccurrence("%read_key ", input.ToSafeString() + " ");
        }
        RuntimeInfo.CurrentLine = args.TrimEnd();
    }

    [Statement("clear", SearchMode.Exact, SpaceAround.None, ConsoleColor.Magenta)]
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Won't work if static")]
    public void Clear(string _)
    {
        Console.Clear();
    }
}
