using System;

using YesNt.Interpreter.Attributes;

namespace YesNt.Interpreter.Runtime;

/// <summary>
/// Pre-calculated statement handler information for faster matching.
/// </summary>
internal record StatementHandler(StatementAttribute Attribute, Action<string> Handler, string FullName);