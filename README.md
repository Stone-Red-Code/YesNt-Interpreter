# YesNt
 
> YesNt is a imperative and interpreted language inspired by the Assembly language.

## Syntax

Current language syntax is documented in [SYNTAX_V2.md](SYNTAX_V2.md).

Example:

```ynt
let name = world
print_line Hello ${name}
```

## Run

```bash
dotnet run --project YesNt.Interpreter -- path/to/script.ynt
```
