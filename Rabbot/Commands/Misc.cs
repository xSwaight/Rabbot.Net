using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
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
        public async Task Help()
        {
            List<CommandInfo> commands = _commandService.Commands.ToList();
            string help = "";
            foreach (var command in commands.OrderBy(p => p.Name))
            {
                if (command.Summary == null)
                    continue;
                if (command.Module.Name == "Administration")
                    continue;
                string param = "";
                foreach (var parameter in command.Parameters)
                {
                    if (parameter.IsOptional)
                        param += $"({parameter}) ";
                    else
                        param += $"[{parameter}] ";

                }
                help += $"**{Config.bot.cmdPrefix}{command.Name} {param}**\n{command.Summary}\n";
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

            Emote emote = Emote.Parse("<:shtaco:555055295806701578>"); //Normal
            Emote emote2 = Emote.Parse("<a:shtaco:555055295806701578>"); //Animated
            //await Context.Channel.SendMessageAsync($"Hi {emote}");
            using (swaightContext db = new swaightContext())
            {
                var test = db.User.FirstOrDefault(p => p.Id == (long)Context.User.Id);
            }
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

        [Command("poll", RunMode = RunMode.Async)]
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
