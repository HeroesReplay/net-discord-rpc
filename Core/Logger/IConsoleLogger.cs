namespace NetDiscordRpc.Core.Logger
{
    public interface IConsoleLogger
    {
        void Trace(string message);
        
        void Info(string message);
        
        void Warning(string message);
        
        void Error(string message);
    }
}