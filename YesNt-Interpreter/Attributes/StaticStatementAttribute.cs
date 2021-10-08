﻿using System;

using YesNt.Interpreter.Enums;

namespace YesNt.Interpreter.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    internal class StaticStatementAttribute : Attribute
    {
        public bool ExecuteInSearchLabelMode { get; set; }
        public Priority Priority { get; set; } = Priority.Normal;
    }
}