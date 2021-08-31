namespace NetDiscordRpc.Events
{
    [System.Flags]
    public enum EventTypes
    {
        None = 0,
        Spectate = 0x1,
        Join = 0x2,
        JoinRequest = 0x4
    }
}