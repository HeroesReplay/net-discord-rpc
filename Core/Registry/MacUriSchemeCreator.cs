using System.IO;
using NetDiscordRpc.Core.Logger;

namespace NetDiscordRpc.Core.Registry
{
    internal class MacUriSchemeCreator: IUriSchemeCreator
    {
        private IConsoleLogger logger;
        public MacUriSchemeCreator(IConsoleLogger logger)
        {
            this.logger = logger;
        }

        public bool RegisterUriScheme(UriSchemeRegister register)
        {
            var exe = register.ExecutablePath;
            
            if (string.IsNullOrEmpty(exe))
            {
                logger.Error("Failed to register because the application could not be located.");
                return false;
            }
            
            logger.Trace("Registering Steam Command");
            
            var command = exe;
            if (register.UsingSteamApp) command = $"steam://rungameid/{register.SteamAppID}";
            else logger.Warning("This library does not fully support MacOS URI Scheme Registration.");
            
            const string filepath = "~/Library/Application Support/discord/games";
            var directory = Directory.CreateDirectory(filepath);
            
            if (!directory.Exists)
            {
                logger.Error($"Failed to register because {filepath} does not exist");
                return false;
            }
            
            File.WriteAllText($"{filepath}/{register.ApplicationID}.json", "{ \"command\": \"" + command + "\" }");
            logger.Trace($"Registered {filepath}/{register.ApplicationID}.json, {command}");
            
            return true;
        }
        
    }
}