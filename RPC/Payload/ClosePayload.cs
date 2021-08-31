using Newtonsoft.Json;

namespace NetDiscordRpc.RPC.Payload
{
    internal class ClosePayload : IPayload
    {
        [JsonProperty("code")]
        public int Code { get; set; }
        
        [JsonProperty("message")]
        public string Reason { get; set; }
    }
}