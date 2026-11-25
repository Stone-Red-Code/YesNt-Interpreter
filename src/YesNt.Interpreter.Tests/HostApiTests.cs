using Microsoft.VisualStudio.TestTools.UnitTesting;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

using YesNt.Interpreter.Enums;
using YesNt.Interpreter.Runtime;

namespace YesNt.Interpreter.Tests;

/// <summary>
/// Tests for what an embedding application gives and gets through the interpreter's API.
/// </summary>
[TestClass]
public class HostApiTests
{
    // A console and file system in memory. A null line or key in the queue is "not yet".
    private sealed class TestHost : IYesNtHost
    {
        public StringBuilder Output { get; } = new();
        public Queue<string?> Lines { get; } = new();
        public Queue<char?> Keys { get; } = new();
        public Dictionary<string, string[]> Files { get; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> Forbidden { get; } = new(StringComparer.OrdinalIgnoreCase);
        public int Clears { get; private set; }

        public void Write(string text)
        {
            lock (Output)
            {
                _ = Output.Append(text);
            }
        }

        public InputStatus TryReadLine(out string line)
        {
            line = string.Empty;
            if (Lines.Count == 0)
            {
                return InputStatus.Closed;
            }

            string? next = Lines.Dequeue();
            if (next is null)
            {
                return InputStatus.Pending;
            }

            line = next;
            return InputStatus.Ready;
        }

        public InputStatus TryReadKey(out char key)
        {
            key = default;
            if (Keys.Count == 0)
            {
                return InputStatus.Closed;
            }

            char? next = Keys.Dequeue();
            if (next is null)
            {
                return InputStatus.Pending;
            }

            key = next.Value;
            return InputStatus.Ready;
        }

        public void Clear()
        {
            Clears++;
        }

        public string[] ReadScript(string path)
        {
            string name = Path.GetFileName(path);
            if (Forbidden.Contains(name))
            {
                throw new UnauthorizedAccessException(name);
            }

            return Files.TryGetValue(name, out string[]? lines) ? lines : throw new FileNotFoundException(name);
        }

        public string Text
        {
            get
            {
                lock (Output)
                {
                    return Output.ToString();
                }
            }
        }
    }

    private static (YesNtInterpreter Interpreter, TestHost Host) Create()
    {
        TestHost host = new();
        YesNtInterpreter interpreter = new() { Host = host };
        return (interpreter, host);
    }

    // Steps until the script ends, as a host's loop would (never more than a few seconds).
    private static void StepToEnd(YesNtInterpreter interpreter)
    {
        DateTime giveUp = DateTime.UtcNow.AddSeconds(5);
        while (interpreter.IsRunning && DateTime.UtcNow < giveUp)
        {
            if (interpreter.Step(10) == StepResult.Waiting)
            {
                Thread.Sleep(1);
            }
        }

        Assert.IsFalse(interpreter.IsRunning, "The script did not end.");
    }

    // --- SetOut ---

    [TestMethod]
    public void SetOutGivesTheLastValueFirstTest()
    {
        (YesNtInterpreter interpreter, TestHost host) = Create();
        interpreter.AddStatement("two_values", SearchMode.Exact, SpaceAround.None, (_, context) => context.SetOut("first", "second"));

        interpreter.Execute(["two_values", "print_line %out", "print_line %out"]);

        StringAssert.StartsWith(host.Text, "second" + Environment.NewLine + "first" + Environment.NewLine);
    }

    [TestMethod]
    public void SetOutReplacesWhatWasOnTheStackTest()
    {
        (YesNtInterpreter interpreter, TestHost host) = Create();
        interpreter.AddStatement("one_value", SearchMode.Exact, SpaceAround.None, (_, context) => context.SetOut("new"));

        interpreter.Execute(["length abc", "one_value", "print_line %out", "print_line %has_out"]);

        StringAssert.StartsWith(host.Text, "new" + Environment.NewLine + "False" + Environment.NewLine);
    }

    [TestMethod]
    public void SetOutKeepsSpacesAndKeywordsInTheValueTest()
    {
        (YesNtInterpreter interpreter, TestHost host) = Create();
        interpreter.AddStatement("text_value", SearchMode.Exact, SpaceAround.None, (_, context) => context.SetOut("a, b goto calc"));

        interpreter.Execute(["text_value", "var text = %out", "print_line ${text}"]);

        StringAssert.StartsWith(host.Text, "a, b goto calc" + Environment.NewLine);
    }

    [TestMethod]
    public void LengthLeavesArgumentsForALaterCallTest()
    {
        (YesNtInterpreter interpreter, TestHost host) = Create();

        interpreter.Execute(["push_in Alice", "length abc", "print_line %out", "call greet", "exit", "func greet:", "print_line Hello %in", "end_func"]);

        StringAssert.StartsWith(host.Text, "3" + Environment.NewLine + "Hello Alice" + Environment.NewLine);
    }

    // --- Host ---

    [TestMethod]
    public void OutputGoesToTheHostTest()
    {
        (YesNtInterpreter interpreter, TestHost host) = Create();

        interpreter.Execute(["print hello ", "print_line world"]);

        StringAssert.StartsWith(host.Text, "helloworld" + Environment.NewLine);
    }

