using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PagedList;
using Rabbot.Database;
using Rabbot.ImageGenerator;
using Rabbot.Models;
using Rabbot.Preconditions;
using Rabbot.Services;
using Serilog;
using Serilog.Core;

namespace Rabbot.Commands
{
    public class Misc : ModuleBase<SocketCommandContext>
    {
        private readonly CommandService _commandService;
        private readonly StreakService _streakService;
        private readonly ApiService _apiService;
        private readonly ImageService _imageService;
        private readonly DatabaseService _databaseService;
        private static readonly ILogger _logger = Log.ForContext(Serilog.Core.Constants.SourceContextPropertyName, nameof(Misc));

        public Misc(IServiceProvider service)
        {
            _streakService = service.GetRequiredService<StreakService>();
            _commandService = service.GetRequiredService<CommandService>();
            _apiService = service.GetRequiredService<ApiService>();
            _imageService = service.GetRequiredService<ImageService>();
            _databaseService = service.GetRequiredService<DatabaseService>();
        }

        [Command("help", RunMode = RunMode.Async)]
        [BotCommand]
        [Cooldown(10)]
        [Summary("Zeigt diese Liste an.")]
        public async Task Help(int seite = 1)
        {
            int pagesize = 15;

            List<CommandInfo> commands = _commandService.Commands.Where(p => p.Summary != null && p.Module.Name != "Administration" && !p.Module.IsSubmodule).ToList();

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
                        aliases += $"{Config.Bot.CmdPrefix}{alias} ";
                }
                aliases = aliases.TrimEnd();
                if (!string.IsNullOrWhiteSpace(aliases))
                    help += $"**{Config.Bot.CmdPrefix}{command.Name} {param}**\n*Alternativen: {aliases}*\n`{command.Summary}`\n";
                else
                    help += $"**{Config.Bot.CmdPrefix}{command.Name} {param}**\n`{command.Summary}`\n";

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
            embed.WithTitle($"About");
            embed.WithColor(new Color(241, 242, 222));
            embed.AddField("Total Users", memberCount.ToFormattedString(), true);
            embed.AddField("Online Users", (memberCount - offlineCount).ToFormattedString(), true);
            embed.AddField("Total Servers", Context.Client.Guilds.Count.ToFormattedString(), true);
            embed.ThumbnailUrl = "https://media.discordapp.net/attachments/210496271000141825/689416678496665671/Unbenanntes_Projekt.png?width=788&height=788";

            // Arize ID: 157616694083190784
            var designCreditUser = Context.Guild.Users.FirstOrDefault(p => p.Id == 157616694083190784);
            // Swaight ID: 128914972829941761
            var creatorCreditUser = Context.Guild.Users.FirstOrDefault(p => p.Id == 128914972829941761);
            // Cranberry ID: 206559109263130624
            var designCreditUser2 = Context.Guild.Users.FirstOrDefault(p => p.Id == 206559109263130624);

