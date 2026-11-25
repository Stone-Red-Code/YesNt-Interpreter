using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
    private Dictionary<StatementInformation, Action<string>> statements;
    private List<StatementHandler> statementHandlers;
    private List<List<StatementHandler>> lineMatchingHandlers = [];
    private readonly List<KeyValuePair<StaticStatementInformation, Action>> staticStatements;
    private readonly Dictionary<string, List<KeyValuePair<StatementInformation, Action<string>>>> disabledStatements = [];

    // Every change the host made to the statements, in order, so a task's interpreter gets the
    // same ones. A step that disables a statement names it, so enabling it again drops the step.
    private readonly List<ConfigurationStep> configuration = [];

    // Tasks run in turns with the script (TaskMode.Cooperative).
    private readonly List<YesNtInterpreter> tasks = [];

    // While the script itself runs (its tasks may outlast it).
    private bool scriptRunning;

    private sealed record ConfigurationStep(string DisabledName, Action<YesNtInterpreter> Apply);

    /// <summary>
    /// How long <see cref="RunToCompletion"/> (and so <c>Execute</c>) sleeps between looks
    /// while the script waits.
    /// </summary>
    private const int WaitPollMilliseconds = 10;

    /// <summary>
    /// Gets a read-only snapshot of all currently registered statements.
    /// Useful for building syntax highlighters or documentation tools.
    /// </summary>
    public ReadOnlyCollection<StatementInformation> StatementInformation => statements.Keys.ToList().AsReadOnly();

    /// <summary>
    /// <see langword="true"/> from the moment <see cref="Prepare(string, bool)"/> (or any
    /// <c>Execute</c> overload) is called until the script finishes or is stopped, and with
    /// <see cref="TaskMode.Cooperative"/> until its tasks have ended too.
    /// Use this to drive step/run loops: <c>while (interpreter.IsRunning) interpreter.Step(10);</c>
    /// </summary>
    public bool IsRunning => scriptRunning || tasks.Count > 0;

    /// <summary>
    /// Gets or sets the script's console and files: where output goes outside debug mode, where
    /// input comes from, what <c>clear</c> clears and how script files are read. Defaults to
    /// a <see cref="ConsoleHost"/>. Set it before the script starts; its tasks share it.
    /// </summary>
    public IYesNtHost Host
    {
        get => runtimeInfo.Host;
        set => runtimeInfo.Host = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Gets or sets how the script's <c>task</c> lines run: each on a thread of its own (the
    /// default) or taking turns with the script on the thread that steps it. Set it before the
    /// script starts; its tasks use the same mode.
    /// </summary>
    public TaskMode TaskMode { get; set; } = TaskMode.Thread;

    /// <summary>
    /// Initializes a new <see cref="YesNtInterpreter"/> and registers all built-in statements.
    /// </summary>
    public YesNtInterpreter()
    {
        GeneratedStatementRegistry.Register(runtimeInfo, out statements, out staticStatements);
        UpdateStatementHandlers();
        runtimeInfo.PreScanLinesAction = PreScanLines;
        runtimeInfo.StartTaskAction = StartTask;

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
    /// Registers a custom statement using a pre-built <see cref="StatementInformation"/>.
    /// If a statement with the same attribute key (identical field values) already exists it will be replaced;
    /// otherwise a new entry is added. Built-in statements use distinct attribute instances, so passing a
    /// newly constructed attribute with the same name will <b>add</b> a second handler rather than replacing
    /// the built-in. Use <see cref="RemoveStatement"/> first to replace a built-in keyword.
    /// The statement list is re-sorted by priority after insertion. Tasks the script starts get
    /// the statement too.
    /// </summary>
    /// <param name="attribute">The attribute describing the keyword, search mode, and priority.</param>
    /// <param name="handler">
    /// The delegate invoked when the statement matches. Receives the argument text
    /// (the part of the line after the keyword, unless <see cref="StatementInformation.KeepStatementInArgs"/> is set).
    /// </param>
    public void AddStatement(StatementInformation attribute, Action<string> handler)
    {
        configuration.Add(new ConfigurationStep(null, interpreter => interpreter.AddStatementCore(attribute, handler)));
        AddStatementCore(attribute, handler);
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
    public void AddStatement(StatementInformation attribute, Action<string, IStatementContext> handler)
    {
        // A task's interpreter hands the handler its own context.
        configuration.Add(new ConfigurationStep(null, interpreter => interpreter.AddStatementCore(attribute, args => handler(args, interpreter.runtimeInfo))));
        AddStatementCore(attribute, args => handler(args, runtimeInfo));
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
        AddStatement(new StatementInformation(name, searchMode, spaceAround), handler);
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
        AddStatement(new StatementInformation(name, searchMode, spaceAround), handler);
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
        AddStatement(new StatementInformation(name, searchMode, spaceAround, consoleColor), handler);
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
        AddStatement(new StatementInformation(name, searchMode, spaceAround, consoleColor), handler);
    }

    private void AddStatementCore(StatementInformation attribute, Action<string> handler)
    {
        statements[attribute] = handler;

        List<KeyValuePair<StatementInformation, Action<string>>> entries = [.. statements];
        entries.Sort((a, b) =>
        {
            int cmp = a.Key.Priority.CompareTo(b.Key.Priority);
            return cmp != 0 ? cmp : b.Key.Name.Length.CompareTo(a.Key.Name.Length);
        });

        statements = [];

        foreach (KeyValuePair<StatementInformation, Action<string>> entry in entries)
        {
            statements.Add(entry.Key, entry.Value);
        }

        UpdateStatementHandlers();
        PreScanLines();
    }

    /// <summary>
    /// Unregisters all handlers matching the specified keyword <paramref name="name"/>.
    /// </summary>
    /// <param name="name">The keyword to remove.</param>
    public void RemoveStatement(string name)
    {
        configuration.Add(new ConfigurationStep(null, interpreter => interpreter.RemoveStatementCore(name)));
        RemoveStatementCore(name);
    }

    private void RemoveStatementCore(string name)
    {
        foreach (StatementInformation key in statements.Keys.Where(k => k.Name == name).ToList())
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
        if (DisableStatementCore(name))
        {
            configuration.Add(new ConfigurationStep(name, interpreter => interpreter.DisableStatementCore(name)));
        }
    }

    private bool DisableStatementCore(string name)
    {
        if (disabledStatements.ContainsKey(name))
        {
            return false;
        }

        List<KeyValuePair<StatementInformation, Action<string>>> matching =
            statements.Where(kv => kv.Key.Name == name).ToList();

        if (matching.Count == 0)
        {
            return false;
        }

        disabledStatements[name] = matching;

        foreach (KeyValuePair<StatementInformation, Action<string>> kv in matching)
        {
            statements[kv.Key] = _ => { };
        }
        UpdateStatementHandlers();
        PreScanLines();
        return true;
    }

    /// <summary>
    /// Re-enables statements previously disabled with <see cref="DisableStatement"/>,
    /// restoring their original handlers.
    /// Has no effect if the statement is not currently disabled.
    /// </summary>
    /// <param name="name">The keyword of the statement(s) to re-enable.</param>
    public void EnableStatement(string name)
    {
        if (!disabledStatements.TryGetValue(name, out List<KeyValuePair<StatementInformation, Action<string>>> saved))
        {
            return;
        }

        foreach (KeyValuePair<StatementInformation, Action<string>> kv in saved)
        {
            statements[kv.Key] = kv.Value;
        }

        _ = disabledStatements.Remove(name);
        UpdateStatementHandlers();
        PreScanLines();

        // Tasks need neither the disabling nor this.
        int disabled = configuration.FindLastIndex(step => step.DisabledName == name);
        if (disabled >= 0)
        {
            configuration.RemoveAt(disabled);
        }
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
    /// Loads a YesNt script file and prepares it for stepped execution.
    /// After this call <see cref="IsRunning"/> is <see langword="true"/> and you can drive
    /// execution with <see cref="Step"/>, <see cref="RunFor"/>, or <see cref="RunToCompletion"/>.
    /// </summary>
    /// <param name="path">The path to the <c>.ynt</c> script file.</param>
    /// <param name="isDebugMode">
    /// When <see langword="true"/>, output is routed through <see cref="OnDebugOutput"/> instead of
    /// <see cref="Console"/> and line-execution events are raised via <see cref="OnLineExecuted"/>.
    /// </param>
    /// <exception cref="IOException">The script could not be read (<see cref="IYesNtHost.ReadScript"/>).</exception>
    /// <exception cref="UnauthorizedAccessException">The script may not be read.</exception>
    public void Prepare(string path, bool isDebugMode = false)
    {
        PrepareRun(isDebugMode);
        scriptRunning = LoadFile(path);
    }

    /// <summary>
    /// Loads an in-memory script and prepares it for stepped execution.
    /// After this call <see cref="IsRunning"/> is <see langword="true"/> and you can drive
    /// execution with <see cref="Step"/>, <see cref="RunFor"/>, or <see cref="RunToCompletion"/>.
    /// </summary>
    /// <param name="lines">The script lines to load.</param>
    /// <param name="isDebugMode">
    /// When <see langword="true"/>, output is routed through <see cref="OnDebugOutput"/> and
    /// line-execution events are raised via <see cref="OnLineExecuted"/>.
    /// </param>
    public void Prepare(IEnumerable<string> lines, bool isDebugMode = false)
    {
        PrepareRun(isDebugMode);

        int i = 0;
        foreach (string line in lines)
        {
            string content = line.Trim().Replace("\r", string.Empty);
            runtimeInfo.Lines.Add(new Line(content, "#Memory#", i++));
        }

        PreScanLines();
        scriptRunning = true;
    }

    private void PrepareRun(bool isDebugMode)
    {
        runtimeInfo.Reset();
        runtimeInfo.IsDebugMode = isDebugMode;
        scriptRunning = false;
        tasks.Clear();
    }

    /// <summary>
    /// Executes a YesNt script file to completion.
    /// </summary>
    /// <param name="path">The path to the <c>.ynt</c> script file.</param>
    /// <param name="isDebugMode">
    /// When <see langword="true"/>, output is routed through <see cref="OnDebugOutput"/> instead of
    /// <see cref="Console"/> and line-execution events are raised via <see cref="OnLineExecuted"/>.
    /// </param>
    public void Execute(string path, bool isDebugMode = false)
    {
        Prepare(path, isDebugMode);
        RunToCompletion();
    }

    /// <summary>
    /// Executes a YesNt script supplied as an in-memory list of lines to completion.
    /// </summary>
    /// <param name="lines">The script lines to execute.</param>
    /// <param name="isDebugMode">
    /// When <see langword="true"/>, output is routed through <see cref="OnDebugOutput"/> and
    /// line-execution events are raised via <see cref="OnLineExecuted"/>.
    /// </param>
    public void Execute(IEnumerable<string> lines, bool isDebugMode = false)
    {
        Prepare(lines, isDebugMode);
        RunToCompletion();
    }

    // Starts a task: the script's lines from the task's line on, run by an interpreter with
    // the same statements, host and task mode, sharing the script's global variables.
    private void StartTask(List<Line> lines, int startLine)
    {
        YesNtInterpreter task = new YesNtInterpreter { TaskMode = TaskMode };
        task.runtimeInfo.Host = runtimeInfo.Host;

        foreach (ConfigurationStep step in configuration)
        {
            step.Apply(task);
            task.configuration.Add(step);
        }

        task.PrepareTask(lines, runtimeInfo.GlobalVariables, startLine, runtimeInfo);

        if (TaskMode == TaskMode.Cooperative)
        {
            tasks.Add(task);
        }
        else
        {
            _ = Task.Run(task.RunToCompletion);
        }
    }

    private void PrepareTask(List<Line> lines, Dictionary<string, string> globalVariables, int startLine, RuntimeInformation parentRuntimeInformation)
    {
        PrepareRun(parentRuntimeInformation.IsDebugMode);
        runtimeInfo.Lines = lines;
        runtimeInfo.LineNumber = startLine;
        runtimeInfo.ParentRuntimeInformation = parentRuntimeInformation;
        runtimeInfo.GlobalVariables = globalVariables;

        // The task's lines are a copy of the script's, so the functions the script already
        // passed are at the same lines; without them the task could only call functions below it.
        foreach (KeyValuePair<string, int> function in parentRuntimeInformation.Functions)
        {
            runtimeInfo.Functions[function.Key] = function.Value;
        }

        if (parentRuntimeInformation.StopAllTasks)
        {
            runtimeInfo.Exit(ExitMessages.TerminatedByParentTask, parentRuntimeInformation.StopAllTasks);
            return;
        }

        PreScanLines();
        scriptRunning = true;
    }

    /// <summary>
    /// Executes up to <paramref name="lines"/> script lines then pauses, leaving
    /// <see cref="IsRunning"/> <see langword="true"/> so execution can be resumed later.
    /// Blank lines and comments are skipped transparently and do not consume the budget.
    /// With <see cref="TaskMode.Cooperative"/>, each line of the script also runs a line of
    /// each of its tasks.
    /// </summary>
    /// <param name="lines">Maximum number of executable lines to run. Defaults to 1.</param>
    /// <returns>
    /// <see cref="StepResult.Paused"/> if the budget was exhausted but the script is not finished;
    /// <see cref="StepResult.Waiting"/> if the script waits (step again later, not at once);
    /// <see cref="StepResult.Finished"/> if the script ended within the budget.
    /// </returns>
    public StepResult Step(int lines = 1)
    {
        for (int i = 0; i < lines; i++)
        {
            StepResult result = StepOnce();
            if (result != StepResult.Continue)
            {
                return result;
            }
        }

        return StepResult.Paused;
    }

    /// <summary>
    /// Runs the script for up to <paramref name="budget"/> of wall-clock time, then pauses.
    /// The check happens between lines, so a single slow statement may overshoot slightly.
    /// </summary>
    /// <param name="budget">How long to run before pausing.</param>
    /// <returns>
    /// <see cref="StepResult.Paused"/> if the budget expired but the script is not finished;
    /// <see cref="StepResult.Waiting"/> if the script waits (step again later, not at once);
    /// <see cref="StepResult.Finished"/> if the script ended within the budget.
    /// </returns>
    public StepResult RunFor(TimeSpan budget)
    {
        if (!IsRunning)
        {
            return StepResult.Finished;
        }

        Stopwatch sw = Stopwatch.StartNew();

        while (sw.Elapsed < budget)
        {
            StepResult result = StepOnce();
            if (result != StepResult.Continue)
            {
                return result;
            }
        }

        return StepResult.Paused;
    }

    /// <summary>
    /// Runs the script to completion from the current position, sleeping on the calling thread
    /// while it waits.
    /// If the script has not been started yet (i.e. <see cref="IsRunning"/> is <see langword="false"/>)
    /// this method returns immediately.
    /// </summary>
    public void RunToCompletion()
    {
        while (IsRunning)
        {
            if (StepOnce() == StepResult.Waiting)
            {
                Thread.Sleep(WaitPollMilliseconds);
            }
        }
    }

    // A line of the script, then one of each task taking turns with it.
    private StepResult StepOnce()
    {
        StepResult result = scriptRunning ? StepScript() : StepResult.Finished;

        if (tasks.Count == 0)
        {
            return result;
        }

        bool taskRan = false;
        foreach (YesNtInterpreter task in tasks)
        {
            taskRan |= task.StepOnce() == StepResult.Continue;
        }

        _ = tasks.RemoveAll(task => !task.IsRunning);

        return result switch
        {
            StepResult.Finished when tasks.Count == 0 => StepResult.Finished,
            StepResult.Continue => StepResult.Continue,
            _ => taskRan ? StepResult.Continue : StepResult.Waiting,
        };
    }

    private StepResult StepScript()
    {
        if (!runtimeInfo.TryEndWait())
        {
            return StepResult.Waiting;
        }

        // Skip blank lines and comments without consuming the step budget.
        while (runtimeInfo.LineNumber < runtimeInfo.Lines.Count)
        {
            if (runtimeInfo.Stop)
            {
                return FinishExecution();
            }

            Line lineObj = runtimeInfo.Lines[runtimeInfo.LineNumber];
            runtimeInfo.CurrentLine = runtimeInfo.TakeRetryText() ?? lineObj.Content;

            if (string.IsNullOrWhiteSpace(runtimeInfo.CurrentLine) || runtimeInfo.CurrentLine.StartsWith('#'))
            {
                runtimeInfo.LineNumber++;
                continue;
            }

            // We have a real executable line — run it.
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

            foreach (KeyValuePair<StaticStatementInformation, Action> staticStatement in staticStatements)
            {
                if (!staticStatement.Key.ExecuteInSearchMode && runtimeInfo.IsSearching)
                {
                    continue;
                }

                staticStatement.Value.Invoke();
            }

            bool statementFound = false;
            bool notSearchingLabel = !runtimeInfo.IsSearching;

            List<StatementHandler> handlers = (runtimeInfo.LineNumber < lineMatchingHandlers.Count)
                ? lineMatchingHandlers[runtimeInfo.LineNumber]
                : [];

            foreach (StatementHandler handler in handlers)
            {
                StatementInformation statementAttribute = handler.Attribute;

                if (!statementAttribute.ExecuteInSearchMode && runtimeInfo.IsSearching)
                {
                    statementFound = true;
                    continue;
                }

                if (runtimeInfo.Stop || runtimeInfo.IsRetryingLine)
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

            // The line runs again once what it waits for is there.
            if (runtimeInfo.TakeRetry())
            {
                return runtimeInfo.Stop ? FinishExecution() : StepResult.Waiting;
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

            runtimeInfo.LineNumber++;

            // A statement may have set Stop (e.g. an explicit exit keyword).
            return runtimeInfo.Stop ? FinishExecution() : StepResult.Continue;
        }

        // Fell off the end of the script.
        return FinishExecution();
    }

    private StepResult FinishExecution()
    {
        scriptRunning = false;

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
        }

        if (runtimeInfo.IsDebugMode)
        {
            runtimeInfo.LineExecuted(null);
        }

        return StepResult.Finished;
    }

    private bool LoadFile(string path)
    {
        path = Path.GetFullPath(path);

        string[] lines;
        try
        {
            lines = runtimeInfo.Host.ReadScript(path);
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            return false;
        }

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
        runtimeInfo.FunctionParameters.Clear();
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

                    if (handler.Attribute.Name == "func"
                        && StatementRuntimeInformation.TryParseFunctionSignature(content["func ".Length..], out string functionName, out List<string> functionParameters))
                    {
                        runtimeInfo.FunctionParameters[functionName] = functionParameters;
                    }

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
        StatementInformation attr = handler.Attribute;
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