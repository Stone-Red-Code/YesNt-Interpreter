using System;
using System.Collections.Generic;

using YesNt.Interpreter.Attributes;
using YesNt.Interpreter.Enums;
using YesNt.Interpreter.Runtime;

namespace YesNt.Interpreter.Statements;

internal class MapStatements : StatementRuntimeInformation
{
    [Statement("map", SearchMode.StartOfLine, SpaceAround.End, ConsoleColor.DarkCyan, Separator = " new")]
    public void Create(string args)
    {
        string[] parts = SplitTwo(args, " new");
        if (parts.Length == 0)
        {
            return;
        }

        string name = parts[0];
        if (!RuntimeInfo.Maps.TryGetValue(name, out Dictionary<string, string> value))
        {
            RuntimeInfo.Maps.Add(name, []);
        }
        else
        {
            value.Clear();
        }
    }

    [Statement("map", SearchMode.StartOfLine, SpaceAround.End, ConsoleColor.DarkCyan, Separator = " delete")]
    public void Delete(string args)
    {
        string[] parts = SplitTwo(args, " delete");
        if (parts.Length == 0)
        {
            return;
        }

        string name = parts[0];

        if (!RuntimeInfo.Maps.ContainsKey(name))
        {
            RuntimeInfo.Exit(ExitMessages.MapNotFound(name), true);
            return;
        }

        _ = RuntimeInfo.Maps.Remove(name);
    }

    [Statement("map", SearchMode.StartOfLine, SpaceAround.End, ConsoleColor.DarkCyan, Separator = " clear")]
    public void Clear(string args)
    {
        string[] parts = SplitTwo(args, " clear");
        if (parts.Length == 0)
        {
            return;
        }

        if (!TryGetMap(parts[0], out Dictionary<string, string> map))
        {
            return;
        }

        map.Clear();
    }

    [Statement("map", SearchMode.StartOfLine, SpaceAround.End, ConsoleColor.DarkCyan, Separator = " size")]
    public void Size(string args)
    {
        string[] parts = SplitTwo(args, " size");
        if (parts.Length == 0)
        {
            return;
        }

        if (!TryGetMap(parts[0], out Dictionary<string, string> map))
        {
            return;
        }

        RuntimeInfo.OutParametersStack.Clear();
        RuntimeInfo.OutParametersStack.Push(map.Count.ToString());
    }

    [Statement("map", SearchMode.StartOfLine, SpaceAround.End, ConsoleColor.DarkCyan, Separator = " put ")]
    public void Put(string args)
    {
        string[] parts = SplitTwo(args, " put ");
        if (parts.Length == 0)
        {
            return;
        }

        if (!TryGetMap(parts[0], out Dictionary<string, string> map))
        {
            return;
        }

        string[] keyAndValue = SplitKeyAndValue(parts[1]);
        if (keyAndValue.Length == 0)
        {
            return;
        }

        map[keyAndValue[0]] = keyAndValue[1];
    }

    [Statement("map", SearchMode.StartOfLine, SpaceAround.End, ConsoleColor.DarkCyan, Separator = " get ")]
    public void Get(string args)
    {
        string[] parts = SplitTwo(args, " get ");
        if (parts.Length == 0)
        {
            return;
        }

        if (!TryGetMap(parts[0], out Dictionary<string, string> map))
        {
            return;
        }

        string key = parts[1];
        if (!map.TryGetValue(key, out string value))
        {
            RuntimeInfo.Exit(ExitMessages.KeyNotFound(key), true);
            return;
        }

        RuntimeInfo.OutParametersStack.Clear();
        RuntimeInfo.OutParametersStack.Push(value);
    }

    [Statement("map", SearchMode.StartOfLine, SpaceAround.End, ConsoleColor.DarkCyan, Separator = " has ")]
    public void Has(string args)
    {
        string[] parts = SplitTwo(args, " has ");
        if (parts.Length == 0)
        {
            return;
        }

        if (!TryGetMap(parts[0], out Dictionary<string, string> map))
        {
            return;
        }

        RuntimeInfo.OutParametersStack.Clear();
        RuntimeInfo.OutParametersStack.Push(map.ContainsKey(parts[1]).ToString());
    }

    [Statement("map", SearchMode.StartOfLine, SpaceAround.End, ConsoleColor.DarkCyan, Separator = " remove ")]
    public void Remove(string args)
    {
        string[] parts = SplitTwo(args, " remove ");
        if (parts.Length == 0)
        {
            return;
        }

        if (!TryGetMap(parts[0], out Dictionary<string, string> map))
        {
            return;
        }

        _ = map.Remove(parts[1]);
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

    private bool TryGetMap(string name, out Dictionary<string, string> map)
    {
        if (!RuntimeInfo.Maps.TryGetValue(name, out map))
        {
            RuntimeInfo.Exit(ExitMessages.MapNotFound(name), true);
            return false;
        }

        return true;
    }

    private string[] SplitKeyAndValue(string input)
    {
        string[] parts = input.Split(',');
        if (parts.Length != 2)
        {
            RuntimeInfo.Exit(ExitMessages.InvalidSyntax, true);
            return [];
        }

        parts[0] = parts[0].Trim();
        parts[1] = parts[1].Trim();

        if (string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
        {
            RuntimeInfo.Exit(ExitMessages.InvalidSyntax, true);
            return [];
        }

        return parts;
    }
}