    [TestMethod]
    public void ClearClearsTheHostTest()
    {
        (YesNtInterpreter interpreter, TestHost host) = Create();

        interpreter.Execute(["clear"]);

        Assert.AreEqual(1, host.Clears);
    }

    [TestMethod]
    public void ScriptAndImportsAreReadThroughTheHostTest()
    {
        (YesNtInterpreter interpreter, TestHost host) = Create();
        host.Files["main.ynt"] = ["import lib", "print_line ${greeting}"];
        host.Files["lib.ynt"] = ["var greeting = from lib"];

        interpreter.Execute("main.ynt");

        StringAssert.StartsWith(host.Text, "from lib" + Environment.NewLine);
    }

    [TestMethod]
    public void MissingScriptDoesNotRunTest()
    {
        (YesNtInterpreter interpreter, _) = Create();

        interpreter.Prepare("missing.ynt");

        Assert.IsFalse(interpreter.IsRunning);
    }

    [TestMethod]
    public void MissingImportEndsTheScriptTest()
    {
        (YesNtInterpreter interpreter, TestHost host) = Create();

        interpreter.Execute(["import missing"]);

        StringAssert.Contains(host.Text, "Could not find file");
    }

    [TestMethod]
    public void ForbiddenImportEndsTheScriptTest()
    {
        (YesNtInterpreter interpreter, TestHost host) = Create();
        host.Files["secret.ynt"] = ["print_line leaked"];
        _ = host.Forbidden.Add("secret.ynt");

        interpreter.Execute(["import secret"]);

        Assert.IsFalse(host.Text.Contains("leaked"));
        StringAssert.Contains(host.Text, "Could not load file");
    }

    [TestMethod]
    public void ReadLineTakesTheHostsLineTest()
    {
        (YesNtInterpreter interpreter, TestHost host) = Create();
        host.Lines.Enqueue("Ada");

        interpreter.Execute(["var name = %read_line", "print_line Hello ${name}"]);

        StringAssert.StartsWith(host.Text, "Hello Ada" + Environment.NewLine);
    }

    [TestMethod]
    public void ClosedInputEndsTheScriptTest()
    {
        (YesNtInterpreter interpreter, TestHost host) = Create();

        interpreter.Execute(["var name = %read_line", "print_line not reached"]);

        Assert.IsFalse(host.Text.Contains("not reached"));
        StringAssert.Contains(host.Text, "Terminated by external process");
    }

    // --- Waiting ---

    [TestMethod]
    public void ReadLineWaitsForInputWithoutBlockingTest()
    {
        (YesNtInterpreter interpreter, TestHost host) = Create();
        host.Lines.Enqueue(null);
        host.Lines.Enqueue("Ada");
        interpreter.Prepare(["var name = %read_line", "print_line Hello ${name}"]);

        Assert.AreEqual(StepResult.Waiting, interpreter.Step(10));
        StepToEnd(interpreter);

        StringAssert.StartsWith(host.Text, "Hello Ada" + Environment.NewLine);
    }

    [TestMethod]
    public void InputAlreadyReadIsKeptWhileWaitingForMoreTest()
    {
        (YesNtInterpreter interpreter, TestHost host) = Create();
        host.Lines.Enqueue("first");
        host.Lines.Enqueue(null);
        host.Lines.Enqueue("second");
        interpreter.Prepare(["print_line %read_line and %read_line"]);

        Assert.AreEqual(StepResult.Waiting, interpreter.Step(10));
        StepToEnd(interpreter);

        StringAssert.StartsWith(host.Text, "first and second" + Environment.NewLine);
    }

    [TestMethod]
    public void WaitingLineDoesNotTakeOutputTwiceTest()
    {
        (YesNtInterpreter interpreter, TestHost host) = Create();
        host.Lines.Enqueue(null);
        host.Lines.Enqueue("typed");
        interpreter.Prepare(["push_in unused", "length abc", "print_line %out %read_line"]);

        StepToEnd(interpreter);

        StringAssert.StartsWith(host.Text, "3 typed" + Environment.NewLine);
    }

    [TestMethod]
    public void ReadKeyWaitsForAKeyTest()
    {
        (YesNtInterpreter interpreter, TestHost host) = Create();
        host.Keys.Enqueue(null);
        host.Keys.Enqueue('y');
        interpreter.Prepare(["print_line %read_key"]);

        Assert.AreEqual(StepResult.Waiting, interpreter.Step(10));
        StepToEnd(interpreter);

        StringAssert.StartsWith(host.Text, "y" + Environment.NewLine);
    }

    [TestMethod]
    public void SleepWaitsWithoutBlockingTest()
    {
        (YesNtInterpreter interpreter, TestHost host) = Create();
        interpreter.Prepare(["sleep 100", "print_line awake"]);

        DateTime start = DateTime.UtcNow;
        _ = interpreter.Step(1);
        Assert.AreEqual(StepResult.Waiting, interpreter.Step(10));
        Assert.IsTrue(DateTime.UtcNow - start < TimeSpan.FromMilliseconds(90), "Step blocked during the sleep.");

        StepToEnd(interpreter);

        Assert.IsTrue(DateTime.UtcNow - start >= TimeSpan.FromMilliseconds(100));
        StringAssert.StartsWith(host.Text, "awake" + Environment.NewLine);
    }

