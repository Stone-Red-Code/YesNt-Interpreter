using Microsoft.VisualStudio.TestTools.UnitTesting;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

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
}
