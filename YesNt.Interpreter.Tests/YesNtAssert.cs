using Microsoft.VisualStudio.TestTools.UnitTesting;

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

using YesNt.Interpreter.Runtime;

namespace YesNt.Interpreter.Tests;

internal static class YesNtAssert
{
    public static void IsLastLineEqual(List<string> lines, string expected, int timeout = 1000)
    {
        (DebugEventArgs? debugEventArgs, _) = ExecuteAndCapture(lines, timeout);
        Assert.AreEqual(expected, debugEventArgs?.CurrentLine);
    }

    public static void IsLineEqual(string line, string expected, int timeout = 1000)
    {
        List<string> lines =
        [
           line
        ];

        (DebugEventArgs? debugEventArgs, _) = ExecuteAndCapture(lines, timeout);
        Assert.AreEqual(expected, debugEventArgs?.CurrentLine);
    }

    public static void IsLineNotEqual(string line, string expected, int timeout = 1000)
    {
        List<string> lines =
        [
           line
        ];

        (DebugEventArgs? debugEventArgs, _) = ExecuteAndCapture(lines, timeout);
        Assert.AreNotEqual(expected, debugEventArgs?.CurrentLine);
    }

    public static void ContainsTerminationMessage(List<string> lines, string expectedMessageFragment, int timeout = 1000)
    {
        (_, string debugOutput) = ExecuteAndCapture(lines, timeout);

        StringAssert.Contains(debugOutput, expectedMessageFragment);
    }

    public static void ContainsDebugOutput(List<string> lines, string expectedFragment, int timeout = 1000)
    {
        (_, string debugOutput) = ExecuteAndCapture(lines, timeout);

        StringAssert.Contains(debugOutput, expectedFragment);
    }

    public static string? GetLastLine(List<string> lines, int timeout = 1000)
    {
        (DebugEventArgs? debugEventArgs, _) = ExecuteAndCapture(lines, timeout);
        return debugEventArgs?.CurrentLine;
    }

    public static void LastLineMatches(List<string> lines, string pattern, int timeout = 1000)
    {
        string? value = GetLastLine(lines, timeout);
        Assert.IsNotNull(value);
        StringAssert.Matches(value, new Regex(pattern));
    }

    private static (DebugEventArgs? LastDebugEvent, string DebugOutput) ExecuteAndCapture(List<string> lines, int timeout)
    {
        YesNtInterpreter yesNtInterpreter = new YesNtInterpreter();
        yesNtInterpreter.Initialize();

        AutoResetEvent onDone = new AutoResetEvent(false);
        DebugEventArgs? debugEventArgs = null;
        StringBuilder outputBuilder = new StringBuilder();

        yesNtInterpreter.OnLineExecuted += (er) =>
        {
            if (er is not null)
            {
                debugEventArgs = er;
            }
            else
            {
                _ = onDone.Set();
            }
        };

        yesNtInterpreter.OnDebugOutput += (s) =>
        {
            _ = outputBuilder.Append(s);
        };

        yesNtInterpreter.Execute(lines, true);

        _ = onDone.WaitOne(TimeSpan.FromMilliseconds(timeout));

        return (debugEventArgs, outputBuilder.ToString());
    }
}
