using System;

namespace YesNt.Interpreter
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length == 1)
            {
                YesNtInterpreter interpreter = new YesNtInterpreter();
                interpreter.Initialize();
                interpreter.Execute(args[0]);
            }
            else
            {
                Console.WriteLine("No path specified!");
            }
        }
    }
}