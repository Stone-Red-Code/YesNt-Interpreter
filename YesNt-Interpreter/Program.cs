namespace YesNt.Interpreter
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            YesNtInterpreter interpreter = new YesNtInterpreter();
            interpreter.Initialize();
            interpreter.Execute(args[0]);
        }
    }
}