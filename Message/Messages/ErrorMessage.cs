using Newtonsoft.Json;

namespace NetDiscordRpc.Message.Messages
{
    public class ErrorMessage: IMessage
    {
        [JsonProperty("code")]
        public ErrorCodes Code { get; internal set; }
        
        [JsonProperty("message")]
        public string Message { get; internal set; }
        
        public override MessageTypes Type => MessageTypes.Error;
    }
}