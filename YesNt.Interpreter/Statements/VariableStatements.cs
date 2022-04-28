using System.Collections.Generic;
using System.Text.RegularExpressions;

using YesNt.Interpreter.Attributes;
using YesNt.Interpreter.Enums;
using YesNt.Interpreter.Runtime;

namespace YesNt.Interpreter.Statements
{
    internal class VariableStatements : StatementRuntimeInformation
    {
        private static readonly Regex variableStatementRegex = new Regex(@">[a-zA-Z0-9]+");

        [Statement("<", SearchMode.StartOfLine, SpaceAround.None, Priority = Priority.VeryLow)]
        public void DefineVariable(string args)
        {
            string[] parts = args.Split('=');
            if (parts.Length == 2)
            {
                string key = parts[0].Trim();
                if (key.Contains(' '))
                {
                    RuntimeInfo.Exit("Invalid Syntax", true);
                }

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
                RuntimeInfo.Exit("Invalid syntax", true);
            }
        }

        [Statement("!<", SearchMode.StartOfLine, SpaceAround.None, Priority = Priority.VeryLow)]
        public void DefineGlobalVariable(string args)
        {
            string[] parts = args.Split('=');
            if (parts.Length == 2)
            {
                string key = parts[0].Trim();
                if (key.Contains(' '))
                {
                    RuntimeInfo.Exit("Invalid Syntax", true);
                }

                if (RuntimeInfo.GloablVariables.ContainsKey(key))
                {
                    RuntimeInfo.GloablVariables[key] = parts[1].Trim();
                }
                else
                {
                    RuntimeInfo.GloablVariables.Add(key, parts[1].Trim());
                }
            }
            else
            {
                RuntimeInfo.Exit("Invalid syntax", true);
            }
        }

        [Statement("del", SearchMode.StartOfLine, SpaceAround.End, System.ConsoleColor.Red, Priority = Priority.VeryLow)]
        public void DeleteVariable(string args)
        {
            string key = args.Trim();

            if (RuntimeInfo.Variables.ContainsKey(key))
            {
                RuntimeInfo.Variables.Remove(key);
            }
            else if (RuntimeInfo.GloablVariables.ContainsKey(key))
            {
                RuntimeInfo.GloablVariables.Remove(key);
            }
            else
            {
                RuntimeInfo.Exit($"Variable \"{key}\" not found", true);
            }
        }

        [Statement(">", SearchMode.Contains, SpaceAround.None, Priority = Priority.Highest)]
        public void ReadVariable(string _)
        {
            if (!RuntimeInfo.CurrentLine.Contains('>'))
            {
                return;
            }

            foreach (KeyValuePair<string, string> variable in RuntimeInfo.Variables)
            {
                RuntimeInfo.CurrentLine = RuntimeInfo.CurrentLine.Replace($">{variable.Key}", variable.Value);
            }

            foreach (KeyValuePair<string, string> variable in RuntimeInfo.GloablVariables)
            {
                RuntimeInfo.CurrentLine = RuntimeInfo.CurrentLine.Replace($">{variable.Key}", variable.Value);
            }

            if (RuntimeInfo.IsSearching)
            {
                return;
            }

            MatchCollection matches = variableStatementRegex.Matches(RuntimeInfo.CurrentLine);

            for (int i = 0; i < matches.Count; i++)
            {
                string varName = matches[i].Value.Replace(">", string.Empty);
                if (!RuntimeInfo.Variables.ContainsKey(varName) && !RuntimeInfo.GloablVariables.ContainsKey(varName))
                {
                    RuntimeInfo.Exit($"Variable \"{varName}\" not found", true);
                    return;
                }
            }
        }
    }
}