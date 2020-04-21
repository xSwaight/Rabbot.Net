using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Rabbot.Database;
using Rabbot.Services;
using Serilog;
using Serilog.Core;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Rabbot
{
    class CommandHandler
    {
        private readonly DiscordShardedClient _client;
        private readonly CommandService _commands;
        private readonly DatabaseService _databaseService;
        private readonly IServiceProvider _provider;
        private static readonly ILogger _logger = Log.ForContext(Serilog.Core.Constants.SourceContextPropertyName, nameof(CommandHandler));

        public CommandHandler(IServiceProvider provider)
        {
            _provider = provider;
            _client = _provider.GetService<DiscordShardedClient>();
            _commands = _provider.GetService<CommandService>();
            _databaseService = _provider.GetService<DatabaseService>();
            _client.MessageReceived += HandleCommandAsync;
        }

        private async Task HandleCommandAsync(SocketMessage s)
        {
            if (!(s is SocketUserMessage msg)) 
                return;
            var context = new ShardedCommandContext(_client, msg);
            if (context.User.IsBot || (context.IsPrivate && !msg.Content.Contains(Config.Bot.CmdPrefix + "hdf")))
                return;
            int argPos = 0;
            IResult result = null;
            if (msg.HasStringPrefix(Config.Bot.CmdPrefix, ref argPos))
            {
                result = await _commands.ExecuteAsync(context, argPos, _provider);
                LogCommandUsage(context, result);
                if (!result.IsSuccess && result.Error != CommandError.UnknownCommand)
                {
                    _logger.Information(result.ErrorReason);
                }
            }
            else
            {
                await SendAnswerIfTagged(msg.Content, context);
            }

            if (result != null && result.Error == CommandError.UnknownCommand)
            {
                await SendAnswerIfTagged(msg.Content, context);
            }
        }
        private async Task SendAnswerIfTagged(string message, SocketCommandContext context)
        {
            Regex regex = new Regex(@"<@[0-9!]{18,19}>");
            var matches = regex.Matches(message);
            if (!matches.Any(p => p.Value.Contains(context.Client.CurrentUser.Id.ToString())))
                return;

            using (var db = _databaseService.Open<RabbotContext>())
            {
                if (!db.RandomAnswers.Any())
                    return;
                using (context.Channel.EnterTypingState())
                {
                    var answerList = db.RandomAnswers.ToList();
                    var rnd = new Random();
                    var index = rnd.Next(0, answerList.Count());
                    await Task.Delay(rnd.Next(500, 2000));
                    await context.Channel.SendMessageAsync(answerList[index].Answer);
                }
            }
        }

        private void LogCommandUsage(SocketCommandContext context, IResult result)
        {
            if (!result.IsSuccess)
                return;
            if (context.Channel is IGuildChannel)
            {
                var logTxt = $"User: [{context.User.Username}] Server: [{context.Guild.Name}] Channel: [{context.Channel.Name}] -> [{context.Message.Content}]";
                _logger.Information(logTxt);
            }
            else
            {
                var logTxt = $"User: [{context.User.Username}] -> [{context.Message.Content}]";
                _logger.Information(logTxt);
            }
        }
    }
}
