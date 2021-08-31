namespace NetDiscordRpc.Message.Messages
{
    public class SpectateMessage: IMessage
    {
        public override MessageTypes Type => MessageTypes.Spectate;
    }
}