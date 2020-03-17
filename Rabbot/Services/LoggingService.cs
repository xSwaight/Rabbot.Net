using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Serilog;
using System.Threading.Tasks;

namespace Rabbot.Services
{
    public class LoggingService
    {
        private readonly ILogger _logger;
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;

        public LoggingService(DiscordSocketClient discord, CommandService commands)
        {
            _discord = discord;
            _commands = commands;
            _logger = Log.ForContext<LoggingService>();

            _discord.Log += OnLogAsync;
            _commands.Log += OnLogAsync;
        }

        public Task OnLogAsync(LogMessage msg)
        {
            string logText = $"{msg.Source}: {msg.Exception?.ToString() ?? msg.Message}";

            switch (msg.Severity)
            {
                case LogSeverity.Critical:
                    {
                        _logger.Fatal(logText);
                        break;
                    }
                case LogSeverity.Warning:
                    {
                        _logger.Warning(logText);
                        break;
                    }
                case LogSeverity.Info:
                    {
                        _logger.Information(logText);
                        break;
                    }
                case LogSeverity.Verbose:
                    {
                        _logger.Verbose(logText);
                        break;
                    }
                case LogSeverity.Debug:
                    {
                        _logger.Debug(logText);
                        break;
                    }
                case LogSeverity.Error:
                    {
                        _logger.Error(logText);
                        break;
                    }
            }
            return Task.CompletedTask;
        }
    }
}
