using System;
using NetDiscordRpc.Core.Logger;

namespace NetDiscordRpc.Core.Registry
{
    internal class WindowsUriSchemeCreator : IUriSchemeCreator
    {
        private IConsoleLogger logger;
        public WindowsUriSchemeCreator(IConsoleLogger logger)
        {
            this.logger = logger;
        }

        public bool RegisterUriScheme(UriSchemeRegister register)
        {
            if (Environment.OSVersion.Platform is PlatformID.Unix or PlatformID.MacOSX)
            {
                throw new PlatformNotSupportedException("URI schemes can only be registered on Windows");
            }
            
            var location = register.ExecutablePath;
            
            if (location == null)
            {
                logger.Error("Failed to register application because the location was null.");
                return false;
            }
            
            var scheme = $"discord-{register.ApplicationID}";
            var friendlyName = $"Run game {register.ApplicationID} protocol";
            var command = location;
            
            if (register.UsingSteamApp)
            {
                var steam = GetSteamLocation();
                if (steam != null) command = $"\"{steam}\" steam://rungameid/{register.SteamAppID}";
            }
            
            CreateUriScheme(scheme, friendlyName, location, command);
            return true;
        }
        
        private void CreateUriScheme(string scheme, string friendlyName, string defaultIcon, string command)
        {
            using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey($"SOFTWARE\\Classes\\{scheme}"))
            {
                key.SetValue("", "URL:" + friendlyName);
                key.SetValue("URL Protocol", "");

                using (var iconKey = key.CreateSubKey("DefaultIcon")) iconKey.SetValue("", defaultIcon);

                using (var commandKey = key.CreateSubKey("shell\\open\\command")) commandKey.SetValue("", command);
            }

            logger.Trace($"Registered {scheme}, {friendlyName}, {command}");
        }
        
        public static string GetSteamLocation()
        {
            using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Software\\Valve\\Steam"))
            {
                return key?.GetValue("SteamExe") as string;
            }
        }
    }
}