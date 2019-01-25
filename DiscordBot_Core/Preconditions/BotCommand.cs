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
        int counter { get; set; }

        public BotCommand(bool adminsAreLimited = false)
        {

        }

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (!AdminsAreLimited && context.User is IGuildUser user && user.GuildPermissions.Administrator)
                return Task.FromResult(PreconditionResult.FromSuccess());

            using (swaightContext db = new swaightContext())
            {
                if (db.Guild.Where(p => p.ServerId == (long)context.Guild.Id).Count() != 0)
                {
                    if (db.Guild.Where(p => p.ServerId == (long)context.Guild.Id).FirstOrDefault().Botchannelid == null)
                        return Task.FromResult(PreconditionResult.FromSuccess());
                    var botChannel = db.Guild.Where(p => p.ServerId == (long)context.Guild.Id).FirstOrDefault().Botchannelid;
                    if (botChannel == (long)context.Channel.Id)
                        return Task.FromResult(PreconditionResult.FromSuccess());
                    else
                    {
                        Task.Run(() => sendMessage(context, botChannel));
                        counter++;

                        if (counter >= 5)
                        {

                            var mutedUser = context.User as SocketGuildUser;
                            string userRoles = "";
                            foreach (var role in mutedUser.Roles)
                            {
                                if (!role.IsEveryone && !role.IsManaged)
                                    userRoles += role.Name + "|";
                            }
                            userRoles = userRoles.TrimEnd('|');
                            DateTime banUntil = DateTime.Now.AddMinutes(10);
                            db.Muteduser.Add(new Muteduser { ServerId = (long)context.Guild.Id, UserId = (long)context.User.Id, Duration = banUntil, Roles = userRoles });
                            db.SaveChanges();
                            Task.Run(() => sendPrivate(context, banUntil));

                        }
                        return Task.FromResult(PreconditionResult.FromError("Wrong channel."));
                    }
                }
                else
                {
                    counter = 0;
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

        private async Task sendPrivate(ICommandContext context, DateTime banUntil)
        {
            await Helper.SendLogBotCommandMute(context);
            var embedPrivate = new EmbedBuilder();
            embedPrivate.WithDescription($"Du wurdest auf **{context.Guild.Name}** für **10 Minuten** gemuted.");
            embedPrivate.AddField("Gemuted bis", banUntil.ToShortDateString() + " " + banUntil.ToShortTimeString());
            embedPrivate.WithFooter($"Bei einem ungerechtfertigten Mute kontaktiere bitte einen Admin vom {context.Guild.Name} Server.");
            embedPrivate.WithColor(new Color(255, 0, 0));
            await context.User.SendMessageAsync(null, false, embedPrivate.Build());
        }
    }
}
