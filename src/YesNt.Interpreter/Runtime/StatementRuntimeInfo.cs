namespace YesNt.Interpreter.Runtime;

using System;
using System.Collections.Generic;

/// <summary>
/// Base class for all classes that host statement handler methods.
/// Subclasses declare methods decorated with <see cref="Attributes.StatementAttribute"/> or
/// <see cref="Attributes.StaticStatementAttribute"/>; the source generator
/// (<c>GeneratedStatementRegistry</c>) discovers these at compile time and wires them up.
/// </summary>
internal abstract class StatementRuntimeInformation
{
    /// <summary>
    /// Gets or sets the runtime state for the current execution context.
    /// Injected by the generated registry before any handler is invoked.
    /// </summary>
    public RuntimeInformation RuntimeInfo { get; set; }

    /// <summary>
    /// Trims surrounding whitespace and a trailing colon from a block or function name.
    /// </summary>
    protected static string NormalizeBlockName(string value)
    {
        return value.Trim().TrimEnd(':').Trim();
    }

    /// <summary>
    /// Parses a function declaration (the text after the <c>func</c> keyword), extracting the
    /// function name and its optional comma-separated named parameters.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the declaration is well formed; otherwise <see langword="false"/>.
    /// </returns>
    internal static bool TryParseFunctionSignature(string declaration, out string name, out List<string> parameters)
    {
        name = string.Empty;
        parameters = [];

        string trimmed = declaration.Trim();
        int colonIndex = trimmed.IndexOf(':');
        if (colonIndex < 0)
        {
            return false;
        }

        name = NormalizeBlockName(trimmed[..colonIndex]);
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        string parameterSection = trimmed[(colonIndex + 1)..].Trim();
        if (string.IsNullOrWhiteSpace(parameterSection))
        {
            return true;
        }

        foreach (string rawParameter in parameterSection.Split(','))
        {
            string parameter = rawParameter.Trim();
            if (string.IsNullOrWhiteSpace(parameter) || parameter.Contains(' '))
            {
                return false;
            }

            parameters.Add(parameter);
        }

        return true;
    }

    /// <summary>
    /// Binds a function's named parameters to local variables by popping values from the
    /// current call scope's argument stack, mirroring <c>var p = %in</c> for each parameter.
    /// </summary>
    protected void BindNamedParameters(string functionKey)
    {
        if (!RuntimeInfo.FunctionParameters.TryGetValue(functionKey, out List<string> parameters) || parameters.Count == 0)
        {
            return;
        }

        if (RuntimeInfo.FunctionCallStack.Count == 0)
        {
            return;
        }

        FunctionScope scope = RuntimeInfo.FunctionCallStack.Peek();
        foreach (string parameter in parameters)
        {
            if (scope.Arguments.Count == 0)
            {
                RuntimeInfo.Exit(ExitMessages.NoInArgumentInStack, true);
                return;
            }

            scope.Variables[parameter] = scope.Arguments.Pop();
        }
    }
}