using System.Diagnostics;
using System.Threading;

using YesNt.Interpreter.Runtime;

namespace YesNt.Interpreter.Utilities;

internal static class ConsoleExtensions
{
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