using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

using YesNt.Interpreter.Attributes;
using YesNt.Interpreter.Enums;
using YesNt.Interpreter.Utilities;

namespace YesNt.Interpreter.Runtime;

/// <summary>
/// The main entry point for executing YesNt scripts.
/// </summary>
/// <example>
/// Running a script file:
/// <code>
/// var interpreter = new YesNtInterpreter();
/// interpreter.Execute("path/to/script.ynt");
/// </code>
/// Running script lines in memory with a custom statement:
/// <code>
/// var interpreter = new YesNtInterpreter();
/// interpreter.AddStatement("log", SearchMode.StartOfLine, SpaceAround.End, args =>
///     Console.WriteLine($"[LOG] {args}"));
/// interpreter.Execute(new List&lt;string&gt; { "log hello world" });
/// </code>
/// </example>
public class YesNtInterpreter
{
    /// <summary>
    /// Raised after each line is executed in debug mode. The argument is <see langword="null"/>
    /// when execution ends (either normally or due to an error), allowing callers to detect completion.
    /// </summary>
    public event Action<DebugEventArgs> OnLineExecuted;

    /// <summary>
    /// Raised in debug mode whenever the script produces output (e.g. via <c>print_line</c>).
    /// In non-debug mode output is written directly to <see cref="Console"/>.
    /// </summary>
    public event Action<string> OnDebugOutput;

    private readonly RuntimeInformation runtimeInfo = new RuntimeInformation();
    private Dictionary<StatementAttribute, Action<string>> statements;
    private readonly List<KeyValuePair<StaticStatementAttribute, Action>> staticStatements;
    private readonly Dictionary<string, List<KeyValuePair<StatementAttribute, Action<string>>>> disabledStatements = new();

    /// <summary>
    /// Gets a read-only snapshot of all currently registered statements.
    /// Useful for building syntax highlighters or documentation tools.
    /// </summary>
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

    /// <summary>
    /// Initializes a new <see cref="YesNtInterpreter"/> and registers all built-in statements.
    /// </summary>
    public YesNtInterpreter()
    {
        GeneratedStatementRegistry.Register(runtimeInfo, out statements, out staticStatements);

        runtimeInfo.OnDebugOutput += (s) => OnDebugOutput?.Invoke(s);
        runtimeInfo.OnLineExecuted += e => OnLineExecuted?.Invoke(e);
    }

    /// <summary>
    /// Registers a custom statement using a pre-built <see cref="StatementAttribute"/>.
    /// If a statement with the same attribute key (identical field values) already exists it will be replaced;
    /// otherwise a new entry is added. Built-in statements use distinct attribute instances, so passing a
    /// newly constructed attribute with the same name will <b>add</b> a second handler rather than replacing
    /// the built-in. Use <see cref="RemoveStatement"/> first to replace a built-in keyword.
    /// The statement list is re-sorted by priority after insertion.
    /// </summary>
    /// <param name="attribute">The attribute describing the keyword, search mode, and priority.</param>
    /// <param name="handler">
    /// The delegate invoked when the statement matches. Receives the argument text
    /// (the part of the line after the keyword, unless <see cref="StatementAttribute.KeepStatementInArgs"/> is set).
    /// </param>
    public void AddStatement(StatementAttribute attribute, Action<string> handler)
    {
        statements[attribute] = handler;
        statements = statements
            .OrderBy(s => s.Key.Priority)
            .ThenByDescending(s => s.Key.Name.Length)
            .ToDictionary(x => x.Key, x => x.Value);
    }

    /// <summary>
    /// Registers a custom statement without a syntax-highlight colour.
    /// </summary>
    /// <param name="name">The keyword that identifies this statement in source code.</param>
    /// <param name="searchMode">Where in the line the keyword is matched.</param>
    /// <param name="spaceAround">Which sides of the keyword require a surrounding space.</param>
    /// <param name="handler">The delegate invoked when the statement matches.</param>
    public void AddStatement(string name, SearchMode searchMode, SpaceAround spaceAround, Action<string> handler)
    {
        AddStatement(new StatementAttribute(name, searchMode, spaceAround), handler);
    }

