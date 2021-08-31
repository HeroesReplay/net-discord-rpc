using System;

namespace NetDiscordRpc.Core.Exceptions
{
    public class BadPresenceException: Exception
    {
        internal BadPresenceException(string message): base(message) { }
    }
}