using System;

using YesNt.Interpreter.Attributes;
using YesNt.Interpreter.Enums;
using YesNt.Interpreter.Runtime;
using YesNt.Interpreter.Utilities;

namespace YesNt.Interpreter.Statements;

internal class PredefinedVariableStatements : StatementRuntimeInformation
{
    private readonly Random random = new Random();

    [Statement("%time", SearchMode.Contains, SpaceAround.None, ConsoleColor.Blue, KeepStatementInArgs = true, Priority = Priority.Highest)]
    public void GetUnixTimestamp(string args)
    {
        RuntimeInfo.CurrentLine = TemplateProcessor.ProcessSimplePlaceholders(args, "%time", DateTimeOffset.Now.ToUnixTimeSeconds().ToString()).TrimEnd();
    }

    [Statement("%os", SearchMode.Contains, SpaceAround.None, ConsoleColor.Blue, KeepStatementInArgs = true, Priority = Priority.Highest)]
    public void GetOperatingSystem(string args)
    {
        RuntimeInfo.CurrentLine = TemplateProcessor.ProcessSimplePlaceholders(args, "%os", Environment.OSVersion.Platform.ToString()).TrimEnd();
    }

    [Statement("%cpu", SearchMode.Contains, SpaceAround.None, ConsoleColor.Blue, KeepStatementInArgs = true, Priority = Priority.Highest)]
    public void GetProcessorArchitecture(string args)
    {
        RuntimeInfo.CurrentLine = TemplateProcessor.ProcessSimplePlaceholders(args, "%cpu", System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString()).TrimEnd();
    }

    [Statement("%is64", SearchMode.Contains, SpaceAround.None, ConsoleColor.Blue, KeepStatementInArgs = true, Priority = Priority.Highest)]
    public void GetIsOperatingSystem64Bit(string args)
    {
        RuntimeInfo.CurrentLine = TemplateProcessor.ProcessSimplePlaceholders(args, "%is64", Environment.Is64BitOperatingSystem.ToString()).TrimEnd();
    }

    [Statement("%pi", SearchMode.Contains, SpaceAround.None, ConsoleColor.Blue, KeepStatementInArgs = true, Priority = Priority.Highest)]
    public void GetPi(string args)
    {
        RuntimeInfo.CurrentLine = TemplateProcessor.ProcessSimplePlaceholders(args, "%pi", Math.PI.ToString()).TrimEnd();
    }

    [Statement("%rand", SearchMode.Contains, SpaceAround.None, ConsoleColor.Blue, KeepStatementInArgs = true, Priority = Priority.Highest)]
    public void GetRandom(string args)
    {
        RuntimeInfo.CurrentLine = TemplateProcessor.ProcessDynamicPlaceholders(args, "%rand", () => random.Next(32767, int.MaxValue).ToString()).TrimEnd();
    }
}