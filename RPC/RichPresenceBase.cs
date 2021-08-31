using System;
using System.Text;
using NetDiscordRpc.Core.Exceptions;
using NetDiscordRpc.Core.Helpers;
using Newtonsoft.Json;

namespace NetDiscordRpc.RPC
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    [Serializable]
    public class RichPresenceBase
    {
        protected internal string _state;
        protected internal string _details;
        
        [JsonProperty("timestamps", NullValueHandling = NullValueHandling.Ignore)]
        public Timestamps Timestamps { get; set; }
        
        [JsonProperty("assets", NullValueHandling = NullValueHandling.Ignore)]
        public Assets Assets { get; set; }
        
        [JsonProperty("party", NullValueHandling = NullValueHandling.Ignore)]
        public Party Party { get; set; }
        
        [JsonProperty("secrets", NullValueHandling = NullValueHandling.Ignore)]
        public Secrets Secrets { get; set; }

        [JsonProperty("instance", NullValueHandling = NullValueHandling.Ignore)]
        private bool Instance { get; set; }

        [JsonProperty("state", NullValueHandling = NullValueHandling.Ignore)]
        public string State
        {
            get => _state;
            set
            {
                if (!ValidateString(value, out _state, 128, Encoding.UTF8))
                {
                    throw new StringOutOfRangeException("State", 0, 128);
                }
            }
        }

        [JsonProperty("details", NullValueHandling = NullValueHandling.Ignore)]
        public string Details
        {
            get => _details;
            set
            {
                if (!ValidateString(value, out _details, 128, Encoding.UTF8))
                {
                    throw new StringOutOfRangeException(128);
                }
            }
        }

        internal static bool ValidateString(string text, out string result, int bytes, Encoding encoding)
        {
            result = text;
            if (text == null) return true;

            var str = text.Trim();

            if (!str.WithinLength(bytes, encoding)) return false;

            result = str.GetNullOrString();

            return true;
        }

        public static implicit operator bool(RichPresenceBase presesnce) => presesnce != null;

        internal virtual bool Matches(RichPresence other)
        {
            if (other == null) return false;

            if (State != other.State || Details != other.Details) return false;

            if (Timestamps == null)
            {
                if (other.Timestamps == null || 
                    other.Timestamps.StartUnixMilliseconds != Timestamps.StartUnixMilliseconds ||
                    other.Timestamps.EndUnixMilliseconds != Timestamps.EndUnixMilliseconds)
                    return false;
            }
            else if (other.Timestamps != null) return false;

            if (Secrets != null)
            {
                if (other.Secrets == null ||
                    other.Secrets.JoinSecret != Secrets.JoinSecret ||
                    other.Secrets.SpectateSecret != Secrets.SpectateSecret)
                    return false;
            }
            else if (other.Secrets != null) return false;
            
            if (Party != null)
            {
                if (other.Party == null ||
                    other.Party.ID != Party.ID ||
                    other.Party.Max != Party.Max ||
                    other.Party.Size != Party.Size ||
                    other.Party.Privacy != Party.Privacy)
                    return false;
            }
            else if(other.Party != null) return false;
            
            if (Assets != null)
            {
                if (other.Assets == null ||
                    other.Assets.LargeImageKey != Assets.LargeImageKey ||
                    other.Assets.LargeImageText != Assets.LargeImageText ||
                    other.Assets.SmallImageKey != Assets.SmallImageKey ||
                    other.Assets.SmallImageText != Assets.SmallImageText)
                    return false;
            }
            else if (other.Assets != null) return false;

            return Instance == other.Instance;
        }
        
        public RichPresence ToRichPresence()
        {
            var presence = new RichPresence();
            presence.State = State;
            presence.Details = Details;

            presence.Party = !HasParty() ? Party : null;
            presence.Secrets = !HasSecrets() ? Secrets : null;

            if (HasAssets())
            {
                presence.Assets = new Assets()
                {
                    SmallImageKey = Assets.SmallImageKey,
                    SmallImageText = Assets.SmallImageText,

                    LargeImageKey = Assets.LargeImageKey,
                    LargeImageText = Assets.LargeImageText
                };
            }

            if (!HasTimestamps()) return presence;
            
            presence.Timestamps = new Timestamps();
            if (Timestamps.Start.HasValue) presence.Timestamps.Start = Timestamps.Start;
            if (Timestamps.End.HasValue) presence.Timestamps.End = Timestamps.End;

            return presence;
        }
        
        public bool HasTimestamps() => Timestamps != null && (Timestamps.Start != null || Timestamps.End != null);

        public bool HasAssets() => Assets != null;
        
        public bool HasParty() => Party != null && Party.ID != null;
        
        public bool HasSecrets() => Secrets != null && (Secrets.JoinSecret != null || Secrets.SpectateSecret != null);
    }
}