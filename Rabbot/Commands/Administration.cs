using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using PagedList;
using Rabbot.Database;
using Rabbot.Services;
using Serilog;
using Serilog.Core;

namespace Rabbot.Commands
{
    public class Administration : ModuleBase<SocketCommandContext>
    {
        private readonly WarnService _warnService;
        private readonly MuteService _muteService;
        private static readonly ILogger _logger = Log.ForContext(Serilog.Core.Constants.SourceContextPropertyName, nameof(Administration));

        public Administration(WarnService warnService, MuteService muteService)
        {
            _warnService = warnService;
            _muteService = muteService;
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
                using (rabbotContext db = new rabbotContext())
                {

                    await Context.Message.DeleteAsync();
                    var msgs = await Context.Channel.GetMessagesAsync(100).FlattenAsync();
                    msgs = msgs.Where(x => x.Author.Id == user.Id).Take((int)amount);
                    await ((ITextChannel)Context.Channel).DeleteMessagesAsync(msgs);
                    var exp = db.Userfeatures.FirstOrDefault(p => p.UserId == user.Id);
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
            using (rabbotContext db = new rabbotContext())
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
            using (rabbotContext db = new rabbotContext())
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

            using (rabbotContext db = new rabbotContext())
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
            using (rabbotContext db = new rabbotContext())
            {
                if (!db.Guild.Where(p => p.ServerId == Context.Guild.Id).Any())
                {
                    await db.Guild.AddAsync(new Guild { ServerId = Context.Guild.Id, LogchannelId = Context.Channel.Id });
                }
                else
                {
                    var defaultChannel = db.Guild.FirstOrDefault(p => p.ServerId == Context.Guild.Id);
                    defaultChannel.LogchannelId = Context.Channel.Id;
                }
                db.Guild.FirstOrDefault(p => p.ServerId == Context.Guild.Id).Log = 1;
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
            using (rabbotContext db = new rabbotContext())
            {
                if (!db.Guild.Where(p => p.ServerId == Context.Guild.Id).Any())
                {
                    await db.Guild.AddAsync(new Guild { ServerId = Context.Guild.Id, Botchannelid = Context.Channel.Id });
                }
                else
                {
                    var defaultChannel = db.Guild.FirstOrDefault(p => p.ServerId == Context.Guild.Id);
                    defaultChannel.Botchannelid = Context.Channel.Id;
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
            using (rabbotContext db = new rabbotContext())
            {
                if (!db.Guild.Where(p => p.ServerId == Context.Guild.Id).Any())
                {
                    await db.Guild.AddAsync(new Guild { ServerId = Context.Guild.Id, TrashchannelId = Context.Channel.Id });
                }
                else
                {
                    var defaultChannel = db.Guild.FirstOrDefault(p => p.ServerId == Context.Guild.Id);
                    defaultChannel.TrashchannelId = Context.Channel.Id;
                    defaultChannel.Trash = 1;
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
            using (rabbotContext db = new rabbotContext())
            {
                if (!db.Guild.Where(p => p.ServerId == Context.Guild.Id).Any())
                {
                    await db.Guild.AddAsync(new Guild { ServerId = Context.Guild.Id, StreamchannelId = Context.Channel.Id });
                }
                else
                {
                    var defaultChannel = db.Guild.FirstOrDefault(p => p.ServerId == Context.Guild.Id);
                    defaultChannel.StreamchannelId = Context.Channel.Id;
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
            using (rabbotContext db = new rabbotContext())
            {
                if (!db.Guild.Where(p => p.ServerId == Context.Guild.Id).Any())
                {
                    await db.Guild.AddAsync(new Guild { ServerId = Context.Guild.Id, NotificationchannelId = Context.Channel.Id });
                }
                else
                {
                    var defaultChannel = db.Guild.FirstOrDefault(p => p.ServerId == Context.Guild.Id);
                    defaultChannel.NotificationchannelId = Context.Channel.Id;
                }
                db.Guild.FirstOrDefault(p => p.ServerId == Context.Guild.Id).Notify = 1;
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
            using (rabbotContext db = new rabbotContext())
            {
                if (!db.Guild.Where(p => p.ServerId == Context.Guild.Id).Any())
                {
                    await db.Guild.AddAsync(new Guild { ServerId = Context.Guild.Id, NotificationchannelId = Context.Channel.Id });
                }
                else
                {
                    var defaultChannel = db.Guild.FirstOrDefault(p => p.ServerId == Context.Guild.Id);
                    defaultChannel.LevelchannelId = Context.Channel.Id;
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
            using (rabbotContext db = new rabbotContext())
            {
                if (!db.Guild.Where(p => p.ServerId == Context.Guild.Id).Any())
                {
                    return;
                }
                else
                {
                    var defaultChannel = db.Guild.FirstOrDefault(p => p.ServerId == Context.Guild.Id);
                    defaultChannel.LevelchannelId = null;
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
            using (rabbotContext db = new rabbotContext())
            {
                if (!db.Guild.Where(p => p.ServerId == Context.Guild.Id).Any())
                {
                    return;
                }
                else
                {
                    var defaultChannel = db.Guild.FirstOrDefault(p => p.ServerId == Context.Guild.Id);
                    defaultChannel.NotificationchannelId = null;
                }
                db.Guild.FirstOrDefault(p => p.ServerId == Context.Guild.Id).Notify = 0;
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
            using (rabbotContext db = new rabbotContext())
            {
                if (!db.Guild.Where(p => p.ServerId == Context.Guild.Id).Any())
                {
                    return;
                }
                else
                {
                    var defaultChannel = db.Guild.FirstOrDefault(p => p.ServerId == Context.Guild.Id);
                    defaultChannel.Botchannelid = null;
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
            using (rabbotContext db = new rabbotContext())
            {
                if (!db.Guild.Where(p => p.ServerId == Context.Guild.Id).Any())
                {
                    return;
                }
                else
                {
                    var defaultChannel = db.Guild.FirstOrDefault(p => p.ServerId == Context.Guild.Id);
                    defaultChannel.Botchannelid = null;
                    defaultChannel.Trash = 0;
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
            using (rabbotContext db = new rabbotContext())
            {
                if (!db.Guild.Where(p => p.ServerId == Context.Guild.Id).Any())
                {
                    return;
                }
                else
                {
                    var defaultChannel = db.Guild.FirstOrDefault(p => p.ServerId == Context.Guild.Id);
                    defaultChannel.LogchannelId = null;
                }
                db.Guild.FirstOrDefault(p => p.ServerId == Context.Guild.Id).Log = 0;
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
            using (rabbotContext db = new rabbotContext())
            {
                var currentNotify = db.Guild.FirstOrDefault(p => p.ServerId == Context.Guild.Id).Notify;
                if (currentNotify == 0)
                {
                    db.Guild.FirstOrDefault(p => p.ServerId == Context.Guild.Id).Notify = 1;
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
                    db.Guild.FirstOrDefault(p => p.ServerId == Context.Guild.Id).Notify = 0;
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
            using (rabbotContext db = new rabbotContext())
            {
                var currentLog = db.Guild.FirstOrDefault(p => p.ServerId == Context.Guild.Id).Log;
                if (currentLog == 0)
                {
                    db.Guild.FirstOrDefault(p => p.ServerId == Context.Guild.Id).Log = 1;
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
                    db.Guild.FirstOrDefault(p => p.ServerId == Context.Guild.Id).Log = 0;
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
                await Context.Guild.CreateRoleAsync("Muted", mutedPermission, Color.Red);
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
            using (rabbotContext db = new rabbotContext())
            {
                var Experience = db.Userfeatures.FirstOrDefault(p => p.ServerId == Context.Guild.Id && p.UserId == user.Id);
                const int delay = 2000;
                var embed = new EmbedBuilder();
                if (Experience.Gain == 0)
                {
                    Experience.Gain = 1;
                    embed.WithDescription($"{user.Mention} bekommt jetzt wieder EXP.");
                }
                else
                {
                    Experience.Gain = 0;
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
            using (rabbotContext db = new rabbotContext())
            {
                if (!db.Event.Where(x => x.Id == eventId).Any() && eventId != 0)
                    return;

                var events = db.Event.ToList();
                foreach (var myEvent in events)
                {
                    myEvent.Status = 0;
                }

                if (eventId == 0)
                {
                    await Context.Client.SetGameAsync($"{Config.Bot.CmdPrefix}rank", null, ActivityType.Watching);
                    await db.SaveChangesAsync();
                    return;
                }

                var Event = db.Event.FirstOrDefault(x => x.Id == eventId);
                Event.Status = 1;
                await Context.Client.SetGameAsync($"{Event.Name} Event aktiv!", null, ActivityType.Watching);
                await db.SaveChangesAsync();
            }
        }

        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Command("addBadword", RunMode = RunMode.Async)]
        public async Task AddBadword([Remainder]string word)
        {
            await Context.Message.DeleteAsync();
            using (rabbotContext db = new rabbotContext())
            {
                word = word.ToLower();
                if (db.Badwords.Where(p => p.BadWord == word && p.ServerId == Context.Guild.Id).Any())
                {
                    await ReplyAsync($"**Das Wort ist bereits in der Liste!**");
                    return;
                }

                await db.Badwords.AddAsync(new Badwords { BadWord = Helper.ReplaceCharacter(word), ServerId = Context.Guild.Id });
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
        [Command("delBadword", RunMode = RunMode.Async)]
        public async Task DelBadword([Remainder]string word)
        {
            await Context.Message.DeleteAsync();
            using (rabbotContext db = new rabbotContext())
            {
                word = word.ToLower();
                var badword = db.Badwords.FirstOrDefault(p => p.BadWord == Helper.ReplaceCharacter(word) && p.ServerId == Context.Guild.Id);
                if (badword == null)
                    return;

                db.Badwords.Remove(badword);
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
            using (rabbotContext db = new rabbotContext())
            {
                var badwords = db.Badwords.Where(p => p.ServerId == Context.Guild.Id).ToList().OrderBy(p => p.BadWord).ToPagedList(page, 25);
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

        [RequireOwner]
        [Command("answers", RunMode = RunMode.Async)]
        [Alias("antworten")]
        public async Task Answers(int page = 1)
        {
            if (page < 1)
                return;
            using (rabbotContext db = new rabbotContext())
            {
                var answers = db.Randomanswer.ToList().ToPagedList(page, 25);
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
            using (rabbotContext db = new rabbotContext())
            {
                var answer = db.Randomanswer.FirstOrDefault(p => p.Id == id);
                if (answer == null)
                    return;

                db.Randomanswer.Remove(answer);
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
            using (rabbotContext db = new rabbotContext())
            {
                await db.Randomanswer.AddAsync(new Randomanswer { Answer = answer });
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
            using (rabbotContext db = new rabbotContext())
            {
                var namechanges = db.Namechanges.Where(p => p.UserId == user.Id).OrderByDescending(p => p.Date).ToList().ToPagedList(page, 25);
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
