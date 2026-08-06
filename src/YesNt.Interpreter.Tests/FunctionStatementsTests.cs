using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Collections.Generic;

namespace YesNt.Interpreter.Tests;

[TestClass]
public class FunctionStatementsTests
{
    [TestMethod]
    public void FunctionCallWithInParameterTest()
    {
        List<string> lines =
        [
            "goto main",
            "func echo:",
            "global result = %in",
            "return",
            "label main:",
            "call echo with hello",
            "${result}"
        ];

        YesNtAssert.IsLastLineEqual(lines, "hello");
    }

    [TestMethod]
    public void HasInAndHasOutTokensTest()
    {
        List<string> lines =
        [
            "goto main",
            "func probe:",
            "global hasInBefore = %has_in",
            "var consume = %in",
            "global hasInAfter = %has_in",
            "push_out ${hasInBefore}",
            "push_out ${hasInAfter}",
            "return",
            "label main:",
            "call probe with x",
            "var hasOut = %has_out",
            "${hasOut}"
        ];

        YesNtAssert.IsLastLineEqual(lines, "True");
    }

    [TestMethod]
    public void OutParameterReadTest()
    {
        List<string> lines =
        [
            "goto main",
            "func make:",
            "push_out out_value",
            "return",
            "label main:",
            "call make with anything",
            "var value = %out",
            "${value}"
        ];

        YesNtAssert.IsLastLineEqual(lines, "out_value");
    }

    [TestMethod]
    public void OutParameterWithoutValueFailsTest()
    {
        List<string> lines =
        [
            "var x = %out"
        ];

        YesNtAssert.ContainsTerminationMessage(lines, "No out argument in stack");
    }

    [TestMethod]
    public void InParameterOutsideFunctionFailsTest()
    {
        List<string> lines =
        [
            "var x = %in"
        ];

        YesNtAssert.ContainsTerminationMessage(lines, "Statement not allowed outside of function");
    }

    [TestMethod]
    public void ReturnOutsideFunctionFailsTest()
    {
        List<string> lines =
        [
            "return"
        ];

        YesNtAssert.ContainsTerminationMessage(lines, "Statement not allowed outside of function");
    }

    [TestMethod]
    public void PushOutOutsideFunctionFailsTest()
    {
        List<string> lines =
        [
            "push_out value"
        ];

        YesNtAssert.ContainsTerminationMessage(lines, "Statement not allowed outside of function");
    }

    [TestMethod]
    public void FunctionWithoutColonFailsTest()
    {
        List<string> lines =
        [
            "func missing"
        ];

        YesNtAssert.ContainsTerminationMessage(lines, "Invalid statement");
    }

    [TestMethod]
    public void NestedFunctionDefinitionFailsTest()
    {
        List<string> lines =
        [
            "goto main",
            "func outer:",
            "func inner:",
            "return",
            "end_func",
            "label main:",
            "call outer"
        ];

        YesNtAssert.ContainsTerminationMessage(lines, "Nested functions are not allowed");
    }

    [TestMethod]
    public void LocalVariableDoesNotLeakToCallerTest()
    {
        List<string> lines =
        [
            "goto main",
            "func modify:",
            "var x = inner",
            "return",
            "label main:",
            "var x = outer",
            "call modify",
            "${x}"
        ];

        YesNtAssert.IsLastLineEqual(lines, "outer");
    }

    [TestMethod]
    public void ClearCallStackRunsTest()
    {
        List<string> lines =
        [
            "clear_call_stack",
            "var result = ok",
            "${result}"
        ];

        YesNtAssert.IsLastLineEqual(lines, "ok");
    }

    // --- Error path tests ---

    [TestMethod]
    public void AccessInWithoutArgFailsTest()
    {
        List<string> lines =
        [
            "goto main",
            "func noin:",
            "var x = %in",
            "return",
            "label main:",
            "call noin"
        ];

        YesNtAssert.ContainsTerminationMessage(lines, "No in argument in stack");
    }

    // --- Nested scope tests ---

    [TestMethod]
    public void GlobalModifiedInsideFunctionIsVisibleAfterReturnTest()
    {
        List<string> lines =
        [
            "goto main",
            "func setglobal:",
            "global shared = modified",
            "return",
            "label main:",
            "global shared = original",
            "call setglobal",
            "${shared}"
        ];

        YesNtAssert.IsLastLineEqual(lines, "modified");
    }

    [TestMethod]
    public void NestedFunctionCallsHaveIndependentLocalScopesTest()
    {
        List<string> lines =
        [
            "goto main",
            "func outer:",
            "var x = outer_val",
            "call inner",
            "push_out ${x}",
            "return",
            "func inner:",
            "var x = inner_val",
            "return",
            "label main:",
            "call outer",
            "var result = %out",
            "${result}"
        ];

        YesNtAssert.IsLastLineEqual(lines, "outer_val");
    }

    // --- end_func / return value tests ---

    [TestMethod]
    public void EndFuncReturnsToCallerTest()
    {
        List<string> lines =
        [
            "goto main",
            "func greet:",
            "global result = done",
            "end_func",
            "label main:",
            "call greet",
            "${result}"
        ];

        YesNtAssert.IsLastLineEqual(lines, "done");
    }

