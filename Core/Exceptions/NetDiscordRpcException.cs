using System;

namespace NetDiscordRpc.Core.Exceptions
{
    public class NetDiscordRpcException: Exception
    {
        public NetDiscordRpcException(string message): base(message) {}
    }
}