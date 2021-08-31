using NetDiscordRpc.Core.Converters;
using Newtonsoft.Json;

namespace NetDiscordRpc.RPC.Payload
{
    internal abstract class IPayload
    {
        [JsonProperty("cmd"), JsonConverter(typeof(EnumSnakeCaseConverter))]
        public Commands Command { get; set; }
        
        [JsonProperty("nonce")]
        public string Nonce { get; set; }

        protected IPayload() { }
        
        protected IPayload(long nonce) => Nonce = nonce.ToString();

        public override string ToString() => $"Payload || Command: {Command.ToString()}, Nonce: {(Nonce != null ? Nonce : "NULL")}";
    }
}