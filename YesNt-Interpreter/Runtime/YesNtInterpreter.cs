using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;

using YesNt.Interpreter.Attributes;
using YesNt.Interpreter.Enums;
using YesNt.Interpreter.Runtime;
using YesNt.Interpreter.Utilities;

namespace YesNt.Interpreter
{
    public class YesNtInterpreter
    {
        private readonly RuntimeInformation runtimeInfo = new RuntimeInformation();
        private Dictionary<StatementAttribute, Action<string>> statements = new();
        private List<KeyValuePair<StaticStatementAttribute, Action>> staticStatements = new();

        public ReadOnlyCollection<StatementInformation> StatementInformation
        {
            get
            {
                List<StatementInformation> informations = statements.Select(s =>
                {
                    return new StatementInformation()
                    {
                        Name = s.Key.Name,
                        SearchMode = s.Key.SearchMode,
                        SpaceAround = s.Key.SpaceAround,
                        IgnoreSyntaxHighlighting = s.Key.IgnoreSyntaxHighlighting
                    };
                }).ToList();
                return new ReadOnlyCollection<StatementInformation>(informations);
            }
        }

        public event Action<DebugEventArgs> OnLineExecuted;

        public event Action<string> OnDebugOutput;

        public void Stop()
        {
            runtimeInfo.Exit("Terminated by external process", true);
        }

        public void Initialize()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            Type[] types = assembly.GetTypes();

            IEnumerable<Type> statementRuntimeInfos = types.Where(t => t.IsSubclassOf(typeof(StatementRuntimeInformation)));

            statements.Clear();

            foreach (Type type in statementRuntimeInfos)
            {
                object statementInfo = Activator.CreateInstance(type);

                MethodInfo[] methodInfos = statementInfo.GetType().GetMethods();

                StatementRuntimeInformation statementRuntimeInfo = statementInfo as StatementRuntimeInformation;
                statementRuntimeInfo.RuntimeInfo = runtimeInfo;

                foreach (MethodInfo methodInfo in methodInfos)
                {
                    StatementAttribute statementAttribute = methodInfo.GetCustomAttribute<StatementAttribute>();
                    if (statementAttribute is not null)
                    {
                        Action<string> method = methodInfo.CreateDelegate(typeof(Action<string>), statementInfo) as Action<string>;
                        statements.Add(statementAttribute, method);
                    }

                    StaticStatementAttribute staticStatementAttribute = methodInfo.GetCustomAttribute<StaticStatementAttribute>();
                    if (staticStatementAttribute is not null)
                    {
                        Action method = methodInfo.CreateDelegate(typeof(Action), statementInfo) as Action;
                        staticStatements.Add(new(staticStatementAttribute, method));
                    }
                }
            }

            statements = statements.OrderBy(s => s.Key.Priority).ToDictionary(x => x.Key, x => x.Value);
            staticStatements = staticStatements.OrderBy(s => s.Key.Priority).ToList();

            runtimeInfo.OnDebugOutput += (s) => OnDebugOutput?.Invoke(s);
            runtimeInfo.OnLineExecuted += (DebugEventArgs e) => OnLineExecuted.Invoke(e);
        }

        public void Execute(string path, bool isDebugMode = false)
        {
            runtimeInfo.Reset();
            runtimeInfo.IsDebugMode = isDebugMode;
            LoadFile(path);
            Execute();
        }

        internal void Execute(List<string> lines, int startLine, RuntimeInformation parentRuntimeInformation)
        {
            runtimeInfo.Reset();
            runtimeInfo.IsDebugMode = parentRuntimeInformation.IsDebugMode;
            runtimeInfo.Lines = lines;
            runtimeInfo.LineNumber = startLine;
            runtimeInfo.ParentRuntimeInformation = parentRuntimeInformation;
            if (parentRuntimeInformation.StopAllTasks)
            {
                runtimeInfo.Exit($"Parent task was terminated!", parentRuntimeInformation.StopAllTasks);
                return;
            }
            Execute();
        }

