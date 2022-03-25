using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using YesNt.Interpreter.Attributes;
using YesNt.Interpreter.Enums;
using YesNt.Interpreter.Runtime;
using YesNt.Interpreter.Utilities;

namespace YesNt.Interpreter.Statements
{
    internal class ProcessingStatements : StatementRuntimeInformation
    {
        private static readonly Regex calculationRegex = new Regex(@"((\)?)+(\(?)+[0-9]+(((\s?)+(\)?)(\s?)+\+(\s?)+(\(?)+(\s?)+|(\s?)+(\)?)(\s?)+\-(\s?)+(\(?)+(\s?)+|(\s?)+(\)?)(\s?)+\*(\s?)+(\(?)+(\s?)+|(\s?)+(\)?)(\s?)+\%(\s?)+(\(?)+(\s?)+|(\s?)+(\)?)(\s?)+\^(\s?)+(\(?)+(\s?)+|(\s?)+(\)?)(\s?)+\/(\s?)+(\(?)+(\s?)+)|[,.])(?=[0-9])+)+[0-9]+(\)?)+");

        //new Regex(@"((\)?)+(\(?)+[0-9]+(((\s?)+(\)?)(\s?)+\+(\s?)+(\(?)+(\s?)+|(\s?)+(\)?)(\s?)+\-(\s?)+(\(?)+(\s?)+|(\s?)+(\)?)(\s?)+\*(\s?)+(\(?)+(\s?)+|(\s?)+(\)?)(\s?)+\/(\s?)+(\(?)+(\s?)+)|[,.])(?=[0-9])+)+[0-9]+(\)?)+");

        [Statement("!calc", SearchMode.EndOfLine, SpaceAround.Start, ConsoleColor.DarkYellow, Priority = Priority.High, ExecuteInSearchMode = true)]
        public void Calculate(string args)
        {
            MatchCollection matches = calculationRegex.Matches(args.FromSaveString());

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
                    RuntimeInfo.Lines.RemoveAt(RuntimeInfo.LineNumber);
                    string[] lines = File.ReadAllLines(path);

                    for (int i = 0; i < lines.Length; i++)
                    {
                        RuntimeInfo.Lines.Add(new Line(lines[i], Path.GetFileName(path), i));
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

        [Statement("mod", SearchMode.StartOfLine, SpaceAround.End, ConsoleColor.DarkYellow, Priority = Priority.VeryLow, Seperator = "|")]
        public void GetModulo(string args)
        {
            string[] parts = args.Split('|');
            if (parts.Length != 2)
            {
                RuntimeInfo.Exit("Invalid syntax", true);
                return;
            }

            string[] nums = parts[1].Split(',');
            if (nums.Length != 2)
            {
                RuntimeInfo.Exit("Invalid arguments", true);
                return;
            }

            string variableName = parts[0].Trim();

            if (ulong.TryParse(nums[0], out ulong num1) && ulong.TryParse(nums[1], out ulong num2))
            {
                if (RuntimeInfo.Variables.ContainsKey(variableName))
                {
                    RuntimeInfo.Variables[variableName] = (num1 % num2).ToString();
                }
                else
                {
                    RuntimeInfo.Variables.Add(variableName, (num1 % num2).ToString());
                }
            }
            else
            {
                RuntimeInfo.Exit("Invalid arguments", true);
            }
        }
    }
}