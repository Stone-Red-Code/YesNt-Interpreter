using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using YesNt.Interpreter.Attributes;
using YesNt.Interpreter.Enums;
using YesNt.Interpreter.Utilities;

namespace YesNt.Interpreter.Statements
{
    internal class ProcessingStatements : StatementRuntimeInformation
    {
        [Statement("!calc", SearchMode.EndOfLine, SpaceAround.Start, ConsoleColor.DarkYellow, Priority = Priority.High, ExecuteInSearchMode = true)]
        public void Calculate(string args)
        {
            MatchCollection matches = Regex.Matches(args.FromSaveString(), @"((\)?)+(\(?)+[0-9]+(((\s?)+(\)?)(\s?)+\+(\s?)+(\(?)+(\s?)+|(\s?)+(\)?)(\s?)+\-(\s?)+(\(?)+(\s?)+|(\s?)+(\)?)(\s?)+\*(\s?)+(\(?)+(\s?)+|(\s?)+(\)?)(\s?)+\/(\s?)+(\(?)+(\s?)+)|[,.])(?=[0-9])+)+[0-9]+(\)?)+");

            for (int i = 0; i < matches.Count; i++)
            {
                string res = Evaluator.Calculate(matches[i].Value);
                if (res is null)
                {
                    RuntimeInfo.Exit("Invalid operation", true);
                    return;
                }
                args = args.FromSaveString().Replace(matches[0].Value, res);
            }

            RuntimeInfo.CurrentLine = args;
        }

        [Statement("!eval", SearchMode.EndOfLine, SpaceAround.Start, ConsoleColor.DarkYellow, Priority = Priority.VeryHigh)]
        public void Evaluate(string args)
        {
            RuntimeInfo.CurrentLine = args.FromSaveString();
        }

        [Statement("!task", SearchMode.EndOfLine, SpaceAround.Start, ConsoleColor.DarkYellow, Priority = Priority.VeryHigh)]
        public void RunTask(string line)
        {
            int lineNumer = RuntimeInfo.LineNumber;
            List<string> lines = RuntimeInfo.Lines.GetRange(0, RuntimeInfo.Lines.Count);
            lines[lineNumer] = line;
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
            _ = int.TryParse(args, out int millisecondsTimeout);
            ConsoleExtentions.Sleep(millisecondsTimeout, RuntimeInfo);
        }

        [Statement("imp", SearchMode.StartOfLine, SpaceAround.End, ConsoleColor.Magenta)]
        public void Import(string path)
        {
            if (string.IsNullOrEmpty(Path.GetExtension(path)))
            {
                path = Path.ChangeExtension(path, "ynt");
            }

            if (File.Exists(path))
            {
                try
                {
                    RuntimeInfo.Lines.RemoveAt(0);
                    RuntimeInfo.Lines.InsertRange(RuntimeInfo.LineNumber, File.ReadAllLines(path));
                    RuntimeInfo.LineNumber--;
                }
                catch
                {
                    RuntimeInfo.Exit($"Could not load file {path}", true);
                }
            }
            else
            {
                RuntimeInfo.Exit($"Could not find file {path}", true);
            }
        }
    }
}