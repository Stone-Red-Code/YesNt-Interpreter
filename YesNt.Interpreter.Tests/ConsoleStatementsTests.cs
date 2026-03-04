using Microsoft.VisualStudio.TestTools.UnitTesting;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using YesNt.Interpreter.Runtime;

namespace YesNt.Interpreter.Tests;

[TestClass]
public class ConsoleStatementsTests
{
    private static readonly object ConsoleLock = new object();

    [TestMethod]
    public void PrintLineWritesOutputTest()
    {
        List<string> lines =
        [
            "print_line hello"
        ];

        YesNtAssert.ContainsDebugOutput(lines, "hello");
    }

    [TestMethod]
    public void PrintWritesOutputTest()
    {
        List<string> lines =
        [
            "print hello"
        ];

        YesNtAssert.ContainsDebugOutput(lines, "hello");
    }

    [TestMethod]
    public void ClearThrowsInNonInteractiveConsoleTest()
    {
        List<string> lines =
        [
            "clear"
        ];

        _ = Assert.ThrowsException<IOException>(() => YesNtAssert.GetLastLine(lines));
    }

    [TestMethod]
    public void ReadLineReplacesTokenTest()
    {
        lock (ConsoleLock)
        {
            TextReader originalIn = Console.In;

            try
            {
                Console.SetIn(new StringReader("typed value" + Environment.NewLine));

                List<string> lines =
                [
                    "var value = %read_line",
                    "${value}"
                ];

                YesNtAssert.IsLastLineEqual(lines, "typed value");
            }
            finally
            {
                Console.SetIn(originalIn);
            }
        }
    }

    [TestMethod]
    public void ReadKeyCanBeInterruptedByStopTest()
    {
        YesNtInterpreter interpreter = new YesNtInterpreter();

        AutoResetEvent onDone= new AutoResetEvent(false);
        StringBuilder output = new StringBuilder();

        interpreter.OnDebugOutput += (s) => _ = output.Append(s);
        interpreter.OnLineExecuted += (e) =>
        {
            if (e is null)
            {
                _ = onDone.Set();
            }
        };

        List<string> lines =
        [
            "var value = %read_key"
        ];

        _ = Task.Run(() => interpreter.Execute(lines, true));

        Thread.Sleep(100);
        interpreter.Stop();

        _ = onDone.WaitOne(TimeSpan.FromSeconds(3));

        StringAssert.Contains(output.ToString(), "Terminated by external process");
    }
}

