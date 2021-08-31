namespace NetDiscordRpc.Message
{
    public enum MessageTypes
    {
        Ready,
        Close,
        Error,
        PresenceUpdate,
        Subscribe,
        Unsubscribe,
        Join,
        Spectate,
        JoinRequest,
        ConnectionEstablished,
        ConnectionFailed
    }
}