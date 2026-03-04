using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

using YesNt.Interpreter.Attributes;
using YesNt.Interpreter.Enums;
using YesNt.Interpreter.Utilities;

namespace YesNt.Interpreter.Runtime;

public class YesNtInterpreter
{
    public event Action<DebugEventArgs> OnLineExecuted;

    public event Action<string> OnDebugOutput;

    private readonly RuntimeInformation runtimeInfo = new RuntimeInformation();
    private Dictionary<StatementAttribute, Action<string>> statements = [];
    private List<KeyValuePair<StaticStatementAttribute, Action>> staticStatements = [];

    public ReadOnlyCollection<StatementInformation> StatementInformation
    {
        get
        {
            List<StatementInformation> information = statements.Select(s =>
            {
                return new StatementInformation()
                {
                    Name = s.Key.Name,
                    SearchMode = s.Key.SearchMode,
                    SpaceAround = s.Key.SpaceAround,
                    Color = s.Key.Color,
                    IgnoreSyntaxHighlighting = s.Key.IgnoreSyntaxHighlighting,
                    Separator = s.Key.Separator
                };
            }).ToList();

            return new ReadOnlyCollection<StatementInformation>(information);
        }
    }

    public void AddStatement(StatementAttribute attribute, Action<string> handler)
    {
        statements[attribute] = handler;
        statements = statements
            .OrderBy(s => s.Key.Priority)
            .ThenByDescending(s => s.Key.Name.Length)
            .ToDictionary(x => x.Key, x => x.Value);
    }

    public void AddStatement(string name, SearchMode searchMode, SpaceAround spaceAround, Action<string> handler)
    {
        AddStatement(new StatementAttribute(name, searchMode, spaceAround), handler);
    }

    public void AddStatement(string name, SearchMode searchMode, SpaceAround spaceAround, ConsoleColor consoleColor, Action<string> handler)
    {
        AddStatement(new StatementAttribute(name, searchMode, spaceAround, consoleColor), handler);
    }

    public YesNtInterpreter()
    {
        GeneratedStatementRegistry.Register(runtimeInfo, out statements, out staticStatements);

        runtimeInfo.OnDebugOutput += (s) => OnDebugOutput?.Invoke(s);
        runtimeInfo.OnLineExecuted += (DebugEventArgs e) => OnLineExecuted?.Invoke(e);
    }

    public void Stop()
    {
        runtimeInfo.Exit(ExitMessages.TerminatedByExternalProcess, true);
    }

    public void Execute(string path, bool isDebugMode = false)
    {
        runtimeInfo.Reset();
        runtimeInfo.IsDebugMode = isDebugMode;
        if (LoadFile(path))
        {
            Execute();
        }
    }

    public void Execute(List<string> lines, bool isDebugMode = false)
    {
        runtimeInfo.Reset();
        runtimeInfo.IsDebugMode = isDebugMode;

        for (int i = 0; i < lines.Count; i++)
        {
            runtimeInfo.Lines.Add(new Line(lines[i], Path.GetFileName("#Memory#"), i));
        }

        Execute();
    }

