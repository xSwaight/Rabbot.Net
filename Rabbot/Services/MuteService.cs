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
    public class MuteService
    {
        private static readonly ILogger _logger = Log.ForContext(Serilog.Core.Constants.SourceContextPropertyName, nameof(MuteService));
        private readonly DiscordSocketClient _client;

        public MuteService(DiscordSocketClient client)
        {
            _client = client;
        }
        public async Task CheckMutes(swaightContext db)
        {
            if (!db.Muteduser.Any() || !_client.Guilds.Any())
                return;
            var muteUsers = db.Muteduser.ToList();
            foreach (var mute in muteUsers)
            {
                var dcGuild = _client.Guilds.FirstOrDefault(p => p.Id == mute.ServerId);
                if (dcGuild == null)
                {
                    db.Muteduser.Remove(mute);
                    await db.SaveChangesAsync();
                    continue;
                }

                var mutedRole = dcGuild.Roles.FirstOrDefault(p => p.Name == "Muted");
                if (mutedRole == null)
                    continue;
                if (dcGuild.CurrentUser == null)
                    continue;
                int position = GetBotRolePosition(dcGuild.CurrentUser);
                var dcTargetUser = dcGuild.Users.FirstOrDefault(p => p.Id == mute.UserId);
                if (dcTargetUser == null)
                {
                    if (mute.Duration < DateTime.Now)
                    {
                        db.Muteduser.Remove(mute);
                        await db.SaveChangesAsync();
                    }
                    continue;
                }

                if (dcGuild.CurrentUser.GuildPermissions.ManageRoles == true && position > mutedRole.Position)
                {
                    if (mute.Duration < DateTime.Now)
                    {
                        db.Muteduser.Remove(mute);
                        try
                        {
                            await dcTargetUser.RemoveRoleAsync(mutedRole);
                            var oldRoles = mute.Roles.Split('|');
                            foreach (var oldRole in oldRoles)
                            {
                                var role = dcGuild.Roles.FirstOrDefault(p => p.Name == oldRole);
                                if (role != null)
                                    await dcTargetUser.AddRoleAsync(role);
                            }
                            await Logging.Unmuted(dcTargetUser);
                        }
                        catch (Exception e)
                        {
                            _logger.Error(e, $"Error while adding roles");
                        }
                    }
                    else
                    {
                        if (!dcTargetUser.Roles.Where(p => p.Id == mutedRole.Id).Any())
                        {
                            try
                            {
                                var oldRoles = mute.Roles.Split('|');
                                foreach (var oldRole in oldRoles)
                                {
                                    if (!oldRole.Contains("everyone"))
                                    {
                                        var role = dcGuild.Roles.FirstOrDefault(p => p.Name == oldRole);
                                        if (role != null)
                                            await dcTargetUser.RemoveRoleAsync(role);
                                    }
                                }
                                await dcTargetUser.AddRoleAsync(mutedRole);
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

        public async Task MuteTargetUser(swaightContext db, IUser user, string duration, SocketCommandContext context)
        {
            var mutedRole = context.Guild.Roles.FirstOrDefault(p => p.Name == "Muted");
            if (mutedRole == null)
            {
                await SendError($"Es existiert keine Muted Rolle!", context);
                return;
            }
            var dcTargetUser = user as SocketGuildUser;
            var dcGuild = context.Guild;
            var targetPosition = GetTargetRolePosition(dcTargetUser);
            var botPosition = GetBotRolePosition(dcGuild.CurrentUser);

            if (!(mutedRole.Position > targetPosition && dcGuild.CurrentUser.GuildPermissions.ManageRoles))
            {
                await SendError($"Mindestens eine meiner Rollen muss in der Reihenfolge über der Muted Rolle stehen!", context);
                return;
            }
            if (targetPosition > botPosition)
            {
                await SendError($"Es fehlen die Berechtigungen um {dcTargetUser.Mention} zu muten!", context);
                return;
            }
            if (context.User.Id == user.Id)
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

            if (!db.Muteduser.Where(p => p.ServerId == context.Guild.Id && p.UserId == user.Id).Any())
            {
                string userRoles = "";
                foreach (var role in dcTargetUser.Roles)
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
            await SendPrivate(dcGuild, banUntil, duration, dcTargetUser);
            await Logging.Mute(context, user, duration);
            await db.SaveChangesAsync();
        }

        public async Task MuteWarnedUser(swaightContext db, SocketGuildUser user, SocketGuild guild)
        {
            var mutedRole = guild.Roles.FirstOrDefault(p => p.Name == "Muted");
            if (mutedRole == null)
                return;

            var targetPosition = GetTargetRolePosition(user);
            var botPosition = GetBotRolePosition(guild.CurrentUser);

            if (!(mutedRole.Position > targetPosition && guild.CurrentUser.GuildPermissions.ManageRoles))
                return;
            if (targetPosition > botPosition)
                return;

            DateTime date = DateTime.Now;
            DateTime banUntil = date.AddHours(1);

            if (!db.Muteduser.Where(p => p.ServerId == guild.Id && p.UserId == user.Id).Any())
            {
                string userRoles = "";
                foreach (var role in user.Roles)
                {
                    if (!role.IsEveryone && !role.IsManaged)
                        userRoles += role.Name + "|";
                }
                userRoles = userRoles.TrimEnd('|');
                await db.Muteduser.AddAsync(new Muteduser { ServerId = guild.Id, UserId = user.Id, Duration = banUntil, Roles = userRoles });
            }
            else
            {
                var ban = db.Muteduser.FirstOrDefault(p => p.ServerId == guild.Id && p.UserId == user.Id);
                ban.Duration = banUntil;
            }
            await SendPrivate(guild, banUntil, "1 Stunde", user);
            await Logging.WarningMute((SocketGuildUser)user);
            await db.SaveChangesAsync();
        }

        public async Task UnmuteTargetUser(swaightContext db, IUser user, SocketCommandContext context)
        {
            var mute = db.Muteduser.Where(p => p.ServerId == context.Guild.Id && p.UserId == user.Id);
            if (!mute.Any())
            {
                await SendError($"{user.Mention} ist nicht gemuted.", context);
                return;
            }
            else
            {
                var dcGuild = db.Guild.FirstOrDefault(p => p.ServerId == context.Guild.Id);
                var dcTargetUser = user as SocketGuildUser;
                var mutedRole = dcTargetUser.Roles.FirstOrDefault(p => p.Name == "Muted");
                if (mutedRole != null)
                {
                    db.Muteduser.Remove(mute.FirstOrDefault());
                    await dcTargetUser.RemoveRoleAsync(mutedRole);
                    var oldRoles = mute.FirstOrDefault().Roles.Split('|');
                    await db.SaveChangesAsync();
                    foreach (var oldRole in oldRoles)
                    {
                        var role = context.Guild.Roles.FirstOrDefault(p => p.Name == oldRole);
                        if (role != null)
                            await dcTargetUser.AddRoleAsync(role);
                    }
                }
                await Logging.Unmuted(context, user);
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
