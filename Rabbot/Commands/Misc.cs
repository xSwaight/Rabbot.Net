using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Org.BouncyCastle.Utilities;
using PagedList;
using Rabbot.Database;
using Rabbot.ImageGenerator;
using Rabbot.Models;
using Rabbot.Preconditions;
using Rabbot.Services;
using Sentry.Protocol;
using Serilog;
using Serilog.Core;
using TagLib.Ogg;

namespace Rabbot.Commands
{
    public class Misc : InteractiveBase
    {
        private readonly CommandService _commandService;
        private readonly StreakService _streakService;
        private readonly ApiService _apiService;
        private readonly ImageService _imageService;
        private DatabaseService Database => DatabaseService.Instance;
        private static readonly ILogger _logger = Log.ForContext(Serilog.Core.Constants.SourceContextPropertyName, nameof(Misc));

        public Misc(IServiceProvider service)
        {
            _streakService = service.GetRequiredService<StreakService>();
            _commandService = service.GetRequiredService<CommandService>();
            _apiService = service.GetRequiredService<ApiService>();
            _imageService = service.GetRequiredService<ImageService>();
        }

        [Command("help", RunMode = RunMode.Async), Alias("hilfe")]
        [BotCommand]
        [Cooldown(10)]
        [Summary("Zeigt diese Liste an.")]
        public async Task Help()
        {
            int pagesize = 15;

            List<CommandInfo> commands = _commandService.Commands.Where(p => p.Summary != null && p.Module.Name != "Administration" && !p.Module.IsSubmodule).ToList();
            List<string> pages = new List<string>();

            for (int i = 1; i <= Math.Ceiling((commands.Count() / (double)pagesize)); i++)
            {
                string help = $"**Alle Commands:**\n\n`(Parameter) -> Optionaler Parameter`\n`[Parameter] -> Pflicht Parameter`\n\n";

                foreach (var command in commands.OrderBy(p => p.Name).Skip((pagesize * (i - 1))).Take(pagesize))
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
                pages.Add(help);
            }

            var paginatedMessage = new PaginatedMessage()
            {
                Pages = pages.ToArray(),
                Options = Globals.PaginatorOptions,
                Color = new Color(255, 242, 212)
            };
            await PagedReplyAsync(paginatedMessage);
        }

        [Command("reaction")]
        [RequireOwner]
        public async Task Test_ReactionReply()
        {
            IUserMessage message = null;
            message = await InlineReactionReplyAsync(new ReactionCallbackData("Upvote for EXP!\nLog:", null, true, true)
                .WithCallback(new Emoji("👍"), c => AddEXP(c, message))
                .WithCallback(new Emoji("👎"), c => c.Channel.SendMessageAsync("You've replied with 👎"))
                );
        }

        private async Task AddEXP(SocketCommandContext context, IUserMessage message)
        {
            using (var db = Database.Open())
            {
                var user = db.Features.FirstOrDefault(p => p.UserId == context.User.Id && p.GuildId == context.Guild.Id);
                user.Exp += 100;
                await db.SaveChangesAsync();
                var content = message.Content;
                await message.ModifyAsync(p => p.Content = (content += "\nSuccessfully added 100 EXP!"));
            }
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
            using (var db = Database.Open())
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
            using (var db = Database.Open())
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
            using (var db = Database.Open())
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
            using (var db = Database.Open())
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
            System.IO.File.Delete(path);
        }

