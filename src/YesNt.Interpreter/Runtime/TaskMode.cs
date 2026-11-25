namespace YesNt.Interpreter.Runtime;

/// <summary>
/// How a script's <c>task</c> lines run.
/// </summary>
public enum TaskMode
{
    /// <summary>
    /// Each task runs on a thread of its own, at the same time as the script. A task goes on
    /// after the script ends.
    /// </summary>
    Thread,

    /// <summary>
    /// Tasks run on the thread that steps the script, taking turns with it: each step of the
    /// interpreter runs a line of the script and a line of each task. For hosts that must keep
    /// everything on one thread, such as a game loop or a single-threaded system. The
    /// interpreter keeps running (<see cref="YesNtInterpreter.IsRunning"/>) until the script
    /// and all its tasks have ended.
    /// </summary>
    Cooperative,
}
