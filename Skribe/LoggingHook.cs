using System;

namespace Skribe
{
    /// <summary>
    /// The hook Scribe should use for logging, specified on startup.
    /// </summary>
    public interface LoggingHook
    {
        public void Info(string message);

        public void Warning(string message);

        public void Error(string message);
    }

    public class ConsoleHook : LoggingHook
    {
        public void Info(string message)
        {
            Console.WriteLine(message);
        }

        public void Warning(string message)
        {
            Console.WriteLine(message);
        }

        public void Error(string message)
        {
            Console.WriteLine(message);
        }
    }
}