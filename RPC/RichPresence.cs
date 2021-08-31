using Newtonsoft.Json;

namespace NetDiscordRpc.RPC
{
    public sealed class RichPresence: RichPresenceBase
    {
        [JsonProperty("buttons", NullValueHandling = NullValueHandling.Ignore)]
        public Button[] Buttons { get; set; }

        public bool HasButtons() => Buttons != null && (uint) Buttons.Length > 0U;

        public RichPresence WithState(string state)
        {
            State = state;
            return this;
        }
        
        public RichPresence WithDetails(string details)
        {
            Details = details;
            return this;
        }
        
        public RichPresence WithTimestamps(Timestamps timestamps)
        {
            Timestamps = timestamps;
            return this;
        }
        
        public RichPresence WithAssets(Assets assets)
        {
            Assets = assets;
            return this;
        }
        
        public RichPresence WithParty(Party party)
        {
            Party = party;
            return this;
        }
        
        public RichPresence WithSecrets(Secrets secrets)
        {
            Secrets = secrets;
            return this;
        }
        
        public RichPresence Clone()
        {
            return new RichPresence
            {
                State = _state != null ? _state.Clone() as string : null,
                Details = _details != null ? _details.Clone() as string : null,
                
                Buttons = !this.HasButtons() ? (Button[]) null : this.Buttons.Clone() as Button[],
                Secrets = !HasSecrets() ? null : new Secrets
                {
                    JoinSecret = Secrets.JoinSecret != null ? Secrets.JoinSecret.Clone() as string : null,
                    SpectateSecret = Secrets.SpectateSecret != null ? Secrets.SpectateSecret.Clone() as string : null
                },

                Timestamps = !HasTimestamps() ? null : new Timestamps
                {
                    Start = Timestamps.Start,
                    End = Timestamps.End
                },

                Assets = !HasAssets() ? null : new Assets
                {
                    LargeImageKey = Assets.LargeImageKey != null ? Assets.LargeImageKey.Clone() as string : null,
                    LargeImageText = Assets.LargeImageText != null ? Assets.LargeImageText.Clone() as string : null,
                    SmallImageKey = Assets.SmallImageKey != null ? Assets.SmallImageKey.Clone() as string : null,
                    SmallImageText = Assets.SmallImageText != null ? Assets.SmallImageText.Clone() as string : null
                },

                Party = !HasParty() ? null : new Party
                {
                    ID = Party.ID,
                    Size = Party.Size,
                    Max = Party.Max,
                    Privacy = Party.Privacy,
                },

            };
        }
        
        internal RichPresence Merge(RichPresenceBase presence)
        {
            _state = presence.State;
            _details = presence.Details;
            Party = presence.Party;
            Timestamps = presence.Timestamps;
            Secrets = presence.Secrets;
            
            if (presence.HasAssets())
            {
                if (!HasAssets()) Assets = presence.Assets;
                else Assets.Merge(presence.Assets);
            }
            else Assets = null;
            
            return this;
        }
        
        internal override bool Matches(RichPresence other)
        {
            if (!base.Matches(other) || Buttons == null ^ other.Buttons == null) return false;
            
            if (Buttons == null) return true;
            
            if (Buttons.Length != other.Buttons.Length) return false;
            for (var index = 0; index < Buttons.Length; ++index)
            {
                var button1 = Buttons[index];
                var button2 = other.Buttons[index];
                if (button1.Label != button2.Label || button1.Url != button2.Url) return false;
            }
            return true;
        }

        public static implicit operator bool(RichPresence presesnce) => presesnce != null;
    }
}