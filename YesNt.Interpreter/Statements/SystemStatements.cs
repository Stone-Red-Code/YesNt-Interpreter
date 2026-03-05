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
        string program = parts[0].Trim();

        foreach (string argument in parts[1].Split(','))
        {
            RuntimeInfo.InParametersStack.Push(argument.Trim());
        }

        try
        {
            StartProcess(program, string.Join(" ", RuntimeInfo.InParametersStack.Reverse()));
        }
        catch (FileNotFoundException)
        {
            RuntimeInfo.Exit(ExitMessages.CannotFindFile(program), false);
        }
        catch (Win32Exception ex)
        {
            RuntimeInfo.Exit(ExitMessages.FailedToStart(program, ex.Message), false);
        }
    }

    [Statement("exec", SearchMode.StartOfLine, SpaceAround.End, ConsoleColor.Magenta, Priority = Priority.VeryLow)]
    public void ExecuteProgram(string input)
    {
        // This is to prevent the "exec with" statement from being triggered by this one, since they both start with "exec"
        if (input.Contains(" with "))
        {
            return;
        }

        input = input.FromSafeString();

        try
        {
            StartProcess(input, string.Join(" ", RuntimeInfo.InParametersStack.Reverse()));
        }
        catch (FileNotFoundException)
        {
            RuntimeInfo.Exit(ExitMessages.CannotFindFile(input), false);
        }
        catch (Win32Exception ex)
        {
            RuntimeInfo.Exit(ExitMessages.FailedToStart(input, ex.Message), false);
        }
    }

    private void Process_ErrorDataReceived(Utilities.DataReceivedEventArgs e, Stack<string> outputStack)
    {
        if (string.IsNullOrWhiteSpace(e.Data))
        {
            return;
        }

        outputStack.Push(e.Data.ToSafeString());
        RuntimeInfo.Write("Error: " + e.Data);
    }

    private void Process_OutputDataReceived(Utilities.DataReceivedEventArgs e, Stack<string> outputStack)
    {
        if (string.IsNullOrWhiteSpace(e.Data))
        {
            return;
        }

        outputStack.Push(e.Data.ToSafeString());
        RuntimeInfo.Write(e.Data);
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