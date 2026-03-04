using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Collections.Generic;

namespace YesNt.Interpreter.Tests;

[TestClass]
public class ListStatementsTests
{
    [TestMethod]
    public void ListCreateAddGetTest()
    {
        List<string> lines =
        [
            "list items new",
            "list items add a",
            "list items add b",
            "list items get 1",
            "var result = %out",
            "${result}"
        ];

        YesNtAssert.IsLastLineEqual(lines, "b");
    }

    [TestMethod]
    public void ListSetAndInsertTest()
    {
        List<string> lines =
        [
            "list items new",
            "list items add a",
            "list items add c",
            "list items insert 1 b",
            "list items set 2 d",
            "list items get 2",
            "var result = %out",
            "${result}"
        ];

        YesNtAssert.IsLastLineEqual(lines, "d");
    }

    [TestMethod]
    public void ListRemoveAndLengthTest()
    {
        List<string> lines =
        [
            "list items new",
            "list items add a",
            "list items add b",
            "list items add c",
            "list items remove 1",
            "list items length",
            "var len = %out",
            "${len}"
        ];

        YesNtAssert.IsLastLineEqual(lines, "2");
    }

    [TestMethod]
    public void ListClearTest()
    {
        List<string> lines =
        [
            "list items new",
            "list items add a",
            "list items clear",
            "list items length",
            "var len = %out",
            "${len}"
        ];

        YesNtAssert.IsLastLineEqual(lines, "0");
    }

    [TestMethod]
    public void ListDeleteTest()
    {
        List<string> lines =
        [
            "list items new",
            "list items delete",
            "list items length"
        ];

        YesNtAssert.ContainsTerminationMessage(lines, "List \"items\" not found");
    }

    [TestMethod]
    public void ListMissingFailsTest()
    {
        List<string> lines =
        [
            "list missing get 0"
        ];

        YesNtAssert.ContainsTerminationMessage(lines, "List \"missing\" not found");
    }

    [TestMethod]
    public void ListInvalidIndexFailsTest()
    {
        List<string> lines =
        [
            "list items new",
            "list items add a",
            "list items get 3"
        ];

        YesNtAssert.ContainsTerminationMessage(lines, "Index 3 out of range");
    }

    [TestMethod]
    public void ListInvalidSyntaxFailsTest()
    {
        List<string> lines =
        [
            "list items add"
        ];

        YesNtAssert.ContainsTerminationMessage(lines, "Invalid statement");
    }

    [TestMethod]
    public void ListScopeInsideFunctionTest()
    {
        List<string> lines =
        [
            "goto main",
            "func make:",
            "list items new",
            "list items add x",
            "list items get 0",
            "push_out %out",
            "return",
            "label main:",
            "call make",
            "var result = %out",
            "${result}"
        ];

        YesNtAssert.IsLastLineEqual(lines, "x");
    }

    [TestMethod]
    public void ListAddWithSpacesTest()
    {
        List<string> lines =
        [
            "list items new",
            "list items add hello~spcworld",
            "list items get 0",
            "var result = %out eval",
            "${result}"
        ];

        YesNtAssert.IsLastLineEqual(lines, "hello world");
    }
}
