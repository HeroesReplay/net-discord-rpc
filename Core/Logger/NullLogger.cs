namespace NetDiscordRpc.Core.Logger
{
    public class NullLogger: IConsoleLogger
    {
        public void Trace(string message) { /* This class does not send logs. */ }

        public void Info(string message) { /* This class does not send logs. */ }

        public void Warning(string message) { /* This class does not send logs. */ }
        public void Error(string message) { /* This class does not send logs. */ }
    }
}