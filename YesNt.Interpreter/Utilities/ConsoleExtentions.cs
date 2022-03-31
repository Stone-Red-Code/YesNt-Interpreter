using System;
using System.Diagnostics;
using System.Threading;

using YesNt.Interpreter.Runtime;

namespace YesNt.Interpreter.Utilities
{
    internal static class ConsoleExtentions
    {
        public static char ReadKey(RuntimeInformation runtimeInformation)
        {
            while (!runtimeInformation.Stop)
            {
                if (Console.KeyAvailable)
                {
                    return Console.ReadKey().KeyChar;
                }
                Thread.Sleep(10);
            }
            return ' ';
        }

        public static void Sleep(int millisecondsTimeout, RuntimeInformation runtimeInformation)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            while (!runtimeInformation.Stop)
            {
                if (stopwatch.ElapsedMilliseconds > millisecondsTimeout)
                {
                    return;
                }
                Thread.Sleep(10);
            }
        }
    }
}