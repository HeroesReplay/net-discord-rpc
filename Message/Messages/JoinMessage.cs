using Newtonsoft.Json;

namespace NetDiscordRpc.Message.Messages
{
    public class JoinMessage: IMessage
    {
        [JsonProperty("secret")]
        public string Secret { get; internal set; }
        
        public override MessageTypes Type => MessageTypes.Join;
    }
}