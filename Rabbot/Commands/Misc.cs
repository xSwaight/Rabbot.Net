using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Rabbot.Database;
using Rabbot.Preconditions;

namespace Rabbot.Commands
{
    public class Misc : ModuleBase<SocketCommandContext>
    {
        private readonly string version = "0.9";
        private readonly CommandService _commandService;
        public Misc(CommandService commandService)
        {
            _commandService = commandService;
        }

        [Command("help", RunMode = RunMode.Async)]
        [BotCommand]
        [Cooldown(30)]
        [Summary("Zeigt diese Liste an.")]
        public async Task Help(int seite = 1)
        {
            int pagesize = 15;

            List<CommandInfo> commands = _commandService.Commands.Where(p => p.Summary != null && p.Module.Name != "Administration").ToList();

            if (seite > Math.Ceiling((commands.Count() / (double)pagesize)) || seite < 1)
                return;

            string help = $"**Seite {seite}/{Math.Ceiling((commands.Count() / (double)pagesize))}**\n\n`(Parameter) -> Optionaler Parameter`\n`[Parameter] -> Pflich Parameter`\n\n";

            seite--;

            foreach (var command in commands.OrderBy(p => p.Name).Skip((pagesize * seite)).Take(pagesize))
            {
                string param = "";
                string aliases = "";

                foreach (var parameter in command.Parameters)
                {
                    if (parameter.IsOptional)
                        param += $"({parameter}) ";
                    else
                        param += $"[{parameter}] ";

                }

                foreach (var alias in command.Aliases)
                {
                    if (alias != command.Name)
                        aliases += $"{Config.bot.cmdPrefix}{alias} ";
                }
                aliases = aliases.TrimEnd();
                if (!string.IsNullOrWhiteSpace(aliases))
                    help += $"**{Config.bot.cmdPrefix}{command.Name} {param}**\n*Alternativen: {aliases}*\n`{command.Summary}`\n";
                else
                    help += $"**{Config.bot.cmdPrefix}{command.Name} {param}**\n`{command.Summary}`\n";

            }

            await Context.Channel.SendMessageAsync(help);
        }

        [Command("about", RunMode = RunMode.Async)]
        [Summary("Gibt Statistiken über den Bot aus.")]
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
        [Summary("Gibt die aktuellen Einstellungen aus.")]
        [BotCommand]
        [Cooldown(30)]
        public async Task Settings()
        {
            using (swaightContext db = new swaightContext())
            {

                var guild = db.Guild.FirstOrDefault(p => p.ServerId == (long)Context.Guild.Id);
                if (guild == null)
                    return;
                var logChannel = Context.Guild.TextChannels.FirstOrDefault(p => (long?)p.Id == guild.LogchannelId);
                var notificationChannel = Context.Guild.TextChannels.FirstOrDefault(p => (long?)p.Id == guild.NotificationchannelId);
                var botcChannel = Context.Guild.TextChannels.FirstOrDefault(p => (long?)p.Id == guild.Botchannelid);

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
        [Summary("Zeigt den Ping vom Bot an.")]
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
            using (swaightContext db = new swaightContext())
            {

            }

            var result = Context.Guild.GetAuditLogsAsync(100).ToList().Result;
            string list = "";
            foreach (var logs in result)
            {
                foreach (var log in logs)
                {
                    list += $"{log.Action}\n";
                }
            }
        }

        [Command("active", RunMode = RunMode.Async)]
        [RequireOwner]
        public async Task Active(int days, string param = null)
        {
            using (swaightContext db = new swaightContext())
            {
                var activeUsers = db.Userfeatures.Include(p => p.User).Where(p => p.Lastmessage > DateTime.Now.AddDays(0 - days) && p.ServerId == (long)Context.Guild.Id);
                if (string.IsNullOrWhiteSpace(param))
                    await ReplyAsync($"**{activeUsers.Count()} User** haben in den **letzten {days} Tagen** eine Nachricht geschrieben.");
                else
                {
                    string output = $"**{activeUsers.Count()} User** haben in den **letzten {days} Tagen** eine Nachricht geschrieben.\n```";
                    int counter = 1;
                    foreach (var user in activeUsers.OrderByDescending(p => p.Lastmessage.Value))
                    {
                        output += $"{counter}. {user.User.Name} - {user.Lastmessage.Value.ToString("dd.MM.yyyy HH:mm")}\n";
                        counter++;
                    }
                    output += "```";
                    if(output.Length > 2000)
                    {
                        await ReplyAsync(output.Substring(0, 2000));
                        return;
                    }
                    await ReplyAsync(output);
                }
            }
        }

        [Command("love", RunMode = RunMode.Async)]
        [BotCommand]
        [Summary("Berechnet mit einer höchst komplexen Formel wie gut man zu dem markierten User passt.")]
        [Cooldown(30)]
        public async Task Love(SocketUser user)
        {
            if (user == null)
                user = Context.User;
            if (user.IsBot)
                return;

            EmbedBuilder embed = new EmbedBuilder();
            embed.Color = Color.Red;
            embed.Title = "Lovemeter";
            Random rnd = new Random();
            var chance = rnd.Next(0, 101);
            embed.Description = $"{Context.User.Mention} du passt mit {user.Mention} zu **{chance}%** zusammen!";
            await Context.Channel.SendMessageAsync(null, false, embed.Build());
        }

        [Command("gay", RunMode = RunMode.Async)]
        [BotCommand]
        [Summary("Berechnet mit einer höchst komplexen Formel wie schwul du bist, oder der markierte User ist.")]
        [Cooldown(30)]
        public async Task Gay(SocketUser user = null)
        {
            if (user == null)
                user = Context.User;
            if (user.IsBot)
                return;

            EmbedBuilder embed = new EmbedBuilder();
            embed.Color = Color.Purple;
            embed.Title = "Gaymeter";
            Random rnd = new Random();
            var chance = rnd.Next(0, 101);
            embed.Description = $"{user.Mention} ist zu **{chance}%** schwul.";
            await Context.Channel.SendMessageAsync(null, false, embed.Build());
        }

        [Command("hdf", RunMode = RunMode.Async)]
        [Summary("Nur nutzbar im Privatchat mit Rabbot. Mit diesem Command kann man die PNs von Rabbot an und ausschalten.")]
        public async Task Hdf()
        {
            if (!Context.IsPrivate)
                return;
            using (swaightContext db = new swaightContext())
            {
                var user = db.User.FirstOrDefault(p => p.Id == (long)Context.User.Id);
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

        [Command("poll", RunMode = RunMode.Async)]
        [Summary("Gibt dir die Möglichkeit eine Abstimmung zu starten.")]
        [Cooldown(100)]
        public async Task Poll()
        {
            await Context.Channel.SendMessageAsync("Gönn dir. https://www.strawpoll.me/");
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
