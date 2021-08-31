using System;
using NetDiscordRpc.Core.Logger;

namespace NetDiscordRpc.Core.IO
{
    public interface INamedPipeClient : IDisposable
    {
        IConsoleLogger Logger { get; set; }
        
        bool IsConnected { get; }
        
        int ConnectedPipe { get; }
        
        bool Connect(int pipe);
        
        bool ReadFrame(out PipeFrame frame);
        
        bool WriteFrame(PipeFrame frame);
        
        void Close();
        
    }
}