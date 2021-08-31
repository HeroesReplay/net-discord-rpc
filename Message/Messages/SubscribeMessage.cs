using NetDiscordRpc.Events;
using NetDiscordRpc.RPC.Payload;

namespace NetDiscordRpc.Message.Messages
{
    public class SubscribeMessage: IMessage
    {
        public EventTypes Event { get; internal set; }
        
        public override MessageTypes Type => MessageTypes.Subscribe;

        internal SubscribeMessage(ServerEvent evt)
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