        [Command("corona", RunMode = RunMode.Async)]
        [BotCommand]
        [Summary("Zeigt Statistiken über den Corona Virus an. Ohne Parameter: Top 10 Länder nach Fallzahl. Mit Parameter: Statistiken zum Land")]
        [Cooldown(10)]
        public async Task Corona([Remainder] string country = null)
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
                    DateTimeOffset lastUpdate = DateTimeOffset.FromUnixTimeMilliseconds(countryStats.Updated);
                    builder.Append($"**{counter}. {countryStats.Country}** ({lastUpdate.DateTime.ToFormattedString()})\nFälle: **{countryStats.Cases.Value.ToFormattedString()}** | Tests: **{countryStats.Tests.Value.ToFormattedString()}** | Heutige Fälle: **{countryStats.TodayCases.Value.ToFormattedString()}** | Heutige Tode: **{countryStats.TodayDeaths.Value.ToFormattedString()}**\n\n");
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
                if (countryStats == null)
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
                DateTimeOffset lastUpdate = DateTimeOffset.FromUnixTimeMilliseconds(countryStats.Updated);
                embed.WithAuthor(author);
                embed.WithDescription($"Corona Statistiken von {countryStats.Country}:");
                embed.AddField("Fälle gesamt", $"**{countryStats.Cases.Value.ToFormattedString()}**", true);
                embed.AddField("Tode gesamt", $"**{countryStats.Deaths.Value.ToFormattedString()}**", true);
                embed.AddField("Geheilte gesamt", $"**{countryStats.Recovered.Value.ToFormattedString()}**", true);
                embed.AddField("Tests gesamt", $"**{countryStats.Tests.Value.ToFormattedString()}**", true);
                embed.AddField("Aktive Fälle", $"**{countryStats.Active.Value.ToFormattedString()}**", true);
                embed.AddField("Kritische Fälle", $"**{countryStats.Critical.Value.ToFormattedString()}**", true);
                embed.AddField("Fälle heute", $"**{countryStats.TodayCases.Value.ToFormattedString()}**", true);
                embed.AddField("Tode heute", $"**{countryStats.TodayDeaths.Value.ToFormattedString()}**", true);
                embed.AddField("Fälle pro 1 mio. Einwohner", $"**{(countryStats.CasesPerOneMillion.HasValue ? countryStats.CasesPerOneMillion.Value.ToString() : "0") }**", true);
                embed.AddField("Tode pro 1 mio. Einwohner", $"**{(countryStats.DeathsPerOneMillion.HasValue ? countryStats.DeathsPerOneMillion.Value.ToString() : "0") }**", true);
                embed.AddField("Tests pro 1 mio. Einwohner", $"**{(countryStats.TestsPerOneMillion.HasValue ? countryStats.TestsPerOneMillion.Value.ToString() : "0") }**", true);
                embed.WithFooter($"Daten vom {lastUpdate.DateTime.ToFormattedString()} Uhr");
                embed.Color = new Color(4, 255, 0);
                await Context.Channel.SendMessageAsync("", false, embed.Build());
            }
        }

        [Command("timerank", RunMode = RunMode.Async)]
        [BotCommand]
        [Summary("Zeigt eine Rangliste aller User nach Zeit an.")]
        public async Task Ranking()
        {

            List<string> pages = new List<string>();
            var userRanks = Context.Guild.Users.OrderBy(p => p.JoinedAt.Value.DateTime);
            double pageSize = 10;
            var pageCount = (int)Math.Ceiling((userRanks.Count() / pageSize));

            for (int i = 1; i <= pageCount; i++)
            {
                var users = userRanks.ToPagedList(i, 10);
                string pageContent = "";
                pageContent += $"**Time Ranking**\n\n\n";
                int count = users.PageSize * users.PageNumber - (users.PageSize - 1);
                foreach (var user in users)
                {
                    try
                    {
                        var timespan = DateTime.Now - user.JoinedAt.Value.DateTime;
                        pageContent += $"{count}. **{user.Nickname ?? user.Username}** \nSeit: **{Math.Floor(timespan.TotalDays)} Tagen** ({user.JoinedAt.Value.DateTime.ToFormattedString()})\n\n";
                        count++;

                    }
                    catch (Exception e)
                    {
                        _logger.Error(e, $"Error while adding fields to embed");
                    }
                }
                pages.Add(pageContent);
            }

            var paginatedMessage = new PaginatedMessage()
            {
                Pages = pages.ToArray(),
                Options = Globals.PaginatorOptions,
                Color = new Color(239, 220, 7)
            };
            await PagedReplyAsync(paginatedMessage);
        }

        [Command("sensitivity", RunMode = RunMode.Async), Alias("sens")]
        [BotCommand]
        [Summary("Berechnet die Maus Sensitivity für S4 Xero.")]
        public async Task CalculateXeroSensitivity(string sensitivity)
        {
            if (!double.TryParse(sensitivity.Replace('.', ','), out double result))
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
        public async Task Say([Remainder] string message)
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

        [Command("pet", RunMode = RunMode.Async)]
        [Cooldown(20)]
        public async Task Pet(SocketGuildUser user = null)
        {
            user ??= Context.User as SocketGuildUser;

            using (var image = await _imageService.DrawPetGif(user.GetAvatarUrl(Discord.ImageFormat.Png, 1024) ?? user.GetDefaultAvatarUrl()))
            {
                await Context.Channel.SendFileAsync(image, $"{user.Nickname ?? user.Username}_pet.gif");
            }
        }

        [Command("petemote", RunMode = RunMode.Async)]
        [Cooldown(20)]
        public async Task PetEmote(string emotetext)
        {
            if (!Emote.TryParse(emotetext, out Emote emote))
                return;

            using (var image = await _imageService.DrawPetGif(emote.Url, true))
            {
                await Context.Channel.SendFileAsync(image, $"{emote.Name}_pet.gif");
            }
        }

        [Command("howlong", RunMode = RunMode.Async)]
        [Cooldown(20)]
        public async Task HowLong()
        {
            var releaseDate = new DateTime(2020, 7, 3, 20, 0, 0, 0);
            if (DateTime.Now > releaseDate)
                return;

            var timeSpan = releaseDate - DateTime.Now;

            await ReplyAsync($"**Xero will be released in: {timeSpan.Days} {(timeSpan.Days == 1 ? "day" : "days")} {timeSpan.Hours}h {timeSpan.Minutes}m**");
        }

        [Command("streaks", RunMode = RunMode.Async)]
        [BotCommand]
        [Cooldown(15)]
        [Summary("Zeigt eine Rangliste aller User nach Streaklevel an.")]
        public async Task StreakRanking(int page = 1)
        {
            if (page < 1)
                return;
            using (var db = Database.Open())
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
            using (var db = Database.Open())
            {

                var ranking = db.Features.AsQueryable().Where(p => p.GuildId == Context.Guild.Id && p.HasLeft == false && p.Eggs > 0).OrderByDescending(p => p.Eggs).ToPagedList(page, 10);
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
