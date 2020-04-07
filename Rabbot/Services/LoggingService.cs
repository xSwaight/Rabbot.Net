using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Core;
using System;
using System.Threading.Tasks;

namespace Rabbot.Services
{
    public class LoggingService
    {
        private static readonly ILogger _logger = Log.ForContext(Serilog.Core.Constants.SourceContextPropertyName, nameof(LoggingService));
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;

        public LoggingService(IServiceProvider services)
        {
            _discord = services.GetRequiredService<DiscordSocketClient>();
            _commands = services.GetRequiredService<CommandService>();

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
