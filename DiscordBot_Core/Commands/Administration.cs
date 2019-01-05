using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using DiscordBot_Core.Database;

namespace DiscordBot_Core.Commands
{
    public class Administration : ModuleBase<SocketCommandContext>
    {

        [Command("delete", RunMode = RunMode.Async)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        [RequireBotPermission(ChannelPermission.ManageMessages)]
        public async Task Delete(uint amount)
        {
            IEnumerable<IMessage> messages = await Context.Channel.GetMessagesAsync((int)amount + 1).FlattenAsync();
            await ((ITextChannel)Context.Channel).DeleteMessagesAsync(messages);
            const int delay = 3000;
            var embed = new EmbedBuilder();
            embed.WithDescription($"Die letzten {amount} Nachrichten wurden gelöscht.");
            embed.WithColor(new Color(90, 92, 96));
            IUserMessage m = await ReplyAsync("", false, embed.Build());
            await Task.Delay(delay);
            await m.DeleteAsync();
        }

        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Command("mute")]
        public async Task Mute(IUser user, string duration)
        {
            using (discordbotContext db = new discordbotContext())
            {
                await Context.Message.DeleteAsync();
                if (user.Id == Context.Guild.CurrentUser.Id)
                    return;
                var mutedRole = Context.Guild.Roles.Where(p => p.Name == "Muted").FirstOrDefault();
                if (mutedRole == null)
                {
                    var embed = new EmbedBuilder();
                    embed.WithDescription($"Es existiert keine Muted Rolle!");
                    embed.WithColor(new Color(255, 0, 0));
                    var message = await Context.Channel.SendMessageAsync("", false, embed.Build());
                    await Task.Delay(5000);
                    await message.DeleteAsync();
                    return;
                }
                var roles = Context.Guild.CurrentUser.Roles;
                var rolesTarget = Context.Guild.Users.Where(p => p.Id == user.Id).FirstOrDefault().Roles;
                int position = 0;
                int targetPosition = 0;
                foreach (var item in roles)
                {
                    if (item.Position > position)
                        position = item.Position;
                }
                foreach (var item in rolesTarget)
                {
                    if (item.Position > targetPosition)
                        targetPosition = item.Position;
                }
                if (!(mutedRole.Position < position) && Context.Guild.CurrentUser.GuildPermissions.ManageRoles)
                {
                    var embed = new EmbedBuilder();
                    embed.WithDescription($"Mindestens eine meiner Rollen muss in der Reihenfolge über der Muted Rolle stehen!");
                    embed.WithColor(new Color(255, 0, 0));
                    var message = await Context.Channel.SendMessageAsync("", false, embed.Build());
                    await Task.Delay(5000);
                    await message.DeleteAsync();
                    return;
                }
                if (targetPosition > position)
                {
                    var embed = new EmbedBuilder();
                    embed.WithDescription($"Es fehlen die Berechtigungen um {user.Mention} zu muten!");
                    embed.WithColor(new Color(255, 0, 0));
                    var message = await Context.Channel.SendMessageAsync("", false, embed.Build());
                    await Task.Delay(5000);
                    await message.DeleteAsync();
                    return;
                }
                if (Context.User.Id == user.Id)
                {
                    var embed = new EmbedBuilder();
                    embed.WithDescription($"{user.Mention} du Trottel kannst dich nicht selber muten!");
                    embed.WithColor(new Color(255, 0, 0));
                    var message = await Context.Channel.SendMessageAsync("", false, embed.Build());
                    await Task.Delay(5000);
                    await message.DeleteAsync();
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
                if (db.Muteduser.Where(p => p.ServerId == (long)Context.Guild.Id && p.UserId == (long)user.Id).Count() == 0)
                {
                    var mutedUser = Context.Guild.Users.Where(p => p.Id == user.Id).FirstOrDefault();
                    string userRoles = "";
                    foreach (var role in mutedUser.Roles)
                    {
                        if (!role.Name.Contains("everyone"))
                            userRoles += role.Name + "|";
                    }
                    userRoles = userRoles.TrimEnd('|');
                    await db.Muteduser.AddAsync(new Muteduser { ServerId = (long)Context.Guild.Id, UserId = (long)user.Id, Duration = banUntil, Roles = userRoles });
                }
                else
                {
                    var ban = db.Muteduser.Where(p => p.ServerId == (long)Context.Guild.Id && p.UserId == (long)user.Id).FirstOrDefault();
                    ban.Duration = banUntil;
                }
                var guild = db.Guild.Where(p => p.ServerId == (long)Context.Guild.Id).FirstOrDefault();
                if (guild.LogchannelId != null && guild.Log == 1)
                {
                    var logchannel = Context.Guild.TextChannels.Where(p => p.Id == (ulong)guild.LogchannelId).FirstOrDefault();
                    var embed = new EmbedBuilder();
                    embed.WithDescription($"{Context.User.Username} hat {user.Mention} für {duration} gemuted.");
                    embed.WithColor(new Color(255, 0, 0));
                    await logchannel.SendMessageAsync("", false, embed.Build());
                }

                await db.SaveChangesAsync();
            }
        }

        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Command("unmute")]
        public async Task Unmute(IUser user)
        {
            using (discordbotContext db = new discordbotContext())
            {
                await Context.Message.DeleteAsync();
                var ban = db.Muteduser.Where(p => p.ServerId == (long)Context.Guild.Id && p.UserId == (long)user.Id);
                if (ban.Count() == 0)
                {
                    var embed = new EmbedBuilder();
                    embed.WithDescription($"{user.Mention} ist nicht gemuted.");
                    embed.WithColor(new Color(255, 0, 0));
                    var message = await Context.Channel.SendMessageAsync("", false, embed.Build());
                    await Task.Delay(5000);
                    await message.DeleteAsync();
                    return;
                }
                else
                {
                    var guild = db.Guild.Where(p => p.ServerId == (long)Context.Guild.Id).FirstOrDefault();
                    var User = Context.Guild.Users.Where(p => p.Id == user.Id).FirstOrDefault();
                    var MutedRole = User.Roles.Where(p => p.Name == "Muted").FirstOrDefault();
                    if (MutedRole != null)
                        await User.RemoveRoleAsync(MutedRole);
                    if (guild.LogchannelId != null && guild.Log == 1)
                    {
                        db.Muteduser.Remove(ban.FirstOrDefault());
                        var oldRoles = ban.FirstOrDefault().Roles.Split('|');
                        foreach (var oldRole in oldRoles)
                        {
                            var role = Context.Guild.Roles.Where(p => p.Name == oldRole).FirstOrDefault();
                            if (role != null)
                                await User.AddRoleAsync(role);
                        }
                        await db.SaveChangesAsync();
                        var logchannel = Context.Guild.TextChannels.Where(p => p.Id == (ulong)guild.LogchannelId).FirstOrDefault();
                        var embed = new EmbedBuilder();
                        embed.WithDescription($"{user.Mention} wurde unmuted.");
                        embed.WithColor(new Color(0, 255, 0));
                        await logchannel.SendMessageAsync("", false, embed.Build());
                    }
                }
            }
        }

        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Command("setLog")]
        public async Task SetLog()
        {
            using (discordbotContext db = new discordbotContext())
            {
                await Context.Message.DeleteAsync();
                if (db.Guild.Where(p => p.ServerId == (long)Context.Guild.Id).Count() == 0)
                {
                    await db.Guild.AddAsync(new Guild { ServerId = (long)Context.Guild.Id, LogchannelId = (long)Context.Channel.Id });
                }
                else
                {
                    var defaultChannel = db.Guild.Where(p => p.ServerId == (long)Context.Guild.Id).FirstOrDefault();
                    defaultChannel.LogchannelId = (long)Context.Channel.Id;
                }
                await db.SaveChangesAsync();
                const int delay = 2000;
                var embed = new EmbedBuilder();
                embed.WithDescription("Log Channel wurde erfolgreich gesetzt.");
                embed.WithColor(new Color(90, 92, 96));
                IUserMessage m = await ReplyAsync("", false, embed.Build());
                await Task.Delay(delay);
                await m.DeleteAsync();
            }
        }

        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Command("setNotification")]
        public async Task SetNotification()
        {
            using (discordbotContext db = new discordbotContext())
            {
                await Context.Message.DeleteAsync();
                if (db.Guild.Where(p => p.ServerId == (long)Context.Guild.Id).Count() == 0)
                {
                    await db.Guild.AddAsync(new Guild { ServerId = (long)Context.Guild.Id, NotificationchannelId = (long)Context.Channel.Id });
                }
                else
                {
                    var defaultChannel = db.Guild.Where(p => p.ServerId == (long)Context.Guild.Id).FirstOrDefault();
                    defaultChannel.NotificationchannelId = (long)Context.Channel.Id;
                }
                await db.SaveChangesAsync();
                const int delay = 2000;
                var embed = new EmbedBuilder();
                embed.WithDescription("Notification Channel wurde erfolgreich gesetzt.");
                embed.WithColor(new Color(90, 92, 96));
                IUserMessage m = await ReplyAsync("", false, embed.Build());
                await Task.Delay(delay);
                await m.DeleteAsync();
            }
        }

        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Command("delNotification")]
        public async Task DelNotification()
        {
            using (discordbotContext db = new discordbotContext())
            {
                await Context.Message.DeleteAsync();
                if (db.Guild.Where(p => p.ServerId == (long)Context.Guild.Id).Count() == 0)
                {
                    return;
                }
                else
                {
                    var defaultChannel = db.Guild.Where(p => p.ServerId == (long)Context.Guild.Id).FirstOrDefault();
                    defaultChannel.NotificationchannelId = null;
                }
                await db.SaveChangesAsync();
                const int delay = 2000;
                var embed = new EmbedBuilder();
                embed.WithDescription("Notification Channel wurde erfolgreich gelöscht.");
                embed.WithColor(new Color(90, 92, 96));
                IUserMessage m = await ReplyAsync("", false, embed.Build());
                await Task.Delay(delay);
                await m.DeleteAsync();
            }
        }

        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Command("delLog")]
        public async Task DelLog()
        {
            using (discordbotContext db = new discordbotContext())
            {
                await Context.Message.DeleteAsync();
                if (db.Guild.Where(p => p.ServerId == (long)Context.Guild.Id).Count() == 0)
                {
                    return;
                }
                else
                {
                    var defaultChannel = db.Guild.Where(p => p.ServerId == (long)Context.Guild.Id).FirstOrDefault();
                    defaultChannel.LogchannelId = null;
                }
                await db.SaveChangesAsync();
                const int delay = 2000;
                var embed = new EmbedBuilder();
                embed.WithDescription("Log Channel wurde erfolgreich gelöscht.");
                embed.WithColor(new Color(90, 92, 96));
                IUserMessage m = await ReplyAsync("", false, embed.Build());
                await Task.Delay(delay);
                await m.DeleteAsync();
            }
        }

        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Command("notification")]
        public async Task Notification()
        {
            using (discordbotContext db = new discordbotContext())
            {
                await Context.Message.DeleteAsync();

                var currentNotify = db.Guild.Where(p => p.ServerId == (long)Context.Guild.Id).FirstOrDefault().Notify;
                if (currentNotify == 0)
                {
                    db.Guild.Where(p => p.ServerId == (long)Context.Guild.Id).FirstOrDefault().Notify = 1;
                    await db.SaveChangesAsync();
                    const int delay = 2000;
                    var embed = new EmbedBuilder();
                    embed.WithDescription("Notifications wurden aktiviert.");
                    embed.WithColor(new Color(90, 92, 96));
                    IUserMessage m = await ReplyAsync("", false, embed.Build());
                    await Task.Delay(delay);
                    await m.DeleteAsync();
                }
                else
                {
                    db.Guild.Where(p => p.ServerId == (long)Context.Guild.Id).FirstOrDefault().Notify = 0;
                    await db.SaveChangesAsync();
                    const int delay = 2000;
                    var embed = new EmbedBuilder();
                    embed.WithDescription("Notifications wurden deaktiviert.");
                    embed.WithColor(new Color(90, 92, 96));
                    IUserMessage m = await ReplyAsync("", false, embed.Build());
                    await Task.Delay(delay);
                    await m.DeleteAsync();
                }
            }
        }

        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Command("log")]
        public async Task Log()
        {
            using (discordbotContext db = new discordbotContext())
            {
                await Context.Message.DeleteAsync();

                var currentLog = db.Guild.Where(p => p.ServerId == (long)Context.Guild.Id).FirstOrDefault().Log;
                if (currentLog == 0)
                {
                    db.Guild.Where(p => p.ServerId == (long)Context.Guild.Id).FirstOrDefault().Log = 1;
                    await db.SaveChangesAsync();
                    const int delay = 2000;
                    var embed = new EmbedBuilder();
                    embed.WithDescription("Logs wurden aktiviert.");
                    embed.WithColor(new Color(90, 92, 96));
                    IUserMessage m = await ReplyAsync("", false, embed.Build());
                    await Task.Delay(delay);
                    await m.DeleteAsync();
                }
                else
                {
                    db.Guild.Where(p => p.ServerId == (long)Context.Guild.Id).FirstOrDefault().Log = 0;
                    await db.SaveChangesAsync();
                    const int delay = 2000;
                    var embed = new EmbedBuilder();
                    embed.WithDescription("Logs wurden deaktiviert.");
                    embed.WithColor(new Color(90, 92, 96));
                    IUserMessage m = await ReplyAsync("", false, embed.Build());
                    await Task.Delay(delay);
                    await m.DeleteAsync();
                }
            }
        }

    }
}
