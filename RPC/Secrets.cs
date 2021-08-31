using System;
using System.Text;
using NetDiscordRpc.Core.Exceptions;
using Newtonsoft.Json;

namespace NetDiscordRpc.RPC
{
    [Serializable]
    public class Secrets
    {
        private string _joinSecret;
        private string _spectateSecret;
        public static Encoding Encoding => Encoding.UTF8;
        public static int SecretLength => 128;
        
        [JsonProperty("join", NullValueHandling = NullValueHandling.Ignore)]
        public string JoinSecret
        {
            get { return _joinSecret; }
            set
            {
                if (!RichPresenceBase.ValidateString(value, out _joinSecret, 128, Encoding.UTF8))
                {
                    throw new StringOutOfRangeException(128);
                }
            }
        }
        
        [JsonProperty("spectate", NullValueHandling = NullValueHandling.Ignore)]
        public string SpectateSecret
        {
            get { return _spectateSecret; }
            set
            {
                if (!RichPresenceBase.ValidateString(value, out _spectateSecret, 128, Encoding.UTF8))
                {
                    throw new StringOutOfRangeException(128);
                }
            }
        }
        
        public static string CreateSecret(Random random)
        {
            var bytes = new byte[SecretLength];
            random.NextBytes(bytes);
            
            return Encoding.GetString(bytes);
        }
        
        public static string CreateFriendlySecret(Random random)
        {
            const string charset = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var secret = "";

            for (var i = 0; i < SecretLength; i++)
            {
                secret += charset[random.Next(charset.Length)];
            }

            return secret;
        }
    }
}