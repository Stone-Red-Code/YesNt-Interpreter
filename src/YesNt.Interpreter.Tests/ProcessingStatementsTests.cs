using Microsoft.VisualStudio.TestTools.UnitTesting;

using System;
using System.Collections.Generic;
using System.IO;

namespace YesNt.Interpreter.Tests;

[TestClass]
public class ProcessingStatementsTests
{
    [TestMethod]
    public void MultiplicationTest()
    {
        YesNtAssert.IsLineEqual("10 * 10 calc", "100");
    }

    [TestMethod]
    public void DivisionTest()
    {
        YesNtAssert.IsLineEqual("90 / 4 calc", "22.5");
    }

    [TestMethod]
    public void AdditionTest()
    {
        YesNtAssert.IsLineEqual("10 + 10 calc", "20");
    }

    [TestMethod]
    public void SubtractionTest()
    {
        YesNtAssert.IsLineEqual("10 - 10 calc", "0");
    }

    [TestMethod]
    public void ModulusTest()
    {
        YesNtAssert.IsLineEqual("10 % 3 calc", "1");
    }

    [TestMethod]
    public void ExponentiationTest()
    {
        YesNtAssert.IsLineEqual("2 ^ 3 calc", "8");
    }

    [TestMethod]
    public void EvalRawTildeSequenceIsLiteralTest()
    {
        YesNtAssert.IsLineEqual("hello~nliworld eval", "hello~nliworld");
    }

    [TestMethod]
    public void EvalDecodesStringLiteralEscapesTest()
    {
        List<string> lines =
        [
            "var x = \"hello\\nworld\"",
            "${x} eval"
        ];

        YesNtAssert.IsLastLineEqual(lines, "hello\nworld");
    }

    [TestMethod]
    public void CalcRespectsPrecedenceTest()
    {
        YesNtAssert.IsLineEqual("2 + 3 * 4 calc", "14");
    }

    [TestMethod]
    public void CalcParenthesesOverridePrecedenceTest()
    {
        YesNtAssert.IsLineEqual("(2 + 3) * 4 calc", "20");
    }

    [TestMethod]
    public void SleepZeroIsValidTest()
    {
        List<string> lines =
        [
            "sleep 0",
            "var result = ok",
            "${result}"
        ];

        YesNtAssert.IsLastLineEqual(lines, "ok");
    }

    [TestMethod]
    public void SleepInvalidValueFailsTest()
    {
        List<string> lines =
        [
            "sleep nope"
        ];

        YesNtAssert.ContainsTerminationMessage(lines, "\"nope\" is not a valid time-out value");
    }

    [TestMethod]
    public void SleepRunsAndContinuesTest()
    {
        List<string> lines =
        [
            "sleep 5",
            "var result = ok",
            "${result}"
        ];

        YesNtAssert.IsLastLineEqual(lines, "ok");
    }

    [TestMethod]
    public void LengthPushesOutParameterTest()
    {
        List<string> lines =
        [
            "length hello",
            "var value = %out",
            "${value}"
        ];

        YesNtAssert.IsLastLineEqual(lines, "5");
    }

    [TestMethod]
    public void ImportLoadsScriptTest()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"yesnt-import-{Guid.NewGuid():N}.ynt");

        try
        {
            File.WriteAllText(tempFile, "var imported = yes");

            List<string> lines =
            [
                $"import {tempFile}",
                "${imported}"
            ];

            YesNtAssert.IsLastLineEqual(lines, "yes");
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [TestMethod]
    public void ImportMissingFileFailsTest()
    {
        List<string> lines =
        [
            $"import {Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))}.ynt"
        ];

        YesNtAssert.ContainsTerminationMessage(lines, "Could not find file");
    }

    [TestMethod]
    public void TaskCanUpdateGlobalVariableTest()
    {
        List<string> lines =
        [
            "global result = 0",
            "global result = 1 task",
            "while ${result} == 0:",
            "sleep 10",
            "end_while",
            "${result}"
        ];

        YesNtAssert.IsLastLineEqual(lines, "1", timeout: 3000);
    }

    [TestMethod]
    public void MultipleTasksRunConcurrentlyTest()
    {
        List<string> lines =
        [
            "global a = 0",
            "global b = 0",
            "global a = 1 task",
            "global b = 2 task",
            "while ${a} == 0:",
            "sleep 10",
            "end_while",
            "while ${b} == 0:",
            "sleep 10",
            "end_while",
            "${b}"
        ];

        YesNtAssert.IsLastLineEqual(lines, "2", timeout: 3000);
    }
}