namespace NetDiscordRpc.Message.Messages
{
    public class ConnectionFailedMessage: IMessage
    {
        public int FailedPipe { get; internal set; }
        
        public override MessageTypes Type => MessageTypes.ConnectionFailed;
    }
}