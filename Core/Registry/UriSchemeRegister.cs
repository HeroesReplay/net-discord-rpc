using System;
using System.Diagnostics;
using NetDiscordRpc.Core.Logger;

namespace NetDiscordRpc.Core.Registry
{
    public class UriSchemeRegister
	{
        public string ApplicationID { get; set; }
        
        public string SteamAppID { get; set; }
        
        public bool UsingSteamApp => !string.IsNullOrEmpty(SteamAppID) && SteamAppID != "";

        public string ExecutablePath { get; set; }

        private IConsoleLogger _logger;
        
        public UriSchemeRegister(IConsoleLogger logger, string applicationID, string steamAppID = null, string executable = null)
        {
            _logger = logger;
            ApplicationID = applicationID.Trim();
            SteamAppID = steamAppID != null ? steamAppID.Trim() : null;
            ExecutablePath = executable ?? GetApplicationLocation();
        }
        
        public bool RegisterUriScheme()
        {
            IUriSchemeCreator creator;
            switch(Environment.OSVersion.Platform)
            {
                case PlatformID.Win32Windows:
                case PlatformID.Win32S:
                case PlatformID.Win32NT:
                case PlatformID.WinCE:
                    _logger.Trace("Creating Windows Scheme Creator");
                    creator = new WindowsUriSchemeCreator(_logger);
                break;

                case PlatformID.Unix:
                    _logger.Trace("Creating Unix Scheme Creator");
                    creator = new UnixUriSchemeCreator(_logger);
                break;
                
                case PlatformID.MacOSX:
                    _logger.Trace("Creating MacOSX Scheme Creator");
                    creator = new MacUriSchemeCreator(_logger);
                break;

                case PlatformID.Xbox:
                case PlatformID.Other:
                    _logger.Error($"Unkown Platform: {Environment.OSVersion.Platform}");
                    throw new PlatformNotSupportedException("Platform does not support registration.");
                default:
                    _logger.Error($"Unkown Platform: {Environment.OSVersion.Platform}");
                    throw new PlatformNotSupportedException("Platform does not support registration.");
            }

            if (!creator.RegisterUriScheme(this)) return false;
            
            _logger.Info("URI scheme registered.");
            
            return true;

        }
        
        public static string GetApplicationLocation() => Process.GetCurrentProcess().MainModule.FileName;
    }
}