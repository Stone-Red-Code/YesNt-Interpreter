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
    private List<StatementHandler> statementHandlers;
    private List<List<StatementHandler>> lineMatchingHandlers = [];
    private readonly List<KeyValuePair<StaticStatementAttribute, Action>> staticStatements;
    private readonly Dictionary<string, List<KeyValuePair<StatementAttribute, Action<string>>>> disabledStatements = [];

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
        UpdateStatementHandlers();
        runtimeInfo.PreScanLinesAction = PreScanLines;

        runtimeInfo.OnDebugOutput += (s) => OnDebugOutput?.Invoke(s);
        runtimeInfo.OnLineExecuted += e => OnLineExecuted?.Invoke(e);
    }

    private void UpdateStatementHandlers()
    {
        statementHandlers = statements.Select(s =>
        {
            string name = s.Key.SpaceAround switch
            {
                SpaceAround.StartEnd => $" {s.Key.Name.Trim()} ",
                SpaceAround.Start => $" {s.Key.Name.Trim()}",
                SpaceAround.End => $"{s.Key.Name.Trim()} ",
                _ => s.Key.Name.Trim()
            };
            return new StatementHandler(s.Key, s.Value, name);
        }).ToList();
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
        UpdateStatementHandlers();
        PreScanLines();
    }

    /// <summary>
    /// Registers a custom statement using a pre-built <see cref="StatementAttribute"/>,
    /// with access to the script's <see cref="IStatementContext"/> (variables, line number, output, etc.).
    /// </summary>
    /// <param name="attribute">The attribute describing the keyword, search mode, and priority.</param>
    /// <param name="handler">
    /// The delegate invoked when the statement matches. Receives the argument text and the current
    /// <see cref="IStatementContext"/> for reading/writing script state.
    /// </param>
    public void AddStatement(StatementAttribute attribute, Action<string, IStatementContext> handler)
    {
        AddStatement(attribute, args => handler(args, runtimeInfo));
    }

    /// <summary>
    /// Registers a simple custom statement with default settings.
    /// </summary>
    /// <param name="name">The keyword to match.</param>
    /// <param name="searchMode">Where in the line the keyword is searched for.</param>
    /// <param name="spaceAround">Which sides of the keyword must be padded with a space.</param>
    /// <param name="handler">The delegate invoked when the statement matches.</param>
    public void AddStatement(string name, SearchMode searchMode, SpaceAround spaceAround, Action<string> handler)
    {
        AddStatement(new StatementAttribute(name, searchMode, spaceAround), handler);
    }

    /// <summary>
    /// Registers a simple custom statement with default settings,
    /// with access to the script's <see cref="IStatementContext"/> (variables, line number, output, etc.).
    /// </summary>
    /// <param name="name">The keyword to match.</param>
    /// <param name="searchMode">Where in the line the keyword is searched for.</param>
    /// <param name="spaceAround">Which sides of the keyword must be padded with a space.</param>
    /// <param name="handler">
    /// The delegate invoked when the statement matches. Receives the argument text and the current
    /// <see cref="IStatementContext"/> for reading/writing script state.
    /// </param>
    public void AddStatement(string name, SearchMode searchMode, SpaceAround spaceAround, Action<string, IStatementContext> handler)
    {
        AddStatement(new StatementAttribute(name, searchMode, spaceAround), handler);
    }

    /// <summary>
    /// Registers a simple custom statement with a specific syntax-highlight color.
    /// </summary>
    /// <param name="name">The keyword to match.</param>
    /// <param name="searchMode">Where in the line the keyword is searched for.</param>
    /// <param name="spaceAround">Which sides of the keyword must be padded with a space.</param>
    /// <param name="consoleColor">The color used for syntax highlighting.</param>
    /// <param name="handler">The delegate invoked when the statement matches.</param>
    public void AddStatement(string name, SearchMode searchMode, SpaceAround spaceAround, ConsoleColor consoleColor, Action<string> handler)
    {
        AddStatement(new StatementAttribute(name, searchMode, spaceAround, consoleColor), handler);
    }

    /// <summary>
    /// Registers a simple custom statement with a specific syntax-highlight color,
    /// with access to the script's <see cref="IStatementContext"/> (variables, line number, output, etc.).
    /// </summary>
    /// <param name="name">The keyword to match.</param>
    /// <param name="searchMode">Where in the line the keyword is searched for.</param>
    /// <param name="spaceAround">Which sides of the keyword must be padded with a space.</param>
    /// <param name="consoleColor">The color used for syntax highlighting.</param>
    /// <param name="handler">
    /// The delegate invoked when the statement matches. Receives the argument text and the current
    /// <see cref="IStatementContext"/> for reading/writing script state.
    /// </param>
    public void AddStatement(string name, SearchMode searchMode, SpaceAround spaceAround, ConsoleColor consoleColor, Action<string, IStatementContext> handler)
    {
        AddStatement(new StatementAttribute(name, searchMode, spaceAround, consoleColor), handler);
    }

    /// <summary>
    /// Unregisters all handlers matching the specified keyword <paramref name="name"/>.
    /// </summary>
    /// <param name="name">The keyword to remove.</param>
    public void RemoveStatement(string name)
    {
        foreach (StatementAttribute key in statements.Keys.Where(k => k.Name == name).ToList())
        {
            _ = statements.Remove(key);
        }

        _ = disabledStatements.Remove(name);
        UpdateStatementHandlers();
        PreScanLines();
    }

    /// <summary>
    /// Disables all statements matching <paramref name="name"/> by replacing their handlers with
    /// a no-op. The keyword still matches (so no "Invalid statement" error is raised), but the
    /// statement has no effect. Use <see cref="EnableStatement"/> to restore original behavior.
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
        UpdateStatementHandlers();
        PreScanLines();
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

        _ = disabledStatements.Remove(name);
        UpdateStatementHandlers();
        PreScanLines();
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
            string content = lines[i].Trim().Replace("\r", string.Empty);
            runtimeInfo.Lines.Add(new Line(content, Path.GetFileName("#Memory#"), i));
        }

        PreScanLines();
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
        PreScanLines();
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

            Line lineObj = runtimeInfo.Lines[runtimeInfo.LineNumber];
            runtimeInfo.CurrentLine = lineObj.Content;

            if (string.IsNullOrWhiteSpace(runtimeInfo.CurrentLine) || runtimeInfo.CurrentLine.StartsWith('#'))
            {
                continue;
            }

            DebugEventArgs debugEventArgs = null;
            if (runtimeInfo.IsDebugMode)
            {
                debugEventArgs = new DebugEventArgs()
                {
                    LineNumber = runtimeInfo.LineNumber + 1,
                    OriginalLine = runtimeInfo.CurrentLine.FromSafeString(),
                    IsTask = runtimeInfo.IsTask,
                    TaskId = runtimeInfo.TaskId
                };
            }

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

            List<StatementHandler> handlers = (runtimeInfo.LineNumber < lineMatchingHandlers.Count) ? lineMatchingHandlers[runtimeInfo.LineNumber] : [];

            foreach (StatementHandler handler in handlers)
            {
                StatementAttribute statementAttribute = handler.Attribute;

                if (!statementAttribute.ExecuteInSearchMode && runtimeInfo.IsSearching)
                {
                    statementFound = true;
                    continue;
                }

                if (runtimeInfo.Stop)
                {
                    break;
                }

                string name = handler.FullName;

                if (statementAttribute.Separator is null || runtimeInfo.CurrentLine.Contains(statementAttribute.Separator, StringComparison.Ordinal))
                {
                    if (statementAttribute.SearchMode == SearchMode.StartOfLine && runtimeInfo.CurrentLine.StartsWith(name, StringComparison.Ordinal))
                    {
                        string copyLine = statementAttribute.KeepStatementInArgs ? runtimeInfo.CurrentLine : runtimeInfo.CurrentLine[name.Length..];
                        handler.Handler.Invoke(copyLine);
                        statementFound = true;
                    }
                    else if (statementAttribute.SearchMode == SearchMode.Contains && runtimeInfo.CurrentLine.Contains(name, StringComparison.Ordinal))
                    {
                        string copyLine = statementAttribute.KeepStatementInArgs ? runtimeInfo.CurrentLine : runtimeInfo.CurrentLine.Replace(name, string.Empty);
                        handler.Handler.Invoke(copyLine);
                        statementFound = true;
                    }
                    else if (statementAttribute.SearchMode == SearchMode.EndOfLine && runtimeInfo.CurrentLine.EndsWith(name, StringComparison.Ordinal))
                    {
                        string copyLine = statementAttribute.KeepStatementInArgs ? runtimeInfo.CurrentLine : runtimeInfo.CurrentLine[..^name.Length];
                        handler.Handler.Invoke(copyLine);
                        statementFound = true;
                    }
                    else if (statementAttribute.SearchMode == SearchMode.Exact && runtimeInfo.CurrentLine.Equals(name, StringComparison.Ordinal))
                    {
                        handler.Handler.Invoke(runtimeInfo.CurrentLine);
                        statementFound = true;
                    }
                }
            }

            if (!statementFound)
            {
                runtimeInfo.Exit(ExitMessages.InvalidStatement, true);
            }
            if (runtimeInfo.IsDebugMode && notSearchingLabel && debugEventArgs != null)
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
            return false;
        }

        string[] lines = File.ReadAllLines(path);

        runtimeInfo.WorkingDirectory = Path.GetDirectoryName(path);

        for (int i = 0; i < lines.Length; i++)
        {
            string content = lines[i].Trim().Replace("\r", string.Empty);
            runtimeInfo.Lines.Add(new Line(content, Path.GetFileName(path), i));
        }

        PreScanLines();
        return true;
    }

    internal void PreScanLines()
    {
        runtimeInfo.BlockBoundaries.Clear();
        lineMatchingHandlers = new List<List<StatementHandler>>(runtimeInfo.Lines.Count);

        // Dictionary to track open blocks by their expected end statement name
        Dictionary<string, Stack<int>> openBlocks = [];

        for (int i = 0; i < runtimeInfo.Lines.Count; i++)
        {
            string content = runtimeInfo.Lines[i].Content;
            List<StatementHandler> matchingHandlers = [];

#pragma warning disable S3267 // foreach + if is intentional here; LINQ .Where() would add overhead in this scan loop
            foreach (StatementHandler handler in statementHandlers)
            {
                if (IsPossibleMatch(content, handler))
#pragma warning restore S3267
                {
                    matchingHandlers.Add(handler);

                    // Track block starts (skip intermediates — they are handled separately below)
                    string blockPair = handler.Attribute.BlockPair;
                    if (!string.IsNullOrEmpty(blockPair) && !handler.Attribute.IsBlockIntermediate)
                    {
                        if (!openBlocks.TryGetValue(blockPair, out Stack<int> stack))
                        {
                            stack = new Stack<int>();
                            openBlocks[blockPair] = stack;
                        }
                        stack.Push(i);
                    }

                    // Track block ends
                    if (handler.Attribute.IsBlockEnd && openBlocks.TryGetValue(handler.Attribute.Name, out Stack<int> endStack) && endStack.Count > 0)
                    {
                        int startLine = endStack.Pop();
                        runtimeInfo.BlockBoundaries[startLine] = i;
                        runtimeInfo.BlockBoundaries[i] = startLine;
                    }

                    // Track block intermediates (e.g., else:): pop the opener, record boundary, push self
                    if (handler.Attribute.IsBlockIntermediate)
                    {
                        string intermediatePair = handler.Attribute.BlockPair;
                        if (!string.IsNullOrEmpty(intermediatePair))
                        {
                            if (!openBlocks.TryGetValue(intermediatePair, out Stack<int> stack))
                            {
                                stack = new Stack<int>();
                                openBlocks[intermediatePair] = stack;
                            }
                            if (stack.Count > 0)
                            {
                                int startLine = stack.Pop();
                                runtimeInfo.BlockBoundaries[startLine] = i;
                            }
                            stack.Push(i);
                        }
                    }
                }
            }
            lineMatchingHandlers.Add(matchingHandlers);
        }
    }

    private static bool IsPossibleMatch(string content, StatementHandler handler)
    {
        StatementAttribute attr = handler.Attribute;
        string fullName = handler.FullName;

        return attr.SearchMode switch
        {
            SearchMode.Exact => content == fullName,
            SearchMode.StartOfLine => content.StartsWith(fullName, StringComparison.Ordinal),
            SearchMode.EndOfLine => content.EndsWith(fullName, StringComparison.Ordinal),
            SearchMode.Contains => content.Contains(fullName, StringComparison.Ordinal),
            _ => false
        };
    }
}