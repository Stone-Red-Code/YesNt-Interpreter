# YesNt Language Reference

YesNt is a line-based scripting language. Every non-empty, non-comment line is one statement.
Execution proceeds top-to-bottom unless a control-flow statement changes the line counter.

---

## Table of contents

1. [Basic rules](#basic-rules)
2. [Comments](#comments)
3. [String literals](#string-literals)
4. [Variables](#variables)
5. [Console I/O](#console-io)
6. [Arithmetic](#arithmetic)
7. [Conditions](#conditions)
8. [Control flow](#control-flow)
9. [Functions](#functions)
10. [Lists](#lists)
11. [Maps](#maps)
12. [Processing](#processing)
13. [System](#system)
14. [Predefined tokens](#predefined-tokens)
15. [Termination](#termination)

---

## Basic rules

- Scripts are plain text files with the `.ynt` extension.
- Each non-empty line is one statement. There are no multi-line expressions.
- Leading and trailing whitespace on each line is ignored.
- Lines starting with `#` are comments. Inline comments are not supported. Everything after the keyword is treated as its argument.
- `${variable}` anywhere in a line is replaced with the variable's value before the statement runs.
- Text wrapped in double quotes (`"..."`) is a string literal. Its contents are not matched as keywords and support escape sequences like `\n` and `\t`.

## Comments

```ynt
# This is a comment.
print_line Hello  # inline comments are NOT supported. Everything after print_line is the argument
```

Only whole-line comments (lines whose first non-whitespace character is `#`) are supported.

---

## String literals

Double-quoted strings protect their content from keyword matching and allow escape sequences.

```ynt
print_line "Hello, world!"
print_line "Line one\nLine two"
print_line "She said \"hi\""
```

| Escape | Meaning              |
| ------ | -------------------- |
| `\n`   | Newline              |
| `\r`   | Carriage return      |
| `\t`   | Horizontal tab       |
| `\b`   | Backspace            |
| `\f`   | Form feed            |
| `\a`   | Alert (bell)         |
| `\v`   | Vertical tab         |
| `\"`   | Literal double-quote |
| `\\`   | Literal backslash    |

Variable interpolation (`${name}`) is **not** evaluated inside string literals - the braces
and content are passed through verbatim.

```ynt
var x = world
print_line "${x}"        # prints the literal text: ${x}
print_line "hello " ${x} # prints: hello world   (interpolation outside the literal)
```

---

## Variables

### Local variables - `var`

```
var <name> = <value>
```

Defines or updates a variable scoped to the current function (or the top level if called outside a function).
The value is everything after `=`, trimmed.

```ynt
var count = 0
var greeting = Hello, world!
```

Variable names may only contain letters and digits (`[a-zA-Z0-9]`).

### Global variables - `global`

```
global <name> = <value>
```

Defines or updates a variable that is visible across all function scopes and background tasks.

```ynt
global total = 100
```

### Reading a variable - `${name}`

`${name}` is an inline token that is replaced with the variable's value before the statement executes.
It can appear anywhere in a line and multiple occurrences are replaced left to right.
Local variables are checked first; if not found, the global table is checked.

```ynt
var a = 5
var b = 10
print_line ${a} plus ${b}
```

### Deleting a variable - `delete`

```
delete <name>
```

Removes the variable. Local scope is checked first; if not found, the global table is used.
Raises an error if the variable does not exist in either scope.

```ynt
var temp = scratch
delete temp
```

---

## Console I/O

### Print with newline - `print_line`

```
print_line <text>
print_line
```

Writes `<text>` followed by a newline. With no argument, writes a blank line.

```ynt
print_line Hello!
print_line
print_line Done.
```

### Print without newline - `print`

```
print <text>
```

Writes `<text>` without a trailing newline.

```ynt
print Enter your name:
var name = %read_line
print_line Hello, ${name}!
```

### Read a line of input - `%read_line`

`%read_line` is an inline token that is replaced with one line of text read from standard input.

```ynt
var answer = %read_line
print_line You typed: ${answer}
```

### Read a single key - `%read_key`

`%read_key` is an inline token that is replaced with the single character pressed by the user (no Enter required).

```ynt
print Press any key...
var key = %read_key
print_line You pressed: ${key}
```

### Clear the console - `clear`

```
clear
```

Clears the console window.

---

## Arithmetic

Arithmetic is a **postfix** modifier applied at the end of a line with the `calc` keyword.

```
<expression> calc
```

Any numeric sub-expression matching the pattern `number op number [op number …]` is evaluated
and replaced with the result. Supported operators (highest to lowest precedence):

| Operator | Operation                     |
| -------- | ----------------------------- |
| `(…)`    | Parentheses (evaluated first) |
| `^`      | Exponentiation                |
| `%`      | Modulo                        |
| `/`      | Division                      |
| `*`      | Multiplication                |
| `-`      | Subtraction                   |
| `+`      | Addition (lowest precedence)  |

Adjacent sign characters (`++`, `--`, `-+`, `+-`) are normalised before evaluation.

```ynt
var x = 3
var y = 4
var sum = ${x} + ${y} calc             # 7
var expr = 2 + 3 * 4 calc              # 14  (* before +)
var parens = (2 + 3) * 4 calc          # 20
var power = 2 ^ 10 calc                # 1024
var remainder = 17 % 5 calc            # 2
```

---

## Conditions

Conditions are used in `if` and `while` statements. A condition is a string of the form:

```
<left> <operator> <right>
```

| Operator | Meaning                                       |
| -------- | --------------------------------------------- |
| `==`     | Equal (string comparison, case-sensitive)     |
| `!=`     | Not equal (string comparison, case-sensitive) |
| `<`      | Less than (numeric)                           |
| `>`      | Greater than (numeric)                        |
| `<=`     | Less than or equal (numeric)                  |
| `>=`     | Greater than or equal (numeric)               |

Numeric comparisons (`<`, `>`, `<=`, `>=`) parse both sides with
culture-invariant decimal rules (`.` or `,` as decimal separator).

A bare value of `True` or `False` (case-insensitive) is also a valid condition.

```ynt
var x = 10
if ${x} > 5:
    print_line x is greater than 5
end_if
```

---

## Control flow

### If / else / end_if

```
if <condition>:
    <body>
else:
    <alternative>
end_if
```

`else:` is optional. `if` / `else:` / `end_if` blocks can be nested.

```ynt
var score = 75
if ${score} >= 60:
    print_line Pass
else:
    print_line Fail
end_if
```

### While loop

```
while <condition>:
    <body>
end_while
```

The condition is checked before each iteration. `while` / `end_while` blocks can be nested.

```ynt
var i = 1
while ${i} <= 5:
    print_line ${i}
    var i = ${i} + 1 calc
end_while
```

### Labels and goto

```
label <name>:
goto <name>
```

`label` marks a target. `goto` performs an unconditional jump to that label.
At the top level, labels are file-scoped, you can jump to any label in the file. Inside a function, labels are restricted to the current function; you cannot jump to a label outside the calling function.

```ynt
label loop:
    print_line tick
    goto loop
```

### Conditional goto

```
if <condition> goto <label>
```

Jumps to `<label>` only when the condition is true.

```ynt
var n = 0
label start:
    var n = ${n} + 1 calc
    if ${n} < 10 goto start
print_line done
```

### Conditional function call

```
if <condition> call <function>
```

Calls `<function>` only when the condition is true.

```ynt
var debug = True
if ${debug} == True call dump_state
```

---

## Functions

### Declaring a function - `func`

```
func <name>:
    <body>
end_func
```

A function declaration registers the function name and the line it starts on.
The body runs until `end_func` (or an early `return`) is reached.
**Nested function declarations are not allowed.**

```ynt
func add:
    var result = %in + %in calc
    return ${result}
end_func
```

#### Named parameters

```
func <name>: <param1>, <param2>, …
    <body>
end_func
```

Parameters can be declared after the function name. They are bound to local variables in
declaration order, consuming the values passed to `call <name> with …` (or pushed with `push_in`).
`func add: a, b` is equivalent to starting the body with `var a = %in` followed by `var b = %in`.

```ynt
func add: a, b
    return ${a} + ${b} calc
end_func

call add with 3, 7
var total = %out
print_line ${total}
```

If fewer arguments are passed than declared parameters, the script terminates with an error.

### Calling a function - `call`

```
call <name>
call <name> with <arg1>, <arg2>, …
```

`call` without `with` uses any values previously pushed with `push_in`.
`call … with …` pushes the comma-separated arguments and then calls the function.

```ynt
call greet
call add with 3, 7
```

### Input arguments

#### Push an input argument - `push_in`

```
push_in <value>
```

Pushes a value onto the input argument stack. Values are consumed in the order they were pushed -
the first `push_in` call is the first value consumed by `%in` inside the function.

```ynt
push_in Alice
call greet
```

#### Pop the next input argument - `%in`

`%in` is an inline token (only valid inside a function) that is replaced with the next value
popped from the argument stack.

```ynt
func greet:
    var name = %in
    print_line Hello, ${name}!
end_func
```

#### Check if input argument exists - `%has_in`

`%has_in` is replaced with `True` or `False` depending on whether the input stack is non-empty.
Only valid inside a function.

```ynt
func greet:
    if %has_in call do_greet
end_func
```

### Output values

#### Push an output value - `push_out`

```
push_out <value>
```

Only valid inside a function. Pushes a return value onto the output stack.

#### Consume an output value - `%out`

`%out` is an inline token that pops and inserts the top value from the output stack.
Valid anywhere after a function call or list/processing statement that pushes to the output stack.

```ynt
call add with 3, 7
var total = %out
print_line ${total}
```

#### Check if output value exists - `%has_out`

`%has_out` is replaced with `True` or `False` depending on whether the output stack is non-empty.

```ynt
call maybe_produce
if %has_out call consume_result
```

### Clear the call stack - `clear_call_stack`

```
clear_call_stack
```

Discards all frames on the function call stack. Useful for error recovery.

### Return from a function - `end_func` and `return`

```
end_func
return
return <value>
```

`end_func` and a bare `return` end the current function call and hand control back to the caller.
`return <value>` does the same but first pushes `<value>` onto the output stack
(it is equivalent to `push_out <value>` followed by `end_func`), so the caller can read it with `%out`.
`return` may be used anywhere in the body to exit a function early.

```ynt
func add:
    var a = %in
    var b = %in
    if ${a} == 0:
        return 0
    end_if
    return ${a} + ${b} calc

call add with 0, 5
var total = %out
print_line ${total}
```

Only valid inside a function; otherwise the script terminates with an error.
---

## Lists

Lists are ordered, mutable sequences of strings. All list operations start with the keyword `list`.

### Create or reset - `list … new`

```
list <name> new
```

Creates an empty list. If the list already exists it is cleared.

### Add an item - `list … add`

```
list <name> add <value>
```

Appends `<value>` to the end of the list.

### Get an item - `list … get`

```
list <name> get <index>
```

Pushes the item at zero-based `<index>` onto the output stack. Access it with `%out`.

```ynt
list fruits new
list fruits add apple
list fruits add banana
list fruits get 0
var first = %out
print_line ${first}
```

### Set an item - `list … set`

```
list <name> set <index> <value>
```

Replaces the item at `<index>` with `<value>`.

### Remove an item - `list … remove`

```
list <name> remove <index>
```

Removes the item at `<index>`. Subsequent items shift down.

### Insert an item - `list … insert`

```
list <name> insert <index> <value>
```

Inserts `<value>` before position `<index>`.

### Get the length - `list … length`

```
list <name> length
```

Pushes the number of items onto the output stack.

```ynt
list fruits length
var n = %out
print_line ${n} items
```

### Clear all items - `list … clear`

```
list <name> clear
```

Removes all items but keeps the list alive.

### Delete a list - `list … delete`

```
list <name> delete
```

Removes the list entirely.

---

## Maps

Maps are key–value stores. Each key is unique; putting a value under an existing key overwrites it. All map operations start with the keyword `map`.

### Create or reset - `map … new`

```
map <name> new
```

Creates an empty map. If the map already exists it is cleared.

### Add or update an entry - `map … put`

```
map <name> put <key>, <value>
```

Sets `<key>` to `<value>`. If the key already exists its value is replaced. The statement takes exactly two comma-separated parts. Keys may contain spaces unquoted. A key or value that itself contains a comma must be wrapped in quotes.

```ynt
map settings new
map settings put name, Alice
map settings put city, Berlin
map settings get name
var who = %out
print_line ${who}
```

### Get a value - `map … get`

```
map <name> get <key>
```

Pushes the value stored under `<key>` onto the output stack. Access it with `%out`. Terminates with an error if the key does not exist.

### Check a key - `map … has`

```
map <name> has <key>
```

Pushes `True` if `<key>` exists, `False` otherwise, onto the output stack.

```ynt
map settings new
map settings put name Alice
map settings has name
var present = %out
print_line ${present}
```

### Remove an entry - `map … remove`

```
map <name> remove <key>
```

Removes `<key>` and its value. Removing a key that does not exist is silently ignored.

### Get the size - `map … size`

```
map <name> size
```

Pushes the number of entries onto the output stack.

```ynt
map settings size
var n = %out
print_line ${n} entries
```

### Clear all entries - `map … clear`

```
map <name> clear
```

Removes all entries but keeps the map alive.

### Delete a map - `map … delete`

```
map <name> delete
```

Removes the map entirely.

---

## Processing

### Arithmetic - `calc`

See [Arithmetic](#arithmetic).

### Decode a safe string - `eval`

```
<expression> eval
```

Decodes any internally-encoded characters in the current line back to their plain-text form.
Useful after producing text from string literal operations that you want to re-use as plain text.

### Run from the current line in a background task - `task`

```
<line> task
```

Starts a background interpreter from the current line. In that background run, the current line is
executed without the trailing `task`, and execution then continues through the remaining lines.
The main script continues immediately. The task shares the global variable table with the main script.

```ynt
print_line Starting background work... task
print_line Main thread continues.
```

#### Important behavior

- `task` does **not** run just one statement; it starts a second execution flow from that point onward.
- Without careful control flow, lines after the `task` statement may run twice:
  once on the main thread and once in the background task.
- Global variables are shared between both flows.

#### Recommended pattern

Use a direct function call with `task`, and terminate inside that function with `exit`.
This keeps the worker logic isolated.

```ynt
call background_job task
print_line Main thread keeps going

func background_job:
    print_line Work in background
exit
```

This works well for task-only worker flows. In non-task/shared flows, prefer `return` if you want
to return to the caller instead of terminating that execution flow with `exit`.

### Sleep - `sleep`

```
sleep <milliseconds>
```

Pauses execution for the given number of milliseconds. The argument must be a whole-number integer.
Respects cancellation: if the script is
stopped (e.g. via `abort_all` from another task), `sleep` returns early.

```ynt
sleep 1000
print_line One second later.
```

### Get string length - `length`

```
length <text>
```

Pushes the character count of `<text>` onto the output stack.

```ynt
length Hello, world!
var len = %out
print_line ${len}
```

### Import another script - `import`

```
import <path>
```

Inlines the contents of another `.ynt` file at the current position.
The extension `.ynt` is appended automatically if omitted.
The path is resolved relative to the directory of the importing script.

```ynt
import utils
import lib/math.ynt
```

---

## System

### Execute a program - `exec`

```
exec <program>
exec <program> with <arg1>, <arg2>, …
```

Runs an external program and waits for it to finish.
Standard output and standard error are forwarded to the console in real time.
The exit code and each line of output are pushed onto the output stack (exit code on top).

```ynt
exec notepad
exec git with status, --short
var exit_code = %out
print_line Exited with ${exit_code}
```

You can also stage arguments with `push_in` before a bare `exec`:

```ynt
push_in --version
exec python
```

---

## Predefined tokens

These inline tokens are replaced with their value before the statement executes.

| Token   | Replaced with                                       |
| ------- | --------------------------------------------------- |
| `%time` | Current Unix timestamp (seconds)                    |
| `%os`   | Operating system platform name (e.g. `Win32NT`)     |
| `%cpu`  | Processor architecture (e.g. `X64`)                 |
| `%is64` | `True` if the OS is 64-bit, otherwise `False`       |
| `%pi`   | The value of π                                      |
| `%rand` | A random integer in the range `[32767, 2147483647)` |

```ynt
print_line Time: %time
print_line OS:   %os
print_line PI:   %pi
var roll = %rand
```

---

## Termination

### Normal exit - `exit`

```
exit
```

Terminates the script gracefully. Background tasks that are still running are not cancelled.

### Exit and cancel all tasks - `abort_all`

```
abort_all
```

Terminates the script and signals all background tasks to stop.

### Throw an error - `throw`

```
throw <message>
```

Terminates the script and all tasks with `<message>` as the error description.
A stack trace is printed showing the line and file where the error occurred.

```ynt
var x = -1
if ${x} < 0 call validate_fail

func validate_fail:
    throw x must not be negative
end_func
```

### Non-fatal error - `error`

```
error <message>
```

Like `throw` but does **not** cancel background tasks.

---

## Full statement reference

| Statement          | Form                           | Description                                                  |
| ------------------ | ------------------------------ | ------------------------------------------------------------ |
| `var`              | `var name = value`             | Define/update local variable                                 |
| `global`           | `global name = value`          | Define/update global variable                                |
| `delete`           | `delete name`                  | Delete variable                                              |
| `print_line`       | `print_line [text]`            | Print line (optional text)                                   |
| `print`            | `print text`                   | Print without newline                                        |
| `clear`            | `clear`                        | Clear console                                                |
| `if … :`           | `if cond:`                     | Start conditional block                                      |
| `else:`            | `else:`                        | Else branch                                                  |
| `end_if`           | `end_if`                       | End conditional block                                        |
| `while … :`        | `while cond:`                  | Start while loop                                             |
| `end_while`        | `end_while`                    | End while loop                                               |
| `label`            | `label name:`                  | Declare a jump target                                        |
| `goto`             | `goto name`                    | Unconditional jump                                           |
| `if … goto`        | `if cond goto name`            | Conditional jump                                             |
| `func`             | `func name:`                   | Declare a function                                           |
| `call`             | `call name`                    | Call a function                                              |
| `call … with`      | `call name with a, b`          | Call a function with arguments                               |
| `if … call`        | `if cond call name`            | Conditional function call                                    |
| `push_in`          | `push_in value`                | Push input argument                                          |
| `push_out`         | `push_out value`               | Push output value (inside function)                          |
| `end_func`         | `end_func`                     | End current function                                         |
| `return`           | `return value`                 | Return from function (optional value via output stack)       |
| `clear_call_stack` | `clear_call_stack`             | Clear call stack                                             |
| `list … new`       | `list name new`                | Create/reset list                                            |
| `list … add`       | `list name add value`          | Append to list                                               |
| `list … get`       | `list name get index`          | Get item → `%out`                                            |
| `list … set`       | `list name set index value`    | Replace item                                                 |
| `list … remove`    | `list name remove index`       | Remove item                                                  |
| `list … insert`    | `list name insert index value` | Insert item                                                  |
| `list … length`    | `list name length`             | Get count → `%out`                                           |
| `list … clear`     | `list name clear`              | Clear all items                                              |
| `list … delete`    | `list name delete`             | Delete list                                                  |
| `map … new`        | `map name new`                 | Create/reset map                                             |
| `map … put`        | `map name put key, value`      | Add or update entry                                          |
| `map … get`        | `map name get key`             | Get value → `%out`                                           |
| `map … has`        | `map name has key`             | `True`/`False` if key exists → `%out`                        |
| `map … remove`     | `map name remove key`          | Remove entry                                                 |
| `map … size`       | `map name size`                | Get entry count → `%out`                                     |
| `map … clear`      | `map name clear`               | Clear all entries                                            |
| `map … delete`     | `map name delete`              | Delete map                                                   |
| `calc`             | `expr calc`                    | Evaluate arithmetic                                          |
| `eval`             | `expr eval`                    | Decode string encoding                                       |
| `task`             | `line task`                    | Run current line (without `task`) and continue in background |
| `sleep`            | `sleep ms`                     | Sleep N milliseconds                                         |
| `length`           | `length text`                  | Get char count → `%out`                                      |
| `import`           | `import path`                  | Inline-include a `.ynt` file                                 |
| `exec`             | `exec prog`                    | Run external program                                         |
| `exec … with`      | `exec prog with a, b`          | Run external program with args                               |
| `exit`             | `exit`                         | Graceful exit                                                |
| `abort_all`        | `abort_all`                    | Exit and cancel all tasks                                    |
| `throw`            | `throw message`                | Fatal error (cancels all tasks)                              |
| `error`            | `error message`                | Non-fatal error                                              |
| `%read_line`       | inline                         | Insert one line of user input                                |
| `%read_key`        | inline                         | Insert one keypress                                          |
| `%in`              | inline                         | Pop next function input argument                             |
| `%has_in`          | inline                         | `True`/`False` if input stack non-empty                      |
| `%out`             | inline                         | Pop top output value                                         |
| `%has_out`         | inline                         | `True`/`False` if output stack non-empty                     |
| `%time`            | inline                         | Unix timestamp                                               |
| `%os`              | inline                         | OS platform                                                  |
| `%cpu`             | inline                         | Processor architecture                                       |
| `%is64`            | inline                         | `True` if 64-bit OS                                          |
| `%pi`              | inline                         | Value of π                                                   |
| `%rand`            | inline                         | Random integer                                               |
