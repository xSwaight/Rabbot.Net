using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBot_Core.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot_Core.Preconditions
{
    class BotCommand : PreconditionAttribute
    {
        bool AdminsAreLimited { get; set; }

        public BotCommand(bool adminsAreLimited = false)
        {

        }

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (!AdminsAreLimited && context.User is IGuildUser user && user.GuildPermissions.Administrator)
                return Task.FromResult(PreconditionResult.FromSuccess());

            using (discordbotContext db = new discordbotContext())
            {
                if (db.Guild.Where(p => p.ServerId == (long)context.Guild.Id).Count() != 0)
                {
                    if(db.Guild.Where(p => p.ServerId == (long)context.Guild.Id).FirstOrDefault().Botchannelid == null)
                        return Task.FromResult(PreconditionResult.FromSuccess());
                    var botChannel = db.Guild.Where(p => p.ServerId == (long)context.Guild.Id).FirstOrDefault().Botchannelid;
                    if(botChannel == (long)context.Channel.Id)
                        return Task.FromResult(PreconditionResult.FromSuccess());
                    else
                    {
                        Task.Run(() => sendMessage(context, botChannel));
                        return Task.FromResult(PreconditionResult.FromError("Wrong channel."));
                    }
                }
                else
                {
                    return Task.FromResult(PreconditionResult.FromSuccess());
                }
            }
        }

        private async Task sendMessage(ICommandContext context, long? botChannel)
        {
            await context.Message.DeleteAsync();
            var guild = context.Guild as SocketGuild;
            var dcBotChannel = guild.TextChannels.Where(p => p.Id == (ulong)botChannel).FirstOrDefault();
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
