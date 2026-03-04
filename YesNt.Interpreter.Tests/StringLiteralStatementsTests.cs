using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Collections.Generic;

namespace YesNt.Interpreter.Tests;

[TestClass]
public class StringLiteralStatementsTests
{
    [TestMethod]
    public void StringLiteralWithSpacesWorksTest()
    {
        List<string> lines =
        [
            "var msg = \"hello world\"",
            "${msg}"
        ];

        YesNtAssert.IsLastLineEqual(lines, "hello world");
    }

    [TestMethod]
    public void StringLiteralEscapesWorkTest()
    {
        List<string> lines =
        [
            "var msg = \"a\\n\\t\\\"b\"",
            "${msg}"
        ];

        YesNtAssert.IsLastLineEqual(lines, "a\n\t\"b");
    }

    [TestMethod]
    public void StringLiteralPreventsVariableInterpolationTest()
    {
        List<string> lines =
        [
            "var x = hidden",
            "print_line \"${x}\""
        ];

        YesNtAssert.ContainsDebugOutput(lines, "${x}");
    }

    [TestMethod]
    public void StringLiteralWorksWithListAddTest()
    {
        List<string> lines =
        [
            "list items new",
            "list items add \"hello world\"",
            "list items get 0",
            "var result = %out",
            "${result}"
        ];

        YesNtAssert.IsLastLineEqual(lines, "hello world");
    }

    [TestMethod]
    public void UnterminatedStringLiteralFailsTest()
    {
        List<string> lines =
        [
            "var msg = \"hello"
        ];

        YesNtAssert.ContainsTerminationMessage(lines, "Invalid string literal");
    }
}
