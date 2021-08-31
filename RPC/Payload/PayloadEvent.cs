using NetDiscordRpc.Core.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NetDiscordRpc.RPC.Payload
{
    internal class EventPayload : IPayload
    {
        [JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
        public JObject Data { get; set; }
        
        [JsonProperty("evt"), JsonConverter(typeof(EnumSnakeCaseConverter))]
        public ServerEvent? Event { get; set; }

        public EventPayload() => Data = null;
        
        public EventPayload(long nonce): base(nonce) { Data = null; }
        
        public T GetObject<T>() => Data == null ? default : Data.ToObject<T>();
        public override string ToString() => $"Event {base.ToString()}, Event: {(Event.HasValue ? Event.ToString() : "N/A")}";
    }
}