    internal void Execute(List<Line> lines, Dictionary<string, string> globalVariables, int startLine, RuntimeInformation parentRuntimeInformation)
    {
        runtimeInfo.Reset();
        runtimeInfo.IsDebugMode = parentRuntimeInformation.IsDebugMode;
        runtimeInfo.Lines = lines;
        runtimeInfo.LineNumber = startLine;
        runtimeInfo.ParentRuntimeInformation = parentRuntimeInformation;
        runtimeInfo.GlobalVariables = globalVariables;
        if (parentRuntimeInformation.StopAllTasks)
        {
            runtimeInfo.Exit(ExitMessages.TerminatedByParentTask, parentRuntimeInformation.StopAllTasks);
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

            runtimeInfo.CurrentLine = runtimeInfo.Lines[runtimeInfo.LineNumber].Content.Trim(' ').Replace("\r", string.Empty);

            if (string.IsNullOrWhiteSpace(runtimeInfo.CurrentLine) || runtimeInfo.CurrentLine.StartsWith('#'))
            {
                continue;
            }

            DebugEventArgs debugEventArgs = new DebugEventArgs()
            {
                LineNumber = runtimeInfo.LineNumber + 1,
                OriginalLine = runtimeInfo.CurrentLine.FromSafeString(),
                IsTask = runtimeInfo.IsTask,
                TaskId = runtimeInfo.TaskId
            };

            foreach (KeyValuePair<StaticStatementAttribute, Action> staticStatement in staticStatements)
            {
                StaticStatementAttribute staticStatementAttribute = staticStatement.Key;
                if (!staticStatementAttribute.ExecuteInSearchMode && runtimeInfo.IsSearching)
                {
                    continue;
                }

                staticStatement.Value.Invoke();
            }

            bool statementFound = false;
            bool notSearchingLabel = !runtimeInfo.IsSearching;

            foreach (KeyValuePair<StatementAttribute, Action<string>> statement in statements)
            {
                StatementAttribute statementAttribute = statement.Key;

                if (!statementAttribute.ExecuteInSearchMode && runtimeInfo.IsSearching)
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
                    _ => statementAttribute.Name.Trim()
                };

                if (statementAttribute.Separator is null || runtimeInfo.CurrentLine.Contains(statementAttribute.Separator))
                {
                    if (statementAttribute.SearchMode == SearchMode.StartOfLine && runtimeInfo.CurrentLine.StartsWith(name))
                    {
                        string copyLine = statementAttribute.KeepStatementInArgs ? runtimeInfo.CurrentLine : runtimeInfo.CurrentLine[name.Length..];
                        statement.Value.Invoke(copyLine);
                        statementFound = true;
                    }
                    else if (statementAttribute.SearchMode == SearchMode.Contains && runtimeInfo.CurrentLine.Contains(name))
                    {
                        string copyLine = statementAttribute.KeepStatementInArgs ? runtimeInfo.CurrentLine : runtimeInfo.CurrentLine.Replace(name, string.Empty);
                        statement.Value.Invoke(copyLine);
                        statementFound = true;
                    }
                    else if (statementAttribute.SearchMode == SearchMode.EndOfLine && runtimeInfo.CurrentLine.EndsWith(name))
                    {
                        string copyLine = statementAttribute.KeepStatementInArgs ? runtimeInfo.CurrentLine : runtimeInfo.CurrentLine[..^name.Length];
                        statement.Value.Invoke(copyLine);
                        statementFound = true;
                    }
                    else if (statementAttribute.SearchMode == SearchMode.Exact && runtimeInfo.CurrentLine.Equals(name))
                    {
                        statement.Value.Invoke(runtimeInfo.CurrentLine);
                        statementFound = true;
                    }
                }
            }

            if (!statementFound)
            {
                runtimeInfo.Exit(ExitMessages.InvalidStatement, true);
            }
            if (runtimeInfo.IsDebugMode && notSearchingLabel)
            {
                debugEventArgs.CurrentLine = runtimeInfo.CurrentLine.FromSafeString();
                runtimeInfo.LineExecuted(debugEventArgs);
            }
        }

        if (!runtimeInfo.Stop)
        {
            if (!string.IsNullOrWhiteSpace(runtimeInfo.SearchLabel))
            {
                runtimeInfo.Exit(ExitMessages.LabelNotFound(runtimeInfo.SearchLabel), true);
            }
            else if (!string.IsNullOrWhiteSpace(runtimeInfo.SearchFunction))
            {
                runtimeInfo.Exit(ExitMessages.FunctionNotFound(runtimeInfo.SearchFunction), true);
            }
            else
            {
                runtimeInfo.Exit(ExitMessages.EndOfFile, false);
            }

            if (runtimeInfo.IsDebugMode)
            {
                runtimeInfo.LineExecuted(null);
            }
        }
    }

    private bool LoadFile(string path)
    {
        path = Path.GetFullPath(path);

        if (!File.Exists(path))
        {
            Console.WriteLine($"File \"{path}\" not found!");
            return false;
        }

        string[] lines = File.ReadAllLines(path);

        if (lines.Length <= 0)
        {
            Console.WriteLine($"File \"{path}\" is empty!");
            return false;
        }

        runtimeInfo.WorkingDirectory = Path.GetDirectoryName(path);

        for (int i = 0; i < lines.Length; i++)
        {
            runtimeInfo.Lines.Add(new Line(lines[i], Path.GetFileName(path), i));
        }

        return true;
    }
}
