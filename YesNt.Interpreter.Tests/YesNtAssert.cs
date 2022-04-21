using Microsoft.VisualStudio.TestTools.UnitTesting;

using System;
using System.Collections.Generic;
using System.Threading;

using YesNt.Interpreter.Runtime;

namespace YesNt.Interpreter.Tests;

internal static class YesNtAssert
{
    private static readonly YesNtInterpreter yesNtInterpreter = new YesNtInterpreter();

    static YesNtAssert()
    {
        yesNtInterpreter.Initialize();
    }

    public static void IsLastLineEqual(List<string> lines, string expected, int timeout = 1000)
    {
        AutoResetEvent onDone = new AutoResetEvent(false);

        DebugEventArgs debugEventArgs = new DebugEventArgs();
        yesNtInterpreter.OnLineExecuted += (er) =>
        {
            debugEventArgs = er ?? debugEventArgs;

            if (er is null)
            {
                onDone.Set();
            }
        };

        yesNtInterpreter.Execute(lines, true);

        _ = onDone.WaitOne(TimeSpan.FromSeconds(timeout));

        Assert.AreEqual(expected, debugEventArgs.CurrentLine);
    }

    public static void IsLineEqual(string line, string expected, int timeout = 1000)
    {
        AutoResetEvent onDone = new AutoResetEvent(false);
        List<string> lines = new List<string>()
        {
           line
        };

        DebugEventArgs debugEventArgs = new DebugEventArgs();
        yesNtInterpreter.OnLineExecuted += (er) =>
        {
            debugEventArgs = er ?? debugEventArgs;
            onDone.Set();
        };

        yesNtInterpreter.Execute(lines, true);

        _ = onDone.WaitOne(TimeSpan.FromMilliseconds(timeout));

        Assert.AreEqual(expected, debugEventArgs.CurrentLine);
    }
}