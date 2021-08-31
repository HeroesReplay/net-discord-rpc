using System;

namespace NetDiscordRpc.Core.Logger
{
    public class ConsoleLogger: IConsoleLogger
    {
        private static string GetLogType(ConsoleLogLevel level)
        {
            var result = level switch
            {
                ConsoleLogLevel.Info => "info ",
                ConsoleLogLevel.Warning => "warn ",
                ConsoleLogLevel.Error => "error",
                ConsoleLogLevel.Trace => "trace",
                ConsoleLogLevel.None => "none ",
                _ => throw new ArgumentOutOfRangeException(nameof(level))
            };

            return result;
        }
        
        public void Trace(string message) => Log(ConsoleLogLevel.Trace, ConsoleColor.Gray, message);
        
        public void Info(string message) => Log(ConsoleLogLevel.Info, ConsoleColor.White, message);
        
        public void Warning(string message) => Log(ConsoleLogLevel.Warning, ConsoleColor.Yellow, message);
        
        public void Error(string message) => Log(ConsoleLogLevel.Error, ConsoleColor.Red, message);
        
        private static void Log(ConsoleLogLevel level, ConsoleColor color, string log)
        {
            Console.ForegroundColor = color;
            Console.WriteLine($"[{DateTime.Now}] {GetLogType(level)}: {log}");
            Console.ResetColor();
        }
    }
}