using NetDiscordRpc.RPC.Payload;
using Newtonsoft.Json;

namespace NetDiscordRpc.RPC.Commands
{
    internal class PresenceCommand : ICommand
    {
        [JsonProperty("pid")]
        public int PID { get; set; }
        
        [JsonProperty("activity")]
        public RichPresence Presence { get; set; }

        public IPayload PreparePayload(long nonce)
        {
            return new ArgumentPayload(this, nonce)
            {
                Command = Payload.Commands.SetActivity
            };
        }
    }
}