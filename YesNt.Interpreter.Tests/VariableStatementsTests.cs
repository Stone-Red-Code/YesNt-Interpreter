using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Collections.Generic;

namespace YesNt.Interpreter.Tests;

[TestClass]
public class VariableStatementsTests
{
    [TestMethod]
    public void LetAndReadVariableTest()
    {
        List<string> lines =
        [
            "var value = hi",
            "${value}"
        ];

        YesNtAssert.IsLastLineEqual(lines, "hi");
    }

    [TestMethod]
    public void GlobalVariableReadTest()
    {
        List<string> lines =
        [
            "global value = hi",
            "${value}"
        ];

        YesNtAssert.IsLastLineEqual(lines, "hi");
    }

    [TestMethod]
    public void LocalVariableOverridesGlobalTest()
    {
        List<string> lines =
        [
            "global value = global",
            "var value = local",
            "${value}"
        ];

        YesNtAssert.IsLastLineEqual(lines, "local");
    }

    [TestMethod]
    public void DeleteLocalVariableTest()
    {
        List<string> lines =
        [
            "var value = a",
            "delete value",
            "global value = b",
            "${value}"
        ];

        YesNtAssert.IsLastLineEqual(lines, "b");
    }

    [TestMethod]
    public void DeleteMissingVariableFailsTest()
    {
        List<string> lines =
        [
            "delete missing"
        ];

        YesNtAssert.ContainsTerminationMessage(lines, "Variable \"missing\" not found");
    }

    [TestMethod]
    public void LetInvalidSyntaxFailsTest()
    {
        List<string> lines =
        [
            "var a"
        ];

        YesNtAssert.ContainsTerminationMessage(lines, "Invalid statement");
    }

    [TestMethod]
    public void LetInvalidNameFailsTest()
    {
        List<string> lines =
        [
            "var a b = 1"
        ];

        YesNtAssert.ContainsTerminationMessage(lines, "Invalid syntax");
    }

    [TestMethod]
    public void MissingVariableReferenceFailsTest()
    {
        List<string> lines =
        [
            "${missing}"
        ];

        YesNtAssert.ContainsTerminationMessage(lines, "Variable \"missing\" not found");
    }
}

