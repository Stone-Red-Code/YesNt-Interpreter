using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Collections.Generic;

namespace YesNt.Interpreter.Tests;

[TestClass]
public class SystemStatementsTests
{
    [TestMethod]
    public void ExecWithArgsRunsProcessTest()
    {
        List<string> lines =
        [
            "exec cmd with /c,echo yesnt",
            "var exitCode = %out",
            "${exitCode}"
        ];

        YesNtAssert.IsLastLineEqual(lines, "0");
    }

    [TestMethod]
    public void ExecWithInStackArgsRunsProcessTest()
    {
        List<string> lines =
        [
            "push_in /c",
            "push_in echo yesnt",
            "exec cmd",
            "var exitCode = %out",
            "${exitCode}"
        ];

        YesNtAssert.IsLastLineEqual(lines, "0");
    }

    [TestMethod]
    public void ExecInvalidProgramFailsTest()
    {
        List<string> lines =
        [
            "exec does_not_exist_abc_xyz"
        ];

        YesNtAssert.ContainsTerminationMessage(lines, "Failed to start \"does_not_exist_abc_xyz\"");
    }
}