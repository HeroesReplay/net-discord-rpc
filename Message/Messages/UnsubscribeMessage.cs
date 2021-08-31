using NetDiscordRpc.Events;
using NetDiscordRpc.RPC.Payload;

namespace NetDiscordRpc.Message.Messages
{
    public class UnsubscribeMessage: IMessage
    {
        public EventTypes Event { get; internal set; }
        
        public override MessageTypes Type { get { return MessageTypes.Unsubscribe; } }
        
        internal UnsubscribeMessage(ServerEvent evt)
        {
            Event = evt switch
            {
                ServerEvent.ActivityJoin => EventTypes.Join,
                ServerEvent.ActivityJoinRequest => EventTypes.JoinRequest,
                ServerEvent.ActivitySpectate => EventTypes.Spectate,
                _ => Event
            };
        }
    }
}