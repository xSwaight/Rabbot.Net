using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using PagedList;
using Rabbot.Database;
using Rabbot.Database.Rabbot;
using Rabbot.Services;
using Serilog;
using Serilog.Core;

namespace Rabbot.Commands
{
    public class Administration : ModuleBase<SocketCommandContext>
    {
        private readonly WarnService _warnService;
        private readonly MuteService _muteService;
        private readonly DatabaseService _databaseService;
        private static readonly ILogger _logger = Log.ForContext(Serilog.Core.Constants.SourceContextPropertyName, nameof(Administration));

        public Administration(IServiceProvider services)
        {
            _warnService = services.GetRequiredService<WarnService>();
            _muteService = services.GetRequiredService<MuteService>();
            _databaseService = services.GetRequiredService<DatabaseService>();
        }

        [Command("del", RunMode = RunMode.Async)]
        [Summary("Löscht die angegebene Anzahl an Nachrichten im aktuellen Channel (Limit von 100 Nachrichten).")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        [RequireBotPermission(ChannelPermission.ManageMessages)]
        public async Task Delete(uint amount, IUser user = null)
        {
            if (amount < 1)
                return;

            if (user == null)
            {
                IEnumerable<IMessage> messages = await Context.Channel.GetMessagesAsync((int)amount + 1).FlattenAsync();
                await ((ITextChannel)Context.Channel).DeleteMessagesAsync(messages);
                const int delay = 3000;
                var embed = new EmbedBuilder();
                embed.WithDescription($"Die letzten {amount} Nachrichten wurden gelöscht.");
                embed.WithColor(new Color(90, 92, 96));
                IUserMessage m = await ReplyAsync("", false, embed.Build());
                await Logging.Delete(Context, (int)amount);
                await Task.Delay(delay);
                await m.DeleteAsync();
            }
            else
            {
                using (var db = _databaseService.Open<RabbotContext>())
                {

                    await Context.Message.DeleteAsync();
                    var msgs = await Context.Channel.GetMessagesAsync(100).FlattenAsync();
                    msgs = msgs.Where(x => x.Author.Id == user.Id).Take((int)amount);
                    await ((ITextChannel)Context.Channel).DeleteMessagesAsync(msgs);
                    var exp = db.Features.FirstOrDefault(p => p.UserId == user.Id);
                    if (exp.Exp > (50 * amount))
                        exp.Exp -= (int)(50 * amount);
                    else
                    {
                        amount = (uint)exp.Exp;
                        exp.Exp = 0;
                    }
                    await db.SaveChangesAsync();
                    const int delay = 3000;
                    var embed = new EmbedBuilder();
                    embed.WithDescription($"Die letzten {amount} Nachrichten von {user.Username} wurden gelöscht.");
                    embed.WithColor(new Color(90, 92, 96));
                    IUserMessage m = await ReplyAsync("", false, embed.Build());
                    await Logging.Delete(user, Context, (int)amount);
                    await Task.Delay(delay);
                    await m.DeleteAsync();
                }
            }
        }

        [RequireOwner]
        [Command("updateGuilds", RunMode = RunMode.Async)]
        public async Task UpdateGuilds()
        {
            await Context.Message.DeleteAsync();
            using (var db = _databaseService.Open<RabbotContext>())
            {
                foreach (var guild in Context.Client.Guilds)
                {
                    var dbGuild = db.Guilds.FirstOrDefault(p => p.GuildId == guild.Id);
                    if (dbGuild == null)
                    {
                        await db.Guilds.AddAsync(new GuildEntity { GuildId = guild.Id, GuildName = guild.Name });
                    }
                    else
                    {
                        dbGuild.GuildName = guild.Name;
                    }
                }
                await db.SaveChangesAsync();
            }
        }

        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Command("activeClients", RunMode = RunMode.Async)]
        public async Task ActiveClients(SocketGuildUser user = null)
        {
            if (user == null)
                user = Context.User as SocketGuildUser;

            if (user.ActiveClients.Any())
            {
                string output = $"{user.Nickname ?? user.Username} ist auf {(user.ActiveClients.Count > 1 ? "folgenden Plattformen" : "folgender Plattform")} online: ";
                foreach (var activeClient in user.ActiveClients)
                {
                    output += activeClient.ToString() + ", ";
                }
                await Context.Channel.SendMessageAsync($"{output.TrimEnd(' ', ',')}");
                return;
            }
            await Context.Channel.SendMessageAsync($"{user.Nickname ?? user.Username} ist Unsichtbar oder auf keiner Plattform online.");
        }

        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Command("mute", RunMode = RunMode.Async)]
        [Summary("Muted den User für angegebene Zeit (Zeitindikatoren: s = Sekunden, m = Minuten, h = Stunden, d = Tage).")]
        public async Task Mute(IUser user, string duration)
        {
            if (duration.Contains('-'))
                return;
            var dcUser = user as SocketGuildUser;
            if (dcUser.GuildPermissions.Administrator || dcUser.GuildPermissions.ManageMessages)
                return;
            await Context.Message.DeleteAsync();
            using (var db = _databaseService.Open<RabbotContext>())
                await _muteService.MuteTargetUser(db, user, duration, Context);
        }

