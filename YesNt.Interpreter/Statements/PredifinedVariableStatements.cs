using System;

using YesNt.Interpreter.Attributes;
using YesNt.Interpreter.Enums;
using YesNt.Interpreter.Runtime;
using YesNt.Interpreter.Utilities;

namespace YesNt.Interpreter.Statements
{
    internal class PredifinedVariableStatements : StatementRuntimeInformation
    {
        private readonly Random random = new Random();

        [Statement("%time", SearchMode.Contains, SpaceAround.None, ConsoleColor.Blue, KeepStatementInArgs = true, Priority = Priority.Highest)]
        public void GetUnixTimestamp(string args)
        {
            args = args.Replace("%time", DateTimeOffset.Now.ToUnixTimeSeconds().ToString());

            RuntimeInfo.CurrentLine = args.TrimEnd();
        }

        [Statement("%os", SearchMode.Contains, SpaceAround.None, ConsoleColor.Blue, KeepStatementInArgs = true, Priority = Priority.Highest)]
        public void GetOperatingSystem(string args)
        {
            args = args.Replace("%os", Environment.OSVersion.Platform.ToString());

            RuntimeInfo.CurrentLine = args.TrimEnd();
        }

        [Statement("%cpu", SearchMode.Contains, SpaceAround.None, ConsoleColor.Blue, KeepStatementInArgs = true, Priority = Priority.Highest)]
        public void GetProcessorArchitecture(string args)
        {
            args = args.Replace("%cpu", System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString());

            RuntimeInfo.CurrentLine = args.TrimEnd();
        }

        [Statement("%is64", SearchMode.Contains, SpaceAround.None, ConsoleColor.Blue, KeepStatementInArgs = true, Priority = Priority.Highest)]
        public void GetIsOperatingSystem64Bit(string args)
        {
            args = args.Replace("%is64", $"{Environment.Is64BitOperatingSystem}");

            RuntimeInfo.CurrentLine = args.TrimEnd();
        }

        [Statement("%pi", SearchMode.Contains, SpaceAround.None, ConsoleColor.Blue, KeepStatementInArgs = true, Priority = Priority.Highest)]
        public void GetPi(string args)
        {
            args = args.Replace("%pi", Math.PI.ToString());

            RuntimeInfo.CurrentLine = args.TrimEnd();
        }

        [Statement("%rnd", SearchMode.Contains, SpaceAround.None, ConsoleColor.Blue, KeepStatementInArgs = true, Priority = Priority.Highest)]
        public void GetRandom(string args)
        {
            while (args.Contains("%rnd"))
            {
                args = args.ReplaceFirstOccurrence("%rnd", random.Next(32767, int.MaxValue).ToString());
            }

            RuntimeInfo.CurrentLine = args.TrimEnd();
        }
    }
}