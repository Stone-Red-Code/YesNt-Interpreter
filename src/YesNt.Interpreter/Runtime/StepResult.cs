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
    /// The script ran to completion (end-of-file, explicit exit, or error).
    /// <see cref="YesNtInterpreter.IsRunning"/> is now <see langword="false"/>.
    /// </summary>
    Finished,
}