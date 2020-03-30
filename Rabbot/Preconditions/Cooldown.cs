using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Rabbot.Database;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rabbot.Preconditions
{
    public class Cooldown : PreconditionAttribute
    {
        TimeSpan CooldownLength { get; set; }
        bool AdminsAreLimited { get; set; }
        readonly ConcurrentDictionary<CooldownInfo, DateTime> _cooldowns = new ConcurrentDictionary<CooldownInfo, DateTime>();

        public Cooldown(int seconds, bool adminsAreLimited = false)
        {
            CooldownLength = TimeSpan.FromSeconds(seconds);
            AdminsAreLimited = adminsAreLimited;
        }
        public struct CooldownInfo
        {
            public ulong UserId { get; }
            public int CommandHashCode { get; }

            public CooldownInfo(ulong userId, int commandHashCode)
            {
                UserId = userId;
                CommandHashCode = commandHashCode;
            }
        }

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (!AdminsAreLimited && context.User is IGuildUser user && user.GuildPermissions.ManageRoles)
                return Task.FromResult(PreconditionResult.FromSuccess());

            var key = new CooldownInfo(context.User.Id, command.GetHashCode());
            if (_cooldowns.TryGetValue(key, out DateTime endsAt))
            {
                var difference = endsAt.Subtract(DateTime.UtcNow);
                if (difference.Ticks > 0)
                {
                    Task.Run(async () => await SendMessage(context, command));
                    return Task.FromResult(PreconditionResult.FromError($"User: [{context.User.Username}] used {context.Message.Content} too fast! [{context.Guild.Name}] Channel: [{context.Channel.Name}]"));
                }
                var time = DateTime.UtcNow.Add(CooldownLength);
                _cooldowns.TryUpdate(key, time, endsAt);
            }
            else
            {
                _cooldowns.TryAdd(key, DateTime.UtcNow.Add(CooldownLength));
            }
            return Task.FromResult(PreconditionResult.FromSuccess());
        }

        private async Task SendMessage(ICommandContext context, CommandInfo command)
        {
            await context.Message.DeleteAsync();
            var key = new CooldownInfo(context.User.Id, command.GetHashCode());
            _cooldowns.TryGetValue(key, out DateTime endsAt);
            var difference = endsAt.Subtract(DateTime.UtcNow);
            const int delay = 3000;
            var embed = new EmbedBuilder();
            embed.WithDescription($"{context.User.Mention} entspann dich. Versuch es in {Convert.ToInt32(difference.TotalSeconds)} Sekunde(n) noch einmal!");
            embed.WithColor(new Color(90, 92, 96));
            IUserMessage m = await context.Channel.SendMessageAsync("", false, embed.Build());
            await Task.Delay(delay);
            await m.DeleteAsync();
        }
    }
}