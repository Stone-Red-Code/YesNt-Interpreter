using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Collections.Generic;

using YesNt.Interpreter.Attributes;
using YesNt.Interpreter.Enums;
using YesNt.Interpreter.Runtime;

namespace YesNt.Interpreter.Tests;

[TestClass]
public class AddStatementTests
{
    [TestMethod]
    public void AddStatementStartOfLineExecutesHandlerTest()
    {
        List<string> lines =
        [
            "my_command hello"
        ];

        string? capturedArgs = null;

        YesNtAssert.GetLastLineWithSetup(lines, interpreter =>
        {
            interpreter.AddStatement("my_command", SearchMode.StartOfLine, SpaceAround.End, args =>
            {
                capturedArgs = args;
            });
        });

        Assert.AreEqual("hello", capturedArgs);
    }

    [TestMethod]
    public void AddStatementConvenienceOverloadHandlerIsCalledTest()
    {
        List<string> lines =
        [
            "custom_cmd world"
        ];

        bool handlerCalled = false;

        YesNtAssert.GetLastLineWithSetup(lines, interpreter =>
        {
            interpreter.AddStatement("custom_cmd", SearchMode.StartOfLine, SpaceAround.End, _ =>
            {
                handlerCalled = true;
            });
        });

        Assert.IsTrue(handlerCalled);
    }

    [TestMethod]
    public void AddStatementAttributeOverloadHandlerIsCalledTest()
    {
        List<string> lines =
        [
            "attr_cmd test"
        ];

        bool handlerCalled = false;

        YesNtAssert.GetLastLineWithSetup(lines, interpreter =>
        {
            var attr = new StatementAttribute("attr_cmd", SearchMode.StartOfLine, SpaceAround.End);
            interpreter.AddStatement(attr, _ =>
            {
                handlerCalled = true;
            });
        });

        Assert.IsTrue(handlerCalled);
    }

    [TestMethod]
    public void AddStatementExactSearchModeTest()
    {
        List<string> lines =
        [
            "exact_cmd"
        ];

        bool handlerCalled = false;

        YesNtAssert.GetLastLineWithSetup(lines, interpreter =>
        {
            interpreter.AddStatement("exact_cmd", SearchMode.Exact, SpaceAround.None, _ =>
            {
                handlerCalled = true;
            });
        });

        Assert.IsTrue(handlerCalled);
    }

    [TestMethod]
    public void AddStatementContainsSearchModeTest()
    {
        List<string> lines =
        [
            "prefix ~mark~ suffix"
        ];

        bool handlerCalled = false;

        YesNtAssert.GetLastLineWithSetup(lines, interpreter =>
        {
            interpreter.AddStatement(" ~mark~ ", SearchMode.Contains, SpaceAround.None, _ =>
            {
                handlerCalled = true;
            });
        });

        Assert.IsTrue(handlerCalled);
    }

    [TestMethod]
    public void AddStatementEndOfLineSearchModeTest()
    {
        List<string> lines =
        [
            "some text !end"
        ];

        bool handlerCalled = false;

        YesNtAssert.GetLastLineWithSetup(lines, interpreter =>
        {
            interpreter.AddStatement(" !end", SearchMode.EndOfLine, SpaceAround.None, _ =>
            {
                handlerCalled = true;
            });
        });

        Assert.IsTrue(handlerCalled);
    }

    [TestMethod]
    public void AddStatementReceivesCorrectArgsTest()
    {
        List<string> lines =
        [
            "capture_cmd the quick brown fox"
        ];

        string? capturedArgs = null;

        YesNtAssert.GetLastLineWithSetup(lines, interpreter =>
        {
            interpreter.AddStatement("capture_cmd", SearchMode.StartOfLine, SpaceAround.End, args =>
            {
                capturedArgs = args;
            });
        });

        Assert.AreEqual("the quick brown fox", capturedArgs);
    }

    [TestMethod]
    public void AddStatementWorksAlongsideBuiltinStatementsTest()
    {
        List<string> lines =
        [
            "custom_log first",
            "print_line second"
        ];

        bool customCalled = false;

        YesNtAssert.ContainsDebugOutputWithSetup(lines, "second", interpreter =>
        {
            interpreter.AddStatement("custom_log", SearchMode.StartOfLine, SpaceAround.End, _ =>
            {
                customCalled = true;
            });
        });

        Assert.IsTrue(customCalled);
    }

    [TestMethod]
    public void AddStatementUnknownStatementFailsTest()
    {
        List<string> lines =
        [
            "unknown_command foo"
        ];

        YesNtAssert.ContainsTerminationMessage(lines, "Invalid statement");
    }

    [TestMethod]
    public void AddStatementWithHighPriorityRunsBeforeNormalTest()
    {
        List<string> lines =
        [
            "priority_cmd arg"
        ];

        int callOrder = 0;
        int highPriorityOrder = -1;
        int normalPriorityOrder = -1;

        YesNtAssert.GetLastLineWithSetup(lines, interpreter =>
        {
            interpreter.AddStatement(
                new StatementAttribute("priority_cmd", SearchMode.StartOfLine, SpaceAround.End) { Priority = Priority.High },
                _ => highPriorityOrder = callOrder++);

            interpreter.AddStatement(
                new StatementAttribute("priority_cmd", SearchMode.StartOfLine, SpaceAround.End) { Priority = Priority.Normal },
                _ => normalPriorityOrder = callOrder++);
        });

        Assert.IsTrue(highPriorityOrder < normalPriorityOrder, "High priority statement should execute before Normal priority");
    }
}
