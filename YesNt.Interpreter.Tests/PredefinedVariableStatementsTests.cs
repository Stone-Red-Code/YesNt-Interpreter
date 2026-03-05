using Microsoft.VisualStudio.TestTools.UnitTesting;

using System;
using System.Collections.Generic;

namespace YesNt.Interpreter.Tests;

[TestClass]
public class PredefinedVariableStatementsTests
{
    [TestMethod]
    public void TimeTokenProducesUnixTimestampTest()
    {
        List<string> lines =
        [
            "%time"
        ];

        string? value = YesNtAssert.GetLastLine(lines);
        Assert.IsNotNull(value);
        Assert.IsTrue(long.TryParse(value, out long parsed));

        long now = DateTimeOffset.Now.ToUnixTimeSeconds();
        Assert.IsTrue(Math.Abs(now - parsed) < 10);
    }

    [TestMethod]
    public void OsTokenProducesValueTest()
    {
        List<string> lines =
        [
            "%os"
        ];

        string? value = YesNtAssert.GetLastLine(lines);
        Assert.IsFalse(string.IsNullOrWhiteSpace(value));
    }

    [TestMethod]
    public void CpuTokenProducesValueTest()
    {
        List<string> lines =
        [
            "%cpu"
        ];

        string? value = YesNtAssert.GetLastLine(lines);
        Assert.IsFalse(string.IsNullOrWhiteSpace(value));
    }

    [TestMethod]
    public void Is64TokenProducesBooleanTest()
    {
        List<string> lines =
        [
            "%is64"
        ];

        string? value = YesNtAssert.GetLastLine(lines);
        Assert.AreEqual(Environment.Is64BitOperatingSystem.ToString(), value);
    }

    [TestMethod]
    public void PiTokenProducesPiTest()
    {
        List<string> lines =
        [
            "%pi"
        ];

        string? value = YesNtAssert.GetLastLine(lines);
        Assert.IsNotNull(value);
        Assert.IsTrue(double.TryParse(value, out double parsed));
        Assert.IsTrue(Math.Abs(parsed - Math.PI) < 0.001d);
    }

    [TestMethod]
    public void RandTokenProducesIntegerTest()
    {
        List<string> lines =
        [
            "%rand"
        ];

        string? value = YesNtAssert.GetLastLine(lines);
        Assert.IsNotNull(value);
        Assert.IsTrue(int.TryParse(value, out int parsed));
        Assert.IsTrue(parsed >= 32767);
    }

    [TestMethod]
    public void MultipleRandTokensAreReplacedTest()
    {
        List<string> lines =
        [
            "%rand %rand"
        ];

        string? value = YesNtAssert.GetLastLine(lines);
        Assert.IsNotNull(value);

        string[] parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        Assert.AreEqual(2, parts.Length);
        Assert.IsTrue(int.TryParse(parts[0], out _));
        Assert.IsTrue(int.TryParse(parts[1], out _));
    }
}