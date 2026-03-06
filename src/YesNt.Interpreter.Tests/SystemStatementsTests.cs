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
            "exec whoami",
            "var exitCode = %out",
            "${exitCode}"
        ];

        YesNtAssert.IsLastLineEqual(lines, "0");
    }

    [TestMethod]
    public void ExecWithArgsRunsProcessTestWithArgument()
    {
        List<string> lines =
        [
            "exec whoami with /?",
            "var exitCode = %out",
            "var dummy = 0",
            "${dummy}"
        ];

        YesNtAssert.IsLastLineEqual(lines, "0");
    }

    [TestMethod]
    public void ExecWithInStackArgsRunsProcessTest()
    {
        List<string> lines =
        [
            "exec whoami",
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