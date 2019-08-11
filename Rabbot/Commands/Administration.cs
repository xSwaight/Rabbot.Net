using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Rabbot.Database;
using Rabbot.Services;

namespace Rabbot.Commands
{
    public class Administration : ModuleBase<SocketCommandContext>
    {

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
                using (swaightContext db = new swaightContext())
                {

                    await Context.Message.DeleteAsync();
                    var msgs = await Context.Channel.GetMessagesAsync(100).FlattenAsync();
                    msgs = msgs.Where(x => x.Author.Id == user.Id).Take((int)amount);
                    await ((ITextChannel)Context.Channel).DeleteMessagesAsync(msgs);
                    var exp = db.Userfeatures.Where(p => p.UserId == (long)user.Id).FirstOrDefault();
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
            MuteService mute = new MuteService(Context.Client);
            await mute.MuteTargetUser(user, duration, Context);
        }

        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Command("unmute", RunMode = RunMode.Async)]
        [Summary("Entmuted den markierten User.")]
        public async Task Unmute(IUser user)
        {
            await Context.Message.DeleteAsync();
            MuteService mute = new MuteService(Context.Client);
            await mute.UnmuteTargetUser(user, Context);
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
            WarnService warn = new WarnService(Context.Client);
            await warn.Warn(user, Context);
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
            using (swaightContext db = new swaightContext())
            {
                if (db.Guild.Where(p => p.ServerId == (long)Context.Guild.Id).Count() == 0)
                {
                    await db.Guild.AddAsync(new Guild { ServerId = (long)Context.Guild.Id, LogchannelId = (long)Context.Channel.Id });
                }
                else
                {
                    var defaultChannel = db.Guild.Where(p => p.ServerId == (long)Context.Guild.Id).FirstOrDefault();
                    defaultChannel.LogchannelId = (long)Context.Channel.Id;
                }
                db.Guild.Where(p => p.ServerId == (long)Context.Guild.Id).FirstOrDefault().Log = 1;
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
            using (swaightContext db = new swaightContext())
            {
                if (db.Guild.Where(p => p.ServerId == (long)Context.Guild.Id).Count() == 0)
                {
                    await db.Guild.AddAsync(new Guild { ServerId = (long)Context.Guild.Id, Botchannelid = (long)Context.Channel.Id });
                }
                else
                {
                    var defaultChannel = db.Guild.Where(p => p.ServerId == (long)Context.Guild.Id).FirstOrDefault();
                    defaultChannel.Botchannelid = (long)Context.Channel.Id;
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
            using (swaightContext db = new swaightContext())
            {
                if (db.Guild.Where(p => p.ServerId == (long)Context.Guild.Id).Count() == 0)
                {
                    await db.Guild.AddAsync(new Guild { ServerId = (long)Context.Guild.Id, TrashchannelId = (long)Context.Channel.Id });
                }
                else
                {
                    var defaultChannel = db.Guild.Where(p => p.ServerId == (long)Context.Guild.Id).FirstOrDefault();
                    defaultChannel.TrashchannelId = (long)Context.Channel.Id;
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
            using (swaightContext db = new swaightContext())
            {
                if (db.Guild.Where(p => p.ServerId == (long)Context.Guild.Id).Count() == 0)
                {
                    await db.Guild.AddAsync(new Guild { ServerId = (long)Context.Guild.Id, StreamchannelId = (long)Context.Channel.Id });
                }
                else
                {
                    var defaultChannel = db.Guild.Where(p => p.ServerId == (long)Context.Guild.Id).FirstOrDefault();
                    defaultChannel.StreamchannelId = (long)Context.Channel.Id;
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
            using (swaightContext db = new swaightContext())
            {
                if (db.Guild.Where(p => p.ServerId == (long)Context.Guild.Id).Count() == 0)
                {
                    await db.Guild.AddAsync(new Guild { ServerId = (long)Context.Guild.Id, NotificationchannelId = (long)Context.Channel.Id });
                }
                else
                {
                    var defaultChannel = db.Guild.Where(p => p.ServerId == (long)Context.Guild.Id).FirstOrDefault();
                    defaultChannel.NotificationchannelId = (long)Context.Channel.Id;
                }
                db.Guild.Where(p => p.ServerId == (long)Context.Guild.Id).FirstOrDefault().Notify = 1;
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
        [Command("delNotification", RunMode = RunMode.Async)]
        [Summary("Löscht den Notification Channel in den Einstellungen.")]
        public async Task DelNotification()
        {
            await Context.Message.DeleteAsync();
            using (swaightContext db = new swaightContext())
            {
                if (db.Guild.Where(p => p.ServerId == (long)Context.Guild.Id).Count() == 0)
                {
                    return;
                }
                else
                {
                    var defaultChannel = db.Guild.Where(p => p.ServerId == (long)Context.Guild.Id).FirstOrDefault();
                    defaultChannel.NotificationchannelId = null;
                }
                db.Guild.Where(p => p.ServerId == (long)Context.Guild.Id).FirstOrDefault().Notify = 0;
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
            using (swaightContext db = new swaightContext())
            {
                if (db.Guild.Where(p => p.ServerId == (long)Context.Guild.Id).Count() == 0)
                {
                    return;
                }
                else
                {
                    var defaultChannel = db.Guild.Where(p => p.ServerId == (long)Context.Guild.Id).FirstOrDefault();
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
            using (swaightContext db = new swaightContext())
            {
                if (db.Guild.Where(p => p.ServerId == (long)Context.Guild.Id).Count() == 0)
                {
                    return;
                }
                else
                {
                    var defaultChannel = db.Guild.Where(p => p.ServerId == (long)Context.Guild.Id).FirstOrDefault();
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
            using (swaightContext db = new swaightContext())
            {
                if (db.Guild.Where(p => p.ServerId == (long)Context.Guild.Id).Count() == 0)
                {
                    return;
                }
                else
                {
                    var defaultChannel = db.Guild.Where(p => p.ServerId == (long)Context.Guild.Id).FirstOrDefault();
                    defaultChannel.LogchannelId = null;
                }
                db.Guild.Where(p => p.ServerId == (long)Context.Guild.Id).FirstOrDefault().Log = 0;
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
            using (swaightContext db = new swaightContext())
            {
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
        [Command("log", RunMode = RunMode.Async)]
        [Summary("Toggled die Logs.")]
        public async Task ToggleLog()
        {
            await Context.Message.DeleteAsync();
            using (swaightContext db = new swaightContext())
            {
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

        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Command("createMuted", RunMode = RunMode.Async)]
        public async Task CreateMutedRole()
        {
            await Context.Message.DeleteAsync();
            if (Context.Guild.Roles.Where(p => p.Name == "Muted").Count() == 0)
            {
                var mutedPermission = new GuildPermissions(false, false, false, false, false, false, false, false, true, false, false, false, false, false, true, false, false, false, false, false, false, false, false, false, false, false, false, false, false);
                await Context.Guild.CreateRoleAsync("Muted", mutedPermission, Color.Red);
            }
            var permission = new OverwritePermissions(PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Inherit, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Inherit, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny);
            foreach (var textChannel in Context.Guild.TextChannels)
            {
                var muted = Context.Guild.Roles.Where(p => p.Name == "Muted").FirstOrDefault();
                await textChannel.AddPermissionOverwriteAsync(muted, permission, null);
            }
            foreach (var voiceChannel in Context.Guild.VoiceChannels)
            {
                var muted = Context.Guild.Roles.Where(p => p.Name == "Muted").FirstOrDefault();
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
            using (swaightContext db = new swaightContext())
            {
                var Experience = db.Userfeatures.Where(p => p.ServerId == (long)Context.Guild.Id && p.UserId == (long)user.Id).FirstOrDefault();
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
            using (swaightContext db = new swaightContext())
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
                    await Context.Client.SetGameAsync($"{Config.bot.cmdPrefix}rank", null, ActivityType.Watching);
                    await db.SaveChangesAsync();
                    return;
                }

                var Event = db.Event.Where(x => x.Id == eventId).FirstOrDefault();
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
            using (swaightContext db = new swaightContext())
            {
                await db.Badwords.AddAsync(new Badwords { BadWord = Helper.ReplaceCharacter(word) });
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
            using (swaightContext db = new swaightContext())
            {
                var badword = db.Badwords.Where(p => p.BadWord == word).FirstOrDefault();
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
        public async Task Badwords()
        {
            await Context.Message.DeleteAsync();
            using (swaightContext db = new swaightContext())
            {
                var badwords = db.Badwords.ToList();
                var eb = new EmbedBuilder();
                eb.WithDescription($"Alle Badwords: ");
                eb.Color = new Color(90, 92, 96);
                foreach (var badword in badwords)
                {
                    eb.AddField("ID: " + badword.Id.ToString(), badword.BadWord);
                }
                await Context.Channel.SendMessageAsync(null, false, eb.Build());
            }
        }
    }
}
