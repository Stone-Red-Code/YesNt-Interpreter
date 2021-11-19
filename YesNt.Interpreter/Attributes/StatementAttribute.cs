using System;

using YesNt.Interpreter.Enums;

namespace YesNt.Interpreter.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    internal class StatementAttribute : Attribute
    {
        public string Name { get; }
        public SearchMode SearchMode { get; }
        public SpaceAround SpaceAround { get; }
        public ConsoleColor Color { get; set; }
        public Priority Priority { get; set; } = Priority.Normal;
        public bool ExecuteInSearchMode { get; set; }
        public bool KeepStatementInArgs { get; set; }
        public bool IgnoreSyntaxHighlighting { get; }
        public string Seperator { get; set; }

        internal StatementAttribute(string name, SearchMode searchMode, SpaceAround spaceAround, ConsoleColor color)
        {
            Name = name;
            SearchMode = searchMode;
            SpaceAround = spaceAround;
            Color = color;
        }

        internal StatementAttribute(string name, SearchMode searchMode, SpaceAround spaceAround)
        {
            Name = name;
            SearchMode = searchMode;
            SpaceAround = spaceAround;
            IgnoreSyntaxHighlighting = true;
        }
    }
}