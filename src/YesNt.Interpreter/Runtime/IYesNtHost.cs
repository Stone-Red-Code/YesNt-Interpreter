namespace YesNt.Interpreter.Runtime;

/// <summary>
/// What a script's console and files are: where its output goes, where <c>%read_line</c> and
/// <c>%read_key</c> read from, what <c>clear</c> clears, and how script files are read (the
/// script itself and each <c>import</c>). The default, <see cref="ConsoleHost"/>, uses the
/// process's console and file system; an application embedding YesNt gives its own, for
/// example to show output in a window or to check a script may read a file before it does.
/// </summary>
public interface IYesNtHost
{
    /// <summary>
    /// Writes output, as plain text. A line's end is part of it. Not called in debug mode,
    /// where output goes to <see cref="YesNtInterpreter.OnDebugOutput"/> instead.
    /// </summary>
    /// <param name="text">The text to write.</param>
    void Write(string text);

    /// <summary>
    /// Takes a line the user entered, for <c>%read_line</c>. Return
    /// <see cref="InputStatus.Pending"/> while there is none yet: the script waits without
    /// blocking and asks again when it is next stepped. A host whose input is a blocking console
    /// may simply wait for the line instead.
    /// </summary>
    /// <param name="line">The line, without its end, when the result is <see cref="InputStatus.Ready"/>.</param>
    /// <returns>Whether there is a line, none yet, or no more input (which ends the script).</returns>
    InputStatus TryReadLine(out string line);

    /// <summary>
    /// Takes a key the user pressed, for <c>%read_key</c>, the same way as <see cref="TryReadLine"/>.
    /// </summary>
    /// <param name="key">The key's character when the result is <see cref="InputStatus.Ready"/>.</param>
    /// <returns>Whether there is a key, none yet, or no more input (which ends the script).</returns>
    InputStatus TryReadKey(out char key);

    /// <summary>Clears the output, for <c>clear</c>.</summary>
    void Clear();

    /// <summary>
    /// Reads a script file's lines: the script given to the interpreter and each file it
    /// imports. Throw <see cref="System.IO.FileNotFoundException"/> when there is no such file;
    /// any other <see cref="System.IO.IOException"/> or
    /// <see cref="System.UnauthorizedAccessException"/> means it could not be read.
    /// </summary>
    /// <param name="path">The file's full path.</param>
    /// <returns>The file's lines.</returns>
    string[] ReadScript(string path);
}

/// <summary>
/// Whether a host has the input a script asked for.
/// </summary>
public enum InputStatus
{
    /// <summary>The input is there.</summary>
    Ready,

    /// <summary>Not yet: the script waits and asks again.</summary>
    Pending,

    /// <summary>There will be no more input: the script ends.</summary>
    Closed,
}
