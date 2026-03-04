using System.Text;

using YesNt.Interpreter.Attributes;
using YesNt.Interpreter.Enums;
using YesNt.Interpreter.Runtime;
using YesNt.Interpreter.Utilities;

namespace YesNt.Interpreter.Statements;

internal class StringLiteralStatements : StatementRuntimeInformation
{
    [Statement("\"", SearchMode.Contains, SpaceAround.None, System.ConsoleColor.DarkYellow, Priority = Priority.PreProcessing, KeepStatementInArgs = true)]
    public void ParseStringLiterals(string args)
    {
        if (!args.Contains('"'))
        {
            return;
        }

        StringBuilder output = new StringBuilder(args.Length);

        for (int i = 0; i < args.Length; i++)
        {
            char current = args[i];
            if (current != '"')
            {
                _ = output.Append(current);
                continue;
            }

            StringBuilder literal = new StringBuilder();
            bool closed = false;
            i++;

            for (; i < args.Length; i++)
            {
                char ch = args[i];
                if (ch == '\\' && i + 1 < args.Length)
                {
                    i++;
                    _ = literal.Append(ParseEscape(args[i]));
                    continue;
                }

                if (ch == '"')
                {
                    closed = true;
                    break;
                }

                _ = literal.Append(ch);
            }

            if (!closed)
            {
                RuntimeInfo.Exit(ExitMessages.InvalidStringLiteral, true);
                return;
            }

            _ = output.Append(literal.ToString().ToSafeString());
        }

        RuntimeInfo.CurrentLine = output.ToString();
    }

    private static char ParseEscape(char escapeChar)
    {
        return escapeChar switch
        {
            'n' => '\n',
            'r' => '\r',
            't' => '\t',
            '"' => '"',
            '\\' => '\\',
            _ => escapeChar
        };
    }
}
