using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Rabbot
{
    class CommandHandler
    {
        DiscordSocketClient _client;
        CommandService _commands;
        private readonly IServiceProvider _provider;
        private readonly ILogger _logger;

        public CommandHandler(IServiceProvider provider, ILogger<CommandHandler> logger)
        {
            _provider = provider;
            _client = _provider.GetService<DiscordSocketClient>();
            _commands = _provider.GetService<CommandService>();
            _client.MessageReceived += HandleCommandAsync;
            _logger = logger;
        }

        private async Task HandleCommandAsync(SocketMessage s)
        {
            SocketUserMessage msg = s as SocketUserMessage;
            if (msg == null) return;
            var context = new SocketCommandContext(_client, msg);
            if (context.User.IsBot || (context.IsPrivate && !msg.Content.Contains(Config.bot.cmdPrefix + "hdf")))
                return;
            int argPos = 0;
            if (msg.HasStringPrefix(Config.bot.cmdPrefix, ref argPos) || msg.HasMentionPrefix(_client.CurrentUser, ref argPos))
            {
                var result = await _commands.ExecuteAsync(context, argPos, _provider);
                await LogCommandUsage(context, result);
                if (!result.IsSuccess && result.Error != CommandError.UnknownCommand)
                {
                    Console.WriteLine(result.ErrorReason);
                }
            }
        }

        private async Task LogCommandUsage(SocketCommandContext context, IResult result)
        {
            if (context.Channel is IGuildChannel)
            {
                var logTxt = $"User: [{context.User.Username}] Discord Server: [{context.Guild.Name}] -> [{context.Message.Content}]";
                _logger.LogInformation(logTxt);
            }
            else
            {
                var logTxt = $"User: [{context.User.Username}] -> [{context.Message.Content}]";
                _logger.LogInformation(logTxt);
            }
        }
    }
}
