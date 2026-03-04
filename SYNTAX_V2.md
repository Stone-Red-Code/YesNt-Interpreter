# YesNt v2 Syntax

This document describes the current, word-based YesNt syntax.

## Goals

- Keep current semantics and execution model.
- Replace terse mnemonics and symbols with readable words.
- Preserve line-based scripting.
- Keep the language line-based and lightweight.

## v2 Core Rules

- Statements are line-based.
- `# ...` remains a comment.
- Variable interpolation inside text uses `${name}`.
- Function and label declarations are block markers with a trailing `:`.
- Conditions retain current evaluator expressions (for example: `a == b`, `x > 5`, `10 + 2 == 12`).

## Quick Example

```ynt
let name = world
func greet:
print_line Hello ${name}
return
call greet
```

## Block Conditionals

```ynt
if 10 > 5:
print_line yes
else:
print_line no
end_if
```

## While Loops

```ynt
let i = 3
while ${i} > 0:
print_line ${i}
let i = ${i} - 1 calc
end_while
```

## Full v1 -> v2 Mapping

| Area | v1 Syntax | v2 Syntax | Notes |
|---|---|---|---|
| Variables | `<x = value` | `let x = value` | Local variable define/update. |
| Variables | `!<x = value` | `global x = value` | Global variable define/update. |
| Variables | `del x` | `delete x` | Deletes local first, then global. |
| Variables | `>x` | `${x}` | Variable read/interpolation token. |
| Console | `cwl` | `print_line` | Empty line print. |
| Console | `cwl text` | `print_line text` | Print with newline. |
| Console | `cw text` | `print text` | Print without newline. |
| Console | `%crl` | `%read_line` | Inline token, inserts user line input. |
| Console | `%cr` | `%read_key` | Inline token, inserts user key input. |
| Console | `cls` | `clear` | Clear console. |
| Code flow | `lbl name` | `label name:` | Label declaration/target. |
| Code flow | `jmp name` | `goto name` | Unconditional jump. |
| Code flow | `jif name \| cond` | `if cond goto name` | Conditional jump. |
| Functions | `fnc name` | `func name:` | Function declaration. |
| Functions | `cal name` | `call name` | Function call without args. |
| Functions | `cal name \| a,b,c` | `call name with a, b, c` | Function call with args. |
| Functions | `in value` | `push_in value` | Push in-arg onto input stack. |
| Functions | `%get` | `%in` | Inline token, pop current call input arg. |
| Functions | `%isi` | `%has_in` | Inline token, bool if input arg exists. |
| Functions | `put value` | `push_out value` | Push out-arg in function. |
| Functions | `%out` | `%out` | Keep token name for familiarity. |
| Functions | `%iso` | `%has_out` | Inline token, bool if out arg exists. |
| Functions | `ret` | `return` | Return from function. |
| Functions | `ccs` | `clear_call_stack` | Clear call stack. |
| Condition-call | `cif name \| cond` | `if cond call name` | Conditional function call. |
| Termination | `end` | `exit` | Planned termination. |
| Termination | `trm` | `abort_all` | Planned termination + cancel tasks. |
| Errors | `trw message` | `throw message` | Error termination. |
| Errors | `err message` | `error message` | Non-fatal/runtime message end state. |
| Processing | `expr !calc` | `expr calc` | Evaluate arithmetic fragments. |
| Processing | `text !eval` | `text eval` | Decode safe string literals. |
| Processing | `line !task` | `line task` | Run line in background task runtime. |
| Processing | `slp ms` | `sleep ms` | Sleep with runtime-aware cancellation. |
| Processing | `len text` | `length text` | Push text length to out stack. |
| Processing | `imp file` | `import file` | Inline include of another `.ynt` file. |
| System | `exc prog` | `exec prog` | Execute process with in-stack args. |
| System | `exc prog \| a,b,c` | `exec prog with a, b, c` | Execute process with explicit args. |
| Predefined | `%time` | `%time` | Unix timestamp token. |
| Predefined | `%os` | `%os` | OS platform token. |
| Predefined | `%cpu` | `%cpu` | Processor architecture token. |
| Predefined | `%is64` | `%is64` | 64-bit OS bool token. |
| Predefined | `%pi` | `%pi` | PI token. |
| Predefined | `%rnd` | `%rand` | Random number token. |

## Notes

- `%out` is intentionally kept as `%out`.
- Postfix operations are `calc`, `eval`, and `task`.
