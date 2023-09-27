using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using YesNt.Interpreter.Attributes;
using YesNt.Interpreter.Enums;
using YesNt.Interpreter.Runtime;
using YesNt.Interpreter.Utilities;

namespace YesNt.Interpreter.Statements;

internal partial class ProcessingStatements : StatementRuntimeInformation
{
    [Statement("!calc", SearchMode.EndOfLine, SpaceAround.Start, ConsoleColor.DarkYellow, Priority = Priority.High)]
    public void Calculate(string args)
    {
        MatchCollection matches = CalculationRegex().Matches(args.FromSafeString());

        for (int i = 0; i < matches.Count; i++)
        {
            string res = Evaluator.Calculate(matches[i].Value);
            if (res is null)
            {
                RuntimeInfo.Exit("Invalid operation", true);
                return;
            }
            args = args.FromSafeString().Replace(matches[i].Value, res);
        }

        RuntimeInfo.CurrentLine = args;
    }

    [Statement("!eval", SearchMode.EndOfLine, SpaceAround.Start, ConsoleColor.DarkYellow, Priority = Priority.VeryHigh)]
    public void Evaluate(string args)
    {
        RuntimeInfo.CurrentLine = args.FromSafeString();
    }

    [Statement("!task", SearchMode.EndOfLine, SpaceAround.Start, ConsoleColor.DarkYellow, Priority = Priority.VeryHigh)]
    public void RunTask(string line)
    {
        int lineNumer = RuntimeInfo.LineNumber;
        List<Line> lines = RuntimeInfo.Lines.GetRange(0, RuntimeInfo.Lines.Count);

        Line oldLine = lines[lineNumer];

        lines[lineNumer] = new Line(line, oldLine.FileName, oldLine.LineNumber);
        _ = Task.Run(() =>
        {
            YesNtInterpreter interpreter = new YesNtInterpreter();
            interpreter.Initialize();
            interpreter.Execute(lines, RuntimeInfo.GloablVariables, lineNumer, RuntimeInfo);
        });

        RuntimeInfo.CurrentLine = string.Empty;
    }

    [Statement("slp", SearchMode.StartOfLine, SpaceAround.End, ConsoleColor.Magenta)]
    public void Sleep(string args)
    {
        if (int.TryParse(args, out int millisecondsTimeout))
        {
            ConsoleExtentions.Sleep(millisecondsTimeout, RuntimeInfo);
        }
        else
        {
            RuntimeInfo.Exit($"\"{args}\" is not a valid time-out value", true);
        }
    }

    [Statement("len", SearchMode.StartOfLine, SpaceAround.End, ConsoleColor.Magenta)]
    public void Length(string args)
    {
        RuntimeInfo.InParametersStack.Clear();
        RuntimeInfo.OutParametersStack.Clear();

        RuntimeInfo.OutParametersStack.Push(args.FromSafeString().Length.ToString());
    }

    [Statement("imp", SearchMode.StartOfLine, SpaceAround.End, ConsoleColor.Magenta)]
    public void Import(string path)
    {
        path = Path.Combine(RuntimeInfo.WorkingDirectory, path);

        if (string.IsNullOrEmpty(Path.GetExtension(path)))
        {
            path = Path.ChangeExtension(path, "ynt");
        }

        if (File.Exists(path))
        {
            try
            {
                RuntimeInfo.Lines.RemoveAt(RuntimeInfo.LineNumber);
                string[] lines = File.ReadAllLines(path);

                for (int i = 0; i < lines.Length; i++)
                {
                    RuntimeInfo.Lines.Insert(RuntimeInfo.LineNumber + i, new Line(lines[i], Path.GetFileName(path), i));
                }
                RuntimeInfo.LineNumber--;
            }
            catch
            {
                RuntimeInfo.Exit($"Could not load file \"{path}\"", true);
            }
        }
        else
        {
            RuntimeInfo.Exit($"Could not find file \"{path}\"", true);
        }
    }

    [GeneratedRegex("[0-9*+().,^%/-]+[0-9*+ ().,^%/-]+[0-9*+().,^%/-]+")]
    private static partial Regex CalculationRegex();
}