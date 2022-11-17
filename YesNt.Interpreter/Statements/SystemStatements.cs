using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;

using YesNt.Interpreter.Attributes;
using YesNt.Interpreter.Enums;
using YesNt.Interpreter.Runtime;
using YesNt.Interpreter.Utilities;

namespace YesNt.Interpreter.Statements
{
    internal class SystemStatements : StatementRuntimeInformation
    {
        [Statement("exc", SearchMode.StartOfLine, SpaceAround.End, ConsoleColor.Magenta, Priority = Priority.Low, Seperator = "|")]
        public void ExecuteProgramWithArgs(string input)
        {
            string[] parts = input.FromSafeString().Split('|');
            parts[0] = parts[0].Trim();

            string[] functionArgumets = parts[1].Split(',');

            foreach (string argumanet in functionArgumets)
            {
                RuntimeInfo.InParametersStack.Push(argumanet.Trim());
            }

            try
            {
                StartProcess(parts[0], string.Join(string.Empty, RuntimeInfo.InParametersStack.Reverse()));
            }
            catch (FileNotFoundException)
            {
                RuntimeInfo.Exit($"Cannot find file \"{parts[0]}\".", false);
            }
            catch (Win32Exception ex)
            {
                RuntimeInfo.Exit($"Failed to start \"{parts[0]}\". {ex.Message}", false);
            }

            //HACK: Clear line to avoid execution from other "exc" statement
            RuntimeInfo.CurrentLine = string.Empty;
        }

        [Statement("exc", SearchMode.StartOfLine, SpaceAround.End, ConsoleColor.Magenta, Priority = Priority.VeryLow)]
        public void ExecuteProgram(string input)
        {
            try
            {
                StartProcess(input, string.Join(string.Empty, RuntimeInfo.InParametersStack.Reverse()));
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

        private void StartProcess(string name, string args)
        {
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

            process.OutputDataReceived += Process_OutputDataReceived;
            process.ErrorDataReceived += Process_ErrorDataReceived;

            _ = process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            RuntimeInfo.InParametersStack.Clear();
            RuntimeInfo.OutParametersStack.Clear();
            RuntimeInfo.OutParametersStack.Push(process.ExitCode.ToString());
        }

        private void Process_ErrorDataReceived(object sender, Utilities.DataReceivedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(e.Data))
            {
                return;
            }

            RuntimeInfo.OutParametersStack.Push(e.Data);
            Console.Write("Error: " + e.Data);
        }

        private void Process_OutputDataReceived(object sender, Utilities.DataReceivedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(e.Data))
            {
                return;
            }

            RuntimeInfo.OutParametersStack.Push(e.Data);
            Console.Write(e.Data);
        }
    }
}