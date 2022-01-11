using System;
using System.Collections.Generic;
using System.Linq;

using YesNt.Interpreter.Attributes;
using YesNt.Interpreter.Enums;
using YesNt.Interpreter.Runtime;
using YesNt.Interpreter.Utilities;

namespace YesNt.Interpreter.Statements
{
    internal class CodeFlowStatements : StatementRuntimeInformation
    {
        [Statement("jmp", SearchMode.StartOfLine, SpaceAround.End, ConsoleColor.Green, Priority = Priority.VeryLow)]
        public void Jump(string args)
        {
            string key = args.Trim();

            if (RuntimeInfo.Labels.ContainsKey(key))
            {
                RuntimeInfo.LineNumber = RuntimeInfo.Labels[key];
            }
            else
            {
                RuntimeInfo.SearchLabel = key;
            }
        }

        [Statement("jif", SearchMode.StartOfLine, SpaceAround.End, ConsoleColor.Green, Priority = Priority.VeryLow, Seperator = "|")]
        public void JumpIf(string args)
        {
            string[] parts = args.Split('|');
            if (parts.Length != 2)
            {
                RuntimeInfo.Exit("Invalid syntax", true);
                return;
            }

            string key = parts[0].Trim();
            string condition = parts[1].Trim();

            bool? result = Evaluator.EvaluateCondition(condition);

            if (result != true)
            {
                if (result is null)
                {
                    RuntimeInfo.Exit("Invalid operation", true);
                }
                return;
            }

            if (RuntimeInfo.Labels.ContainsKey(key))
            {
                RuntimeInfo.LineNumber = RuntimeInfo.Labels[key];
            }
            else
            {
                RuntimeInfo.SearchLabel = key;
            }
        }

        [Statement("lbl", SearchMode.StartOfLine, SpaceAround.End, ConsoleColor.Green, ExecuteInSearchMode = true)]
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

        [Statement("cal", SearchMode.StartOfLine, SpaceAround.End, ConsoleColor.DarkYellow, Priority = Priority.VeryLow)]
        public void Call(string args)
        {
            string key = args.Trim();

            RuntimeInfo.FunctionCallStack.Push(new FunctionScope(RuntimeInfo.LineNumber, new Stack<string>(RuntimeInfo.InParametersStack.Reverse())));
            RuntimeInfo.InParametersStack.Clear();

            if (RuntimeInfo.Functions.ContainsKey(key))
            {
                RuntimeInfo.LineNumber = RuntimeInfo.Functions[key];
            }
            else
            {
                RuntimeInfo.SearchFunction = key;
            }
        }

        [Statement("cif", SearchMode.StartOfLine, SpaceAround.End, ConsoleColor.DarkYellow, Priority = Priority.VeryLow, Seperator = "|")]
        public void CallIf(string args)
        {
            string[] parts = args.Split('|');
            if (parts.Length != 2)
            {
                RuntimeInfo.Exit("Invalid syntax", true);
                return;
            }

            string key = parts[0].Trim();
            string condition = parts[1].Trim();

            bool? result = Evaluator.EvaluateCondition(condition);

            if (result != true)
            {
                if (result is null)
                {
                    RuntimeInfo.Exit("Invalid operation", true);
                }
                return;
            }

            RuntimeInfo.FunctionCallStack.Push(new FunctionScope(RuntimeInfo.LineNumber, new Stack<string>(RuntimeInfo.InParametersStack.Reverse())));
            RuntimeInfo.InParametersStack.Clear();

            if (RuntimeInfo.Functions.ContainsKey(key))
            {
                RuntimeInfo.LineNumber = RuntimeInfo.Functions[key];
            }
            else
            {
                RuntimeInfo.SearchFunction = key;
            }
        }

        [Statement("end", SearchMode.Exact, SpaceAround.None, ConsoleColor.Red, ExecuteInSearchMode = true)]
        public void End(string _)
        {
            if (RuntimeInfo.IsSearching)
            {
                RuntimeInfo.IsInFunction = false;
                return;
            }
            else
            {
                RuntimeInfo.IsInFunction = false;
            }

            RuntimeInfo.Exit("Planned termination by code", false);
        }

        [Statement("trm", SearchMode.Exact, SpaceAround.None, ConsoleColor.Red, ExecuteInSearchMode = true)]
        public void Terminate(string _)
        {
            if (RuntimeInfo.IsSearching)
            {
                RuntimeInfo.IsInFunction = false;
                return;
            }
            else
            {
                RuntimeInfo.IsInFunction = false;
            }

            RuntimeInfo.Exit("Planned termination by code. Canceling all tasks", true);
        }
    }
}