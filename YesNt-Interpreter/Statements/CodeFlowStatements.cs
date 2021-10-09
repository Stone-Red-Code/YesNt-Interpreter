using YesNt.Interpreter.Attributes;
using YesNt.Interpreter.Enums;
using YesNt.Interpreter.Utilities;

namespace YesNt.Interpreter.Statements
{
    internal class CodeFlowStatements : StatementRuntimeInformation
    {
        [Statement("jmp", SearchMode.StartOfLine, SpaceAround.End, Priority = Priority.VeryLow)]
        public void Jump(string args)
        {
            string key = args.Trim();

            if (RuntimeInfo.LabelStack.Count == 0 || RuntimeInfo.LabelStack.Peek() != RuntimeInfo.LineNumber)
            {
                RuntimeInfo.LabelStack.Push(RuntimeInfo.LineNumber);
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

        [Statement("jif", SearchMode.StartOfLine, SpaceAround.End, Priority = Priority.VeryLow)]
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

            if (RuntimeInfo.LabelStack.Count == 0 || RuntimeInfo.LabelStack.Peek() != RuntimeInfo.LineNumber)
            {
                RuntimeInfo.LabelStack.Push(RuntimeInfo.LineNumber);
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
                RuntimeInfo.Exit("No label in stack", true);
            }
        }

        [Statement("end", SearchMode.Exact, SpaceAround.None)]
        public void End(string _)
        {
            RuntimeInfo.Exit("Planned termination by code", false);
        }

        [Statement("trm", SearchMode.Exact, SpaceAround.None)]
        public void Terminate(string _)
        {
            RuntimeInfo.Exit("Planned termination by code. Canceling all tasks", true);
        }
    }
}