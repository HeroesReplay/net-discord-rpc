using System;
using Newtonsoft.Json;

namespace NetDiscordRpc.RPC
{
    [Serializable]
    public class Timestamps
    {
        [JsonIgnore]
        public DateTime? Start { get; set; }
        
        [JsonIgnore]
        public DateTime? End { get; set; }

        public Timestamps()
        {
            Start = null;
            End = null;
        }
        
        public Timestamps(DateTime start, DateTime? end = null)
        {
            Start = start;
            End = end;
        }
        
        public static Timestamps FromTimeSpan(double seconds) => FromTimeSpan(TimeSpan.FromSeconds(seconds));
        
        public static Timestamps FromTimeSpan(TimeSpan timespan)
        {
            return new Timestamps()
            {
                Start = DateTime.UtcNow,
                End = DateTime.UtcNow + timespan
            };
        }
        
        public static Timestamps Now => new Timestamps(DateTime.UtcNow, end: null);

        [JsonProperty("start", NullValueHandling = NullValueHandling.Ignore)]
        public ulong? StartUnixMilliseconds
        {
            get => Start.HasValue ? ToUnixMilliseconds(Start.Value) : null;
            set => Start = value.HasValue ? FromUnixMilliseconds(value.Value) : null;
        }
        
        [JsonProperty("end", NullValueHandling = NullValueHandling.Ignore)]
        public ulong? EndUnixMilliseconds
        {
            get => End.HasValue ? ToUnixMilliseconds(End.Value) : null;
            set => End = value.HasValue ? FromUnixMilliseconds(value.Value) : null;
        }
        
        public static DateTime FromUnixMilliseconds(ulong unixTime)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return epoch.AddMilliseconds(Convert.ToDouble(unixTime));
        }
        
        public static ulong ToUnixMilliseconds(DateTime date)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return Convert.ToUInt64((date - epoch).TotalMilliseconds);
        }
    }
}