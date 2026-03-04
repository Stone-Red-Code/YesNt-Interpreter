using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Collections.Generic;

namespace YesNt.Interpreter.Tests;

[TestClass]
public class CodeFlowTests
{
    [TestMethod]
    public void FunctionTest()
    {
        List<string> lines =
        [
            "call yes",
            "func yes:",
            "global result = 1",
            "return",
            "${result}"
        ];
        YesNtAssert.IsLastLineEqual(lines, "1");
    }

    [TestMethod]
    public void LabelsTest()
    {
        List<string> lines =
        [
            "let result = 1",
            "goto yes",
            "let result = 0",
            "label yes:",
            "${result}"
        ];
        YesNtAssert.IsLastLineEqual(lines, "1");
    }

    [TestMethod]
    public void IfBlockTrueTest()
    {
        List<string> lines =
        [
            "let result = low",
            "if 6 > 5:",
            "let result = high",
            "end_if",
            "${result}"
        ];

        YesNtAssert.IsLastLineEqual(lines, "high");
    }

    [TestMethod]
    public void IfElseFalseBranchTest()
    {
        List<string> lines =
        [
            "let result = low",
            "if 6 < 5:",
            "let result = high",
            "else:",
            "let result = medium",
            "end_if",
            "${result}"
        ];

        YesNtAssert.IsLastLineEqual(lines, "medium");
    }

    [TestMethod]
    public void NestedIfElseTest()
    {
        List<string> lines =
        [
            "let result = 0",
            "if 1 == 1:",
            "if 2 == 3:",
            "let result = 1",
            "else:",
            "let result = 2",
            "end_if",
            "end_if",
            "${result}"
        ];

        YesNtAssert.IsLastLineEqual(lines, "2");
    }

    [TestMethod]
    public void WhileLoopTest()
    {
        List<string> lines =
        [
            "let i = 3",
            "while ${i} > 0:",
            "let i = ${i} - 1 calc",
            "end_while",
            "${i}"
        ];

        YesNtAssert.IsLastLineEqual(lines, "0");
    }

    [TestMethod]
    public void WhileSkipBodyWhenFalseTest()
    {
        List<string> lines =
        [
            "let i = 0",
            "while ${i} > 0:",
            "let i = 99",
            "end_while",
            "${i}"
        ];

        YesNtAssert.IsLastLineEqual(lines, "0");
    }

    [TestMethod]
    public void NestedWhileLoopTest()
    {
        List<string> lines =
        [
            "let outer = 2",
            "let count = 0",
            "while ${outer} > 0:",
            "let inner = 2",
            "while ${inner} > 0:",
            "let count = ${count} + 1 calc",
            "let inner = ${inner} - 1 calc",
            "end_while",
            "let outer = ${outer} - 1 calc",
            "end_while",
            "${count}"
        ];

        YesNtAssert.IsLastLineEqual(lines, "4");
    }
}
