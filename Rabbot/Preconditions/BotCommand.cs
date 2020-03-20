using Discord;
using Discord.Commands;
using Discord.WebSocket;
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

            using (swaightContext db = new swaightContext())
            {
                if (db.Guild.Where(p => p.ServerId == context.Guild.Id).Any())
                {
                    if (db.Guild.FirstOrDefault(p => p.ServerId == context.Guild.Id).Botchannelid == null)
                        return Task.FromResult(PreconditionResult.FromSuccess());
                    var botChannel = db.Guild.FirstOrDefault(p => p.ServerId == context.Guild.Id).Botchannelid;
                    if (botChannel == context.Channel.Id)
                        return Task.FromResult(PreconditionResult.FromSuccess());
                    else
                    {
                        Task.Run(() => sendMessage(context, botChannel));
                        var EXP = db.Userfeatures.FirstOrDefault(p => p.UserId == context.User.Id && p.ServerId == context.Guild.Id);
                        if (EXP != null && EXP.Exp > 500)
                        {
                            EXP.Exp -= 100;
                        }
                        db.SaveChanges();
                        return Task.FromResult(PreconditionResult.FromError("Wrong channel."));
                    }
                }
                else
                {
                    return Task.FromResult(PreconditionResult.FromSuccess());
                }
            }
        }

        private async Task sendMessage(ICommandContext context, ulong? botChannel)
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
