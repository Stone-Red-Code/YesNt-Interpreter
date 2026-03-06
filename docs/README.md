# YesNt Documentation

YesNt is a line-based, interpreted scripting language.
Each line is one statement. There are no multi-line expressions.

## Guides

| Document                                    | Description                                          |
| ------------------------------------------- | ---------------------------------------------------- |
| [Language Reference](language-reference.md) | Every statement, token, and operator in the language |
| [Library API](library-api.md)               | How to embed the interpreter in a C# project         |
| [Editor](editor.md)                         | How to use the terminal code editor                  |

## Quick start

### Running a script from the command line

```bash
yesnt path/to/script.ynt
```

### Hello world

```ynt
print_line Hello, world!
```

### Variables and output

```ynt
var name = Alice
print_line Hello, ${name}!
```

### Functions

```ynt
func greet:
    var msg = Hello, ${name}!
    print_line ${msg}
return

var name = Bob
call greet
```

Script files use the `.ynt` extension by convention.
