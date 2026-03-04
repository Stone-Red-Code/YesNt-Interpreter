# YesNt Code Editor

`YesNt.CodeEditor` is a terminal editor for writing, formatting, running, and debugging YesNt scripts.

## Start the editor

```bash
dotnet run --project YesNt.CodeEditor -- [optional-path-to-file.ynt]
```

If you pass a file path, it is loaded on startup.

## Modes

- **Command mode:** enter editor commands in the `>>>` prompt.
- **Edit mode:** direct text editing with keyboard navigation.
- **Debug mode:** script output/debug information while running.

## Command mode commands

| Command               | Description                                               |
| --------------------- | --------------------------------------------------------- |
| `edit`                | Switch to edit mode                                       |
| `line <n>`            | Jump to line `n` and switch to edit mode                  |
| `save [path]`         | Save to current path or a new path                        |
| `load <path>`         | Load a file                                               |
| `new`                 | Create new file                                           |
| `format`              | Auto-format indentation                                   |
| `run [path]`          | Run script                                                |
| `debug [path] [step]` | Run in debug mode (`step` enables step-by-step execution) |
| `exit`                | Close the editor                                          |

## Edit mode controls

- Arrow keys: move cursor
- Enter: split line
- Backspace/Delete: remove characters/merge lines
- **Alt+C:** return to command mode
- **Alt+T:** jump to top
- **Alt+B:** jump to bottom
- **Alt+S:** jump to start of line
- **Alt+E:** jump to end of line
- **Alt+R:** run script
- **Alt+D:** run debug mode
- **Alt+F:** format current file

## Formatter behavior (quick summary)

- Indents `func`, `if`, `else`, and `while` blocks.
- Dedents on `return`, `end_if`, and `end_while`.
- `exit` / `throw` / `error` close active non-function blocks for following lines.
- Comment lines (`# ...`) are kept unindented.
