using System;
using NetDiscordRpc.Core.Exceptions;
using Newtonsoft.Json;

namespace NetDiscordRpc.Users
{
    public class User
    {
        [JsonProperty("id")]
        public ulong ID { get; private set; }
        
        [JsonProperty("username")]
        public string Username { get; private set; }
        
        [JsonProperty("discriminator")]
        public int Discriminator { get; private set; }
        
        [JsonProperty("avatar")]
        public string Avatar { get; private set; }
        
        [JsonProperty("flags")]
        public UserFlags Flags { get; private set; }
        
        [JsonProperty("premium_type")]
        public PremiumTypes Premium { get; private set; }
        public string CdnEndpoint { get; private set; }

        internal User() => CdnEndpoint = "cdn.discordapp.com";

        internal void SetConfiguration(Configuration configuration) => CdnEndpoint = configuration.CdnHost;

        public string GetAvatar(AvatarFormats format, AvatarSizes size = AvatarSizes.x128)
        {
            var endpoint = $"/avatars/{ID}/{Avatar}";

            if (!string.IsNullOrEmpty(Avatar))
            {
                return $"{CdnEndpoint}/{endpoint}.{GetAvatarExtension(format)}?size={size}";
            }

            if (format != AvatarFormats.PNG)
            {
                throw new BadImageFormatException($"The user has no avatar and the requested format {format.ToString()} is not supported. (Only supports PNG).");
            }
            
            endpoint = $"/embed/avatars/{Discriminator % 5}";

            return $"https://{CdnEndpoint}/{endpoint}.{GetAvatarExtension(format)}?size={size}";
        }

        public static string GetAvatarExtension(AvatarFormats format) => format.ToString().ToLowerInvariant();

        public override string ToString() => $"{Username}#{Discriminator.ToString("D4")}";
    }
}