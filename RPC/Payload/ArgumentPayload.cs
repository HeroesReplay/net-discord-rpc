using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NetDiscordRpc.RPC.Payload
{
    internal class ArgumentPayload: IPayload
    {
        [JsonProperty("args", NullValueHandling = NullValueHandling.Ignore)]
        public JObject Arguments { get; set; }
		
        public ArgumentPayload() => Arguments = null;
        
        public ArgumentPayload(long nonce): base(nonce) => Arguments = null;
        
        public ArgumentPayload(object args, long nonce): base(nonce) => SetObject(args);

        public void SetObject(object obj) => Arguments = JObject.FromObject(obj);
        
        public T GetObject<T>() => Arguments.ToObject<T>();

        public override string ToString() => $"Argument {base.ToString()}";
    }
}