    /// <summary>
    /// Registers a custom statement with a syntax-highlight colour.
    /// </summary>
    /// <param name="name">The keyword that identifies this statement in source code.</param>
    /// <param name="searchMode">Where in the line the keyword is matched.</param>
    /// <param name="spaceAround">Which sides of the keyword require a surrounding space.</param>
    /// <param name="consoleColor">The colour used for syntax highlighting in the code editor.</param>
    /// <param name="handler">The delegate invoked when the statement matches.</param>
    public void AddStatement(string name, SearchMode searchMode, SpaceAround spaceAround, ConsoleColor consoleColor, Action<string> handler)
    {
        AddStatement(new StatementAttribute(name, searchMode, spaceAround, consoleColor), handler);
    }

    /// <summary>
    /// Permanently removes all built-in or custom statements that match <paramref name="name"/>.
    /// After removal, any script line that would have matched triggers an "Invalid statement" error.
    /// </summary>
    /// <param name="name">The keyword of the statement(s) to remove.</param>
    public void RemoveStatement(string name)
    {
        foreach (StatementAttribute key in statements.Keys.Where(k => k.Name == name).ToList())
        {
            statements.Remove(key);
        }

        disabledStatements.Remove(name);
    }

    /// <summary>
    /// Disables all statements matching <paramref name="name"/> by replacing their handlers with
    /// a no-op. The keyword still matches (so no "Invalid statement" error is raised), but the
    /// statement has no effect. Use <see cref="EnableStatement"/> to restore original behaviour.
    /// </summary>
    /// <param name="name">The keyword of the statement(s) to disable.</param>
    public void DisableStatement(string name)
    {
        if (disabledStatements.ContainsKey(name))
        {
            return;
        }

        List<KeyValuePair<StatementAttribute, Action<string>>> matching =
            statements.Where(kv => kv.Key.Name == name).ToList();

        if (matching.Count == 0)
        {
            return;
        }

        disabledStatements[name] = matching;

        foreach (KeyValuePair<StatementAttribute, Action<string>> kv in matching)
        {
            statements[kv.Key] = _ => { };
        }
    }

    /// <summary>
    /// Re-enables statements previously disabled with <see cref="DisableStatement"/>,
    /// restoring their original handlers.
    /// Has no effect if the statement is not currently disabled.
    /// </summary>
    /// <param name="name">The keyword of the statement(s) to re-enable.</param>
    public void EnableStatement(string name)
    {
        if (!disabledStatements.TryGetValue(name, out List<KeyValuePair<StatementAttribute, Action<string>>> saved))
        {
            return;
        }

        foreach (KeyValuePair<StatementAttribute, Action<string>> kv in saved)
        {
            statements[kv.Key] = kv.Value;
        }

        disabledStatements.Remove(name);
    }

    /// <summary>
    /// Requests a graceful stop of the currently executing script.
    /// The interpreter will terminate at the next line boundary.
    /// </summary>
    public void Stop()
    {
        runtimeInfo.Exit(ExitMessages.TerminatedByExternalProcess, true);
    }

    /// <summary>
    /// Executes a YesNt script file.
    /// </summary>
    /// <param name="path">The path to the <c>.ynt</c> script file.</param>
    /// <param name="isDebugMode">
    /// When <see langword="true"/>, output is routed through <see cref="OnDebugOutput"/> instead of
    /// <see cref="Console"/> and line-execution events are raised via <see cref="OnLineExecuted"/>.
    /// </param>
    public void Execute(string path, bool isDebugMode = false)
    {
        runtimeInfo.Reset();
        runtimeInfo.IsDebugMode = isDebugMode;
        if (LoadFile(path))
        {
            Execute();
        }
    }

    /// <summary>
    /// Executes a YesNt script supplied as an in-memory list of lines.
    /// </summary>
    /// <param name="lines">The script lines to execute.</param>
    /// <param name="isDebugMode">
    /// When <see langword="true"/>, output is routed through <see cref="OnDebugOutput"/> and
    /// line-execution events are raised via <see cref="OnLineExecuted"/>.
    /// </param>
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
