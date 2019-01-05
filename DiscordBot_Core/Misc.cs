using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using DiscordBot_Core.API;
using DiscordBot_Core.Models;
using DiscordBot_Core.ImageGenerator;
using System.Text;

namespace DiscordBot_Core
{
    public class Misc : ModuleBase<SocketCommandContext>
    {
        discordbotContext db = new discordbotContext();
        private readonly string version = "0.2";

        [Command("help")]
        public async Task Help()
        {
            var embed = new EmbedBuilder();
            embed.Description = "Commandlist:";
            embed.AddField("Legende:", "Pflicht Argumente: [argument] | Optionale Argumente: (argument)");
            embed.AddField("*Normal:* \n" + Config.bot.cmdPrefix + "player [S4 Username]", "Gibt die Stats eines S4 Spielers aus.");
            embed.AddField(Config.bot.cmdPrefix + "playercard [S4 Username]", "Erstellt eine Playercard Grafik.");
            embed.AddField(Config.bot.cmdPrefix + "s4dbcard [S4 Username]", "Erstellt eine Playercard Grafik im S4DB Style.");
            embed.AddField(Config.bot.cmdPrefix + "server", "Gibt die aktuelle Spielerzahl aus.");
            embed.AddField(Config.bot.cmdPrefix + "about", "Gibt Statistiken zum Bot aus.");
            embed.AddField(Config.bot.cmdPrefix + "ping", "Gibt die Verzögerung zum Bot aus.");
            embed.AddField(Config.bot.cmdPrefix + "level (User Mention)", "Ohne Argument gibt es das eigene Level aus, mit Argument das Level des Markierten Users.");
            if (Context.Guild.Roles.Where(p => p.Name == "S4 League").Count() > 0)
                embed.AddField(Config.bot.cmdPrefix + "s4", "Gibt dir die S4 League Rolle.");
            embed.AddField("\n*Administration:* \n" + Config.bot.cmdPrefix + "delete [anzahl]", "Löscht die angegebene Anzahl an Nachrichten im aktuellen Channel (Limit von 100 Nachrichten).");
            embed.AddField(Config.bot.cmdPrefix + "mute [User Mention] [duration]", "Muted den User für angegebene Zeit (Zeitindikatoren: s = sekunden, m = minuten, h = stunden, d = tage).");
            embed.AddField(Config.bot.cmdPrefix + "unmute [User Mention]", "Unmuted den User.");
            embed.AddField(Config.bot.cmdPrefix + "settings", "Zeigt die aktuellen Einstellungen an.");
            embed.AddField(Config.bot.cmdPrefix + "setLog", "Setzt den aktuellen Channel als Log Channel.");
            embed.AddField(Config.bot.cmdPrefix + "delLog", "Löscht den aktuell gesetzten Log Channel.");
            embed.AddField(Config.bot.cmdPrefix + "setNotification", "Setzt den aktuellen Channel als Notification Channel.");
            embed.AddField(Config.bot.cmdPrefix + "delNotification", "Löscht den aktuell gesetzten Log Channel.");
            embed.AddField(Config.bot.cmdPrefix + "notification", "Aktiviert oder deaktiviert die Notifications auf dem Server.");
            embed.WithFooter(new EmbedFooterBuilder() { Text = "Version " + version, IconUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/2/25/Info_icon-72a7cf.svg/2000px-Info_icon-72a7cf.svg.png" });
            await Context.Channel.SendMessageAsync(null, false, embed.Build());
        }

        [Command("player")]
        public async Task Player([Remainder]string arg)
        {
            if (!String.IsNullOrWhiteSpace(arg))
            {
                Player player = new Player();
                S4DB DB = new S4DB();
                player = await DB.GetPlayer(arg);
                if (player == null)
                {
                    var embed = new EmbedBuilder();
                    embed.WithTitle("Fehler");
                    embed.WithDescription("Spieler nicht gefunden ¯\\_(ツ)_/¯");
                    embed.WithColor(new Color(255, 0, 0));
                    await Context.Channel.SendMessageAsync("", false, embed.Build());
                }
                else
                {
                    var embedInfo = new EmbedBuilder();
                    embedInfo.WithColor(new Color(42, 46, 53));
                    embedInfo.AddField("Name", player.Name, true);
                    if (player.Clan != null)
                        embedInfo.AddField("Clan", player.Clan, true);
                    else
                        embedInfo.AddField("Clan", "-", true);
                    embedInfo.AddField("Level", player.Level.ToString(), true);
                    string[] exp = player.Levelbar.Split(':');
                    var percent = (Convert.ToDecimal(exp[0]) / Convert.ToDecimal(exp[1])) * 100;
                    embedInfo.AddField("Percent", Math.Round(percent, 2).ToString() + "%", true);
                    embedInfo.AddField("EXP", player.Exp.ToString("N0"), true);
                    TimeSpan time = DateTime.Now.AddSeconds(player.Playtime) - DateTime.Now;
                    string playtime = time.Days + "D " + time.Hours + "H " + time.Minutes + "M ";
                    embedInfo.AddField("Playtime", playtime, true);
                    embedInfo.AddField("TD Rate", player.Tdrate.ToString(), true);
                    embedInfo.AddField("KD Rate", player.Kdrate.ToString(), true);
                    embedInfo.AddField("Matches played", player.Matches_played.ToString("N0"), true);
                    embedInfo.AddField("Matches won", player.Matches_won.ToString("N0"), true);
                    embedInfo.AddField("Matches lost", player.Matches_lost.ToString("N0"), true);
                    embedInfo.AddField("Last online", Convert.ToDateTime(player.Last_online).ToShortDateString(), true);
                    embedInfo.AddField("Views", player.Views.ToString("N0"), true);
                    embedInfo.AddField("Favorites", player.Favorites.ToString("N0"), true);
                    embedInfo.AddField("Fame", player.Fame.ToString() + "%", true);
                    embedInfo.AddField("Hate", player.Hate.ToString() + "%", true);
                    embedInfo.ThumbnailUrl = "https://s4db.net/assets/img/icon192.png";
                    await Context.Channel.SendMessageAsync("", false, embedInfo.Build());
                }
            }
        }

        [Command("server")]
        public async Task Server()
        {
            List<Server> server = new List<Server>();
            S4DB DB = new S4DB();
            server = await DB.GetServer();
            int onlinecount = 0;
            foreach (var item in server)
            {
                if (item.Player_online >= 0)
                    onlinecount += item.Player_online;
            }
            await Context.Channel.SendMessageAsync($"Es sind {onlinecount} Spieler online!");
        }

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

        [Command("about")]
        public async Task About()
        {
            int memberCount = 0;
            int offlineCount = 0;
            foreach (var server in Context.Client.Guilds)
            {
                memberCount += server.MemberCount;
                offlineCount += server.Users.Where(p => p.Status == UserStatus.Offline).Count();
            }

            var embed = new EmbedBuilder();
            embed.WithDescription($"**Statistiken**");
            embed.WithColor(new Color(197, 122, 255));
            embed.AddField("Total Users", memberCount.ToString(), true);
            embed.AddField("Online Users", (memberCount - offlineCount).ToString(), true);
            embed.AddField("Total Servers", Context.Client.Guilds.Count.ToString(), true);
            embed.ThumbnailUrl = "https://cdn.discordapp.com/attachments/210496271000141825/529839617113980929/robo2.png";
            embed.AddField("Bot created at", Context.Client.CurrentUser.CreatedAt.ToString(), false);
            embed.WithFooter(new EmbedFooterBuilder() { Text = "Version " + version, IconUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/2/25/Info_icon-72a7cf.svg/2000px-Info_icon-72a7cf.svg.png" });
            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Command("settings")]
        public async Task Settings()
        {
            var channel = db.Guild.Where(p => p.ServerId == (long)Context.Guild.Id).FirstOrDefault();
            if (channel == null)
                return;
            var logChannel = Context.Guild.TextChannels.Where(p => (long?)p.Id == channel.LogchannelId).FirstOrDefault();
            var notificationChannel = Context.Guild.TextChannels.Where(p => (long?)p.Id == channel.NotificationchannelId).FirstOrDefault();

            var embed = new EmbedBuilder();
            embed.WithDescription($"**Settings**");
            embed.WithColor(new Color(197, 122, 255));
            if (logChannel != null)
                embed.AddField("Log Channel", logChannel.Mention, true);
            else
                embed.AddField("Log Channel", "Nicht gesetzt.", true);

            if (notificationChannel != null)
                embed.AddField("Notification Channel", notificationChannel.Mention, true);
            else
                embed.AddField("Notification Channel", "Nicht gesetzt.", true);

            switch (channel.Notify)
            {
                case 0:
                    embed.AddField("Notification", "Disabled");
                    break;
                case 1:
                    embed.AddField("Notification", "Enabled");
                    break;
                default:
                    embed.AddField("Notification", "Unknown");
                    break;
            }

            embed.ThumbnailUrl = "https://cdn.pixabay.com/photo/2018/03/27/23/58/silhouette-3267855_960_720.png";
            embed.WithFooter(new EmbedFooterBuilder() { Text = "Version " + version, IconUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/2/25/Info_icon-72a7cf.svg/2000px-Info_icon-72a7cf.svg.png" });
            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("ping")]
        public async Task Ping()
        {
            var dbContext = new discordbotContext();
            var users = dbContext.User.ToList();
            await Context.Channel.SendMessageAsync("Pong! `" + Context.Client.Latency + "ms`");
        }

        [Command("level")]
        public async Task Level(IGuildUser user = null)
        {
            if (user != null)
            {
                var exp = db.Experience.Where(p => p.UserId == (long)user.Id).FirstOrDefault();
                if (exp != null)
                {
                    uint level = GetLevel(exp.Exp);
                    await Context.Channel.SendMessageAsync($"{user.Username} ist Level {level} und hat {exp.Exp} EXP!");
                }
                else
                {
                    await Context.Channel.SendMessageAsync($"{user.Username} hat keine EXP!");
                }
            }
            else
            {
                var exp = db.Experience.Where(p => p.UserId == (long)Context.User.Id).FirstOrDefault();
                if (exp != null)
                {
                    uint level = GetLevel(exp.Exp);
                    await Context.Channel.SendMessageAsync($"{Context.User.Mention} du bist Level {level} und hast {exp.Exp} EXP!");
                }
                else
                {
                    await Context.Channel.SendMessageAsync($"{Context.User.Mention} du hast keine EXP!");
                }
            }
        }

        private uint GetLevel(int? exp)
        {
            uint level = (uint)Math.Sqrt((uint)exp / 50);
            return level;
        }

        [RequireUserPermission(GuildPermission.MuteMembers)]
        [Command("mute")]
        public async Task Mute(IUser user, string duration)
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
            if(targetPosition > position)
            {
                var embed = new EmbedBuilder();
                embed.WithDescription($"Es fehlen die Berechtigungen um {user.Mention} zu muten!");
                embed.WithColor(new Color(255, 0, 0));
                var message = await Context.Channel.SendMessageAsync("", false, embed.Build());
                await Task.Delay(5000);
                await message.DeleteAsync();
                return;
            }
            if(Context.User.Id == user.Id)
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
            var logchannelId = db.Guild.Where(p => p.ServerId == (long)Context.Guild.Id).FirstOrDefault().LogchannelId;
            if (logchannelId != null)
            {
                var logchannel = Context.Guild.TextChannels.Where(p => p.Id == (ulong)logchannelId).FirstOrDefault();
                var embed = new EmbedBuilder();
                embed.WithDescription($"{Context.User.Username} hat {user.Mention} für {duration} gemuted.");
                embed.WithColor(new Color(255, 0, 0));
                await logchannel.SendMessageAsync("", false, embed.Build());
            }

            await db.SaveChangesAsync();
        }

        [RequireUserPermission(GuildPermission.MuteMembers)]
        [Command("unmute")]
        public async Task Unmute(IUser user)
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
                var logchannelId = db.Guild.Where(p => p.ServerId == (long)Context.Guild.Id).FirstOrDefault().LogchannelId;
                var User = Context.Guild.Users.Where(p => p.Id == user.Id).FirstOrDefault();
                var MutedRole = User.Roles.Where(p => p.Name == "Muted").FirstOrDefault();
                if (MutedRole != null)
                    await User.RemoveRoleAsync(MutedRole);
                if (logchannelId != null)
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
                    var logchannel = Context.Guild.TextChannels.Where(p => p.Id == (ulong)logchannelId).FirstOrDefault();
                    var embed = new EmbedBuilder();
                    embed.WithDescription($"{user.Mention} wurde unmuted.");
                    embed.WithColor(new Color(0, 255, 0));
                    await logchannel.SendMessageAsync("", false, embed.Build());
                }
            }
        }


        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Command("setLog")]
        public async Task SetLog()
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

        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Command("setNotification")]
        public async Task SetNotification()
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

        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Command("delNotification")]
        public async Task DelNotification()
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

        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Command("delLog")]
        public async Task DelLog()
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

        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Command("notification")]
        public async Task Notification()
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


        [Command("playercard")]
        public async Task Playercard([Remainder]string arg)
        {
            Player player = new Player();
            S4DB DB = new S4DB();
            player = await DB.GetPlayer(arg);
            if (player == null)
            {
                var embed = new EmbedBuilder();
                embed.WithTitle("Fehler");
                embed.WithDescription("Spieler nicht gefunden ¯\\_(ツ)_/¯");
                embed.WithColor(new Color(255, 0, 0));
                await Context.Channel.SendMessageAsync("", false, embed.Build());
            }
            else
            {
                var template = new HtmlTemplate(Directory.GetCurrentDirectory() + "/playercardTemplate.html");
                var html = template.Render(new
                {
                    BACKGROUND = "Background.png",
                    COLOR = "#949494",
                    LEVEL = player.Level.ToString(),
                    IGNAME = player.Name,
                    EXP = player.Exp.ToString("N0"),
                    TOUCHDOWN = player.Tdrate.ToString(),
                    MATCHES = player.Matches_played.ToString("N0"),
                    DEATHMATCH = player.Kdrate.ToString()
                });

                var path = HtmlToImage.Generate(RemoveSpecialCharacters(arg), html);
                await Context.Channel.SendFileAsync(path);
                File.Delete(path);
            }
        }

        [Command("s4dbcard")]
        public async Task S4dbcard([Remainder]string arg)
        {
            Player player = new Player();
            S4DB DB = new S4DB();
            player = await DB.GetPlayer(arg);
            if (player == null)
            {
                var embed = new EmbedBuilder();
                embed.WithTitle("Fehler");
                embed.WithDescription("Spieler nicht gefunden ¯\\_(ツ)_/¯");
                embed.WithColor(new Color(255, 0, 0));
                await Context.Channel.SendMessageAsync("", false, embed.Build());
            }
            else
            {
                var template = new HtmlTemplate(Directory.GetCurrentDirectory() + "/playercardTemplate.html");
                var html = template.Render(new
                {
                    BACKGROUND = "S4DB_background.png",
                    COLOR = "#403e3e",
                    LEVEL = player.Level.ToString(),
                    IGNAME = player.Name,
                    EXP = player.Exp.ToString("N0"),
                    TOUCHDOWN = player.Tdrate.ToString(),
                    MATCHES = player.Matches_played.ToString("N0"),
                    DEATHMATCH = player.Kdrate.ToString()
                });

                var path = HtmlToImage.Generate(RemoveSpecialCharacters(arg), html);
                await Context.Channel.SendFileAsync(path);
                File.Delete(path);
            }
        }

        [Command("s4")]
        public async Task S4()
        {
            await Context.Message.DeleteAsync();
            var s4Role = Context.Guild.Roles.Where(p => p.Name == "S4 League");
            if (s4Role.Count() == 0)
                return;

            var user = Context.Guild.Users.Where(p => p.Id == Context.User.Id).FirstOrDefault();
            if (user.Roles.Where(p => p.Name == "S4 League").Count() != 0)
                return;

            await user.AddRoleAsync(s4Role.FirstOrDefault());
            var logchannelId = db.Guild.Where(p => p.ServerId == (long)Context.Guild.Id).FirstOrDefault().LogchannelId;
            if (logchannelId != null)
            {
                var logchannel = Context.Guild.TextChannels.Where(p => p.Id == (ulong)logchannelId).FirstOrDefault();
                var embed = new EmbedBuilder();
                embed.WithDescription($"{Context.User.Mention} hat sich die S4 League Rolle gegeben.");
                embed.WithColor(new Color(0, 255, 0));
                await logchannel.SendMessageAsync("", false, embed.Build());
            }
        }

        private string RemoveSpecialCharacters(string str)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in str)
            {
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '.' || c == '_')
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }
    }
}
