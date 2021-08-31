using NetDiscordRpc.Users;
using Newtonsoft.Json;

namespace NetDiscordRpc.Message.Messages
{
    public class ReadyMessage: IMessage
    {
        [JsonProperty("config")]
        public Configuration Configuration { get; set; }
        
        [JsonProperty("user")]
        public User User { get; set; }
        
        [JsonProperty("v")]
        public int Version { get; set; }
        
        public override MessageTypes Type => MessageTypes.Ready;
    }
}