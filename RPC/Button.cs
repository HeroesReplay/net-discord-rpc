using System;
using System.Text;
using NetDiscordRpc.Core.Exceptions;
using Newtonsoft.Json;

namespace NetDiscordRpc.RPC
{
    public class Button
    {
        [JsonProperty("label")]
        public string Label
        {
            get => _label;
            set
            {
                if (!RichPresenceBase.ValidateString(value, out _label, 32, Encoding.UTF8))
                {
                    throw new StringOutOfRangeException(512);
                }
            }
        }
        private string _label;
        
        [JsonProperty("url")]
        public string Url
        {
            get => _url;
            set
            {
                if(!RichPresenceBase.ValidateString(value, out _url, 512, Encoding.UTF8))
                {
                    throw new StringOutOfRangeException(512);
                }

                if (!Uri.TryCreate(_url, UriKind.Absolute, out var uriResult))
                {
                    throw new ArgumentException("Url must be a valid URI");
                }
            }
        }
        private string _url;
    }
}