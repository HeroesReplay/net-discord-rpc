using System;
using System.Diagnostics;
using System.IO;
using NetDiscordRpc.Core.Logger;

namespace NetDiscordRpc.Core.Registry
{
    internal class UnixUriSchemeCreator : IUriSchemeCreator
    {
        private IConsoleLogger logger;
        public UnixUriSchemeCreator(IConsoleLogger logger)
        {
            this.logger = logger;
        }

        public bool RegisterUriScheme(UriSchemeRegister register)
        {
            var home = Environment.GetEnvironmentVariable("HOME");
            
            if (string.IsNullOrEmpty(home))
            {
                logger.Error("Failed to register because the HOME variable was not set.");
                return false;
            }

            var exe = register.ExecutablePath;
            
            if (string.IsNullOrEmpty(exe))
            {
                logger.Error("Failed to register because the application was not located.");
                return false;
            }
            
            string command;
            if (register.UsingSteamApp) command = $"xdg-open steam://rungameid/{register.SteamAppID}";
            
            else command = exe;

            const string desktopFileFormat = @"[Desktop Entry]Name=Game {0}Exec={1} %uType=ApplicationNoDisplay=trueCategories=Discord;Games;MimeType=x-scheme-handler/discord-{2}";
            
            var file = string.Format(desktopFileFormat, register.ApplicationID, command, register.ApplicationID);
            
            var filename = $"/discord-{register.ApplicationID}.desktop";
            var filepath = home + "/.local/share/applications";
            
            var directory = Directory.CreateDirectory(filepath);
            if (!directory.Exists)
            {
                logger.Error($"Failed to register because {filepath} does not exist");
                return false;
            }
            
            File.WriteAllText($"{filepath + filename}", file);
            
            if (!RegisterMime(register.ApplicationID))
            {
                logger.Error("Failed to register because the Mime failed.");
                return false;
            }

            logger.Trace($"Registered {filepath + filename}, {file}, {command}");
            return true;
        }

        private static bool RegisterMime(string appid)
        {
            const string format = "default discord-{0}.desktop x-scheme-handler/discord-{0}";
            var arguments = string.Format(format, appid);
            
            var process = Process.Start("xdg-mime", arguments);
            process.WaitForExit();
            
            return process.ExitCode >= 0;
        }
    }
}