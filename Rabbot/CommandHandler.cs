using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Rabbot.Database;
using Serilog;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Rabbot
{
    class CommandHandler
    {
        DiscordSocketClient _client;
        CommandService _commands;
        private readonly IServiceProvider _provider;
        private readonly ILogger _logger;

        public CommandHandler(IServiceProvider provider)
        {
            _provider = provider;
            _client = _provider.GetService<DiscordSocketClient>();
            _commands = _provider.GetService<CommandService>();
            _client.MessageReceived += HandleCommandAsync;
            _logger = Log.ForContext<CommandHandler>();
        }

        private async Task HandleCommandAsync(SocketMessage s)
        {
            SocketUserMessage msg = s as SocketUserMessage;
            if (msg == null) return;
            var context = new SocketCommandContext(_client, msg);
            if (context.User.IsBot || (context.IsPrivate && !msg.Content.Contains(Config.bot.cmdPrefix + "hdf")))
                return;
            int argPos = 0;
            IResult result = null;
            if (msg.HasStringPrefix(Config.bot.cmdPrefix, ref argPos))
            {
                result = await _commands.ExecuteAsync(context, argPos, _provider);
                LogCommandUsage(context, result);
                if (!result.IsSuccess && result.Error != CommandError.UnknownCommand)
                {
                    _logger.Warning(result.ErrorReason);
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
            var match = regex.Match(message);
            if (!match.Value.Contains(context.Client.CurrentUser.Id.ToString()))
                return;

            using (swaightContext db = new swaightContext())
            {
                if (!db.Randomanswer.Any())
                    return;
                using (context.Channel.EnterTypingState())
                {
                    var answerList = db.Randomanswer.ToList();
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
                var logTxt = $"User: [{context.User.Username}] Discord Server: [{context.Guild.Name}] -> [{context.Message.Content}]";
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
