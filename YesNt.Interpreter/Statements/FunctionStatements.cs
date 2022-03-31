using System;
using System.Collections.Generic;

using YesNt.Interpreter.Attributes;
using YesNt.Interpreter.Enums;
using YesNt.Interpreter.Runtime;

namespace YesNt.Interpreter.Statements
{
    internal class FunctionStatements : StatementRuntimeInformation
    {
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

        [Statement("in", SearchMode.StartOfLine, SpaceAround.End, ConsoleColor.Yellow)]
        public void AddInParameter(string args)
        {
            RuntimeInfo.InParametersStack.Push(args);
        }

        [Statement("out", SearchMode.StartOfLine, SpaceAround.End, ConsoleColor.Yellow)]
        public void GetOutParameter(string args)
        {
            if (RuntimeInfo.OutParametersStack.Count == 0)
            {
                RuntimeInfo.Exit("No out argument in stack", true);
                return;
            }

            if (RuntimeInfo.Variables.ContainsKey(args))
            {
                RuntimeInfo.Variables[args] = RuntimeInfo.OutParametersStack.Pop();
            }
            else
            {
                RuntimeInfo.Variables.Add(args, RuntimeInfo.OutParametersStack.Pop());
            }
        }

        [Statement("%iso", SearchMode.Contains, SpaceAround.None, ConsoleColor.Yellow, KeepStatementInArgs = true, Priority = Priority.Highest)]
        public void CheckIfOutParameterAvalible(string args)
        {
            args += " ";
            args = args.Replace("%iso", (RuntimeInfo.OutParametersStack.Count > 0).ToString());

            RuntimeInfo.CurrentLine = args.TrimEnd();
        }

        [Statement("cal", SearchMode.StartOfLine, SpaceAround.End, ConsoleColor.DarkYellow, Priority = Priority.Low, Seperator = "|")]
        public void Call(string args)
        {
            string[] parts = args.Split('|');
            if (parts.Length != 2)
            {
                RuntimeInfo.Exit("Invalid syntax", true);
                return;
            }

            string key = parts[0].Trim();
            string[] functionArgumets = parts[1].Split(',');

            foreach (string argumanet in functionArgumets)
            {
                RuntimeInfo.InParametersStack.Push(argumanet.Trim());
            }

            RuntimeInfo.FunctionCallStack.Push(new FunctionScope(RuntimeInfo.LineNumber, new Stack<string>(RuntimeInfo.InParametersStack)));
            RuntimeInfo.InParametersStack.Clear();
            RuntimeInfo.CurrentLine = string.Empty;

            if (RuntimeInfo.Functions.ContainsKey(key))
            {
                RuntimeInfo.LineNumber = RuntimeInfo.Functions[key];
            }
            else
            {
                RuntimeInfo.SearchFunction = key;
            }
        }

        [Statement("get", SearchMode.StartOfLine, SpaceAround.End, ConsoleColor.Yellow)]
        public void GetInParameter(string args)
        {
            if (!RuntimeInfo.IsInFunction)
            {
                RuntimeInfo.Exit("Statement not allowed outside of function", true);
                return;
            }

            if (RuntimeInfo.FunctionCallStack.Peek().Arguemtns.Count == 0)
            {
                RuntimeInfo.Exit("No in argument in stack", true);
                return;
            }

            if (RuntimeInfo.Variables.ContainsKey(args))
            {
                RuntimeInfo.Variables[args] = RuntimeInfo.FunctionCallStack.Peek().Arguemtns.Pop();
            }
            else
            {
                RuntimeInfo.Variables.Add(args, RuntimeInfo.FunctionCallStack.Peek().Arguemtns.Pop());
            }
        }

        [Statement("%isi", SearchMode.Contains, SpaceAround.End, ConsoleColor.Yellow, KeepStatementInArgs = true, Priority = Priority.Highest)]
        public void CheckIfInParameterAvalible(string args)
        {
            if (!RuntimeInfo.IsInFunction)
            {
                RuntimeInfo.Exit("Statement not allowed outside of function", true);
                return;
            }

            args += " ";
            args = args.Replace("%isi", (RuntimeInfo.FunctionCallStack.Peek().Arguemtns.Count > 0).ToString());

            RuntimeInfo.CurrentLine = args.TrimEnd();
        }

        [Statement("put", SearchMode.StartOfLine, SpaceAround.End, ConsoleColor.Yellow)]
        public void AddOutParameter(string args)
        {
            if (!RuntimeInfo.IsInFunction)
            {
                RuntimeInfo.Exit("Statement not allowed outside of function", true);
                return;
            }

            RuntimeInfo.FunctionCallStack.Peek().Results.Push(args);
        }

        [Statement("ret", SearchMode.Exact, SpaceAround.None, ConsoleColor.DarkYellow, ExecuteInSearchMode = true)]
        public void Return(string _)
        {
            if (!RuntimeInfo.IsInFunction)
            {
                RuntimeInfo.Exit("Statement not allowed outside of function", true);
                return;
            }

            if (RuntimeInfo.IsSearching)
            {
                RuntimeInfo.IsInFunction = false;

                if (RuntimeInfo.IsLocalSearch)
                {
                    RuntimeInfo.Exit($"Label \"{RuntimeInfo.SearchLabel}\" not found", true);
                }
                return;
            }
            else
            {
                RuntimeInfo.IsInFunction = false;
            }

            if (RuntimeInfo.FunctionCallStack.Count > 0)
            {
                FunctionScope functionScope = RuntimeInfo.FunctionCallStack.Pop();

                RuntimeInfo.OutParametersStack = new Stack<string>(functionScope.Results);
                RuntimeInfo.LineNumber = functionScope.CallerLine;
            }
            else
            {
                RuntimeInfo.Exit("No function in stack", true);
            }
        }

        [Statement("ccs", SearchMode.Exact, SpaceAround.None, ConsoleColor.Red)]
        public void ClearCallStack(string _)
        {
            RuntimeInfo.FunctionCallStack.Clear();
        }
    }
}