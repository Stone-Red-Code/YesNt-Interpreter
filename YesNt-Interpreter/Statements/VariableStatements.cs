using System.Collections.Generic;

using YesNt.Interpreter.Attributes;
using YesNt.Interpreter.Enums;

namespace YesNt.Interpreter.Statements
{
    internal class VariableStatements : StatementRuntimeInformation
    {
        [Statement("<", SearchMode.StartOfLine, SpaceAround.None, Priority = Priority.VeryLow, IgnoreSyntaxHighlighting = true)]
        public void DefineVariable(string args)
        {
            string[] parts = args.Split('=');
            if (parts.Length == 2)
            {
                string key = parts[0].Replace("<", "").Trim();
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