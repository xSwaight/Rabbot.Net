using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using DiscordBot_Core.Database;
using DiscordBot_Core.Services;

namespace DiscordBot_Core.Commands
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
                await Log.Delete(Context, (int)amount);
                await Task.Delay(delay);
                await m.DeleteAsync();
            }
            else
            {
                await Context.Message.DeleteAsync();
                var msgs = await Context.Channel.GetMessagesAsync(100).FlattenAsync();
                msgs = msgs.Where(x => x.Author.Id == user.Id).Take((int)amount);
                await ((ITextChannel)Context.Channel).DeleteMessagesAsync(msgs);
                using (swaightContext db = new swaightContext())
                {
                    var exp = db.Userfeatures.Where(p => p.UserId == (long)user.Id).FirstOrDefault();
                    if (exp.Exp > (50 * amount))
                        exp.Exp -= (int)(50 * amount);
                    else
                    {
                        amount = (uint)exp.Exp;
                        exp.Exp = 0;
                    }
                    await db.SaveChangesAsync();
                }
                const int delay = 3000;
                var embed = new EmbedBuilder();
                embed.WithDescription($"Die letzten {amount} Nachrichten von {user.Username} wurden gelöscht.");
                embed.WithColor(new Color(90, 92, 96));
                IUserMessage m = await ReplyAsync("", false, embed.Build());
                await Log.Delete(user, Context, (int)amount);
                await Task.Delay(delay);
                await m.DeleteAsync();

            }
        }

        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Command("mute", RunMode = RunMode.Async)]
        public async Task Mute(IUser user, string duration)
        {
            if (duration.Contains('-'))
                return;

            await Context.Message.DeleteAsync();
            MuteService mute = new MuteService(Context.Client);
            await mute.MuteTargetUser(user, duration, Context);
        }

        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Command("unmute", RunMode = RunMode.Async)]
        public async Task Unmute(IUser user)
        {
            await Context.Message.DeleteAsync();
            MuteService mute = new MuteService(Context.Client);
            await mute.UnmuteTargetUser(user, Context);
        }

        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Command("warn", RunMode = RunMode.Async)]
        public async Task Warn(IUser user)
        {
            await Context.Message.DeleteAsync();
            if (user == null)
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
            TimeSpan span = DateTime.Now - Program.startTime;
            await Context.Channel.SendMessageAsync($"`Uptime: {span.Days}D {span.Hours}H {span.Minutes}M`");
            
        }

        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Command("setLog", RunMode = RunMode.Async)]
        public async Task SetLog()
        {
            using (swaightContext db = new swaightContext())
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
        public async Task SetBot()
        {
            using (swaightContext db = new swaightContext())
            {
                await Context.Message.DeleteAsync();
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
        [Command("setNotification", RunMode = RunMode.Async)]
        public async Task SetNotification()
        {
            using (swaightContext db = new swaightContext())
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
        public async Task DelNotification()
        {
            using (swaightContext db = new swaightContext())
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
        public async Task DelBot()
        {
            using (swaightContext db = new swaightContext())
            {
                await Context.Message.DeleteAsync();
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
        [Command("delLog", RunMode = RunMode.Async)]
        public async Task DelLog()
        {
            using (swaightContext db = new swaightContext())
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
        public async Task Notification()
        {
            using (swaightContext db = new swaightContext())
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
        [Command("log", RunMode = RunMode.Async)]
        public async Task ToggleLog()
        {
            using (swaightContext db = new swaightContext())
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
        public async Task Event()
        {
            await Context.Message.DeleteAsync();
            using (swaightContext db = new swaightContext())
            {
                var myEvent = db.Event.FirstOrDefault();
                if (myEvent.Status == 0)
                {
                    myEvent.Status = 1;
                    await Context.Client.SetGameAsync($"{myEvent.Name} Event aktiv!", null, ActivityType.Watching);
                }
                else
                {
                    myEvent.Status = 0;
                    await Context.Client.SetGameAsync($">rank", null, ActivityType.Watching);
                }
                await db.SaveChangesAsync();
            }
        }

        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Command("addBadword", RunMode = RunMode.Async)]
        public async Task AddBadword(string word)
        {
            await Context.Message.DeleteAsync();
            using (swaightContext db = new swaightContext())
            {
                await db.Badwords.AddAsync(new Badwords { BadWord = word });
                await db.SaveChangesAsync();
            }

            const int delay = 2000;
            var embed = new EmbedBuilder();
            embed.WithDescription($"{word} wurde erfolgreich zum Wortfilter hinzugefügt.");
            embed.WithColor(new Color(90, 92, 96));
            IUserMessage m = await ReplyAsync("", false, embed.Build());
            await Task.Delay(delay);
            await m.DeleteAsync();
        }

        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Command("delBadword", RunMode = RunMode.Async)]
        public async Task DelBadword(string word)
        {
            await Context.Message.DeleteAsync();
            using (swaightContext db = new swaightContext())
            {
                var badword = db.Badwords.Where(p => p.BadWord == word).FirstOrDefault();
                if (badword == null)
                    return;

                db.Badwords.Remove(badword);
                await db.SaveChangesAsync();
            }

            const int delay = 2000;
            var embed = new EmbedBuilder();
            embed.WithDescription($"{word} wurde erfolgreich vom Wortfilter gelöscht.");
            embed.WithColor(new Color(90, 92, 96));
            IUserMessage m = await ReplyAsync("", false, embed.Build());
            await Task.Delay(delay);
            await m.DeleteAsync();
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
