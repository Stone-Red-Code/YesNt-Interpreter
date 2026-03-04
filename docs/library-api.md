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
6. [Stopping a script](#stopping-a-script)
7. [Reading registered statements](#reading-registered-statements)
8. [API reference](#api-reference)

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

Custom statements run at `Priority.Normal` by default. Lower priority values run first.
Use `StatementAttribute.Priority` to control ordering relative to built-in statements.

---

## Stopping a script

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
public event Action<string>        OnDebugOutput;
public event Action<DebugEventArgs> OnLineExecuted;
```

Only raised in debug mode (`isDebugMode: true`).
`OnLineExecuted` receives `null` only on EOF completion.

#### Methods

```csharp
// Execute a .ynt file
public void Execute(string path, bool isDebugMode = false);

// Execute in-memory lines
public void Execute(List<string> lines, bool isDebugMode = false);

// Register a custom statement (full control)
public void AddStatement(StatementAttribute attribute, Action<string> handler);

// Register a custom statement (convenience overloads)
public void AddStatement(string name, SearchMode searchMode, SpaceAround spaceAround, Action<string> handler);
public void AddStatement(string name, SearchMode searchMode, SpaceAround spaceAround, ConsoleColor color, Action<string> handler);

// Request graceful stop
public void Stop();
```

#### Properties

```csharp
// Read-only snapshot of all registered statements
public ReadOnlyCollection<StatementInformation> StatementInformation { get; }
```
