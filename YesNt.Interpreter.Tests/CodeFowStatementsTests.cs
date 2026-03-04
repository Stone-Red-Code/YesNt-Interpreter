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
}
