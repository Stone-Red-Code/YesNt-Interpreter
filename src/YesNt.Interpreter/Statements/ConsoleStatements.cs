using System;

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
        SubstituteInput(args, "%read_line", (IYesNtHost host, out string input) => host.TryReadLine(out input));
    }

    [Statement("%read_key", SearchMode.Contains, SpaceAround.None, ConsoleColor.DarkGreen, KeepStatementInArgs = true, Priority = Priority.Highest)]
    public void ReadKey(string args)
    {
        SubstituteInput(args, "%read_key", (IYesNtHost host, out string input) =>
        {
            InputStatus status = host.TryReadKey(out char key);
            input = key.ToString();
            return status;
        });
    }

    [Statement("clear", SearchMode.Exact, SpaceAround.None, ConsoleColor.Magenta)]
    public void Clear(string _)
    {
        RuntimeInfo.Host.Clear();
    }

    private delegate InputStatus InputReader(IYesNtHost host, out string input);

    // Puts input in place of each token, in order. Without input yet, the line runs again at
    // the next step, keeping what was already read in it.
    private void SubstituteInput(string args, string token, InputReader read)
    {
        args += " ";
        while (args.Contains(token))
        {
            switch (read(RuntimeInfo.Host, out string input))
            {
                case InputStatus.Ready:
                    args = args.ReplaceFirstOccurrence(token, input.ToSafeString());
                    break;

                case InputStatus.Pending:
                    RuntimeInfo.CurrentLine = args.TrimEnd();
                    RuntimeInfo.RetryLine();
                    return;

                default:
                    RuntimeInfo.Exit(ExitMessages.TerminatedByExternalProcess, true);
                    return;
            }
        }
        RuntimeInfo.CurrentLine = args.TrimEnd();
    }
}