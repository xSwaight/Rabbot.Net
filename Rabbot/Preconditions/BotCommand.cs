using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Rabbot.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rabbot.Preconditions
{
    public class BotCommand : PreconditionAttribute
    {
        bool AdminsAreLimited { get; set; }
        public BotCommand(bool adminsAreLimited = false)
        {
            AdminsAreLimited = adminsAreLimited;
        }

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (!AdminsAreLimited && context.User is IGuildUser user && user.GuildPermissions.ManageRoles)
                return Task.FromResult(PreconditionResult.FromSuccess());

            using (RabbotContext db = services.GetRequiredService<RabbotContext>())
            {
                if (db.Guilds.Where(p => p.GuildId == context.Guild.Id).Any())
                {
                    if (db.Guilds.FirstOrDefault(p => p.GuildId == context.Guild.Id).BotChannelId == null)
                        return Task.FromResult(PreconditionResult.FromSuccess());
                    var botChannel = db.Guilds.FirstOrDefault(p => p.GuildId == context.Guild.Id).BotChannelId;
                    if (botChannel == context.Channel.Id)
                        return Task.FromResult(PreconditionResult.FromSuccess());
                    else
                    {
                        Task.Run(async () => await SendMessage(context, botChannel));
                        return Task.FromResult(PreconditionResult.FromError($"User: [{context.User.Username}] used {context.Message.Content} in a wrong channel. Server: [{context.Guild.Name}] Channel: [{context.Channel.Name}]"));
                    }
                }
                else
                {
                    return Task.FromResult(PreconditionResult.FromSuccess());
                }
            }
        }

        private async Task SendMessage(ICommandContext context, ulong? botChannel)
        {
            await context.Message.DeleteAsync();
            var guild = context.Guild as SocketGuild;
            var dcBotChannel = guild.TextChannels.FirstOrDefault(p => p.Id == botChannel);
            const int delay = 3000;
            var embed = new EmbedBuilder();
            embed.WithDescription($"Dieser Command kann nur im {dcBotChannel.Mention} Channel ausgeführt werden.");
            embed.WithColor(new Color(90, 92, 96));
            IUserMessage m = await context.Channel.SendMessageAsync("", false, embed.Build());
            await Task.Delay(delay);
            await m.DeleteAsync();
        }
    }
}
