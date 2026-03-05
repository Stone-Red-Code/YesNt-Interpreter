namespace YesNt.Interpreter.Runtime;

/// <summary>
/// Central repository of all exit/error message strings used by <see cref="RuntimeInformation.Exit"/>.
/// Keeping messages here ensures consistency and makes them easy to find or localise.
/// </summary>
internal static class ExitMessages
{
    internal const string InvalidSyntax = "Invalid syntax";
    internal const string InvalidSyntaxColonRequired = "Invalid syntax. Statement must end with ':'";
    internal const string InvalidOperation = "Invalid operation";
    internal const string InvalidStatement = "Invalid statement";
    internal const string InvalidStringLiteral = "Invalid string literal";
    internal const string EndOfFile = "End of file";
    internal const string TerminatedByExternalProcess = "Terminated by external process";
    internal const string TerminatedByChildTask = "Terminated by child task";
    internal const string TerminatedByParentTask = "Terminated by parent task";
    internal const string PlannedTermination = "Planned termination by code";
    internal const string PlannedTerminationCancelingTasks = "Planned termination by code. Canceling all tasks";
    internal const string NoMatchingEndIf = "No matching end_if found";
    internal const string NoMatchingEndWhile = "No matching end_while found";
    internal const string NoMatchingWhile = "No matching while found";
    internal const string NestedFunctionsNotAllowed = "Nested functions are not allowed";
    internal const string NoOutArgumentInStack = "No out argument in stack";
    internal const string StatementNotAllowedOutsideFunction = "Statement not allowed outside of function";
    internal const string NoInArgumentInStack = "No in argument in stack";
    internal const string NoFunctionInStack = "No function in stack";

    internal static string LabelNotFound(string label)
    {
        return $"Label \"{label}\" not found";
    }

    internal static string FunctionNotFound(string function)
    {
        return $"Function \"{function}\" not found";
    }

    internal static string VariableNotFound(string variable)
    {
        return $"Variable \"{variable}\" not found";
    }

    internal static string ListNotFound(string list)
    {
        return $"List \"{list}\" not found";
    }

    internal static string InvalidIndex(string rawIndex)
    {
        return $"\"{rawIndex}\" is not a valid index";
    }

    internal static string IndexOutOfRange(int index)
    {
        return $"Index {index} out of range";
    }

    internal static string InvalidTimeoutValue(string value)
    {
        return $"\"{value}\" is not a valid time-out value";
    }

    internal static string CouldNotLoadFile(string path)
    {
        return $"Could not load file \"{path}\"";
    }

    internal static string CouldNotFindFile(string path)
    {
        return $"Could not find file \"{path}\"";
    }

    internal static string CannotFindFile(string path)
    {
        return $"Cannot find file \"{path}\".";
    }

    internal static string FailedToStart(string program, string message)
    {
        return $"Failed to start \"{program}\". {message}";
    }
}
