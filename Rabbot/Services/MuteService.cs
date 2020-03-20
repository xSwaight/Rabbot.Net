using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Rabbot.Database;
using Serilog;
using Serilog.Core;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Rabbot.Services
{
    class MuteService
    {
        private SocketGuildUser DcTargetUser { get; set; }
        private SocketGuild DcGuild { get; set; }
        private DiscordSocketClient DcClient { get; set; }
        private User User { get; set; }
        private Guild Guild { get; set; }
        private Muteduser MuteUser { get; set; }
        private SocketRole MutedRole { get; set; }

        private static readonly ILogger _logger = Log.ForContext(Serilog.Core.Constants.SourceContextPropertyName, nameof(MuteService));
        public MuteService(DiscordSocketClient client)
        {
            DcClient = client;
        }
        public async Task CheckMutes()
        {
            using (swaightContext db = new swaightContext())
            {
                if (!db.Muteduser.Any() || !DcClient.Guilds.Any())
                    return;
                var muteUsers = db.Muteduser.ToList();
                foreach (var mute in muteUsers)
                {
                    DcGuild = DcClient.Guilds.FirstOrDefault(p => p.Id == (ulong)mute.ServerId);
                    if (DcGuild == null)
                    {
                        db.Muteduser.Remove(mute);
                        await db.SaveChangesAsync();
                        continue;
                    }

                    MutedRole = DcGuild.Roles.FirstOrDefault(p => p.Name == "Muted");
                    if (MutedRole == null)
                        continue;
                    if (DcGuild.CurrentUser == null)
                        continue;
                    int position = GetBotRolePosition(DcGuild.CurrentUser);
                    DcTargetUser = DcGuild.Users.FirstOrDefault(p => p.Id == (ulong)mute.UserId);
                    if(DcTargetUser == null)
                    {
                        if (mute.Duration < DateTime.Now)
                        {
                            db.Muteduser.Remove(mute);
                            await db.SaveChangesAsync();
                        }
                        continue;
                    }

                    if (DcGuild.CurrentUser.GuildPermissions.ManageRoles == true && position > MutedRole.Position)
                    {
                        if (mute.Duration < DateTime.Now)
                        {
                            db.Muteduser.Remove(mute);
                            try
                            {
                                await DcTargetUser.RemoveRoleAsync(MutedRole);
                                var oldRoles = mute.Roles.Split('|');
                                foreach (var oldRole in oldRoles)
                                {
                                    var role = DcGuild.Roles.FirstOrDefault(p => p.Name == oldRole);
                                    if (role != null)
                                        await DcTargetUser.AddRoleAsync(role);
                                }
                                await Logging.Unmuted(DcTargetUser);
                            }
                            catch (Exception e)
                            {
                                _logger.Error(e, $"Error while adding roles");
                            }
                        }
                        else
                        {
                            if (!DcTargetUser.Roles.Where(p => p.Id == MutedRole.Id).Any())
                            {
                                try
                                {
                                    var oldRoles = mute.Roles.Split('|');
                                    foreach (var oldRole in oldRoles)
                                    {
                                        if (!oldRole.Contains("everyone"))
                                        {
                                            var role = DcGuild.Roles.FirstOrDefault(p => p.Name == oldRole);
                                            if (role != null)
                                                await DcTargetUser.RemoveRoleAsync(role);
                                        }
                                    }
                                    await DcTargetUser.AddRoleAsync(MutedRole);
                                }
                                catch (Exception e)
                                {
                                    _logger.Error(e, $"Error while removing roles");
                                }
                            }
                        }
                    }
                }
                await db.SaveChangesAsync();
            }
        }

        public async Task MuteTargetUser(IUser user, string duration, SocketCommandContext context)
        {
            MutedRole = context.Guild.Roles.FirstOrDefault(p => p.Name == "Muted");
            if(MutedRole == null)
            {
                await SendError($"Es existiert keine Muted Rolle!", context);
                return;
            }
            DcTargetUser = user as SocketGuildUser;
            DcGuild = context.Guild;
            var targetPosition = GetTargetRolePosition(DcTargetUser);
            var botPosition = GetBotRolePosition(DcGuild.CurrentUser);

            if (!(MutedRole.Position > targetPosition && DcGuild.CurrentUser.GuildPermissions.ManageRoles))
            {
                await SendError($"Mindestens eine meiner Rollen muss in der Reihenfolge über der Muted Rolle stehen!", context);
                return;
            }
            if(targetPosition > botPosition)
            {
                await SendError($"Es fehlen die Berechtigungen um {DcTargetUser.Mention} zu muten!", context);
                return;
            }
            if(context.User.Id == user.Id)
            {
                await SendError($"{user.Mention} du Trottel kannst dich nicht selber muten!", context);
                return;
            }

            DateTime date = DateTime.Now;
            DateTime banUntil;

            if (duration.Contains('s'))
                banUntil = date.AddSeconds(Convert.ToDouble(duration.Trim('s')));
            else if (duration.Contains('m'))
                banUntil = date.AddMinutes(Convert.ToDouble(duration.Trim('m')));
            else if (duration.Contains('h'))
                banUntil = date.AddHours(Convert.ToDouble(duration.Trim('h')));
            else if (duration.Contains('d'))
                banUntil = date.AddDays(Convert.ToDouble(duration.Trim('d')));
            else
                return;

            using (swaightContext db = new swaightContext())
            {
                if(!db.Muteduser.Where(p => p.ServerId == context.Guild.Id && p.UserId == user.Id).Any())
                {
                    string userRoles = "";
                    foreach (var role in DcTargetUser.Roles)
                    {
                        if (!role.IsEveryone && !role.IsManaged)
                            userRoles += role.Name + "|";
                    }
                    userRoles = userRoles.TrimEnd('|');
                    await db.Muteduser.AddAsync(new Muteduser { ServerId = context.Guild.Id, UserId = user.Id, Duration = banUntil, Roles = userRoles });
                }
                else
                {
                    var ban = db.Muteduser.FirstOrDefault(p => p.ServerId == context.Guild.Id && p.UserId == user.Id);
                    ban.Duration = banUntil;
                }
                await SendPrivate(DcGuild, banUntil, duration, DcTargetUser);
                await Logging.Mute(context, user, duration);
                await db.SaveChangesAsync();
            }
        }

        public async Task MuteWarnedUser(SocketGuildUser user, SocketGuild guild)
        {
            DcGuild = guild;
            DcTargetUser = user;
            MutedRole = DcGuild.Roles.FirstOrDefault(p => p.Name == "Muted");
            if (MutedRole == null)
                return;

            var targetPosition = GetTargetRolePosition(DcTargetUser);
            var botPosition = GetBotRolePosition(DcGuild.CurrentUser);

            if (!(MutedRole.Position > targetPosition && DcGuild.CurrentUser.GuildPermissions.ManageRoles))
                return;
            if (targetPosition > botPosition)
                return;

            DateTime date = DateTime.Now;
            DateTime banUntil = date.AddHours(1);


            using (swaightContext db = new swaightContext())
            {
                if (!db.Muteduser.Where(p => p.ServerId == DcGuild.Id && p.UserId == user.Id).Any())
                {
                    string userRoles = "";
                    foreach (var role in DcTargetUser.Roles)
                    {
                        if (!role.IsEveryone && !role.IsManaged)
                            userRoles += role.Name + "|";
                    }
                    userRoles = userRoles.TrimEnd('|');
                    await db.Muteduser.AddAsync(new Muteduser { ServerId = DcGuild.Id, UserId = user.Id, Duration = banUntil, Roles = userRoles });
                }
                else
                {
                    var ban = db.Muteduser.FirstOrDefault(p => p.ServerId == DcGuild.Id && p.UserId == user.Id);
                    ban.Duration = banUntil;
                }
                await SendPrivate(DcGuild, banUntil, "1 Stunde", user);
                await Logging.WarningMute(DcTargetUser);
                await db.SaveChangesAsync();
            }
        }

        public async Task UnmuteTargetUser(IUser user, SocketCommandContext context)
        {
            using (swaightContext db = new swaightContext())
            {
                var mute = db.Muteduser.Where(p => p.ServerId == context.Guild.Id && p.UserId == user.Id);
                if (!mute.Any())
                {
                    await SendError($"{user.Mention} ist nicht gemuted.", context);
                    return;
                }
                else
                {
                    Guild = db.Guild.FirstOrDefault(p => p.ServerId == context.Guild.Id);
                    DcTargetUser = user as SocketGuildUser;
                    MutedRole = DcTargetUser.Roles.FirstOrDefault(p => p.Name == "Muted");
                    if(MutedRole != null)
                    {
                        db.Muteduser.Remove(mute.FirstOrDefault());
                        await DcTargetUser.RemoveRoleAsync(MutedRole);
                        var oldRoles = mute.FirstOrDefault().Roles.Split('|');
                        await db.SaveChangesAsync();
                        foreach (var oldRole in oldRoles)
                        {
                            var role = context.Guild.Roles.FirstOrDefault(p => p.Name == oldRole);
                            if (role != null)
                                await DcTargetUser.AddRoleAsync(role);
                        }
                    }
                    await Logging.Unmuted(context, user);
                }

            }
        }

        private async Task SendError(string message, SocketCommandContext context)
        {
            var embed = new EmbedBuilder();
            embed.WithDescription(message);
            embed.WithColor(new Color(255, 0, 0));
            var Message = await context.Channel.SendMessageAsync("", false, embed.Build());
            await Task.Delay(5000);
            await Message.DeleteAsync();
        }

        private async Task SendPrivate(SocketGuild Guild, DateTime banUntil, string duration, SocketGuildUser user)
        {
            try
            {
                var embedPrivate = new EmbedBuilder();
                embedPrivate.WithDescription($"Du wurdest auf **{Guild.Name}** für **{duration}** gemuted.");
                embedPrivate.AddField("Gemuted bis", banUntil.ToShortDateString() + " " + banUntil.ToShortTimeString());
                embedPrivate.WithFooter($"Bei einem ungerechtfertigten Mute kontaktiere bitte einen Admin vom {Guild.Name} Server.");
                embedPrivate.WithColor(new Color(255, 0, 0));
                await user.SendMessageAsync(null, false, embedPrivate.Build());
            }
            catch (Exception e)
            {
                _logger.Error(e, $"Failed to send a private message");
            }

        }

        private int GetBotRolePosition(SocketGuildUser bot)
        {
            var roles = bot.Roles;
            int position = 0;
            foreach (var item in roles)
            {
                if (item.Position > position)
                    position = item.Position;
            }
            return position;
        }

        private int GetTargetRolePosition(SocketGuildUser user)
        {
            var roles = user.Roles;
            int position = 0;
            foreach (var item in roles)
            {
                if (item.Position > position)
                    position = item.Position;
            }
            return position;
        }
    }
}
