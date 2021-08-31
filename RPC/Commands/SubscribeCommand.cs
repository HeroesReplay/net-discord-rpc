using NetDiscordRpc.RPC.Payload;

namespace NetDiscordRpc.RPC.Commands
{
    internal class SubscribeCommand : ICommand
    {
        public ServerEvent Event { get; set; }
        public bool IsUnsubscribe { get; set; }
		
        public IPayload PreparePayload(long nonce)
        {
            return new EventPayload(nonce)
            {
                Command = IsUnsubscribe ? Payload.Commands.Unsubscribe : Payload.Commands.Subscribe,
                Event = Event
            };
        }
    }
}