using System;
using NetDiscordRpc.Core.Helpers;
using Newtonsoft.Json;

namespace NetDiscordRpc.RPC
{
    [Serializable]
    public class Party
    {
        private string _partyid;
        
        [JsonIgnore] public int Size { get; set; }
        
        [JsonIgnore] public int Max { get; set; }
        
        [JsonProperty("privacy", NullValueHandling = NullValueHandling.Include, DefaultValueHandling = DefaultValueHandling.Include)]
        public PartyPrivacySettings Privacy { get; set; }

        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public string ID
        {
            get => _partyid;
            set => _partyid = value.GetNullOrString();
        }
        
        [JsonProperty("size", NullValueHandling = NullValueHandling.Ignore)]
        private int[] _size
        {
            get
            {
                //see issue https://github.com/discordapp/discord-rpc/issues/111
                var size = Math.Max(1, Size);
                return new int[] { size, Math.Max(size, Max) };
            }

            set
            {
                if (value.Length != 2)
                {
                    Size = 0; Max = 0;
                }
                else
                {
                    Size = value[0]; Max = value[1];
                }
            }

        }
    }
}