using NetDiscordRpc.RPC.Payload;
using Newtonsoft.Json;

namespace NetDiscordRpc.RPC.Commands
{
    internal class RespondCommand : ICommand
    {
        [JsonProperty("user_id")]
        public string UserID { get; set; }
        
        [JsonIgnore]
        public bool Accept { get; set; }

        public IPayload PreparePayload(long nonce)
        {
            return new ArgumentPayload(this, nonce)
            {
                Command = Accept ? Payload.Commands.SendActivityJoinInvite : Payload.Commands.CloseActivityJoinRequest
            };
        }
    }
}