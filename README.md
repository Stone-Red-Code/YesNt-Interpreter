# YesNt

> YesNt is a imperative and interpreted language inspired by the Assembly language.

## Documentation

| Document                                                 | Description                               |
| -------------------------------------------------------- | ----------------------------------------- |
| [docs/README.md](docs/README.md)                         | Getting started                           |
| [docs/language-reference.md](docs/language-reference.md) | Full language reference                   |
| [docs/library-api.md](docs/library-api.md)               | Embedding the interpreter as a C# library |
| [docs/editor.md](docs/editor.md)                         | Using the terminal code editor            |

## Quick example

```ynt
var name = world
print_line Hello ${name}
```

## Run

```bash
dotnet run --project YesNt.Interpreter.App -- path/to/script.ynt
```
