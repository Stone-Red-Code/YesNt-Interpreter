# YesNt Library API

The `YesNt.Interpreter` project is a .NET 8 class library. You can reference it from any C# project
to embed the YesNt interpreter and run scripts programmatically.

---

## Table of contents

1. [Adding the reference](#adding-the-reference)
2. [Running a script file](#running-a-script-file)
3. [Running an in-memory script](#running-an-in-memory-script)
4. [Capturing output (debug mode)](#capturing-output-debug-mode)
5. [Adding custom statements](#adding-custom-statements)
6. [Removing and disabling built-in statements](#removing-and-disabling-built-in-statements)
7. [Stopping a script](#stopping-a-script)
8. [Reading registered statements](#reading-registered-statements)
9. [API reference](#api-reference)

---

## Adding the reference

Add a project reference to `YesNt.Interpreter` in your `.csproj`:

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
and the ability to terminate execution.

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
            ctx.Exit("set_var requires: <name> <value>", isError: true);
    });
```

`IStatementContext` provides:

| Property                 | Type                        | Description                                                                |
| ------------------------ | --------------------------- | -------------------------------------------------------------------------- |
| `Variables`              | `Dictionary<string,string>` | Local variable table for the current scope                                 |
| `GlobalVariables`        | `Dictionary<string,string>` | Global variable table shared across all scopes                             |
| `CurrentLine`            | `string`                    | The line text being processed; write here for inline-substitution handlers |
| `LineNumber`             | `int`                       | Zero-based index of the _next_ line to execute; set to implement jumps     |
| `Exit(message, isError)` | `void`                      | Terminate execution; `isError: true` signals an error condition            |

### With a syntax-highlight colour

```csharp
interpreter.AddStatement("log", SearchMode.StartOfLine, SpaceAround.End,
    ConsoleColor.Cyan,
    args => Console.WriteLine($"[LOG] {args}"));
```

### Using a pre-built `StatementAttribute`

```csharp
using YesNt.Interpreter.Attributes;

var attr = new StatementAttribute("log", SearchMode.StartOfLine, SpaceAround.End)
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
Use `StatementAttribute.Priority` to control ordering relative to built-in statements.

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
var thread = new System.Threading.Thread(() =>
    interpreter.Execute(new List<string> { "while True:", "sleep 100", "end_while" }));

thread.Start();
System.Threading.Thread.Sleep(500);
interpreter.Stop();   // signals the script to terminate at the next line boundary
thread.Join();
```

### Stopping a script that blocks on `%read_key`

When a script blocks waiting for keyboard input, use the `OnWaitingForInput` event instead of a
fixed `Thread.Sleep`. The event fires at the exact moment the interpreter enters the blocking poll
loop, so calling `Stop()` immediately after is always safe regardless of system load.

```csharp
var interpreter = new YesNtInterpreter();
var waitingForInput = new System.Threading.AutoResetEvent(false);

interpreter.OnWaitingForInput += () => waitingForInput.Set();

var thread = new System.Threading.Thread(() =>
    interpreter.Execute(new List<string> { "var key = %read_key" }));

thread.Start();
waitingForInput.WaitOne(TimeSpan.FromSeconds(5)); // wait until blocked on input
interpreter.Stop();
thread.Join();
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
public event Action                 OnWaitingForInput;
```

`OnDebugOutput` and `OnLineExecuted` are only raised in debug mode (`isDebugMode: true`).
`OnLineExecuted` receives `null` only on EOF completion.
`OnWaitingForInput` is raised (in any mode) immediately before the interpreter blocks on
`%read_key`. Use it to call `Stop()` deterministically without relying on `Thread.Sleep`.

#### Methods

```csharp
// Execute a .ynt file
public void Execute(string path, bool isDebugMode = false);

// Execute in-memory lines
public void Execute(List<string> lines, bool isDebugMode = false);

// Register a custom statement (full control)
public void AddStatement(StatementAttribute attribute, Action<string> handler);
public void AddStatement(StatementAttribute attribute, Action<string, IStatementContext> handler);

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
```

---

### `IStatementContext`

```csharp
public interface IStatementContext  // YesNt.Interpreter.Runtime
```

Passed to `Action<string, IStatementContext>` handlers registered via `AddStatement`.
Provides access to the script state that a built-in statement handler would have.

| Member                   | Type                        | Description                                                               |
| ------------------------ | --------------------------- | ------------------------------------------------------------------------- |
| `Variables`              | `Dictionary<string,string>` | Local variable table for the current scope                                |
| `GlobalVariables`        | `Dictionary<string,string>` | Global variable table shared across all scopes                            |
| `CurrentLine`            | `string`                    | The line being processed; write here for inline-substitution handlers     |
| `LineNumber`             | `int`                       | Zero-based index of the next line to execute; set this to implement jumps |
| `Exit(message, isError)` | `void`                      | Terminate execution with a message; `isError: true` signals an error      |
