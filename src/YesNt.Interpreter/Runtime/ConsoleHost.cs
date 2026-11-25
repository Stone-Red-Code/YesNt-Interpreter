using System;
using System.IO;

namespace YesNt.Interpreter.Runtime;

/// <summary>
/// The default <see cref="IYesNtHost"/>: the process's console and file system. Derive from it
/// to change only part, for example to check permissions in <see cref="ReadScript"/>.
/// </summary>
public class ConsoleHost : IYesNtHost
{
    /// <inheritdoc/>
    public virtual void Write(string text)
    {
        Console.Write(text);
    }

    /// <summary>
    /// Waits for a line on the console. The end of the console's input ends the script.
    /// </summary>
    /// <inheritdoc/>
    public virtual InputStatus TryReadLine(out string line)
    {
        line = Console.ReadLine();
        return line is null ? InputStatus.Closed : InputStatus.Ready;
    }

    /// <summary>
    /// Takes a key if one was pressed, without waiting.
    /// </summary>
    /// <inheritdoc/>
    public virtual InputStatus TryReadKey(out char key)
    {
        key = default;

        try
        {
            if (!Console.KeyAvailable)
            {
                return InputStatus.Pending;
            }
        }
        catch (InvalidOperationException)
        {
            // Redirected input has no keys: read a character instead.
            int read = Console.Read();
            if (read < 0)
            {
                return InputStatus.Closed;
            }

            key = (char)read;
            return InputStatus.Ready;
        }

        key = Console.ReadKey().KeyChar;
        return InputStatus.Ready;
    }

    /// <summary>Clears the console, unless it is not interactive.</summary>
    /// <inheritdoc/>
    public virtual void Clear()
    {
        try
        {
            Console.Clear();
        }
        catch (IOException)
        {
            // Silently fail if the console is not interactive
        }
    }

    /// <inheritdoc/>
    public virtual string[] ReadScript(string path)
    {
        return File.ReadAllLines(path);
    }
}
