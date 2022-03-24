﻿using System;

using YesNt.Interpreter.Attributes;
using YesNt.Interpreter.Enums;
using YesNt.Interpreter.Utilities;

namespace YesNt.Interpreter.Statements
{
    internal class PredifinedVariableStatements : StatementRuntimeInformation
    {
        private readonly Random random = new Random();

        [Statement("%tim", SearchMode.Contains, SpaceAround.End, ConsoleColor.Blue, KeepStatementInArgs = true, Priority = Priority.Highest)]
        public void GetUnixTimestamp(string args)
        {
            args += " ";

            while (args.Contains("%tim "))
            {
                args = args.ReplaceFirstOccurrence("%tim ", $"{DateTimeOffset.Now.ToUnixTimeSeconds()}");
            }

            RuntimeInfo.CurrentLine = args.TrimEnd();
        }

        [Statement("%pi", SearchMode.Contains, SpaceAround.End, ConsoleColor.Blue, KeepStatementInArgs = true, Priority = Priority.Highest)]
        public void GetPi(string args)
        {
            args += " ";
            args = args.Replace("%pi ", $"{Math.PI} ");

            RuntimeInfo.CurrentLine = args.TrimEnd();
        }

        [Statement("%rnd", SearchMode.Contains, SpaceAround.End, ConsoleColor.Blue, KeepStatementInArgs = true, Priority = Priority.Highest)]
        public void GetRandom(string args)
        {
            args += " ";

            while (args.Contains("%rnd "))
            {
                args = args.ReplaceFirstOccurrence("%rnd ", $"{random.Next() % 2} ");
            }

            RuntimeInfo.CurrentLine = args.TrimEnd();
        }
    }
}