using System;

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

        [Statement("cal", SearchMode.StartOfLine, SpaceAround.End, ConsoleColor.DarkYellow, Priority = Priority.VeryLow)]
        public void Call(string args)
        {
            string key = args.Trim();

            RuntimeInfo.FunctionCallStack.Push(new FunctionScope(RuntimeInfo.LineNumber));

            if (RuntimeInfo.Functions.ContainsKey(key))
            {
                RuntimeInfo.LineNumber = RuntimeInfo.Functions[key];
            }
            else
            {
                RuntimeInfo.SearchFunction = key;
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

            RuntimeInfo.FunctionCallStack.Push(new FunctionScope(RuntimeInfo.LineNumber));

            if (RuntimeInfo.Functions.ContainsKey(key))
            {
                RuntimeInfo.LineNumber = RuntimeInfo.Functions[key];
            }
            else
            {
                RuntimeInfo.SearchFunction = key;
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

        [Statement("fnc", SearchMode.StartOfLine, SpaceAround.End, ConsoleColor.DarkYellow, ExecuteInSearchMode = true)]
        public void FindFunction(string args)
        {
            if (RuntimeInfo.InternalIsInFunction)
            {
                RuntimeInfo.Exit("Nested functions are not allowed", true);
                return;
            }

            string key = args.Trim();
            if (RuntimeInfo.Functions.ContainsKey(key))
            {
                RuntimeInfo.Functions[key] = RuntimeInfo.LineNumber;
            }
            else
            {
                RuntimeInfo.Functions.Add(key, RuntimeInfo.LineNumber);
            }

            if (!string.IsNullOrWhiteSpace(RuntimeInfo.SearchFunction) && RuntimeInfo.SearchFunction == key)
            {
                RuntimeInfo.SearchFunction = string.Empty;
            }

            RuntimeInfo.IsInFunction = true;
        }

        [Statement("ret", SearchMode.Exact, SpaceAround.None, ConsoleColor.DarkYellow, ExecuteInSearchMode = true)]
        public void Return(string _)
        {
            if (!RuntimeInfo.IsInFunction)
            {
                RuntimeInfo.Exit("Not in function", true);
                return;
            }

            if (RuntimeInfo.IsSearching)
            {
                RuntimeInfo.IsInFunction = false;
                return;
            }
            else
            {
                RuntimeInfo.IsInFunction = false;
            }

            if (RuntimeInfo.FunctionCallStack.Count > 0)
            {
                RuntimeInfo.LineNumber = RuntimeInfo.FunctionCallStack.Pop().CallerLine;
            }
            else
            {
                RuntimeInfo.Exit("No function in stack", true);
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