        private void Execute()
        {
            for (; runtimeInfo.LineNumber < runtimeInfo.Lines.Count; runtimeInfo.LineNumber++)
            {
                if (runtimeInfo.Stop)
                {
                    break;
                }

                runtimeInfo.CurrentLine = runtimeInfo.Lines[runtimeInfo.LineNumber].TrimEnd().Replace("\r", string.Empty);

                DebugEventArgs debugEventArgs = new DebugEventArgs()
                {
                    LineNumber = runtimeInfo.LineNumber + 1,
                    OriginalLine = runtimeInfo.CurrentLine.FromSaveString()
                };

                if (string.IsNullOrWhiteSpace(runtimeInfo.CurrentLine))
                {
                    continue;
                }

                foreach (KeyValuePair<StaticStatementAttribute, Action> staticStatement in staticStatements)
                {
                    StaticStatementAttribute staticStatementAttribute = staticStatement.Key;
                    if (staticStatementAttribute.ExecuteInSearchLabelMode == false && !string.IsNullOrWhiteSpace(runtimeInfo.SearchLabel))
                    {
                        continue;
                    }

                    staticStatement.Value.Invoke();
                }

                bool statementFound = false;
                bool searchingLabel = string.IsNullOrWhiteSpace(runtimeInfo.SearchLabel);

                foreach (KeyValuePair<StatementAttribute, Action<string>> statement in statements)
                {
                    StatementAttribute statementAttribute = statement.Key;

                    if (statementAttribute.ExecuteInSearchLabelMode == false && !string.IsNullOrWhiteSpace(runtimeInfo.SearchLabel))
                    {
                        statementFound = true;
                        continue;
                    }

                    if (runtimeInfo.Stop)
                    {
                        break;
                    }

                    string name = statementAttribute.SpaceAround switch
                    {
                        SpaceAround.StartEnd => $" {statementAttribute.Name.Trim()} ",
                        SpaceAround.Start => $" {statementAttribute.Name.Trim()}",
                        SpaceAround.End => $"{statementAttribute.Name.Trim()} ",
                        _ => statementAttribute.Name
                    };

                    if (statementAttribute.SearchMode == SearchMode.StartOfLine && runtimeInfo.CurrentLine.StartsWith(name))
                    {
                        string copyLine = statementAttribute.KeepStatementInArgs ? runtimeInfo.CurrentLine : runtimeInfo.CurrentLine.Remove(0, name.Length);
                        statement.Value.Invoke(copyLine);
                        statementFound = true;
                    }
                    else if (statementAttribute.SearchMode == SearchMode.Contains && $"{runtimeInfo.CurrentLine} ".Contains(name))
                    {
                        runtimeInfo.CurrentLine = $"{runtimeInfo.CurrentLine} ";
                        string copyLine = statementAttribute.KeepStatementInArgs ? runtimeInfo.CurrentLine : runtimeInfo.CurrentLine.Replace(name, string.Empty);
                        statement.Value.Invoke(copyLine);
                        statementFound = true;
                    }
                    else if (statementAttribute.SearchMode == SearchMode.EndOfLine && runtimeInfo.CurrentLine.EndsWith(name))
                    {
                        string copyLine = statementAttribute.KeepStatementInArgs ? runtimeInfo.CurrentLine : runtimeInfo.CurrentLine.Remove(runtimeInfo.CurrentLine.Length - name.Length);
                        statement.Value.Invoke(copyLine);
                        statementFound = true;
                    }
                    else if (statementAttribute.SearchMode == SearchMode.Exact && runtimeInfo.CurrentLine.Equals(name))
                    {
                        statement.Value.Invoke(runtimeInfo.CurrentLine);
                        statementFound = true;
                    }
                }

                if (!statementFound)
                {
                    runtimeInfo.Exit("Invalid statement", true);
                }
                if (runtimeInfo.IsDebugMode && searchingLabel)
                {
                    debugEventArgs.CurrentLine = runtimeInfo.CurrentLine.FromSaveString();
                    runtimeInfo.LineExecuted(debugEventArgs);
                }
            }

            if (runtimeInfo.Stop == false)
            {
                if (string.IsNullOrWhiteSpace(runtimeInfo.SearchLabel))
                {
                    runtimeInfo.Exit("End of file", false);
                }
                else
                {
                    runtimeInfo.Exit($"Label \"{runtimeInfo.SearchLabel}\" not found", true);
                }
            }
        }

        private void LoadFile(string path)
        {
            if (!File.Exists(path))
            {
                runtimeInfo.Exit($"File \"{path}\" not found!", true);
                return;
            }

            IEnumerable<string> lines = File.ReadAllLines(path);
            runtimeInfo.Lines.AddRange(lines);
        }
    }
}