    [TestMethod]
    public void SleepStillWaitsInExecuteTest()
    {
        (YesNtInterpreter interpreter, _) = Create();

        DateTime start = DateTime.UtcNow;
        interpreter.Execute(["sleep 50"]);

        Assert.IsTrue(DateTime.UtcNow - start >= TimeSpan.FromMilliseconds(50));
    }

    [TestMethod]
    public void CustomStatementWaitsUntilReadyTest()
    {
        (YesNtInterpreter interpreter, TestHost host) = Create();
        bool ready = false;
        interpreter.AddStatement("wait_for_host", SearchMode.Exact, SpaceAround.None, (_, context) => context.WaitUntil(() => ready));
        interpreter.Prepare(["wait_for_host", "print_line done"]);

        _ = interpreter.Step(10);
        Assert.AreEqual(StepResult.Waiting, interpreter.Step(10));
        Assert.AreEqual(string.Empty, host.Text);

        ready = true;
        StepToEnd(interpreter);

        StringAssert.StartsWith(host.Text, "done" + Environment.NewLine);
    }

    [TestMethod]
    public void StoppingEndsAWaitTest()
    {
        (YesNtInterpreter interpreter, _) = Create();
        interpreter.Prepare(["sleep 100000"]);
        _ = interpreter.Step(10);

        interpreter.Stop();

        Assert.AreEqual(StepResult.Finished, interpreter.Step(10));
    }

    // --- Tasks inherit the script's setup ---

    // Waits for a task on its own thread to write the text (never more than a few seconds).
    private static string WaitForText(TestHost host, string text)
    {
        DateTime giveUp = DateTime.UtcNow.AddSeconds(5);
        while (!host.Text.Contains(text) && DateTime.UtcNow < giveUp)
        {
            Thread.Sleep(5);
        }

        StringAssert.Contains(host.Text, text);
        return host.Text;
    }

    [TestMethod]
    public void TaskHasCustomStatementsTest()
    {
        (YesNtInterpreter interpreter, TestHost host) = Create();
        using ManualResetEventSlim taskRan = new();
        interpreter.AddStatement("mark", SearchMode.Exact, SpaceAround.None, _ => taskRan.Set());

        interpreter.Execute(["call worker task", "exit", "func worker:", "mark", "exit", "end_func"]);

        Assert.IsTrue(taskRan.Wait(TimeSpan.FromSeconds(3)), host.Text);
    }

    [TestMethod]
    public void TaskGetsItsOwnContextTest()
    {
        (YesNtInterpreter interpreter, TestHost host) = Create();
        interpreter.AddStatement("where", SearchMode.Exact, SpaceAround.None, (_, context) => context.SetOut(context.Variables.ContainsKey("inside") ? "task" : "script"));

        interpreter.Execute(["call worker task", "where", "print_line script sees %out", "exit", "func worker:", "var inside = 1", "where", "print_line task sees %out", "exit", "end_func"]);

        _ = WaitForText(host, "task sees task");
        StringAssert.Contains(host.Text, "script sees script");
    }

    [TestMethod]
    public void TaskCannotUseARemovedStatementTest()
    {
        (YesNtInterpreter interpreter, TestHost host) = Create();
        interpreter.RemoveStatement("length");

        interpreter.Execute(["call worker task", "exit", "func worker:", "length abc", "print_line escaped", "exit", "end_func"]);

        string text = WaitForText(host, "Invalid statement");
        Assert.IsFalse(text.Contains("escaped"));
    }

    [TestMethod]
    public void TaskKeepsADisabledStatementDisabledTest()
    {
        (YesNtInterpreter interpreter, TestHost host) = Create();
        interpreter.DisableStatement("print_line");

        interpreter.Execute(["call worker task", "exit", "func worker:", "print_line from worker", "exit", "end_func"]);

        string text = WaitForText(host, "Task ");
        Assert.IsFalse(text.Contains("from worker"));
    }

    [TestMethod]
    public void TaskHasAStatementEnabledAgainTest()
    {
        (YesNtInterpreter interpreter, TestHost host) = Create();
        interpreter.DisableStatement("print_line");
        interpreter.EnableStatement("print_line");

        interpreter.Execute(["call worker task", "exit", "func worker:", "print_line from worker", "exit", "end_func"]);

        _ = WaitForText(host, "from worker");
    }

    [TestMethod]
    public void TaskCallsAFunctionDefinedAboveItTest()
    {
        (YesNtInterpreter interpreter, TestHost host) = Create();

        interpreter.Execute(["func worker:", "print_line in worker", "end_func", "call worker task"]);

        string text = WaitForText(host, "in worker");
        Assert.IsFalse(text.Contains("not found"), text);
    }
}
