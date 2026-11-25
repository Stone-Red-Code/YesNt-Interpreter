using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

using YesNt.Interpreter.Attributes;
using YesNt.Interpreter.Enums;
using YesNt.Interpreter.Runtime;
using YesNt.Interpreter.Utilities;

namespace YesNt.Interpreter.Statements;

internal partial class ProcessingStatements : StatementRuntimeInformation
{
    [Statement("calc", SearchMode.EndOfLine, SpaceAround.Start, ConsoleColor.DarkYellow, Priority = Priority.High)]
    public void Calculate(string args)
    {
        RuntimeInfo.CurrentLine = TemplateProcessor.ProcessCalculations(args.FromSafeString(), RuntimeInfo, CalculationRegex());
    }

    [Statement("eval", SearchMode.EndOfLine, SpaceAround.Start, ConsoleColor.DarkYellow, Priority = Priority.VeryHigh)]
    public void Evaluate(string args)
    {
        RuntimeInfo.CurrentLine = args.FromSafeString();
    }

    [Statement("task", SearchMode.EndOfLine, SpaceAround.Start, ConsoleColor.DarkYellow, Priority = Priority.VeryHigh)]
    public void RunTask(string line)
    {
        int lineNumber = RuntimeInfo.LineNumber;
        List<Line> lines = RuntimeInfo.Lines.GetRange(0, RuntimeInfo.Lines.Count);

        Line oldLine = lines[lineNumber];

        lines[lineNumber] = new Line(line, oldLine.FileName, oldLine.LineNumber);
        RuntimeInfo.StartTaskAction(lines, lineNumber);

        RuntimeInfo.CurrentLine = string.Empty;
    }

    [Statement("sleep", SearchMode.StartOfLine, SpaceAround.End, ConsoleColor.Magenta)]
    public void Sleep(string args)
    {
        if (int.TryParse(args, out int millisecondsTimeout))
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            RuntimeInfo.WaitUntil(() => stopwatch.ElapsedMilliseconds >= millisecondsTimeout);
        }
        else
        {
            RuntimeInfo.Exit(ExitMessages.InvalidTimeoutValue(args), true);
        }
    }

    [Statement("length", SearchMode.StartOfLine, SpaceAround.End, ConsoleColor.Magenta)]
    public void Length(string args)
    {
        // Its argument is on the line: values pushed for a later call stay where they are.
        RuntimeInfo.SetOut(args.FromSafeString().Length.ToString());
    }

    [Statement("import", SearchMode.StartOfLine, SpaceAround.End, ConsoleColor.Magenta)]
    public void Import(string path)
    {
        path = path.FromSafeString();
        path = Path.Combine(RuntimeInfo.WorkingDirectory, path);

        if (string.IsNullOrEmpty(Path.GetExtension(path)))
        {
            path = Path.ChangeExtension(path, "ynt");
        }

        string[] lines;
        try
        {
            lines = RuntimeInfo.Host.ReadScript(path);
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            RuntimeInfo.Exit(ExitMessages.CouldNotFindFile(path), true);
            return;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            RuntimeInfo.Exit(ExitMessages.CouldNotLoadFile(path), true);
            return;
        }

        RuntimeInfo.Lines.RemoveAt(RuntimeInfo.LineNumber);

        for (int i = 0; i < lines.Length; i++)
        {
            RuntimeInfo.Lines.Insert(RuntimeInfo.LineNumber + i, new Line(lines[i], Path.GetFileName(path), i));
        }
        RuntimeInfo.PreScanLinesAction?.Invoke();
        RuntimeInfo.LineNumber--;
    }

    [GeneratedRegex("[0-9*+().,^%/-]+[0-9*+ ().,^%/-]+[0-9*+().,^%/-]+")]
    private static partial Regex CalculationRegex();
}