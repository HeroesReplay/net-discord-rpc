using NetDiscordRpc.Users;
using Newtonsoft.Json;

namespace NetDiscordRpc.Message.Messages
{
    public class JoinRequestMessage: IMessage
    {
        [JsonProperty("user")]
        public User User { get; internal set; }
        
        public override MessageTypes Type => MessageTypes.JoinRequest;
    }
}