# YesNt Library API

The `YesNt.Interpreter` project is a .NET 10 class library. You can reference it from any C# project
to embed the YesNt interpreter and run scripts programmatically.

---

## Table of contents

1. [Adding the reference](#adding-the-reference)
2. [Running a script file](#running-a-script-file)
3. [Running an in-memory script](#running-an-in-memory-script)
4. [Stepwise and time-budgeted execution](#stepwise-and-time-budgeted-execution)
5. [Capturing output (debug mode)](#capturing-output-debug-mode)
6. [Giving the script a console and files](#giving-the-script-a-console-and-files)
7. [Adding custom statements](#adding-custom-statements)
8. [Removing and disabling built-in statements](#removing-and-disabling-built-in-statements)
9. [Stopping a script](#stopping-a-script)
10. [Reading registered statements](#reading-registered-statements)
11. [API reference](#api-reference)

---

## Adding the reference

### Via NuGet (recommended)

Install the [`YesNt.Interpreter`](https://www.nuget.org/packages/YesNt.Interpreter/) package using one of the following methods.

**.NET CLI:**
```
dotnet add package YesNt.Interpreter
```

**Package Manager Console:**
```
Install-Package YesNt.Interpreter
```

**`.csproj`:**
```xml
<ItemGroup>
  <PackageReference Include="YesNt.Interpreter" Version="*" />
</ItemGroup>
```

### Via project reference

If you have cloned the repository and want to reference the source directly, add a project reference in your `.csproj`:

```xml
<ItemGroup>
  <ProjectReference Include="..\YesNt.Interpreter\YesNt.Interpreter.csproj" />
</ItemGroup>
```

Then add the using directive:

```csharp
using YesNt.Interpreter.Runtime;
```

---

## Running a script file

```csharp
var interpreter = new YesNtInterpreter();
interpreter.Execute("path/to/script.ynt");
```

`Execute` is synchronous - it returns when the script finishes (or terminates with an error).

---

## Running an in-memory script

Supply a `List<string>` instead of a file path:

```csharp
var lines = new List<string>
{
    "var x = 42",
    "print_line The answer is ${x}",
};

var interpreter = new YesNtInterpreter();
interpreter.Execute(lines);
```

---

## Stepwise and time-budgeted execution

For interactive environments, game loops, or time-sliced applications, you can prepare a script and run it incrementally.

### Preparing the interpreter
Call `Prepare` instead of `Execute` to load the script without running it immediately:

```csharp
var interpreter = new YesNtInterpreter();
interpreter.Prepare("path/to/script.ynt");
// or: interpreter.Prepare(lines);
```

### Driving execution
Use `IsRunning` to check if there are more lines to execute, and step by line count or run with a time budget:

```csharp
// Execute 5 lines of the script
StepResult result = interpreter.Step(5);

if (result == StepResult.Paused)
{
    // The line budget was exhausted; resume execution later
}
```

Or run the interpreter with a wall-clock time limit (useful for preventing freezing in game loops):

```csharp
// Run for up to 10 milliseconds
StepResult result = interpreter.RunFor(TimeSpan.FromMilliseconds(10));
```

You can also run the remaining script to completion:

```csharp
interpreter.RunToCompletion();
```

---

## Capturing output (debug mode)

Pass `isDebugMode: true` to suppress direct console writes. Output is delivered through the
`OnDebugOutput` event instead, and `OnLineExecuted` fires after executed lines.

```csharp
var interpreter = new YesNtInterpreter();
var output = new System.Text.StringBuilder();

interpreter.OnDebugOutput += text => output.Append(text);
interpreter.OnLineExecuted += args =>
{
    if (args is null)
    {
        // null means execution reached end-of-file (EOF)
        Console.WriteLine("Script reached EOF.");
        return;
    }
    Console.WriteLine($"Line {args.LineNumber}: {args.CurrentLine}");
};

interpreter.Execute(new List<string> { "print_line hello" }, isDebugMode: true);
Console.Write(output);
```

`OnLineExecuted` receives a `DebugEventArgs` with:

| Property       | Type     | Description                                 |
| -------------- | -------- | ------------------------------------------- |
| `LineNumber`   | `int`    | 1-based line number                         |
| `OriginalLine` | `string` | Raw source text                             |
| `CurrentLine`  | `string` | Text after all substitutions                |
| `IsTask`       | `bool`   | `true` if executed inside a background task |
| `TaskId`       | `int`    | Task identifier (0 for the main thread)     |

`OnLineExecuted` is invoked with `null` only when execution reaches end-of-file (EOF).
If execution stops via `exit`, `throw`, `error`, or `Stop()`, no terminal `null` event is emitted.

To show output somewhere else without debug mode, give the interpreter a host instead (next section).

---

## Giving the script a console and files

Outside debug mode a script's console and files are its **host**, an `IYesNtHost`. The host
decides:

- where `print` and `print_line` write;
- where `%read_line` and `%read_key` read from;
- what `clear` clears;
- how script files are read: the script given to `Execute`/`Prepare`, and every file it `import`s.

The default, `ConsoleHost`, uses the process's console and file system. Set `Host` before the
script starts.

Derive from `ConsoleHost` to change part of it, for example to check that a script may read a
file before it does:

```csharp
sealed class SandboxedHost(string root) : ConsoleHost
{
    public override string[] ReadScript(string path)
    {
        if (!Path.GetFullPath(path).StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException(path);   // import ends with "Could not load file"

        return base.ReadScript(path);
    }
}

var interpreter = new YesNtInterpreter { Host = new SandboxedHost(@"C:\scripts\") };
```

Or implement `IYesNtHost` to put the script in a window. Input need not be there yet: return
`InputStatus.Pending` and the interpreter asks again shortly. `InputStatus.Closed` ends the
script.

```csharp
sealed class WindowHost(TextBox output, ConcurrentQueue<string> typedLines) : IYesNtHost
{
    public void Write(string text) => output.AppendText(text);

    public InputStatus TryReadLine(out string line)
    {
        return typedLines.TryDequeue(out line) ? InputStatus.Ready : InputStatus.Pending;
    }

    public InputStatus TryReadKey(out char key)
    {
        key = default;
        if (!typedLines.TryDequeue(out string line))
            return InputStatus.Pending;

        key = line.Length > 0 ? line[0] : ' ';
        return InputStatus.Ready;
    }

    public void Clear() => output.Clear();

    public string[] ReadScript(string path) => File.ReadAllLines(path);
}
```

`ReadScript` throws:

- `FileNotFoundException` for a file that is not there. A missing script does not start; a
  missing import ends the script with "Could not find file".
- any other `IOException`, or `UnauthorizedAccessException`, for a file that cannot be read.
  The script ends with "Could not load file".

---

## Adding custom statements

Use `AddStatement` to register keywords before calling `Execute`.

### Simple keyword at the start of a line

```csharp
using YesNt.Interpreter.Enums;

var interpreter = new YesNtInterpreter();

interpreter.AddStatement("log", SearchMode.StartOfLine, SpaceAround.End, args =>
{
    Console.WriteLine($"[LOG] {args}");
});

interpreter.Execute(new List<string> { "log Hello from custom statement" });
```

### Accessing script state from a handler

Pass an `Action<string, IStatementContext>` instead of `Action<string>` to receive the current
script state. `IStatementContext` exposes the variable tables, the current line, the line number,
the output stack, and the ability to terminate execution.

```csharp
using YesNt.Interpreter.Runtime;

interpreter.AddStatement("set_var", SearchMode.StartOfLine, SpaceAround.End,
    (args, ctx) =>
    {
        // args is e.g. "result 42" — parse however your syntax demands
        string[] parts = args.Split(' ', 2);
        if (parts.Length == 2)
            ctx.Variables[parts[0]] = parts[1];
        else
            ctx.Exit("set_var requires: <name> <value>", stopAllTasks: true);
    });
```

`IStatementContext` provides:

| Property                      | Type                        | Description                                                                |
| ----------------------------- | --------------------------- | -------------------------------------------------------------------------- |
| `Variables`                   | `Dictionary<string,string>` | Local variable table for the current scope                                 |
| `GlobalVariables`             | `Dictionary<string,string>` | Global variable table shared across all scopes                             |
| `CurrentLine`                 | `string`                    | The line text being processed; write here for inline-substitution handlers |
| `LineNumber`                  | `int`                       | Zero-based index of the _next_ line to execute; set to implement jumps     |
| `SetOut(params values)`       | `void`                      | Replace the output stack with `values`, the last on top, for `%out`        |
| `Exit(message, stopAllTasks)` | `void`                      | Terminate execution; `stopAllTasks: true` also ends all tasks              |

### Returning a value

A statement that produces a value gives it to the script the way the built-in `length` and
`list get` do: through the output stack, which the script reads with `%out`. `SetOut` empties
the stack, then pushes the values in order, so the last is read first. Values are plain text:
spaces and keywords in them stay part of the value.

```csharp
interpreter.AddStatement("user_name", SearchMode.Exact, SpaceAround.None,
    (_, ctx) => ctx.SetOut(Environment.UserName));
```

```ynt
user_name
var name = %out
print_line Hello ${name}
```

### With a syntax-highlight colour

```csharp
interpreter.AddStatement("log", SearchMode.StartOfLine, SpaceAround.End,
    ConsoleColor.Cyan,
    args => Console.WriteLine($"[LOG] {args}"));
```

### Using a `StatementInformation`

```csharp
using YesNt.Interpreter.Runtime;

var attr = new StatementInformation("log", SearchMode.StartOfLine, SpaceAround.End)
{
    Priority = Priority.VeryLow,
};

interpreter.AddStatement(attr, args => Console.WriteLine($"[LOG] {args}"));
```

### `SearchMode` values

| Value         | The keyword matches when…                    |
| ------------- | -------------------------------------------- |
| `StartOfLine` | the line **starts with** the keyword         |
| `EndOfLine`   | the line **ends with** the keyword           |
| `Contains`    | the keyword appears **anywhere** in the line |
| `Exact`       | the line is **exactly** the keyword          |

### `SpaceAround` values

| Value      | Space requirement                |
| ---------- | -------------------------------- |
| `None`     | No surrounding spaces required   |
| `Start`    | A space must precede the keyword |
| `End`      | A space must follow the keyword  |
| `StartEnd` | Spaces required on both sides    |

Custom statements run at `Priority.Normal` by default. Statements with a higher-ranking enum member (`PreProcessing` → `Highest` → … → `VeryLow`) run first; `VeryLow` runs last.
Use `StatementInformation.Priority` to control ordering relative to built-in statements.

---

## Removing and disabling built-in statements

Use these methods to restrict which built-in keywords are available, useful for sandboxing
or replacing a built-in with a custom implementation.

### `RemoveStatement` - permanent removal

Removes all handlers for the given keyword. Any script line that would have matched the
keyword now triggers an **"Invalid statement"** error.

```csharp
var interpreter = new YesNtInterpreter();

// Prevent scripts from launching external processes.
interpreter.RemoveStatement("exec");

interpreter.Execute(new List<string> { "exec notepad" });
// Terminates with: Invalid statement
```

### `DisableStatement` - silent no-op

Disables all handlers for the keyword. The keyword still **matches** (so no error is raised),
but has no effect. Use `EnableStatement` to restore the original behaviour.

```csharp
var interpreter = new YesNtInterpreter();

// Make sleep a no-op so tests don't actually wait.
interpreter.DisableStatement("sleep");

interpreter.Execute(new List<string>
{
    "sleep 10000",         // does nothing
    "var x = done",
    "print_line ${x}",     // prints: done
});
```

### `EnableStatement` - restore a disabled statement

Restores the original handlers saved when `DisableStatement` was called.
Has no effect if the statement is not currently disabled.

```csharp
interpreter.DisableStatement("sleep");
// ... configure other things ...
interpreter.EnableStatement("sleep");   // sleep works normally again
```

### Replacing a built-in statement

Call `RemoveStatement` to remove the built-in handlers, then `AddStatement` to install your own.
Simply calling `AddStatement` with the same keyword name will **not** replace the built-in statement,
it will add a second handler that fires alongside the original.

```csharp
// Replace the built-in 'exec' with a sandboxed version that only allows 'echo'.
interpreter.RemoveStatement("exec");
interpreter.AddStatement("exec", SearchMode.StartOfLine, SpaceAround.End, args =>
{
    if (args.Trim() != "echo")
        throw new InvalidOperationException("exec is restricted");

    System.Diagnostics.Process.Start("cmd", "/c echo (sandboxed)");
});
```

---

## Stopping a script

Call `Stop()` from any thread to request graceful termination. The script stops at the next line
boundary (or immediately if it is currently blocked waiting for console input).

```csharp
var interpreter = new YesNtInterpreter();

// Start the script on a background thread so we can stop it from this thread.
var task = Task.Run(() =>
    interpreter.Execute(new List<string> { "while True:", "sleep 100", "end_while" }));

await Task.Delay(500);
interpreter.Stop();   // signals the script to terminate at the next line boundary
await task;
```

---

## Reading registered statements

`StatementInformation` returns a read-only snapshot of every registered statement.
This is useful for building syntax highlighters or tooling.

```csharp
var interpreter = new YesNtInterpreter();

foreach (var info in interpreter.StatementInformation)
{
    Console.WriteLine($"{info.Name,-20} {info.SearchMode,-12} color={info.Color}");
}
```

Each `StatementInformation` object exposes:

| Property                   | Type           | Description                  |
| -------------------------- | -------------- | ---------------------------- |
| `Name`                     | `string`       | The keyword                  |
| `SearchMode`               | `SearchMode`   | Where the keyword is matched |
| `SpaceAround`              | `SpaceAround`  | Required surrounding spaces  |
| `Color`                    | `ConsoleColor` | Syntax-highlight colour      |
| `IgnoreSyntaxHighlighting` | `bool`         | Whether to skip highlighting |
| `Separator`                | `string?`      | Optional required sub-string |

---

## API reference

### `YesNtInterpreter`

```csharp
public class YesNtInterpreter
```

#### Constructor

```csharp
public YesNtInterpreter()
```

Creates a new interpreter instance and registers all built-in statements.

#### Events

```csharp
public event Action<string>         OnDebugOutput;
public event Action<DebugEventArgs> OnLineExecuted;
```

`OnDebugOutput` and `OnLineExecuted` are only raised in debug mode (`isDebugMode: true`).
`OnLineExecuted` receives `null` only on EOF completion.

#### Methods

```csharp
// Execute a .ynt file to completion
public void Execute(string path, bool isDebugMode = false);

// Execute in-memory lines to completion
public void Execute(IEnumerable<string> lines, bool isDebugMode = false);

// Prepare a script file for stepwise execution
public void Prepare(string path, bool isDebugMode = false);

// Prepare in-memory lines for stepwise execution
public void Prepare(IEnumerable<string> lines, bool isDebugMode = false);

// Execute up to standard line count then pause
public StepResult Step(int lines = 1);

// Run the script for up to budget duration then pause
public StepResult RunFor(TimeSpan budget);

// Execute the remaining script lines to completion
public void RunToCompletion();

// Register a custom statement (full control)
public void AddStatement(StatementInformation attribute, Action<string> handler);
public void AddStatement(StatementInformation attribute, Action<string, IStatementContext> handler);

// Register a custom statement (convenience overloads)
public void AddStatement(string name, SearchMode searchMode, SpaceAround spaceAround, Action<string> handler);
public void AddStatement(string name, SearchMode searchMode, SpaceAround spaceAround, Action<string, IStatementContext> handler);
public void AddStatement(string name, SearchMode searchMode, SpaceAround spaceAround, ConsoleColor color, Action<string> handler);
public void AddStatement(string name, SearchMode searchMode, SpaceAround spaceAround, ConsoleColor color, Action<string, IStatementContext> handler);

// Remove a built-in or custom statement permanently
public void RemoveStatement(string name);

// Disable a statement (silent no-op; reversible)
public void DisableStatement(string name);

// Re-enable a previously disabled statement
public void EnableStatement(string name);

// Request graceful stop
public void Stop();
```

#### Properties

```csharp
// Read-only snapshot of all registered statements
public ReadOnlyCollection<StatementInformation> StatementInformation { get; }

// Whether a prepared script is currently active/running
public bool IsRunning { get; }

// The script's console and files (default: ConsoleHost); set before the script starts
public IYesNtHost Host { get; set; }
```

---

### `IStatementContext`

```csharp
public interface IStatementContext  // YesNt.Interpreter.Runtime
```

Passed to `Action<string, IStatementContext>` handlers registered via `AddStatement`.
Provides access to the script state that a built-in statement handler would have.

| Member                        | Type                        | Description                                                                  |
| ----------------------------- | --------------------------- | ---------------------------------------------------------------------------- |
| `Variables`                   | `Dictionary<string,string>` | Local variable table for the current scope                                   |
| `GlobalVariables`             | `Dictionary<string,string>` | Global variable table shared across all scopes                               |
| `CurrentLine`                 | `string`                    | The line being processed; write here for inline-substitution handlers        |
| `LineNumber`                  | `int`                       | Zero-based index of the next line to execute; set this to implement jumps    |
| `SetOut(params values)`       | `void`                      | Replace the output stack with `values`, the last on top, for `%out`          |
| `Exit(message, stopAllTasks)` | `void`                      | Terminate execution with a message; `stopAllTasks: true` also ends all tasks |

---

### `StepResult`

```csharp
public enum StepResult  // YesNt.Interpreter.Runtime
```

Returned by `Step` and `RunFor` to indicate the outcome of the incremental execution.

| Value      | Description                                                    |
| ---------- | -------------------------------------------------------------- |
| `Continue` | A line was executed and more lines remain.                     |
| `Paused`   | The step or time budget was exhausted before the script ended. |
| `Finished` | The script ran to completion (or terminated/exited).           |

---

### `IYesNtHost` and `ConsoleHost`

```csharp
public interface IYesNtHost  // YesNt.Interpreter.Runtime
{
    void Write(string text);                    // output outside debug mode, line ends included
    InputStatus TryReadLine(out string line);   // %read_line
    InputStatus TryReadKey(out char key);       // %read_key
    void Clear();                               // clear
    string[] ReadScript(string path);           // the script and its imports
}

public class ConsoleHost : IYesNtHost          // the default; every member is virtual
```

`ConsoleHost.TryReadLine` waits for a console line. `TryReadKey` returns `Pending` until a key
is pressed.

### `InputStatus`

| Value     | Description                                       |
| --------- | ------------------------------------------------- |
| `Ready`   | The input is there.                               |
| `Pending` | Not yet: the interpreter asks again.              |
| `Closed`  | There will be no more input: the script ends.     |
