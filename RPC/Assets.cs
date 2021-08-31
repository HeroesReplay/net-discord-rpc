using System;
using System.Text;
using NetDiscordRpc.Core.Exceptions;
using Newtonsoft.Json;

namespace NetDiscordRpc.RPC
{
    [Serializable]
    public class Assets
    {
        private string _largeimagekey;
        private string _largeimagetext;
        private string _smallimagekey;
        private string _smallimagetext;

        [JsonIgnore]
        public ulong? LargeImageID => _largeimageID;

        private ulong? _largeimageID;
        
        [JsonIgnore]
        public ulong? SmallImageID => _smallimageID;

        private ulong? _smallimageID;
        
        [JsonProperty("large_image", NullValueHandling = NullValueHandling.Ignore)]
        public string LargeImageKey
        {
            get => _largeimagekey;
            set
            {
                if (!RichPresenceBase.ValidateString(value, out _largeimagekey, 32, Encoding.UTF8))
                {
                    throw new StringOutOfRangeException(32);
                }

                _largeimageID = null;
            }
        }
        
        [JsonProperty("large_text", NullValueHandling = NullValueHandling.Ignore)]
        public string LargeImageText
        {
            get => _largeimagetext;
            set
            {
                if (!RichPresenceBase.ValidateString(value, out _largeimagetext, 128, Encoding.UTF8))
                {
                    throw new StringOutOfRangeException(128);
                }
            }
        }
        
        [JsonProperty("small_image", NullValueHandling = NullValueHandling.Ignore)]
        public string SmallImageKey
        {
            get => _smallimagekey;
            set
            {
                if (!RichPresenceBase.ValidateString(value, out _smallimagekey, 32, Encoding.UTF8))
                {
                    throw new StringOutOfRangeException(32);
                }
                
                _smallimageID = null;
            }
        }
        
        [JsonProperty("small_text", NullValueHandling = NullValueHandling.Ignore)]
        public string SmallImageText
        {
            get => _smallimagetext;
            set
            {
                if (!RichPresenceBase.ValidateString(value, out _smallimagetext, 128, Encoding.UTF8))
                {
                    throw new StringOutOfRangeException(128);
                }
            }
        }

        internal void Merge(Assets other)
        {
            _smallimagetext = other._smallimagetext;
            _largeimagetext = other._largeimagetext;
            
            ulong largeID;
            if (ulong.TryParse(other._largeimagekey, out largeID))
            {
                _largeimageID = largeID;
            }
            else
            {
                _largeimagekey = other._largeimagekey;
                _largeimageID = null;
            }
            
            ulong smallID;
            if (ulong.TryParse(other._smallimagekey, out smallID))
            {
                _smallimageID = smallID;
            }
            else
            {
                _smallimagekey = other._smallimagekey;
                _smallimageID = null;
            }
        }
    }
}