![YesNt-Interpreter](https://socialify.git.ci/Stone-Red-Code/YesNt-Interpreter/image?description=1&font=Inter&forks=1&issues=1&logo=https%3A%2F%2Fraw.githubusercontent.com%2FStone-Red-Code%2FYesNt-Interpreter%2Frefs%2Fheads%2Fdevelop%2Fassets%2FLogo.svg&name=1&pattern=Diagonal+Stripes&pulls=1&stargazers=1&theme=Auto)

- [Documentation](docs/README.md)
- [Language Reference](docs/language-reference.md)
- [Releases](https://github.com/Stone-Red-Code/YesNt-Interpreter/releases)

## What is it?

YesNt is a line-based, interpreted scripting language. The core idea is simple: each line maps to one primary statement. Lines are processed top-to-bottom, and inline tokens (like `${variable}` or `%read_line`) are substituted before the statement executes. Postfix modifiers like `calc` and `task` can also appear at the end of a line to evaluate arithmetic or fork execution into a background thread.

**Key characteristics:**

- **Line-based execution:** Lines are processed top-to-bottom. Each line is a self-contained statement
- **Explicit control flow:** Labels, `goto`, and conditional jumps alongside structured `if`/`while` blocks
- **Explicit scoping:** `var` for function-local variables, `global` for cross-scope shared state
- **Functions with stacks:** Arguments and return values are passed via explicit push/pop stacks (`push_in`, `%in`, `push_out`, `%out`)
- **Background tasks:** Any line can be forked into a background execution flow with `task`
- **Embeddable:** The interpreter ships as a C# library. Custom statements can be registered, and built-in ones can be removed or disabled for sandboxing

YesNt is intentionally minimal and is well suited for:

- Scripting in games or applications where simple syntax and customizability are needed
- Automation tasks where a lightweight embedded language is beneficial
- Educational purposes to learn language design by adding new statements or modifying existing ones
- Embedding as a scripting engine in larger C# projects, with the ability to expose custom functionality through registered statements

## Usage

1. Download & install the latest release
   - Chocolatey (Windows): `choco install yesnt`
   - Snapcraft (Linux): `snap install yesnt`
   - GitHub (Windows/Linux): [releases](https://github.com/Stone-Red-Code/YesNt-Interpreter/releases)
2. Run a script with the interpreter CLI
   - `yesnt script.ynt` runs a script
3. Or use the terminal code editor
   - `yesntcode` opens the editor (optional path to load a file)
   - `run` / `debug` to execute the open script
   - `format` to auto-format indentation

## Example

```ynt
func greet:
    var name = %in
    print_line Hello, ${name}!
return

func add:
    var result = %in + %in calc
    push_out ${result}
return

call greet with Alice
call greet with Bob

call add with 3, 7
var sum = %out
print_line 3 + 7 = ${sum}
```
