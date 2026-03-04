using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Collections.Generic;

namespace YesNt.Interpreter.Tests;

[TestClass]
public class FunctionStatementsTests
{
    [TestMethod]
    public void FunctionCallWithInParameterTest()
    {
        List<string> lines =
        [
            "goto main",
            "func echo:",
            "global result = %in",
            "return",
            "label main:",
            "call echo with hello",
            "${result}"
        ];

        YesNtAssert.IsLastLineEqual(lines, "hello");
    }

    [TestMethod]
    public void HasInAndHasOutTokensTest()
    {
        List<string> lines =
        [
            "goto main",
            "func probe:",
            "global hasInBefore = %has_in",
            "let consume = %in",
            "global hasInAfter = %has_in",
            "push_out ${hasInBefore}",
            "push_out ${hasInAfter}",
            "return",
            "label main:",
            "call probe with x",
            "let hasOut = %has_out",
            "${hasOut}"
        ];

        YesNtAssert.IsLastLineEqual(lines, "True");
    }

    [TestMethod]
    public void OutParameterReadTest()
    {
        List<string> lines =
        [
            "goto main",
            "func make:",
            "push_out out_value",
            "return",
            "label main:",
            "call make with anything",
            "let value = %out",
            "${value}"
        ];

        YesNtAssert.IsLastLineEqual(lines, "out_value");
    }

    [TestMethod]
    public void OutParameterWithoutValueFailsTest()
    {
        List<string> lines =
        [
            "let x = %out"
        ];

        YesNtAssert.ContainsTerminationMessage(lines, "No out argument in stack");
    }

    [TestMethod]
    public void InParameterOutsideFunctionFailsTest()
    {
        List<string> lines =
        [
            "let x = %in"
        ];

        YesNtAssert.ContainsTerminationMessage(lines, "Statement not allowed outside of function");
    }

    [TestMethod]
    public void ReturnOutsideFunctionFailsTest()
    {
        List<string> lines =
        [
            "return"
        ];

        YesNtAssert.ContainsTerminationMessage(lines, "Statement not allowed outside of function");
    }

    [TestMethod]
    public void PushOutOutsideFunctionFailsTest()
    {
        List<string> lines =
        [
            "push_out value"
        ];

        YesNtAssert.ContainsTerminationMessage(lines, "Statement not allowed outside of function");
    }

    [TestMethod]
    public void FunctionWithoutColonFailsTest()
    {
        List<string> lines =
        [
            "func missing"
        ];

        YesNtAssert.ContainsTerminationMessage(lines, "Invalid statement");
    }

    [TestMethod]
    public void NestedFunctionDefinitionFailsTest()
    {
        List<string> lines =
        [
            "func outer:",
            "func inner:",
            "return"
        ];

        YesNtAssert.ContainsTerminationMessage(lines, "Nested functions are not allowed");
    }

    [TestMethod]
    public void ClearCallStackRunsTest()
    {
        List<string> lines =
        [
            "clear_call_stack",
            "let result = ok",
            "${result}"
        ];

        YesNtAssert.IsLastLineEqual(lines, "ok");
    }
}
