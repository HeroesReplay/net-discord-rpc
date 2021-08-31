using System;

namespace NetDiscordRpc.Message
{
    public abstract class IMessage
    {
        private DateTime _timecreated;

        public abstract MessageTypes Type { get; }
        
        public DateTime TimeCreated => _timecreated;

        public IMessage() => _timecreated = DateTime.Now;
    }
}