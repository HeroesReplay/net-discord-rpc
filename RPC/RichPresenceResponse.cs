using Newtonsoft.Json;

namespace NetDiscordRpc.RPC
{
    internal sealed class RichPresenceResponse: RichPresenceBase
    {
        [JsonProperty("application_id")]
        public string ClientID { get; private set; }
        
        [JsonProperty("name")]
        public string Name { get; private set; }
    }
}