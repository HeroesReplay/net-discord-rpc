using System;

namespace NetDiscordRpc.Core.Exceptions
{
    public class InvalidConfigurationException: Exception
    {
        internal InvalidConfigurationException(string message): base(message) { }
    }
}