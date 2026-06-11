using System;

namespace YesNt.Interpreter.Runtime;

/// <summary>
/// Pre-calculated statement handler information for faster matching.
/// </summary>
internal record StatementHandler(StatementInformation Attribute, Action<string> Handler, string FullName);