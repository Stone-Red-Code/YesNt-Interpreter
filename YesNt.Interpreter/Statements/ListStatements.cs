using System;
using System.Collections.Generic;

using YesNt.Interpreter.Attributes;
using YesNt.Interpreter.Enums;
using YesNt.Interpreter.Runtime;

namespace YesNt.Interpreter.Statements;

internal class ListStatements : StatementRuntimeInformation
{
    [Statement("list", SearchMode.StartOfLine, SpaceAround.End, ConsoleColor.DarkCyan, Separator = " new")]
    public void Create(string args)
    {
        string[] parts = SplitTwo(args, " new");
        if (parts.Length == 0)
        {
            return;
        }

        string name = parts[0];
        if (!RuntimeInfo.Lists.TryGetValue(name, out List<string> value))
        {
            RuntimeInfo.Lists.Add(name, []);
        }
        else
        {
            value.Clear();
        }
    }

    [Statement("list", SearchMode.StartOfLine, SpaceAround.End, ConsoleColor.DarkCyan, Separator = " delete")]
    public void Delete(string args)
    {
        string[] parts = SplitTwo(args, " delete");
        if (parts.Length == 0)
        {
            return;
        }

        string name = parts[0];

        if (!RuntimeInfo.Lists.ContainsKey(name))
        {
            RuntimeInfo.Exit(ExitMessages.ListNotFound(name), true);
            return;
        }

        _ = RuntimeInfo.Lists.Remove(name);
    }

    [Statement("list", SearchMode.StartOfLine, SpaceAround.End, ConsoleColor.DarkCyan, Separator = " clear")]
    public void Clear(string args)
    {
        string[] parts = SplitTwo(args, " clear");
        if (parts.Length == 0)
        {
            return;
        }

        if (!TryGetList(parts[0], out List<string> list))
        {
            return;
        }

        list.Clear();
    }

    [Statement("list", SearchMode.StartOfLine, SpaceAround.End, ConsoleColor.DarkCyan, Separator = " length")]
    public void Length(string args)
    {
        string[] parts = SplitTwo(args, " length");
        if (parts.Length == 0)
        {
            return;
        }

        if (!TryGetList(parts[0], out List<string> list))
        {
            return;
        }

        RuntimeInfo.OutParametersStack.Clear();
        RuntimeInfo.OutParametersStack.Push(list.Count.ToString());
    }

    [Statement("list", SearchMode.StartOfLine, SpaceAround.End, ConsoleColor.DarkCyan, Separator = " add ")]
    public void Add(string args)
    {
        string[] parts = SplitTwo(args, " add ");
        if (parts.Length == 0)
        {
            return;
        }

        if (!TryGetList(parts[0], out List<string> list))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(parts[1]))
        {
            RuntimeInfo.Exit(ExitMessages.InvalidSyntax, true);
            return;
        }

        list.Add(parts[1].Trim());
    }

    [Statement("list", SearchMode.StartOfLine, SpaceAround.End, ConsoleColor.DarkCyan, Separator = " get ")]
    public void Get(string args)
    {
        string[] parts = SplitTwo(args, " get ");
        if (parts.Length == 0)
        {
            return;
        }

        if (!TryGetList(parts[0], out List<string> list))
        {
            return;
        }

        if (!TryParseIndex(parts[1], out int index, list.Count))
        {
            return;
        }

        RuntimeInfo.OutParametersStack.Clear();
        RuntimeInfo.OutParametersStack.Push(list[index]);
    }

    [Statement("list", SearchMode.StartOfLine, SpaceAround.End, ConsoleColor.DarkCyan, Separator = " remove ")]
    public void Remove(string args)
    {
        string[] parts = SplitTwo(args, " remove ");
        if (parts.Length == 0)
        {
            return;
        }

        if (!TryGetList(parts[0], out List<string> list))
        {
            return;
        }

        if (!TryParseIndex(parts[1], out int index, list.Count))
        {
            return;
        }

        list.RemoveAt(index);
    }

    [Statement("list", SearchMode.StartOfLine, SpaceAround.End, ConsoleColor.DarkCyan, Separator = " set ")]
    public void Set(string args)
    {
        string[] parts = SplitTwo(args, " set ");
        if (parts.Length == 0)
        {
            return;
        }

        if (!TryGetList(parts[0], out List<string> list))
        {
            return;
        }

        string[] indexAndValue = SplitIndexAndValue(parts[1]);
        if (indexAndValue.Length == 0)
        {
            return;
        }

        if (!TryParseIndex(indexAndValue[0], out int index, list.Count))
        {
            return;
        }

        list[index] = indexAndValue[1];
    }

    [Statement("list", SearchMode.StartOfLine, SpaceAround.End, ConsoleColor.DarkCyan, Separator = " insert ")]
    public void Insert(string args)
    {
        string[] parts = SplitTwo(args, " insert ");
        if (parts.Length == 0)
        {
            return;
        }

        if (!TryGetList(parts[0], out List<string> list))
        {
            return;
        }

        string[] indexAndValue = SplitIndexAndValue(parts[1]);
        if (indexAndValue.Length == 0)
        {
            return;
        }

        if (!TryParseIndex(indexAndValue[0], out int index, list.Count + 1))
        {
            return;
        }

        list.Insert(index, indexAndValue[1]);
    }

    private string[] SplitTwo(string input, string separator)
    {
        string[] parts = input.Split(separator, 2, StringSplitOptions.None);
        if (parts.Length != 2)
        {
            RuntimeInfo.Exit(ExitMessages.InvalidSyntax, true);
            return [];
        }

        parts[0] = parts[0].Trim();
        parts[1] = parts[1].Trim();

        if (string.IsNullOrWhiteSpace(parts[0]))
        {
            RuntimeInfo.Exit(ExitMessages.InvalidSyntax, true);
            return [];
        }

        return parts;
    }

    private bool TryGetList(string name, out List<string> list)
    {
        if (!RuntimeInfo.Lists.TryGetValue(name, out list))
        {
            RuntimeInfo.Exit(ExitMessages.ListNotFound(name), true);
            return false;
        }

        return true;
    }

    private string[] SplitIndexAndValue(string input)
    {
        string[] parts = input.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            RuntimeInfo.Exit(ExitMessages.InvalidSyntax, true);
            return [];
        }

        parts[0] = parts[0].Trim();
        parts[1] = parts[1].Trim();
        return parts;
    }

    private bool TryParseIndex(string rawIndex, out int index, int maxExclusive)
    {
        bool success = int.TryParse(rawIndex.Trim(), out index);
        if (!success)
        {
            RuntimeInfo.Exit(ExitMessages.InvalidIndex(rawIndex), true);
            return false;
        }

        if (index < 0 || index >= maxExclusive)
        {
            RuntimeInfo.Exit(ExitMessages.IndexOutOfRange(index), true);
            return false;
        }

        return true;
    }
}
