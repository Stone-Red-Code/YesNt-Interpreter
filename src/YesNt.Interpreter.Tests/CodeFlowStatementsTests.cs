using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Collections.Generic;

namespace YesNt.Interpreter.Tests;

[TestClass]
public class CodeFlowStatementsTests
{
    [TestMethod]
    public void ExitStopsExecutionTest()
    {
        List<string> lines =
        [
            "var result = before",
            "exit",
            "var result = after",
            "${result}"
        ];

        YesNtAssert.ContainsTerminationMessage(lines, "Planned termination by code");
    }

    [TestMethod]
    public void AbortAllStopsExecutionTest()
    {
        List<string> lines =
        [
            "abort_all",
            "var result = after",
            "${result}"
        ];

        YesNtAssert.ContainsTerminationMessage(lines, "Canceling all tasks");
    }

    [TestMethod]
    public void ThrowTerminatesWithErrorFlagTest()
    {
        List<string> lines =
        [
            "throw bad"
        ];

        YesNtAssert.ContainsTerminationMessage(lines, "with the message: bad");
    }

    [TestMethod]
    public void ErrorTerminatesWithMessageTest()
    {
        List<string> lines =
        [
            "error soft"
        ];

        YesNtAssert.ContainsTerminationMessage(lines, "with the message: soft");
    }

    [TestMethod]
    public void MissingLabelFailsTest()
    {
        List<string> lines =
        [
            "goto nowhere"
        ];

        YesNtAssert.ContainsTerminationMessage(lines, "Label \"nowhere\" not found");
    }

    [TestMethod]
    public void MissingFunctionFailsTest()
    {
        List<string> lines =
        [
            "call nowhere"
        ];

        YesNtAssert.ContainsTerminationMessage(lines, "Function \"nowhere\" not found");
    }
}