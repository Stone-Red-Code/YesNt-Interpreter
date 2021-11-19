using System;

using YesNt.Interpreter.Enums;

namespace YesNt.Interpreter.Runtime
{
    public class StatementInformation
    {
        public string Name { get; internal set; }
        public SearchMode SearchMode { get; internal set; }
        public SpaceAround SpaceAround { get; internal set; }
        public ConsoleColor Color { get; internal set; }
        public bool IgnoreSyntaxHighlighting { get; internal set; }
        public string Seperator { get; set; }
    }
}