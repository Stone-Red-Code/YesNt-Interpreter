using System;
using System.IO;

using YesNt.Interpreter.Runtime;

if (args.Length == 1)
{
    if (!File.Exists(args[0]))
    {
        Console.WriteLine("File not found: " + args[0]);
        return;
    }

    YesNtInterpreter interpreter = new YesNtInterpreter();
    interpreter.Execute(args[0]);
}
else
{
    Console.WriteLine("No path specified!");
}