    [TestMethod]
    public void ReturnWithValueTest()
    {
        List<string> lines =
        [
            "goto main",
            "func add:",
            "var a = %in",
            "var b = %in",
            "var sum = ${a} + ${b} calc",
            "return ${sum}",
            "label main:",
            "call add with 3, 4",
            "var total = %out",
            "${total}"
        ];

        YesNtAssert.IsLastLineEqual(lines, "7");
    }

    [TestMethod]
    public void ReturnWithLiteralValueTest()
    {
        List<string> lines =
        [
            "goto main",
            "func make:",
            "return out_value",
            "label main:",
            "call make",
            "var value = %out",
            "${value}"
        ];

        YesNtAssert.IsLastLineEqual(lines, "out_value");
    }

    [TestMethod]
    public void ReturnEarlyExitSkipsRestOfFunctionTest()
    {
        List<string> lines =
        [
            "goto main",
            "func probe:",
            "push_out should_not_escape",
            "return",
            "push_out should_never_run",
            "label main:",
            "call probe",
            "var value = %out",
            "${value}"
        ];

        YesNtAssert.IsLastLineEqual(lines, "should_not_escape");
    }

    [TestMethod]
    public void ReturnWithValueOutsideFunctionFailsTest()
    {
        List<string> lines =
        [
            "return 42"
        ];

        YesNtAssert.ContainsTerminationMessage(lines, "Statement not allowed outside of function");
    }

    [TestMethod]
    public void EndFuncOutsideFunctionFailsTest()
    {
        List<string> lines =
        [
            "end_func"
        ];

        YesNtAssert.ContainsTerminationMessage(lines, "Statement not allowed outside of function");
    }

    // --- Named parameter tests ---

    [TestMethod]
    public void FunctionNamedParametersTest()
    {
        List<string> lines =
        [
            "goto main",
            "func add: a, b",
            "return ${a} + ${b} calc",
            "label main:",
            "call add with 3, 4",
            "var total = %out",
            "${total}"
        ];

        YesNtAssert.IsLastLineEqual(lines, "7");
    }

    [TestMethod]
    public void FunctionNamedParametersRespectOrderTest()
    {
        List<string> lines =
        [
            "goto main",
            "func pair: first, second",
            "push_out ${first}",
            "push_out ${second}",
            "end_func",
            "label main:",
            "call pair with A, B",
            "var f = %out",
            "var s = %out",
            "${f}-${s}"
        ];

        YesNtAssert.IsLastLineEqual(lines, "A-B");
    }

    [TestMethod]
    public void FunctionNamedParametersMissingArgumentFailsTest()
    {
        List<string> lines =
        [
            "goto main",
            "func add: a, b",
            "return ${a} calc",
            "label main:",
            "call add with 5"
        ];

        YesNtAssert.ContainsTerminationMessage(lines, "No in argument in stack");
    }

    [TestMethod]
    public void FunctionNamedParameterInvalidSyntaxTest()
    {
        List<string> lines =
        [
            "func add: a b"
        ];

        YesNtAssert.ContainsTerminationMessage(lines, "Invalid syntax");
    }

    [TestMethod]
    public void FunctionNamedParametersExtraArgumentsRemainTest()
    {
        List<string> lines =
        [
            "goto main",
            "func probe: a",
            "global first = ${a}",
            "global hasExtra = %has_in",
            "push_out ${hasExtra}",
            "end_func",
            "label main:",
            "call probe with A, B",
            "var extra = %out",
            "${extra}"
        ];

        YesNtAssert.IsLastLineEqual(lines, "True");
    }

    [TestMethod]
    public void FunctionNamedParametersSearchEntryTest()
    {
        List<string> lines =
        [
            "call add with 3, 4",
            "func add: a, b",
            "return ${a} + ${b} calc",
            "end_func",
            "var total = %out",
            "${total}"
        ];

        YesNtAssert.IsLastLineEqual(lines, "7");
    }

    // --- Top-down function body skip tests ---

    [TestMethod]
    public void FunctionDeclaredFirstThenCalledTest()
    {
        List<string> lines =
        [
            "func add: a, b",
            "return ${a} + ${b} calc",
            "end_func",
            "call add with 3, 7",
            "var total = %out",
            "${total}"
        ];

        YesNtAssert.IsLastLineEqual(lines, "10");
    }

    [TestMethod]
    public void FunctionBodyWithNestedReturnSkippedTest()
    {
        List<string> lines =
        [
            "func test: a, b",
            "if ${a} == ${b}:",
            "print_line %in",
            "return 999",
            "end_if",
            "return ${a} + ${b} calc",
            "end_func",
            "call test with 1, 2, 4",
            "var value = %out",
            "${value}"
        ];

        YesNtAssert.IsLastLineEqual(lines, "3");
    }

    [TestMethod]
    public void FunctionWithoutEndFuncFailsTest()
    {
        List<string> lines =
        [
            "func answer:",
            "return 42",
            "call answer",
            "var value = %out",
            "${value}"
        ];

        YesNtAssert.ContainsTerminationMessage(lines, "no matching \"end_func\"");
    }
}