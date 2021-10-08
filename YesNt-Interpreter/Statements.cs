using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using YesNt.Interpreter.Attributes;
using YesNt.Interpreter.Enums;
using YesNt.Interpreter.Utilities;

namespace YesNt.Interpreter
{
    internal class Statements : StatementRuntimeInformation
    {
        [Statement("cwl", SearchMode.StartOfLine, SpaceAround.End, Priority = Priority.VeryLow)]
        public void WriteLine(string args)
        {
            RuntimeInfo.WriteLine(args);
        }

        [Statement("cw", SearchMode.StartOfLine, SpaceAround.End, Priority = Priority.VeryLow)]
        public void Write(string args)
        {
            RuntimeInfo.Write(args);
        }

        [Statement("%crl", SearchMode.Contains, SpaceAround.End, KeepStatementInArgs = true, Priority = Priority.Highest)]
        public void ReadLine(string args)
        {
            args += " ";
            while (args.Contains("%crl "))
            {
                args = args.ReplaceFirstOccurrence("%crl ", Console.ReadLine().ToSaveString() + " ");
            }
            RuntimeInfo.CurrentLine = args.TrimEnd();
        }

        [Statement("%cr", SearchMode.Contains, SpaceAround.End, KeepStatementInArgs = true, Priority = Priority.Highest)]
        public void ReadKey(string args)
        {
            args += " ";
            while (args.Contains("%cr "))
            {
                args = args.ReplaceFirstOccurrence("%cr ", Console.ReadKey().KeyChar.ToString().ToSaveString() + " ");
            }
            RuntimeInfo.CurrentLine = args.TrimEnd();
        }

        [Statement("jmp", SearchMode.StartOfLine, SpaceAround.End)]
        public void Jump(string args)
        {
            string key = args.Trim();
            RuntimeInfo.LabelStack.Push(RuntimeInfo.LineNumber);

            if (RuntimeInfo.Labels.ContainsKey(key))
            {
                RuntimeInfo.LineNumber = RuntimeInfo.Labels[key];
            }
            else
            {
                RuntimeInfo.SearchLabel = key;
            }
        }

        [Statement("jif", SearchMode.StartOfLine, SpaceAround.End)]
        public void JumpIf(string args)
        {
            string[] parts = args.Split('|');
            if (parts.Length != 2)
            {
                RuntimeInfo.Exit("Invalid syntax");
                return;
            }

            string key = parts[0].Trim();
            string condition = parts[1].Trim();

            bool? result = Evaluator.EvaluateCondition(condition);

            if (result != true)
            {
                if (result is null)
                {
                    RuntimeInfo.Exit("Invalid operation");
                }
                return;
            }

            RuntimeInfo.LabelStack.Push(RuntimeInfo.LineNumber);

            if (RuntimeInfo.Labels.ContainsKey(key))
            {
                RuntimeInfo.LineNumber = RuntimeInfo.Labels[key];
            }
            else
            {
                RuntimeInfo.SearchLabel = key;
            }
        }

        [Statement("lbl", SearchMode.StartOfLine, SpaceAround.End, ExecuteInSearchLabelMode = true)]
        public void FindLabel(string args)
        {
            string key = args.Trim();
            if (RuntimeInfo.Labels.ContainsKey(key))
            {
                RuntimeInfo.Labels[key] = RuntimeInfo.LineNumber;
            }
            else
            {
                RuntimeInfo.Labels.Add(key, RuntimeInfo.LineNumber);
            }

            if (!string.IsNullOrWhiteSpace(RuntimeInfo.SearchLabel) && RuntimeInfo.SearchLabel == key)
            {
                RuntimeInfo.SearchLabel = string.Empty;
            }
        }

        [Statement("ret", SearchMode.Exact, SpaceAround.None)]
        public void Return(string _)
        {
            if (RuntimeInfo.LabelStack.Count > 0)
            {
                RuntimeInfo.LineNumber = RuntimeInfo.LabelStack.Pop();
            }
            else
            {
                RuntimeInfo.Exit("No label in stack");
            }
        }

        [Statement("end", SearchMode.Exact, SpaceAround.None)]
        public void End(string _)
        {
            RuntimeInfo.Exit("Planned termination by code");
        }

        [Statement("!calc", SearchMode.EndOfLine, SpaceAround.Start, Priority = Priority.High, ExecuteInSearchLabelMode = true)]
        public void Calculate(string args)
        {
            MatchCollection matches = Regex.Matches(args, @"((\)?)+(\(?)+[0-9]+(((\s?)+(\)?)(\s?)+\+(\s?)+(\(?)+(\s?)+|(\s?)+(\)?)(\s?)+\-(\s?)+(\(?)+(\s?)+|(\s?)+(\)?)(\s?)+\*(\s?)+(\(?)+(\s?)+|(\s?)+(\)?)(\s?)+\/(\s?)+(\(?)+(\s?)+)|[,.])(?=[0-9])+)+[0-9]+(\)?)+");

            for (int i = 0; i < matches.Count; i++)
            {
                string res = Evaluator.Calculate(matches[i].Value);
                if (res is null)
                {
                    RuntimeInfo.Exit("Invalid operation");
                    return;
                }
                args = args.Replace(matches[0].Value, res);
            }

            RuntimeInfo.CurrentLine = args;
        }

        [Statement("!eval", SearchMode.EndOfLine, SpaceAround.Start, Priority = Priority.VeryHigh, ExecuteInSearchLabelMode = true)]
        public void Evaluate(string args)
        {
            RuntimeInfo.CurrentLine = args.FromSaveString();
        }

        [Statement("<", SearchMode.StartOfLine, SpaceAround.None, Priority = Priority.VeryLow, IgnoreSyntaxHighlighting = true)]
        public void DefineVariable(string args)
        {
            string[] parts = args.Split('=');
            if (parts.Length == 2)
            {
                string key = parts[0].Replace("<", "").Trim();
                if (RuntimeInfo.Variables.ContainsKey(key))
                {
                    RuntimeInfo.Variables[key] = parts[1].Trim();
                }
                else
                {
                    RuntimeInfo.Variables.Add(key, parts[1].Trim());
                }
            }
            else
            {
                RuntimeInfo.Exit("Invalid syntax");
                return;
            }
        }

        [StaticStatement(ExecuteInSearchLabelMode = true)]
        public void ReadVariable()
        {
            if (!RuntimeInfo.CurrentLine.Contains('>'))
            {
                return;
            }

            foreach (KeyValuePair<string, string> variable in RuntimeInfo.Variables)
            {
                RuntimeInfo.CurrentLine = RuntimeInfo.CurrentLine.Replace($">{variable.Key}", variable.Value);
            }
        }
    }
}