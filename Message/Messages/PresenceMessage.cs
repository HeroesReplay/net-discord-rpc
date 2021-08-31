using NetDiscordRpc.RPC;

namespace NetDiscordRpc.Message.Messages
{
    public class PresenceMessage: IMessage
    {
        public string Name { get; internal set; }
        public string ApplicationID { get; internal set; }
        public RichPresenceBase Presence { get; internal set; }
        public override MessageTypes Type => MessageTypes.PresenceUpdate;

        internal PresenceMessage(): this(null) { }
        
        internal PresenceMessage(RichPresenceResponse rpr)
        {
            if (rpr == null)
            {
                Presence = null;
                Name = "No Rich Presence";
                ApplicationID = "";
            }
            else
            {
                Presence = rpr;
                Name = rpr.Name;
                ApplicationID = rpr.ClientID;
            }
        }
    }
}