namespace NetDiscordRpc.Message.Messages
{
    public class CloseMessage: IMessage
    {
        public string Reason { get; internal set; }
        public int Code { get; internal set; }
        
        public override MessageTypes Type => MessageTypes.Close;

        internal CloseMessage() { }
        
        internal CloseMessage(string reason) => Reason = reason;
    }
}