using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBot_Core.Database;
using DiscordBot_Core.Preconditions;

namespace DiscordBot_Core.Commands
{
    public class Misc : ModuleBase<SocketCommandContext>
    {
        private readonly string version = "0.8";

        [Command("help", RunMode = RunMode.Async)]
        [BotCommand]
        [Cooldown(30)]
        public async Task Help(int page = 1)
        {
            var embed = new EmbedBuilder();
            embed.WithColor(new Color(241, 242, 222));
            if (page == 1)
            {
                embed.Description = "Commandlist Seite 1:";
                embed.AddField("Hinweis", "Pflicht Argumente: [argument] | Optionale Argumente: (argument)");
                embed.AddField("__**Normal:**__ \n" + Config.bot.cmdPrefix + "player [S4 Username]", "Gibt die Stats eines S4 Spielers aus.");
                embed.AddField(Config.bot.cmdPrefix + "clan [S4 Clanname]", "Gibt die Stats eines S4 Clans aus.");
                embed.AddField(Config.bot.cmdPrefix + "playercard [S4 Username]", "Erstellt eine Playercard Grafik.");
                embed.AddField(Config.bot.cmdPrefix + "s4dbcard [S4 Username]", "Erstellt eine Playercard Grafik im S4DB Style.");
                embed.AddField(Config.bot.cmdPrefix + "about", "Gibt Statistiken zum Bot aus.");
                embed.AddField(Config.bot.cmdPrefix + "ping", "Gibt die Verzögerung zum Bot aus.");
                embed.AddField(Config.bot.cmdPrefix + "profile (User Mention)", "Ohne Argument gibt er das eigene Profil aus, mit Argument das Profil des markierten Users.");
                embed.AddField(Config.bot.cmdPrefix + "daily", "Du hast die tägliche Chance deinen Stall mit Ziegen zu füllen, mit denen du handeln kannst.");
                embed.AddField(Config.bot.cmdPrefix + "shop", "Zeigt aktuelle Angebote zur Verwendung von Ziegen.");
                embed.AddField(Config.bot.cmdPrefix + "angriff [User Mention]", "Startet einen Angriff gegen markierten User.");
                embed.AddField(Config.bot.cmdPrefix + "stats (User Mention)", "Gibt deine Kampfstatistiken aus.");
                embed.AddField(Config.bot.cmdPrefix + "stall (User Mention)", "Zeigt deinen aktuellen Stall an.");
                embed.AddField(Config.bot.cmdPrefix + "stalls", "Zeigt eine Liste mit allen Stallen an.");
                if (Context.Guild.Roles.Where(p => p.Name == "S4 League").Count() > 0)
                    embed.AddField(Config.bot.cmdPrefix + "s4", "Gibt dir die S4 League Rolle.");
            }
            else if (page == 2)
            {
                embed.Description = "Commandlist Seite 2:";
                embed.AddField("\n__**Administration:**__ \n" + Config.bot.cmdPrefix + "del [anzahl] (User Mention)", "Löscht die angegebene Anzahl an Nachrichten im aktuellen Channel (Limit von 100 Nachrichten).");
                embed.AddField(Config.bot.cmdPrefix + "mute [User Mention] [duration]", "Muted den User für angegebene Zeit (Zeitindikatoren: s = sekunden, m = minuten, h = stunden, d = tage).");
                embed.AddField(Config.bot.cmdPrefix + "unmute [User Mention]", "Unmuted den User.");
                embed.AddField(Config.bot.cmdPrefix + "warn [User Mention]", "Warnt den User.");
                embed.AddField(Config.bot.cmdPrefix + "addBadword [word]", "Fügt das Wort zum Wortfilter hinzu.");
                embed.AddField(Config.bot.cmdPrefix + "delBadword [word]", "Löscht das Wort aus dem Wortfilter.");
                embed.AddField(Config.bot.cmdPrefix + "badwords", "Listet alle Badwords auf.");
                embed.AddField(Config.bot.cmdPrefix + "settings", "Zeigt die aktuellen Einstellungen an.");
                embed.AddField(Config.bot.cmdPrefix + "setBot", "Setzt den aktuellen Channel als Bot Channel.");
                embed.AddField(Config.bot.cmdPrefix + "delBot", "Löscht den aktuell gesetzten Bot Channel.");
                embed.AddField(Config.bot.cmdPrefix + "setLog", "Setzt den aktuellen Channel als Log Channel.");
                embed.AddField(Config.bot.cmdPrefix + "delLog", "Löscht den aktuell gesetzten Log Channel.");
                embed.AddField(Config.bot.cmdPrefix + "log", "Aktiviert oder deaktiviert die Logs auf dem Server.");
                embed.AddField(Config.bot.cmdPrefix + "setNotification", "Setzt den aktuellen Channel als Notification Channel.");
                embed.AddField(Config.bot.cmdPrefix + "delNotification", "Löscht den aktuell gesetzten Log Channel.");
                embed.AddField(Config.bot.cmdPrefix + "setupLevels", "Erstellt S4 League Level Gruppen die automatisch beim erreichen des jeweiligen Levels gesetzt werden.");
                embed.AddField(Config.bot.cmdPrefix + "levelNotification", "Aktiviert oder deaktiviert die Level Nofitications auf dem Server.");
                embed.AddField(Config.bot.cmdPrefix + "notification", "Aktiviert oder deaktiviert die Notifications auf dem Server.");
            }
            else
            {
                return;
            }
            embed.WithFooter(new EmbedFooterBuilder() { Text = "Version " + version, IconUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/2/25/Info_icon-72a7cf.svg/2000px-Info_icon-72a7cf.svg.png" });
            await Context.Channel.SendMessageAsync(null, false, embed.Build());
        }

        [Command("about", RunMode = RunMode.Async)]
        [BotCommand]
        [Cooldown(30)]
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
            embed.WithColor(new Color(241, 242, 222));
            embed.AddField("Total Users", memberCount.ToString(), true);
            embed.AddField("Online Users", (memberCount - offlineCount).ToString(), true);
            embed.AddField("Total Servers", Context.Client.Guilds.Count.ToString(), true);
            embed.ThumbnailUrl = "https://cdn.discordapp.com/attachments/210496271000141825/533052805582290972/hasi.png";
            embed.AddField("Bot created at", Context.Client.CurrentUser.CreatedAt.DateTime.ToShortDateString(), false);
            embed.WithFooter(new EmbedFooterBuilder() { Text = "Version " + version, IconUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/2/25/Info_icon-72a7cf.svg/2000px-Info_icon-72a7cf.svg.png" });
            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Command("settings", RunMode = RunMode.Async)]
        [BotCommand]
        [Cooldown(30)]
        public async Task Settings()
        {
            using (swaightContext db = new swaightContext())
            {
                var guild = db.Guild.Where(p => p.ServerId == (long)Context.Guild.Id).FirstOrDefault();
                if (guild == null)
                    return;
                var logChannel = Context.Guild.TextChannels.Where(p => (long?)p.Id == guild.LogchannelId).FirstOrDefault();
                var notificationChannel = Context.Guild.TextChannels.Where(p => (long?)p.Id == guild.NotificationchannelId).FirstOrDefault();
                var botcChannel = Context.Guild.TextChannels.Where(p => (long?)p.Id == guild.Botchannelid).FirstOrDefault();

                var embed = new EmbedBuilder();
                embed.WithDescription($"**Settings**");
                embed.WithColor(new Color(241, 242, 222));
                if (logChannel != null)
                    embed.AddField("Log Channel", logChannel.Mention, true);
                else
                    embed.AddField("Log Channel", "Nicht gesetzt.", true);

                if (notificationChannel != null)
                    embed.AddField("Notification Channel", notificationChannel.Mention, true);
                else
                    embed.AddField("Notification Channel", "Nicht gesetzt.", true);


                switch (guild.Log)
                {
                    case 0:
                        embed.AddField("Log", "Disabled", true);
                        break;
                    case 1:
                        embed.AddField("Log", "Enabled", true);
                        break;
                    default:
                        embed.AddField("Log", "Unknown", true);
                        break;
                }

                switch (guild.Notify)
                {
                    case 0:
                        embed.AddField("Notification", "Disabled", true);
                        break;
                    case 1:
                        embed.AddField("Notification", "Enabled", true);
                        break;
                    default:
                        embed.AddField("Notification", "Unknown", true);
                        break;
                }

                switch (guild.Level)
                {
                    case 0:
                        embed.AddField("Level", "Disabled", true);
                        break;
                    case 1:
                        embed.AddField("Level", "Enabled", true);
                        break;
                    default:
                        embed.AddField("Level", "Unknown", true);
                        break;
                }

                if (botcChannel != null)
                    embed.AddField("Bot Channel", botcChannel.Mention, true);
                else
                    embed.AddField("Bot Channel", "Nicht gesetzt.", true);

                embed.ThumbnailUrl = "https://cdn.pixabay.com/photo/2018/03/27/23/58/silhouette-3267855_960_720.png";
                embed.WithFooter(new EmbedFooterBuilder() { Text = "Version " + version, IconUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/2/25/Info_icon-72a7cf.svg/2000px-Info_icon-72a7cf.svg.png" });
                await Context.Channel.SendMessageAsync("", false, embed.Build());
            }
        }

        [Command("ping", RunMode = RunMode.Async)]
        [BotCommand]
        [Cooldown(30)]
        public async Task Ping()
        {
            await Context.Channel.SendMessageAsync("Pong! `" + Context.Client.Latency + "ms`");
        }

        [Command("test", RunMode = RunMode.Async)]
        [BotCommand]
        [RequireOwner]
        [Cooldown(30)]
        public async Task Test()
        {

            Emote emote = Emote.Parse("<:shtaco:555055295806701578>"); //Normal
            Emote emote2 = Emote.Parse("<a:shtaco:555055295806701578>"); //Animated
            await Context.Channel.SendMessageAsync($"Hi {emote}");
        }

        [Command("hdf", RunMode = RunMode.Async)]
        public async Task Hdf()
        {
            if (!Context.IsPrivate)
                return;

            using (swaightContext db = new swaightContext())
            {
                var user = db.User.Where(p => p.Id == (long)Context.User.Id).FirstOrDefault();
                if (user.Notify == 1)
                {
                    user.Notify = 0;
                    await Context.Channel.SendMessageAsync("Na gut.");
                }
                else
                {
                    user.Notify = 1;
                    await Context.Channel.SendMessageAsync("Yay!");
                }
                await db.SaveChangesAsync();
            }
        }

        public ulong GetCrossSum(ulong n)
        {
            ulong sum = 0;
            while (n != 0)
            {
                sum += n % 10;
                n /= 10;
            }

            return sum;
        }
    }
}
