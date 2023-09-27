using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Collections.Generic;

namespace YesNt.Interpreter.Tests;

[TestClass]
public class CodeFlowTests
{
    [TestMethod]
    public void FunctionTest()
    {
        List<string> lines = new List<string>()
        {
            "cal yes",
            "fnc yes",
            "!<result = 1",
            "ret",
            ">result"
        };
        YesNtAssert.IsLastLineEqual(lines, "1");
    }

    [TestMethod]
    public void LabelsTest()
    {
        List<string> lines = new List<string>()
        {
            "<result = 1",
            "jmp yes",
            "<result = 0",
            "lbl yes",
            ">result"
        };
        YesNtAssert.IsLastLineEqual(lines, "1");
    }

    [TestMethod]
    public void CalculationsTest()
    {
        YesNtAssert.IsLineEqual("10 * 10 !calc", 100.ToString());
    }
}