using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Collections.Generic;

namespace YesNt.Interpreter.Tests;

[TestClass]
public class MapStatementsTests
{
    [TestMethod]
    public void MapCreatePutGetTest()
    {
        List<string> lines =
        [
            "map settings new",
            "map settings put a, 1",
            "map settings put b, 2",
            "map settings get b",
            "var result = %out",
            "${result}"
        ];

        YesNtAssert.IsLastLineEqual(lines, "2");
    }

    [TestMethod]
    public void MapPutOverwritesTest()
    {
        List<string> lines =
        [
            "map settings new",
            "map settings put a, 1",
            "map settings put a, 2",
            "map settings get a",
            "var result = %out",
            "${result}"
        ];

        YesNtAssert.IsLastLineEqual(lines, "2");
    }

    [TestMethod]
    public void MapHasTest()
    {
        List<string> lines =
        [
            "map settings new",
            "map settings put a, 1",
            "map settings has a",
            "var hasA = %out",
            "map settings has b",
            "var hasB = %out",
            "${hasA} ${hasB}"
        ];

        YesNtAssert.IsLastLineEqual(lines, "True False");
    }

    [TestMethod]
    public void MapRemoveTest()
    {
        List<string> lines =
        [
            "map settings new",
            "map settings put a, 1",
            "map settings put b, 2",
            "map settings remove a",
            "map settings size",
            "var n = %out",
            "map settings has a",
            "var hasA = %out",
            "${n} ${hasA}"
        ];

        YesNtAssert.IsLastLineEqual(lines, "1 False");
    }

    [TestMethod]
    public void MapRemoveMissingKeyNoOpTest()
    {
        List<string> lines =
        [
            "map settings new",
            "map settings put a, 1",
            "map settings remove b",
            "map settings size",
            "var n = %out",
            "${n}"
        ];

        YesNtAssert.IsLastLineEqual(lines, "1");
    }

    [TestMethod]
    public void MapSizeTest()
    {
        List<string> lines =
        [
            "map settings new",
            "map settings put a, 1",
            "map settings put b, 2",
            "map settings size",
            "var n = %out",
            "${n}"
        ];

        YesNtAssert.IsLastLineEqual(lines, "2");
    }

    [TestMethod]
    public void MapClearTest()
    {
        List<string> lines =
        [
            "map settings new",
            "map settings put a, 1",
            "map settings clear",
            "map settings size",
            "var n = %out",
            "${n}"
        ];

        YesNtAssert.IsLastLineEqual(lines, "0");
    }

    [TestMethod]
    public void MapDeleteTest()
    {
        List<string> lines =
        [
            "map settings new",
            "map settings put a, 1",
            "map settings delete",
            "map settings size"
        ];

        YesNtAssert.ContainsTerminationMessage(lines, "Map \"settings\" not found");
    }

    [TestMethod]
    public void MapMissingFailsTest()
    {
        List<string> lines =
        [
            "map missing size"
        ];

        YesNtAssert.ContainsTerminationMessage(lines, "Map \"missing\" not found");
    }

    [TestMethod]
    public void MapGetMissingKeyFailsTest()
    {
        List<string> lines =
        [
            "map settings new",
            "map settings get a"
        ];

        YesNtAssert.ContainsTerminationMessage(lines, "Key \"a\" not found");
    }

    [TestMethod]
    public void MapInvalidSyntaxFailsTest()
    {
        List<string> lines =
        [
            "map settings put"
        ];

        YesNtAssert.ContainsTerminationMessage(lines, "Invalid statement");
    }

    [TestMethod]
    public void MapPutExtraCommaFailsTest()
    {
        List<string> lines =
        [
            "map settings new",
            "map settings put test, abc, htuerthieu"
        ];

        YesNtAssert.ContainsTerminationMessage(lines, "Invalid syntax");
    }

    [TestMethod]
    public void MapScopeInsideFunctionTest()
    {
        List<string> lines =
        [
            "goto main",
            "func make:",
            "map data new",
            "map data put k, v",
            "map data get k",
            "push_out %out",
            "return",
            "label main:",
            "call make",
            "var result = %out",
            "${result}"
        ];

        YesNtAssert.IsLastLineEqual(lines, "v");
    }

    [TestMethod]
    public void MapDoesNotLeakToCallerTest()
    {
        List<string> lines =
        [
            "goto main",
            "func make:",
            "map data new",
            "map data put k, v",
            "return",
            "label main:",
            "call make",
            "map data size"
        ];

        YesNtAssert.ContainsTerminationMessage(lines, "Map \"data\" not found");
    }

    [TestMethod]
    public void MapPutValueWithSpacesTest()
    {
        List<string> lines =
        [
            "map settings new",
            "map settings put \"display name\", \"John Doe\"",
            "map settings get \"display name\"",
            "var result = %out eval",
            "${result}"
        ];

        YesNtAssert.IsLastLineEqual(lines, "John Doe");
    }

    [TestMethod]
    public void MapKeyWithSpacesTest()
    {
        List<string> lines =
        [
            "map settings new",
            "map settings put first name, John",
            "map settings get first name",
            "var result = %out",
            "${result}"
        ];

        YesNtAssert.IsLastLineEqual(lines, "John");
    }

    [TestMethod]
    public void MapValueWithCommaTest()
    {
        List<string> lines =
        [
            "map settings new",
            "map settings put name, \"Doe, John\"",
            "map settings get name",
            "var result = %out eval",
            "${result}"
        ];

        YesNtAssert.IsLastLineEqual(lines, "Doe, John");
    }
}
