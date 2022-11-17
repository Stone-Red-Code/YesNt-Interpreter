using System;
using System.Diagnostics.CodeAnalysis;

using YesNt.Interpreter.Attributes;
using YesNt.Interpreter.Enums;
using YesNt.Interpreter.Runtime;
using YesNt.Interpreter.Utilities;

namespace YesNt.Interpreter.Statements;

internal class ConsoleStatements : StatementRuntimeInformation
{
    [Statement("cwl", SearchMode.Exact, SpaceAround.None, ConsoleColor.DarkGreen, Priority = Priority.VeryLow)]
    public void WriteLineEmpty(string _)
    {
        RuntimeInfo.WriteLine(string.Empty);
    }

    [Statement("cwl", SearchMode.StartOfLine, SpaceAround.End, ConsoleColor.DarkGreen, Priority = Priority.VeryLow)]
    public void WriteLine(string args)
    {
        RuntimeInfo.WriteLine(args);
    }

    [Statement("cw", SearchMode.StartOfLine, SpaceAround.End, ConsoleColor.DarkGreen, Priority = Priority.VeryLow)]
    public void Write(string args)
    {
        RuntimeInfo.Write(args);
    }

    [Statement("%crl", SearchMode.Contains, SpaceAround.End, ConsoleColor.DarkGreen, KeepStatementInArgs = true, Priority = Priority.Highest)]
    public void ReadLine(string args)
    {
        args += " ";
        while (args.Contains("%crl "))
        {
            string input = Console.ReadLine();
            if (input is null)
            {
                RuntimeInfo.Exit("Terminated by external process", true);
                return;
            }
            args = args.ReplaceFirstOccurrence("%crl ", input.ToSafeString() + " ");
        }
        RuntimeInfo.CurrentLine = args.TrimEnd();
    }

    [Statement("%cr", SearchMode.Contains, SpaceAround.End, ConsoleColor.DarkGreen, KeepStatementInArgs = true, Priority = Priority.Highest)]
    public void ReadKey(string args)
    {
        args += " ";
        while (args.Contains("%cr "))
        {
            string input = ConsoleExtentions.ReadKey(RuntimeInfo).ToString();
            args = args.ReplaceFirstOccurrence("%cr ", input.ToSafeString() + " ");
        }
        RuntimeInfo.CurrentLine = args.TrimEnd();
    }

    [Statement("cls", SearchMode.Exact, SpaceAround.None, ConsoleColor.Magenta)]
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Won't work if static")]
    public void Clear(string _)
    {
        Console.Clear();
    }
}