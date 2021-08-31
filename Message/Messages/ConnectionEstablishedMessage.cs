namespace NetDiscordRpc.Message.Messages
{
    public class ConnectionEstablishedMessage: IMessage
    {
        public int ConnectedPipe { get; internal set; }
        
        public override MessageTypes Type => MessageTypes.ConnectionEstablished;
    }
}