            embed.AddField("Bot creator", creatorCreditUser?.Mention ?? "Swaight", true);
            embed.AddField("Bot created at", Context.Client.CurrentUser.CreatedAt.DateTime.ToFormattedString(), true);
            embed.AddField("Designs by", $"{designCreditUser?.Mention ?? "Arize"} & {designCreditUser2?.Mention ?? "Cranberry"}", false);
            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Command("settings", RunMode = RunMode.Async)]
        [Summary("Gibt die aktuellen Einstellungen aus.")]
        [BotCommand]
        [Cooldown(30)]
        public async Task Settings()
        {
            using (var db = _databaseService.Open<RabbotContext>())
            {

                var guild = db.Guilds.FirstOrDefault(p => p.GuildId == Context.Guild.Id);
                if (guild == null)
                    return;
                var logChannel = Context.Guild.TextChannels.FirstOrDefault(p => p.Id == guild.LogChannelId);
                var notificationChannel = Context.Guild.TextChannels.FirstOrDefault(p => p.Id == guild.NotificationChannelId);
                var botcChannel = Context.Guild.TextChannels.FirstOrDefault(p => p.Id == guild.BotChannelId);

                var embed = new EmbedBuilder();
                embed.WithTitle($"Settings");
                embed.WithColor(new Color(241, 242, 222));

                switch (guild.Notify)
                {
                    case false:
                        embed.AddField("Notification", "Disabled", true);
                        break;
                    case true:
                        embed.AddField("Notification", "Enabled", true);
                        break;
                    default:
                        embed.AddField("Notification", "Unknown", true);
                        break;
                }

                if (notificationChannel != null)
                    embed.AddField("Notification Channel", notificationChannel.Mention, true);
                else
                    embed.AddField("Notification Channel", "Nicht gesetzt.", true);

                if (logChannel != null)
                    embed.AddField("Log Channel", logChannel.Mention, true);
                else
                    embed.AddField("Log Channel", "Nicht gesetzt.", true);

                switch (guild.Log)
                {
                    case false:
                        embed.AddField("Log", "Disabled", true);
                        break;
                    case true:
                        embed.AddField("Log", "Enabled", true);
                        break;
                    default:
                        embed.AddField("Log", "Unknown", true);
                        break;
                }

                if (botcChannel != null)
                    embed.AddField("Bot Channel", botcChannel.Mention, true);
                else
                    embed.AddField("Bot Channel", "Nicht gesetzt.", true);

                switch (guild.Level)
                {
                    case false:
                        embed.AddField("Level", "Disabled", true);
                        break;
                    case true:
                        embed.AddField("Level", "Enabled", true);
                        break;
                    default:
                        embed.AddField("Level", "Unknown", true);
                        break;
                }

                embed.ThumbnailUrl = "https://cdn.pixabay.com/photo/2018/03/27/23/58/silhouette-3267855_960_720.png";
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
            //await Context.Message.DeleteAsync();
            //await Context.Guild.ModifyAsync(p => p.RegionId = "frankfurt");
            //Console.WriteLine(Context.Guild.VoiceRegionId);
            //var result = Context.Guild.GetAuditLogsAsync(100).ToList().Result;
            //string list = "";
            //foreach (var logs in result)
            //{
            //    foreach (var log in logs)
            //    {
            //        list += $"{log.Action}\n";
            //    }
            //}
        }

        [Command("active", RunMode = RunMode.Async)]
        [RequireOwner]
        public async Task Active(int days, string param = null)
        {
            using (var db = _databaseService.Open<RabbotContext>())
            {
                var activeUsers = db.Features.Include(p => p.User).Where(p => p.LastMessage > DateTime.Now.AddDays(0 - days) && p.GuildId == Context.Guild.Id);
                if (string.IsNullOrWhiteSpace(param))
                    await ReplyAsync($"**{activeUsers.Count()} User** haben in den **letzten {days} Tagen** eine Nachricht geschrieben.");
                else
                {
                    string output = $"**{activeUsers.Count()} User** haben in den **letzten {days} Tagen** eine Nachricht geschrieben.\n```";
                    int counter = 1;
                    foreach (var user in activeUsers.OrderByDescending(p => p.LastMessage))
                    {
                        output += $"{counter}. {user.User.Name} - {user.LastMessage.Value.ToFormattedString()}\n";
                        counter++;
                    }
                    output += "```";
                    if (output.Length > 2000)
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
            using (var db = _databaseService.Open<RabbotContext>())
            {
                var user = db.Users.FirstOrDefault(p => p.Id == Context.User.Id);
                if (user.Notify == true)
                {
                    user.Notify = false;
                    await Context.Channel.SendMessageAsync("Na gut.");
                }
                else
                {
                    user.Notify = false;
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

        [Command("checkleft", RunMode = RunMode.Async)]
        [RequireOwner]
        public async Task CheckLeftUsers()
        {
            using (var db = _databaseService.Open<RabbotContext>())
            {
                var userfeatures = db.Features;
                foreach (var userfeature in userfeatures)
                {
                    if (Context.Client.Guilds.Where(p => p.Id == userfeature.GuildId).Any())
                    {
                        if (!Context.Client.Guilds.First(p => p.Id == userfeature.GuildId).Users.Where(p => p.Id == userfeature.UserId).Any())
                            userfeature.HasLeft = true;
                        else
                            userfeature.HasLeft = false;
                    }
                }
                await db.SaveChangesAsync();
            }
        }

        [Command("protect", RunMode = RunMode.Async)]
        [Summary("Generiert dir einen Corona geschützten Avatar!")]
        [BotCommand]
        [Cooldown(10)]
        public async Task Protect(IUser user = null)
        {
            if (user == null)
                user = Context.User;
            string path = "";
            using (Context.Channel.EnterTypingState())
            {
                string profilePicture = user.GetAvatarUrl(Discord.ImageFormat.Auto, 1024);
                if (profilePicture == null)
                    profilePicture = user.GetDefaultAvatarUrl();
                var template = new HtmlTemplate(Directory.GetCurrentDirectory() + "/Resources/Templates/corona.html");
                var html = template.Render(new
                {
                    AVATAR = profilePicture
                });

                path = HtmlToImage.Generate(Helper.RemoveSpecialCharacters(user.Username) + "_Corona", html, 616, 616, ImageGenerator.ImageFormat.Png);
                await Context.Channel.SendFileAsync(path);
            }
            File.Delete(path);
        }

        [Command("corona", RunMode = RunMode.Async)]
        [BotCommand]
        [Summary("Zeigt Statistiken über den Corona Virus an. Ohne Parameter: Top 10 Länder nach Fallzahl. Mit Parameter: Statistiken zum Land")]
        [Cooldown(10)]
        public async Task Corona([Remainder]string country = null)
        {
            if (country == null)
            {
                var coronaStats = _apiService.GetCoronaRanking(10);
                if (coronaStats == null)
                {
                    await Context.Channel.SendMessageAsync($"Oops, irgendwas ist schiefgelaufen :/");
                    return;
                }
                EmbedBuilder embed = new EmbedBuilder();
                embed.WithTitle($"Corona Statistiken");
                StringBuilder builder = new StringBuilder();
                int counter = 1;
                foreach (var countryStats in coronaStats)
                {
                    builder.Append($"**{counter}. {countryStats.Country}**\nFälle: **{countryStats.Cases.Value.ToFormattedString()}** | Heutige Fälle: **{countryStats.TodayCases.Value.ToFormattedString()}** | Heutige Tode: **{countryStats.TodayDeaths.Value.ToFormattedString()}**\n\n");
                    counter++;
                }
                embed.Color = new Color(4, 255, 0);
                embed.WithFooter($"Daten vom {DateTime.Now.ToFormattedString()} Uhr");
                embed.WithDescription(builder.ToString());
                await Context.Channel.SendMessageAsync("", false, embed.Build());
                return;
            }
            else
            {
                var countryStats = _apiService.GetCoronaCountry(country);
                if(countryStats == null)
                {
                    await Context.Channel.SendMessageAsync($"**{country}** konnte nicht gefunden werden!");
                    return;
                }
                EmbedBuilder embed = new EmbedBuilder();
                var author = new EmbedAuthorBuilder
                {
                    Name = countryStats.Country,
                    IconUrl = countryStats.CountryInfo.Flag
                };
                embed.WithAuthor(author);
                embed.WithDescription($"Corona Statistiken von {countryStats.Country}:");
                embed.AddField("Fälle gesamt", $"**{countryStats.Cases.Value.ToFormattedString()}**", true);
                embed.AddField("Tode gesamt", $"**{countryStats.Deaths.Value.ToFormattedString()}**", true);
                embed.AddField("Geheilte gesamt", $"**{countryStats.Recovered.Value.ToFormattedString()}**", true);
                embed.AddField("Aktive Fälle", $"**{countryStats.Active.Value.ToFormattedString()}**", true);
                embed.AddField("Kritische Fälle", $"**{countryStats.Critical.Value.ToFormattedString()}**", true);
                embed.AddField("Fälle heute", $"**{countryStats.TodayCases.Value.ToFormattedString()}**", true);
                embed.AddField("Tode heute", $"**{countryStats.TodayDeaths.Value.ToFormattedString()}**");
                embed.AddField("Fälle pro 1 mio. Einwohner", $"**{(countryStats.CasesPerOneMillion.HasValue ? countryStats.CasesPerOneMillion.Value.ToString() : "0") }**", true);
                embed.AddField("Tode pro 1 mio. Einwohner", $"**{(countryStats.DeathsPerOneMillion.HasValue ? countryStats.DeathsPerOneMillion.Value.ToString() : "0") }**", true);
                embed.WithFooter($"Daten vom {DateTime.Now.ToFormattedString()} Uhr");
                embed.Color = new Color(4, 255, 0);
                await Context.Channel.SendMessageAsync("", false, embed.Build());
            }
        }

        [Command("timerank", RunMode = RunMode.Async)]
        [BotCommand]
        [Summary("Zeigt eine Rangliste aller User nach Zeit an.")]
        public async Task Ranking(int page = 1)
        {
            if (page < 1)
                return;
            var users = Context.Guild.Users.OrderBy(p => p.JoinedAt.Value.DateTime).ToPagedList(page, 10);
            if (page > users.PageCount)
                return;
            EmbedBuilder embed = new EmbedBuilder();
            embed.Description = $"Time Ranking Seite {users.PageNumber}/{users.PageCount}";
            embed.WithColor(new Color(239, 220, 7));
            int i = users.PageSize * users.PageNumber - (users.PageSize - 1);
            foreach (var user in users)
            {
                try
                {
                    var timespan = DateTime.Now - user.JoinedAt.Value.DateTime;
                    embed.AddField($"{i}. {user.Nickname ?? user.Username}", $"Seit: **{Math.Floor(timespan.TotalDays)} Tagen** ({user.JoinedAt.Value.DateTime.ToFormattedString()})");
                    i++;

                }
                catch (Exception e)
                {
                    _logger.Error(e, $"Error while adding fields to embed");
                }
            }
            await Context.Channel.SendMessageAsync(null, false, embed.Build());
        }

        [Command("sensitivity", RunMode = RunMode.Async)]
        [BotCommand]
        [Summary("Berechnet die Maus Sensitivity für S4 Xero.")]
        public async Task CalculateXeroSensitivity(string sensitivity)
        {
            if (!double.TryParse(sensitivity.Replace(',', '.'), out double result))
            {
                await ReplyAsync("Invalid Input");
                return;
            }

            double remSensitivity = result * 100;

            double xero = (((0.2 * ((remSensitivity - 4700) / 5000)) + 0.2) * 150 * 100) / 100;
            await ReplyAsync($"Xero Sensitivity: {xero.ToString("N2").Replace(',', '.')}");
        }

        [Command("say", RunMode = RunMode.Async)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task Say([Remainder]string message)
        {
            ISocketMessageChannel channel = Context.Channel;
            if (TryGetChannel(message, Context, out ISocketMessageChannel newChannel, out string messageTag))
            {
                message = message.Replace(messageTag, string.Empty).Replace("  ", " ");
                message = message.Replace("  ", " ");
                channel = newChannel;
            }
            await Context.Message.DeleteAsync();
            await channel.SendMessageAsync(message);
        }

        [Command("streaks", RunMode = RunMode.Async)]
        [BotCommand]
        [Cooldown(15)]
        [Summary("Zeigt eine Rangliste aller User nach Streaklevel an.")]
        public async Task StreakRanking(int page = 1)
        {
            if (page < 1)
                return;
            using (var db = _databaseService.Open<RabbotContext>())
            {
                var streakList = _streakService.GetRanking(db.Features.Include(p => p.User).Where(p => p.GuildId == Context.Guild.Id && p.HasLeft == false)).ToPagedList(page, 25);

                if (page > streakList.PageCount)
                    return;
                var eb = new EmbedBuilder();
                eb.Color = Color.DarkPurple;

                int i = streakList.PageSize * streakList.PageNumber - (streakList.PageSize - 1);
                eb.WithDescription($"Seite {page} von {streakList.PageCount}");
                foreach (var streak in streakList)
                {
                    eb.AddField($"{i}. {streak.User.Name}", $"{Constants.Fire} {streak.StreakLevel} | Heutige Worte: {streak.TodaysWords.ToFormattedString()}");
                    i++;
                }
                eb.WithTitle($"**Streak Ranking**");
                await Context.Channel.SendMessageAsync($"Erreiche **am Tag** mindestens **{Constants.MinimumWordCount.ToFormattedString()}** Wörter, damit das Streak Level **höher** wird.\nWenn das Ziel **an einem** Tag **nicht** erreicht wird, fällt man **zurück** auf **Level 0**.", false, eb.Build());
            }
        }

        private bool TryGetChannel(string message, SocketCommandContext context, out ISocketMessageChannel channel, out string messageTag)
        {
            Regex regex = new Regex(@"<#[0-9!]{18,19}>");
            var match = regex.Match(message);
            var channelId = match.Value.Replace("<", string.Empty).Replace("#", string.Empty).Replace(">", string.Empty);
            if (ulong.TryParse(channelId, out ulong result))
            {
                channel = context.Guild.TextChannels.FirstOrDefault(p => p.Id == result) ?? context.Channel;
                messageTag = match.Value;
                return true;
            }

            channel = null;
            messageTag = null;
            return false;
        }

        [Command("osterrank", RunMode = RunMode.Async)]
        [BotCommand]
        public async Task OsterRank(int page = 1)
        {
            if (DateTime.Now < Constants.StartTime)
                return;

            if (page < 1)
                return;
            using (var db = _databaseService.Open<RabbotContext>())
            {

                var ranking = db.Features.Where(p => p.GuildId == Context.Guild.Id && p.HasLeft == false && p.Eggs > 0).OrderByDescending(p => p.Eggs).ToPagedList(page, 10);
                if (page > ranking.PageCount)
                    return;
                EmbedBuilder embed = new EmbedBuilder();
                embed.Description = $"Oster Ranking Seite {ranking.PageNumber}/{ranking.PageCount}";
                embed.WithColor(new Color(239, 220, 7));
                int i = ranking.PageSize * ranking.PageNumber - (ranking.PageSize - 1);
                foreach (var top in ranking)
                {
                    try
                    {
                        var user = db.Users.FirstOrDefault(p => p.Id == top.UserId);
                        embed.AddField($"{i}. {user.Name}", $"{Constants.EggGoatR} {top.Eggs.ToFormattedString()}");
                        i++;

                    }
                    catch (Exception e)
                    {
                        _logger.Error(e, $"Error in command {nameof(OsterRank)}");
                    }
                }
                await Context.Channel.SendMessageAsync(null, false, embed.Build());
            }
        }

    }
}
