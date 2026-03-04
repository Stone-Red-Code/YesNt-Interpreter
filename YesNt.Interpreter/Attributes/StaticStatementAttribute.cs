using System;

using YesNt.Interpreter.Enums;

namespace YesNt.Interpreter.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class StaticStatementAttribute : Attribute
{
    public bool ExecuteInSearchMode { get; set; }
    public Priority Priority { get; set; } = Priority.Normal;
}