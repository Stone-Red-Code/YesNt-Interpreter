using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;

using YesNt.Interpreter.Attributes;
using YesNt.Interpreter.Enums;
using YesNt.Interpreter.Runtime;
using YesNt.Interpreter.Utilities;

namespace YesNt.Interpreter.Statements;

internal class SystemStatements : StatementRuntimeInformation
{
    [Statement("exec", SearchMode.StartOfLine, SpaceAround.End, ConsoleColor.Magenta, Priority = Priority.Low, Separator = " with ")]
    public void ExecuteProgramWithArgs(string input)
    {
        string[] parts = input.FromSafeString().Split(" with ", 2, StringSplitOptions.None);
        parts[0] = parts[0].Trim();

        string[] functionArguments = parts[1].Split(',');

        foreach (string argument in functionArguments)
        {
            RuntimeInfo.InParametersStack.Push(argument.Trim());
        }

        try
        {
            StartProcess(parts[0], string.Join(" ", RuntimeInfo.InParametersStack.Reverse()));
        }
        catch (FileNotFoundException)
        {
            RuntimeInfo.Exit($"Cannot find file \"{parts[0]}\".", false);
        }
        catch (Win32Exception ex)
        {
            RuntimeInfo.Exit($"Failed to start \"{parts[0]}\". {ex.Message}", false);
        }

        // HACK: Clear line to avoid execution from another "exec" statement.
        RuntimeInfo.CurrentLine = string.Empty;
    }

    [Statement("exec", SearchMode.StartOfLine, SpaceAround.End, ConsoleColor.Magenta, Priority = Priority.VeryLow)]
    public void ExecuteProgram(string input)
    {
        try
        {
            StartProcess(input, string.Join(" ", RuntimeInfo.InParametersStack.Reverse()));
        }
        catch (FileNotFoundException)
        {
            RuntimeInfo.Exit($"Cannot find file \"{input}\".", false);
        }
        catch (Win32Exception ex)
        {
            RuntimeInfo.Exit($"Failed to start \"{input}\". {ex.Message}", false);
        }
    }

    private static void Process_ErrorDataReceived(Utilities.DataReceivedEventArgs e, Stack<string> outputStack)
    {
        if (string.IsNullOrWhiteSpace(e.Data))
        {
            return;
        }

        outputStack.Push(e.Data.ToSafeString());
        Console.Write("Error: " + e.Data);
    }

    private static void Process_OutputDataReceived(Utilities.DataReceivedEventArgs e, Stack<string> outputStack)
    {
        if (string.IsNullOrWhiteSpace(e.Data))
        {
            return;
        }

        outputStack.Push(e.Data.ToSafeString());
        Console.Write(e.Data);
    }

    private void StartProcess(string name, string args)
    {
        RuntimeInfo.OutParametersStack.Clear();

        Stack<string> outputStack = new Stack<string>();

        FixedProcess process = new FixedProcess
        {
            StartInfo = new ProcessStartInfo()
            {
                FileName = name,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };

        process.OutputDataReceived += (s, e) => Process_OutputDataReceived(e, outputStack);
        process.ErrorDataReceived += (s, e) => Process_ErrorDataReceived(e, outputStack);

        _ = process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        RuntimeInfo.InParametersStack.Clear();

        RuntimeInfo.OutParametersStack = new(outputStack);
        RuntimeInfo.OutParametersStack.Push(process.ExitCode.ToString());
    }
}
