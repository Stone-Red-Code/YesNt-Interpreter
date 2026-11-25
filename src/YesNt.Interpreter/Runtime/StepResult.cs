namespace YesNt.Interpreter.Runtime;

/// <summary>
/// The outcome of a single <see cref="YesNtInterpreter.StepOnce"/> call,
/// or the aggregate result of <see cref="YesNtInterpreter.Step"/> /
/// <see cref="YesNtInterpreter.RunFor"/>.
/// </summary>
public enum StepResult
{
    /// <summary>
    /// A line was executed and more lines remain. Keep stepping to continue.
    /// </summary>
    Continue,

    /// <summary>
    /// The step or time budget was exhausted before the script finished.
    /// <see cref="YesNtInterpreter.IsRunning"/> is still <see langword="true"/>;
    /// call any of the run methods again to resume.
    /// </summary>
    Paused,

    /// <summary>
    /// The script waits for something (a <c>sleep</c>, input that is not there yet, or a custom
    /// statement's <see cref="IStatementContext.WaitUntil"/>) and running it now would do nothing.
    /// <see cref="YesNtInterpreter.IsRunning"/> is still <see langword="true"/>; step again later,
    /// for example at the next frame, instead of at once.
    /// </summary>
    Waiting,

    /// <summary>
    /// The script ran to completion (end-of-file, explicit exit, or error).
    /// <see cref="YesNtInterpreter.IsRunning"/> is now <see langword="false"/>.
    /// </summary>
    Finished,
}