        [RequireUserPermission(GuildPermission.ManageGuild)]
        [RequireBotPermission(GuildPermission.ManageGuild)]
        [Command("setregion", RunMode = RunMode.Async), Alias("frankfurt")]
        public async Task SetRegion(string regionId = "frankfurt")
        {
            await Context.Message.DeleteAsync();
            await Context.Guild.ModifyAsync(p => p.RegionId = regionId);
        }


        [RequireUserPermission(GuildPermission.ManageGuild)]
        [RequireBotPermission(GuildPermission.ManageGuild)]
        [Command("getregions", RunMode = RunMode.Async)]
        public async Task GetRegions()
        {
            string output = "**Verfügbare Voice Regionen:**\n\n";
            foreach (var region in Context.Client.VoiceRegions)
            {
                output += $"**Region:** *{region.Name}* - **RegionId:** *{region.Id}*\n";
            }
            await ReplyAsync(output);
        }

        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Command("unmute", RunMode = RunMode.Async)]
        [Summary("Entmuted den markierten User.")]
        public async Task Unmute(IUser user)
        {
            await Context.Message.DeleteAsync();
            using (var db = _databaseService.Open<RabbotContext>())
                await _muteService.UnmuteTargetUser(db, user, Context);
        }

        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Command("warn", RunMode = RunMode.Async)]
        [Summary("Warnt den markierten User.")]
        public async Task Warn(IUser user)
        {
            await Context.Message.DeleteAsync();
            if (user == null)
                return;
            var dcUser = user as SocketGuildUser;
            if (dcUser.GuildPermissions.Administrator || dcUser.GuildPermissions.ManageMessages)
                return;

            using (var db = _databaseService.Open<RabbotContext>())
                await _warnService.Warn(db, user, Context);
        }

        [Command("setStatus", RunMode = RunMode.Async)]
        [RequireOwner]
        public async Task SetStatus(int type, [Remainder]string msg)
        {
            await Context.Message.DeleteAsync();
            ActivityType action;
            switch (type)
            {
                case 0:
                    action = ActivityType.Playing;
                    break;
                case 1:
                    action = ActivityType.Watching;
                    break;
                case 2:
                    action = ActivityType.Listening;
                    break;
                case 3:
                    action = ActivityType.Streaming;
                    break;
                default:
                    return;
            }
            await Context.Client.SetGameAsync(msg, null, action);
        }

        [Command("stream", RunMode = RunMode.Async)]
        [RequireOwner]
        public async Task Stream()
        {
            await Context.Message.DeleteAsync();
            await Context.Client.SetGameAsync("Swaight is live!", "https://www.twitch.tv/swaight", ActivityType.Streaming);
        }

        [Command("uptime")]
        [RequireOwner]
        public async Task Uptime()
        {
            TimeSpan span = DateTime.Now - Process.GetCurrentProcess().StartTime;
            await Context.Channel.SendMessageAsync($"`Uptime: {span.Days}D {span.Hours}H {span.Minutes}M`");
        }

        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Command("setLog", RunMode = RunMode.Async)]
        [Summary("Setzt den aktuellen Channel als Log Channel.")]
        public async Task SetLog()
        {
            await Context.Message.DeleteAsync();
            using (var db = _databaseService.Open<RabbotContext>())
            {
                if (!db.Guilds.AsQueryable().Where(p => p.GuildId == Context.Guild.Id).Any())
                {
                    await db.Guilds.AddAsync(new GuildEntity { GuildId = Context.Guild.Id, LogChannelId = Context.Channel.Id });
                }
                else
                {
                    var defaultChannel = db.Guilds.FirstOrDefault(p => p.GuildId == Context.Guild.Id);
                    defaultChannel.LogChannelId = Context.Channel.Id;
                }
                db.Guilds.FirstOrDefault(p => p.GuildId == Context.Guild.Id).Log = true;
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
        [Command("setBot", RunMode = RunMode.Async)]
        [Summary("Setzt den aktuellen Channel als Bot Channel.")]
        public async Task SetBot()
        {
            await Context.Message.DeleteAsync();
            using (var db = _databaseService.Open<RabbotContext>())
            {
                if (!db.Guilds.AsQueryable().Where(p => p.GuildId == Context.Guild.Id).Any())
                {
                    await db.Guilds.AddAsync(new GuildEntity { GuildId = Context.Guild.Id, BotChannelId = Context.Channel.Id });
                }
                else
                {
                    var defaultChannel = db.Guilds.FirstOrDefault(p => p.GuildId == Context.Guild.Id);
                    defaultChannel.BotChannelId = Context.Channel.Id;
                }
                await db.SaveChangesAsync();
                const int delay = 2000;
                var embed = new EmbedBuilder();
                embed.WithDescription("Bot Channel wurde erfolgreich gesetzt.");
                embed.WithColor(new Color(90, 92, 96));
                IUserMessage m = await ReplyAsync("", false, embed.Build());
                await Task.Delay(delay);
                await m.DeleteAsync();
            }
        }

        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Command("setTrash", RunMode = RunMode.Async)]
        [Summary("Setzt den aktuellen Channel als Trash Channel.")]
        public async Task SetTrash()
        {
            await Context.Message.DeleteAsync();
            using (var db = _databaseService.Open<RabbotContext>())
            {
                if (!db.Guilds.AsQueryable().Where(p => p.GuildId == Context.Guild.Id).Any())
                {
                    await db.Guilds.AddAsync(new GuildEntity { GuildId = Context.Guild.Id, TrashChannelId = Context.Channel.Id });
                }
                else
                {
                    var defaultChannel = db.Guilds.FirstOrDefault(p => p.GuildId == Context.Guild.Id);
                    defaultChannel.TrashChannelId = Context.Channel.Id;
                    defaultChannel.Trash = true;
                }
                await db.SaveChangesAsync();
                const int delay = 2000;
                var embed = new EmbedBuilder();
                embed.WithDescription("Trash Channel wurde erfolgreich gesetzt.");
                embed.WithColor(new Color(90, 92, 96));
                IUserMessage m = await ReplyAsync("", false, embed.Build());
                await Task.Delay(delay);
                await m.DeleteAsync();
            }
        }

        [RequireOwner]
        [Command("setStream", RunMode = RunMode.Async)]
        public async Task SetStream()
        {
            await Context.Message.DeleteAsync();
            using (var db = _databaseService.Open<RabbotContext>())
            {
                if (!db.Guilds.AsQueryable().Where(p => p.GuildId == Context.Guild.Id).Any())
                {
                    await db.Guilds.AddAsync(new GuildEntity { GuildId = Context.Guild.Id, StreamChannelId = Context.Channel.Id });
                }
                else
                {
                    var defaultChannel = db.Guilds.FirstOrDefault(p => p.GuildId == Context.Guild.Id);
                    defaultChannel.StreamChannelId = Context.Channel.Id;
                }
                await db.SaveChangesAsync();
                const int delay = 2000;
                var embed = new EmbedBuilder();
                embed.WithDescription("Stream Channel wurde erfolgreich gesetzt.");
                embed.WithColor(new Color(90, 92, 96));
                IUserMessage m = await ReplyAsync("", false, embed.Build());
                await Task.Delay(delay);
                await m.DeleteAsync();
            }
        }

        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Command("setNotification", RunMode = RunMode.Async)]
        [Summary("Setzt den aktuellen Channel als Notification Channel.")]
        public async Task SetNotification()
        {
            await Context.Message.DeleteAsync();
            using (var db = _databaseService.Open<RabbotContext>())
            {
                if (!db.Guilds.AsQueryable().Where(p => p.GuildId == Context.Guild.Id).Any())
                {
                    await db.Guilds.AddAsync(new GuildEntity { GuildId = Context.Guild.Id, NotificationChannelId = Context.Channel.Id });
                }
                else
                {
                    var defaultChannel = db.Guilds.FirstOrDefault(p => p.GuildId == Context.Guild.Id);
                    defaultChannel.NotificationChannelId = Context.Channel.Id;
                }
                db.Guilds.FirstOrDefault(p => p.GuildId == Context.Guild.Id).Notify = true;
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
        [Command("setLevelChannel", RunMode = RunMode.Async)]
        [Summary("Setzt den aktuellen Channel als Level Channel.")]
        public async Task SetLevelChannel()
        {
            await Context.Message.DeleteAsync();
            using (var db = _databaseService.Open<RabbotContext>())
            {
                if (!db.Guilds.AsQueryable().Where(p => p.GuildId == Context.Guild.Id).Any())
                {
                    await db.Guilds.AddAsync(new GuildEntity { GuildId = Context.Guild.Id, NotificationChannelId = Context.Channel.Id });
                }
                else
                {
                    var defaultChannel = db.Guilds.FirstOrDefault(p => p.GuildId == Context.Guild.Id);
                    defaultChannel.LevelChannelId = Context.Channel.Id;
                }
                await db.SaveChangesAsync();
                const int delay = 2000;
                var embed = new EmbedBuilder();
                embed.WithDescription("Level Channel wurde erfolgreich gesetzt.");
                embed.WithColor(new Color(90, 92, 96));
                IUserMessage m = await ReplyAsync("", false, embed.Build());
                await Task.Delay(delay);
                await m.DeleteAsync();
            }
        }

        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Command("delLevelChannel", RunMode = RunMode.Async)]
        [Summary("Löscht den Level Channel in den Einstellungen.")]
        public async Task DelLevelChannel()
        {
            await Context.Message.DeleteAsync();
            using (var db = _databaseService.Open<RabbotContext>())
            {
                if (!db.Guilds.AsQueryable().Where(p => p.GuildId == Context.Guild.Id).Any())
                {
                    return;
                }
                else
                {
                    var defaultChannel = db.Guilds.FirstOrDefault(p => p.GuildId == Context.Guild.Id);
                    defaultChannel.LevelChannelId = null;
                }
                await db.SaveChangesAsync();
                const int delay = 2000;
                var embed = new EmbedBuilder();
                embed.WithDescription("Level Channel wurde erfolgreich gelöscht.");
                embed.WithColor(new Color(90, 92, 96));
                IUserMessage m = await ReplyAsync("", false, embed.Build());
                await Task.Delay(delay);
                await m.DeleteAsync();
            }
        }

        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Command("delNotification", RunMode = RunMode.Async)]
        [Summary("Löscht den Notification Channel in den Einstellungen.")]
        public async Task DelNotification()
        {
            await Context.Message.DeleteAsync();
            using (var db = _databaseService.Open<RabbotContext>())
            {
                if (!db.Guilds.AsQueryable().Where(p => p.GuildId == Context.Guild.Id).Any())
                {
                    return;
                }
                else
                {
                    var defaultChannel = db.Guilds.FirstOrDefault(p => p.GuildId == Context.Guild.Id);
                    defaultChannel.NotificationChannelId = null;
                }
                db.Guilds.FirstOrDefault(p => p.GuildId == Context.Guild.Id).Notify = false;
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
        [Command("delBot", RunMode = RunMode.Async)]
        [Summary("Löscht den Bot Channel in den Einstellungen.")]
        public async Task DelBot()
        {
            await Context.Message.DeleteAsync();
            using (var db = _databaseService.Open<RabbotContext>())
            {
                if (!db.Guilds.AsQueryable().Where(p => p.GuildId == Context.Guild.Id).Any())
                {
                    return;
                }
                else
                {
                    var defaultChannel = db.Guilds.FirstOrDefault(p => p.GuildId == Context.Guild.Id);
                    defaultChannel.BotChannelId = null;
                }
                await db.SaveChangesAsync();
                const int delay = 2000;
                var embed = new EmbedBuilder();
                embed.WithDescription("Bot Channel wurde erfolgreich gelöscht.");
                embed.WithColor(new Color(90, 92, 96));
                IUserMessage m = await ReplyAsync("", false, embed.Build());
                await Task.Delay(delay);
                await m.DeleteAsync();
            }
        }

        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Command("delTrash", RunMode = RunMode.Async)]
        [Summary("Löscht den Trash Channel in den Einstellungen.")]
        public async Task DelTrash()
        {
            await Context.Message.DeleteAsync();
            using (var db = _databaseService.Open<RabbotContext>())
            {
                if (!db.Guilds.AsQueryable().Where(p => p.GuildId == Context.Guild.Id).Any())
                {
                    return;
                }
                else
                {
                    var defaultChannel = db.Guilds.FirstOrDefault(p => p.GuildId == Context.Guild.Id);
                    defaultChannel.BotChannelId = null;
                    defaultChannel.Trash = false;
                }
                await db.SaveChangesAsync();
                const int delay = 2000;
                var embed = new EmbedBuilder();
                embed.WithDescription("Trash Channel wurde erfolgreich gelöscht.");
                embed.WithColor(new Color(90, 92, 96));
                IUserMessage m = await ReplyAsync("", false, embed.Build());
                await Task.Delay(delay);
                await m.DeleteAsync();
            }
        }

        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Command("delLog", RunMode = RunMode.Async)]
        [Summary("Löscht den Log Channel in den Einstellungen.")]
        public async Task DelLog()
        {
            await Context.Message.DeleteAsync();
            using (var db = _databaseService.Open<RabbotContext>())
            {
                if (!db.Guilds.AsQueryable().Where(p => p.GuildId == Context.Guild.Id).Any())
                {
                    return;
                }
                else
                {
                    var defaultChannel = db.Guilds.FirstOrDefault(p => p.GuildId == Context.Guild.Id);
                    defaultChannel.LogChannelId = null;
                }
                db.Guilds.FirstOrDefault(p => p.GuildId == Context.Guild.Id).Log = false;
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
        [Command("notification", RunMode = RunMode.Async)]
        [Summary("Toggled die Notifications.")]
        public async Task Notification()
        {
            await Context.Message.DeleteAsync();
            using (var db = _databaseService.Open<RabbotContext>())
            {
                var currentNotify = db.Guilds.FirstOrDefault(p => p.GuildId == Context.Guild.Id).Notify;
                if (currentNotify == false)
                {
                    db.Guilds.FirstOrDefault(p => p.GuildId == Context.Guild.Id).Notify = true;
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
                    db.Guilds.FirstOrDefault(p => p.GuildId == Context.Guild.Id).Notify = false;
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
        [Command("log", RunMode = RunMode.Async)]
        [Summary("Toggled die Logs.")]
        public async Task ToggleLog()
        {
            await Context.Message.DeleteAsync();
            using (var db = _databaseService.Open<RabbotContext>())
            {
                var currentLog = db.Guilds.FirstOrDefault(p => p.GuildId == Context.Guild.Id).Log;
                if (currentLog == false)
                {
                    db.Guilds.FirstOrDefault(p => p.GuildId == Context.Guild.Id).Log = true;
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
                    db.Guilds.FirstOrDefault(p => p.GuildId == Context.Guild.Id).Log = false;
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

        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Command("createMuted", RunMode = RunMode.Async)]
        public async Task CreateMutedRole()
        {
            await Context.Message.DeleteAsync();
            if (!Context.Guild.Roles.Where(p => p.Name == "Muted").Any())
            {
                var mutedPermission = new GuildPermissions(false, false, false, false, false, false, false, false, true, false, false, false, false, false, true, false, false, false, false, false, false, false, false, false, false, false, false, false, false);
                await Context.Guild.CreateRoleAsync("Muted", mutedPermission, Color.Red, false, false);
            }
            var permission = new OverwritePermissions(PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Inherit, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Inherit, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny);
            foreach (var textChannel in Context.Guild.TextChannels)
            {
                var muted = Context.Guild.Roles.FirstOrDefault(p => p.Name == "Muted");
                await textChannel.AddPermissionOverwriteAsync(muted, permission, null);
            }
            foreach (var voiceChannel in Context.Guild.VoiceChannels)
            {
                var muted = Context.Guild.Roles.FirstOrDefault(p => p.Name == "Muted");
                await voiceChannel.AddPermissionOverwriteAsync(muted, permission, null);
            }
            const int delay = 2000;
            var embed = new EmbedBuilder();
            embed.WithDescription("Die Muted Rolle und die Berechtigungen wurden neu gesetzt.");
            embed.WithColor(new Color(90, 92, 96));
            IUserMessage m = await ReplyAsync("", false, embed.Build());
            await Task.Delay(delay);
            await m.DeleteAsync();
        }

        [RequireOwner]
        [Command("toggleEXP", RunMode = RunMode.Async)]
        public async Task DisableExp(IUser user)
        {
            await Context.Message.DeleteAsync();
            using (var db = _databaseService.Open<RabbotContext>())
            {
                var Experience = db.Features.FirstOrDefault(p => p.GuildId == Context.Guild.Id && p.UserId == user.Id);
                const int delay = 2000;
                var embed = new EmbedBuilder();
                if (Experience.GainExp == false)
                {
                    Experience.GainExp = true;
                    embed.WithDescription($"{user.Mention} bekommt jetzt wieder EXP.");
                }
                else
                {
                    Experience.GainExp = false;
                    embed.WithDescription($"{user.Mention} bekommt jetzt keine EXP mehr.");
                }
                await db.SaveChangesAsync();
                embed.WithColor(new Color(90, 92, 96));
                IUserMessage m = await ReplyAsync("", false, embed.Build());
                await Task.Delay(delay);
                await m.DeleteAsync();
            }
        }

        [RequireOwner]
        [Command("event", RunMode = RunMode.Async)]
        public async Task Event(int eventId)
        {
            await Context.Message.DeleteAsync();
            using (var db = _databaseService.Open<RabbotContext>())
            {
                if (!db.Events.AsQueryable().Where(x => x.Id == eventId).Any() && eventId != 0)
                    return;

                var events = db.Events.ToList();
                foreach (var myEvent in events)
                {
                    myEvent.Status = false;
                }

                if (eventId == 0)
                {
                    await Context.Client.SetGameAsync($"{Config.Bot.CmdPrefix}rank", null, ActivityType.Watching);
                    await db.SaveChangesAsync();
                    return;
                }

                var Event = db.Events.FirstOrDefault(x => x.Id == eventId);
                Event.Status = true;
                await Context.Client.SetGameAsync($"{Event.Name} Event aktiv!", null, ActivityType.Watching);
                await db.SaveChangesAsync();
            }
        }

        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Command("addBadword", RunMode = RunMode.Async)]
        public async Task AddBadword([Remainder]string word)
        {
            await Context.Message.DeleteAsync();
            using (var db = _databaseService.Open<RabbotContext>())
            {
                word = word.ToLower();
                if (db.BadWords.AsQueryable().Where(p => p.BadWord == word && p.GuildId == Context.Guild.Id).Any())
                {
                    await ReplyAsync($"**Das Wort ist bereits in der Liste!**");
                    return;
                }

                await db.BadWords.AddAsync(new BadWordEntity { BadWord = Helper.ReplaceCharacter(word), GuildId = Context.Guild.Id });
                await db.SaveChangesAsync();

                const int delay = 2000;
                var embed = new EmbedBuilder();
                embed.WithDescription($"{word} wurde erfolgreich zum Wortfilter hinzugefügt.");
                embed.WithColor(new Color(90, 92, 96));
                IUserMessage m = await ReplyAsync("", false, embed.Build());
                await Task.Delay(delay);
                await m.DeleteAsync();
            }
        }

        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Command("addGoodWord", RunMode = RunMode.Async), Alias("addwl")]
        public async Task AddGoodWord([Remainder]string word)
        {
            await Context.Message.DeleteAsync();
            using (var db = _databaseService.Open<RabbotContext>())
            {
                word = word.ToLower();
                if (db.GoodWords.AsQueryable().Where(p => p.GoodWord == word && p.GuildId == Context.Guild.Id).Any())
                {
                    await ReplyAsync($"**Das Wort ist bereits in der Liste!**");
                    return;
                }

                await db.GoodWords.AddAsync(new GoodWordEntry { GoodWord = Helper.ReplaceCharacter(word), GuildId = Context.Guild.Id });
                await db.SaveChangesAsync();

                const int delay = 2000;
                var embed = new EmbedBuilder();
                embed.WithDescription($"{word} wurde erfolgreich zur Whitelist hinzugefügt.");
                embed.WithColor(new Color(90, 92, 96));
                IUserMessage m = await ReplyAsync("", false, embed.Build());
                await Task.Delay(delay);
                await m.DeleteAsync();
            }
        }

        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Command("delGoodWord", RunMode = RunMode.Async), Alias("delwl")]
        public async Task DelGoodWord([Remainder]string word)
        {
            await Context.Message.DeleteAsync();
            using (var db = _databaseService.Open<RabbotContext>())
            {
                word = word.ToLower();
                var goodword = db.GoodWords.FirstOrDefault(p => p.GoodWord == Helper.ReplaceCharacter(word) && p.GuildId == Context.Guild.Id);
                if (goodword == null)
                    return;

                db.GoodWords.Remove(goodword);
                await db.SaveChangesAsync();

                const int delay = 2000;
                var embed = new EmbedBuilder();
                embed.WithDescription($"{word} wurde erfolgreich von der Whitelist gelöscht.");
                embed.WithColor(new Color(90, 92, 96));
                IUserMessage m = await ReplyAsync("", false, embed.Build());
                await Task.Delay(delay);
                await m.DeleteAsync();
            }
        }

        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Command("delBadword", RunMode = RunMode.Async)]
        public async Task DelBadword([Remainder]string word)
        {
            await Context.Message.DeleteAsync();
            using (var db = _databaseService.Open<RabbotContext>())
            {
                word = word.ToLower();
                var badword = db.BadWords.FirstOrDefault(p => p.BadWord == Helper.ReplaceCharacter(word) && p.GuildId == Context.Guild.Id);
                if (badword == null)
                    return;

                db.BadWords.Remove(badword);
                await db.SaveChangesAsync();

                const int delay = 2000;
                var embed = new EmbedBuilder();
                embed.WithDescription($"{word} wurde erfolgreich vom Wortfilter gelöscht.");
                embed.WithColor(new Color(90, 92, 96));
                IUserMessage m = await ReplyAsync("", false, embed.Build());
                await Task.Delay(delay);
                await m.DeleteAsync();
            }
        }

        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Command("badwords", RunMode = RunMode.Async)]
        public async Task Badwords(int page = 1)
        {
            if (page < 1)
                return;
            using (var db = _databaseService.Open<RabbotContext>())
            {
                var badwords = db.BadWords.AsQueryable().Where(p => p.GuildId == Context.Guild.Id).ToList().OrderBy(p => p.BadWord).ToPagedList(page, 25);
                if (page > badwords.PageCount)
                    return;
                var eb = new EmbedBuilder();
                eb.Color = new Color(90, 92, 96);
                string output = "**Alle Badwords:**\n\n";
                foreach (var badword in badwords)
                {
                    output += $"**{badword.BadWord}**\n";
                }
                eb.WithDescription(output);
                eb.WithTitle($"Seite {page} von {badwords.PageCount}");
                await Context.Channel.SendMessageAsync(null, false, eb.Build());
            }
        }

        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Command("goodwords", RunMode = RunMode.Async), Alias("whitelist")]
        public async Task Goodwords(int page = 1)
        {
            if (page < 1)
                return;
            using (var db = _databaseService.Open<RabbotContext>())
            {
                var goodwords = db.GoodWords.AsQueryable().Where(p => p.GuildId == Context.Guild.Id).ToList().OrderBy(p => p.GoodWord).ToPagedList(page, 25);
                if (page > goodwords.PageCount)
                    return;
                var eb = new EmbedBuilder();
                eb.Color = new Color(90, 92, 96);
                string output = "**Alle Whitlisted Wörter:**\n\n";
                foreach (var goodword in goodwords)
                {
                    output += $"**{goodword.GoodWord}**\n";
                }
                eb.WithDescription(output);
                eb.WithTitle($"Seite {page} von {goodwords.PageCount}");
                await Context.Channel.SendMessageAsync(null, false, eb.Build());
            }
        }

        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Command("streamer", RunMode = RunMode.Async)]
        public async Task Streamers(int page = 1)
        {
            if (page < 1)
                return;

            using var db = _databaseService.Open<RabbotContext>();
            var streamers = db.TwitchChannels.AsQueryable().Where(p => p.GuildId == Context.Guild.Id).ToList().OrderBy(p => p.ChannelName).ToPagedList(page, 25);
            if (page > streamers.PageCount && streamers.PageCount != 0)
                return;

            var streamChannelId = db.Guilds.AsQueryable().FirstOrDefault(p => p.GuildId == Context.Guild.Id)?.StreamChannelId;
            bool hasStreamChannel = streamChannelId != null;
            string mentionStreamChannel = string.Empty;
            if (hasStreamChannel)
            {
                var streamChannel = Context.Guild.TextChannels.FirstOrDefault(p => p.Id == streamChannelId);
                if (streamChannel != null)
                    mentionStreamChannel = streamChannel.Mention;
                else
                    hasStreamChannel = false;
            }

            var eb = new EmbedBuilder();
            eb.Color = new Color(90, 92, 96);
            string output = $"Aktueller Stream Announcement Channel: {(hasStreamChannel ? $"{mentionStreamChannel}" : $"Kein Channel gesetzt ({Config.Bot.CmdPrefix}setstream im gewünschten Channel zum setzen)")}\n**Alle eingetragenen Streamer:**\n\n";
            foreach (var streamer in streamers)
            {
                output += $"**{streamer.ChannelName}**\n";
            }
            eb.WithDescription(output);
            if (streamers.PageCount != 0)
                eb.WithTitle($"Seite {page} von {streamers.PageCount}");
            await Context.Channel.SendMessageAsync(null, false, eb.Build());
        }

        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Command("addStreamer", RunMode = RunMode.Async)]
        public async Task AddStreamer(string username)
        {
            username = username.ToLower();
            using var db = _databaseService.Open<RabbotContext>();
            if (db.TwitchChannels.AsQueryable().Where(p => p.GuildId == Context.Guild.Id && p.ChannelName == username).Any())
            {
                await ReplyAsync($"**Der Streamer ist bereits in der Liste!**");
                return;
            }

            await db.TwitchChannels.AddAsync(new TwitchChannelEntity { ChannelName = username, GuildId = Context.Guild.Id });
            await db.SaveChangesAsync();

            var embed = new EmbedBuilder();
            embed.WithDescription($"{username} wurde erfolgreich zur Streamer Liste hinzugefügt.");
            embed.WithColor(new Color(90, 92, 96));
            await ReplyAsync("", false, embed.Build());
        }

        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Command("delStreamer", RunMode = RunMode.Async)]
        public async Task DelStreamer(string username)
        {
            using var db = _databaseService.Open<RabbotContext>();
            username = username.ToLower();
            var twitchChannel = db.TwitchChannels.FirstOrDefault(p => p.ChannelName == username && p.GuildId == Context.Guild.Id);
            if (twitchChannel == null)
                return;

            db.TwitchChannels.Remove(twitchChannel);
            await db.SaveChangesAsync();

            var embed = new EmbedBuilder();
            embed.WithDescription($"{username} wurde erfolgreich von der Streamer Liste gelöscht.");
            embed.WithColor(new Color(90, 92, 96));
            await ReplyAsync("", false, embed.Build());
        }

        [RequireOwner]
        [Command("answers", RunMode = RunMode.Async)]
        [Alias("antworten")]
        public async Task Answers(int page = 1)
        {
            if (page < 1)
                return;
            using (var db = _databaseService.Open<RabbotContext>())
            {
                var answers = db.RandomAnswers.ToList().ToPagedList(page, 25);
                if (page > answers.PageCount)
                    return;
                var eb = new EmbedBuilder();
                eb.Color = new Color(90, 92, 96);
                string output = "**Alle Antworten:**\n\n";
                foreach (var answer in answers)
                {
                    output += $"ID: {answer.Id} - **{answer.Answer}**\n\n";
                }
                eb.WithDescription(output);
                eb.WithTitle($"Seite {page} von {answers.PageCount}");
                await Context.Channel.SendMessageAsync(null, false, eb.Build());
            }
        }

        [RequireOwner]
        [Command("delAnswer", RunMode = RunMode.Async)]
        public async Task DelAnswer(int id)
        {
            await Context.Message.DeleteAsync();
            using (var db = _databaseService.Open<RabbotContext>())
            {
                var answer = db.RandomAnswers.FirstOrDefault(p => p.Id == id);
                if (answer == null)
                    return;

                db.RandomAnswers.Remove(answer);
                await db.SaveChangesAsync();

                const int delay = 2000;
                var embed = new EmbedBuilder();
                embed.WithDescription($"'{answer.Answer}' wurde erfolgreich gelöscht.");
                embed.WithColor(new Color(90, 92, 96));
                IUserMessage m = await ReplyAsync("", false, embed.Build());
                await Task.Delay(delay);
                await m.DeleteAsync();
            }
        }

        [RequireOwner]
        [Command("addAnswer", RunMode = RunMode.Async)]
        public async Task AddAnswer([Remainder]string answer)
        {
            await Context.Message.DeleteAsync();
            using (var db = _databaseService.Open<RabbotContext>())
            {
                await db.RandomAnswers.AddAsync(new RandomAnswerEntity { Answer = answer });
                await db.SaveChangesAsync();

                const int delay = 2000;
                var embed = new EmbedBuilder();
                embed.WithDescription($"'{answer}' wurde erfolgreich hinzugefügt.");
                embed.WithColor(new Color(90, 92, 96));
                IUserMessage m = await ReplyAsync("", false, embed.Build());
                await Task.Delay(delay);
                await m.DeleteAsync();
            }
        }

        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Command("namechanges", RunMode = RunMode.Async)]
        public async Task Namechanges(IUser user, int page = 1)
        {
            if (page < 1)
                return;
            using (var db = _databaseService.Open<RabbotContext>())
            {
                var namechanges = db.Namechanges.AsQueryable().Where(p => p.UserId == user.Id).OrderByDescending(p => p.Date).ToList().ToPagedList(page, 25);
                if (!namechanges.Any())
                {
                    await Context.Channel.SendMessageAsync($"Der User {user.Username} hat bislang noch keine Namechanges.");
                    return;
                }

                if (page > namechanges.PageCount)
                    return;
                var eb = new EmbedBuilder();
                eb.Color = new Color(90, 92, 96);

                string output = $"Seite {page} von {namechanges.PageCount}\n\n";
                foreach (var namechange in namechanges)
                {
                    output += $"{namechange.Date.ToFormattedString()} - `{namechange.NewName}`\n";
                }
                eb.WithDescription(output);
                eb.WithTitle($"**Alle Namechanges von: {user.Username}**");
                await Context.Channel.SendMessageAsync(null, false, eb.Build());
            }
        }
    }
}
