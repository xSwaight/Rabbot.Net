using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Rabbot.API;
using Rabbot.Database;
using Rabbot.ImageGenerator;
using Rabbot.API.Models;
using Discord.WebSocket;
using Rabbot.Preconditions;
using Serilog;

namespace Rabbot.Commands
{
    public class S4League : ModuleBase<SocketCommandContext>
    {
        private readonly ILogger _logger;

        public S4League()
        {
            _logger = Log.ForContext<S4League>();
        }

        [Command("player", RunMode = RunMode.Async)]
        [Cooldown(10)]
        [Summary("Zeigt Statistiken vom eingegebenen S4 Spieler an.")]
        public async Task Player([Remainder]string playername)
        {

            if (!String.IsNullOrWhiteSpace(playername))
            {
                Player player = new Player();
                player = await ApiRequest.GetPlayer(playername);
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
                    embedInfo.AddField("EXP", player.Exp.ToString("N0", new System.Globalization.CultureInfo("de-DE")), true);
                    TimeSpan time = DateTime.Now.AddSeconds(player.Playtime) - DateTime.Now;
                    string playtime = time.Days + "D " + time.Hours + "H " + time.Minutes + "M ";
                    embedInfo.AddField("Playtime", playtime, true);
                    embedInfo.AddField("TD Rate", player.Tdrate.ToString(), true);
                    embedInfo.AddField("KD Rate", player.Kdrate.ToString(), true);
                    embedInfo.AddField("Matches played", player.Matches_played.ToString("N0", new System.Globalization.CultureInfo("de-DE")), true);
                    embedInfo.AddField("Matches won", player.Matches_won.ToString("N0", new System.Globalization.CultureInfo("de-DE")), true);
                    embedInfo.AddField("Matches lost", player.Matches_lost.ToString("N0", new System.Globalization.CultureInfo("de-DE")), true);
                    embedInfo.AddField("Last online", Convert.ToDateTime(player.Last_online).ToShortDateString(), true);
                    embedInfo.AddField("Views", player.Views.ToString("N0", new System.Globalization.CultureInfo("de-DE")), true);
                    embedInfo.AddField("Favorites", player.Favorites.ToString("N0", new System.Globalization.CultureInfo("de-DE")), true);
                    embedInfo.AddField("Fame", player.Fame.ToString() + "%", true);
                    embedInfo.AddField("Hate", player.Hate.ToString() + "%", true);
                    embedInfo.ThumbnailUrl = "https://s4db.net/assets/img/icon192.png";
                    await Context.Channel.SendMessageAsync("", false, embedInfo.Build());
                }
            }
        }

        [Command("clan", RunMode = RunMode.Async)]
        [Cooldown(10)]
        [Summary("Zeigt Statistiken zum eingegebenen S4 Clan an.")]
        public async Task Clan([Remainder]string clanname)
        {

            if (!String.IsNullOrWhiteSpace(clanname))
            {
                Clan clan = new Clan();
                clan = await ApiRequest.GetClan(clanname);
                if (clan == null)
                {
                    var embed = new EmbedBuilder();
                    embed.WithTitle("Fehler");
                    embed.WithDescription("Clan nicht gefunden ¯\\_(ツ)_/¯");
                    embed.WithColor(new Color(255, 0, 0));
                    await Context.Channel.SendMessageAsync("", false, embed.Build());
                }
                else
                {
                    var embedInfo = new EmbedBuilder();
                    embedInfo.WithColor(new Color(42, 46, 53));
                    embedInfo.AddField("Name", clan.Name, true);
                    embedInfo.AddField("Master", clan.Master, true);
                    embedInfo.AddField("Member Count", clan.Member_count.ToString(), true);
                    if (!String.IsNullOrWhiteSpace(clan.Announcement))
                        embedInfo.AddField("Announcement", clan.Announcement, true);
                    if (!String.IsNullOrWhiteSpace(clan.Description))
                        embedInfo.AddField("Description", clan.Description, true);
                    embedInfo.AddField("Views", clan.Views.ToString("N0", new System.Globalization.CultureInfo("de-DE")), true);
                    embedInfo.AddField("Favorites", clan.Favorites.ToString(), true);
                    embedInfo.AddField("Fame", clan.Fame.ToString() + "%", true);
                    embedInfo.AddField("Hate", clan.Hate.ToString() + "%", true);
                    embedInfo.ThumbnailUrl = "https://s4db.net/assets/img/icon192.png";
                    await Context.Channel.SendMessageAsync("", false, embedInfo.Build());
                }
            }
        }

        [Command("playercard", RunMode = RunMode.Async)]
        [Cooldown(10)]
        [Summary("Gibt eine Grafik mit S4 Spielerdaten aus.")]
        public async Task Playercard([Remainder]string playername)
        {

            Player player = new Player();
            player = await ApiRequest.GetPlayer(playername);
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
                    EXP = player.Exp.ToString("N0", new System.Globalization.CultureInfo("de-DE")),
                    TOUCHDOWN = player.Tdrate.ToString(),
                    MATCHES = player.Matches_played.ToString("N0", new System.Globalization.CultureInfo("de-DE")),
                    DEATHMATCH = player.Kdrate.ToString()
                });

                var path = HtmlToImage.Generate(Helper.RemoveSpecialCharacters(playername), html, 300, 170);
                await Context.Channel.SendFileAsync(path);
                File.Delete(path);
            }
        }

        [BotCommand]
        [Command("s4")]
        [Summary("Gibt dir die S4 League Rolle.")]
        public async Task S4()
        {
            var s4Role = Context.Guild.Roles.Where(p => p.Name == "S4 League");
            if (!s4Role.Any())
                return;

            var user = Context.Guild.Users.FirstOrDefault(p => p.Id == Context.User.Id);
            if (user.Roles.Where(p => p.Name == "S4 League").Any())
                return;

            await user.AddRoleAsync(s4Role.FirstOrDefault());
            await Logging.S4Role(Context);
            await ReplyAsync($"{Context.User.Mention} hat sich erfolgreich die **S4 League** Rolle gegeben.");
        }

        [BotCommand]
        [Command("psbat")]
        [Summary("Gibt dir die PS & Bat Rolle.")]
        public async Task Psbat()
        {
            var psbatRole = Context.Guild.Roles.Where(p => p.Name == "PS & Bat");
            if (!psbatRole.Any())
                return;

            var user = Context.Guild.Users.FirstOrDefault(p => p.Id == Context.User.Id);
            if (user.Roles.Where(p => p.Name == "PS & Bat").Any())
                return;

            await user.AddRoleAsync(psbatRole.FirstOrDefault());
            await Logging.PsbatRole(Context);
            await ReplyAsync($"{Context.User.Mention} hat sich erfolgreich die **PS & Bat** Rolle gegeben.");
        }

        [BotCommand]
        [Command("remPlayer")]
        public async Task Scrape(string name)
        {
            var player = RemScraper.Scrape(name);
            if (player == null)
            {
                await ReplyAsync("Den Spieler gibts nicht, du **Kek**");
                return;
            }
            var embed = new EmbedBuilder();
            embed.WithTitle($"{player.Name} - Clan: {player.Clan}");
            embed.WithDescription($"Level: {player.Level}\nMatches: {player.Matches} | Won: {player.Won} - Lost: {player.Lost}");
            embed.Color = Color.DarkGreen;
            await ReplyAsync(null, false, embed.Build());
        }

        [BotCommand]
        [Summary("Zeigt Statistiken von S4 Remnants an oder S4 League Official an. Nutze als Parameter entweder 'official' oder 'remnants'")]
        [Alias("remStats")]
        [Command("s4stats")]
        public async Task S4Stats(string param = "remnants")
        {
            using (swaightContext db = new swaightContext())
            {
                using (Context.Channel.EnterTypingState())
                {
                    if (param == "remnants" || param == "rem" || param == "both")
                    {
                        // Remnants 
                        var dailyPlayer = db.Remnantsplayer.Where(p => p.Date > DateTime.Now.AddDays(-10)).GroupBy(p => p.Date.Value.ToShortDateString());

                        var playerPeak = db.Remnantsplayer.OrderByDescending(p => p.Playercount).FirstOrDefault();
                        var firstDate = db.Remnantsplayer.OrderBy(p => p.Date).FirstOrDefault();
                        var lastDate = db.Remnantsplayer.OrderByDescending(p => p.Date).FirstOrDefault();

                        var yesterday = dailyPlayer.FirstOrDefault(p => p.Key == DateTime.Now.AddDays(-1).ToShortDateString());
                        var today = dailyPlayer.FirstOrDefault(p => p.Key == DateTime.Now.ToShortDateString());
                        var lastWeek = dailyPlayer.FirstOrDefault(p => p.Key == DateTime.Now.AddDays(-8).ToShortDateString());

                        if (yesterday == null || today == null || lastWeek == null)
                        {
                            await ReplyAsync("Shit, hier lief was schief.");
                            return;
                        }

                        var culture = new System.Globalization.CultureInfo("de-DE");
                        var dayYesterday = culture.DateTimeFormat.GetDayName(DateTime.Now.AddDays(-1).DayOfWeek);
                        var dayLastWeek = culture.DateTimeFormat.GetDayName(DateTime.Now.AddDays(-8).DayOfWeek);

                        var avgYesteray = Math.Floor(yesterday.Average(p => p.Playercount));
                        var avgLastWeek = Math.Floor(lastWeek.Average(p => p.Playercount));

                        var peakYesteray = (double)yesterday.Max(p => p.Playercount);
                        var peakLastWeek = (double)lastWeek.Max(p => p.Playercount);

                        var percentAvg = Math.Floor(100 / avgLastWeek * avgYesteray) - 100;
                        var percentPeak = Math.Floor(100 / peakLastWeek * peakYesteray) - 100;

                        string percentOutputAvg = percentAvg.ToString();
                        string percentOutputPeak = percentPeak.ToString();
                        if (percentAvg > 0)
                            percentOutputAvg = "+" + percentAvg;
                        if (percentPeak > 0)
                            percentOutputPeak = "+" + percentPeak;

                        var embedRemnants = new EmbedBuilder();
                        embedRemnants.WithTitle($"S4 Remnants Spieler Statistiken\nDaten seit dem {firstDate.Date.Value.ToString("dd.MM.yyyy")}");
                        embedRemnants.AddField($"Spieler Online", $"**{lastDate.Playercount.ToString("N0", new System.Globalization.CultureInfo("de-DE"))} Spieler** ({lastDate.Date.Value.ToString("dd.MM.yyyy HH:mm")})");
                        embedRemnants.AddField($"All Time Spieler Peak", $"**{playerPeak.Playercount.ToString("N0", new System.Globalization.CultureInfo("de-DE"))} Spieler** ({playerPeak.Date.Value.ToString("dd.MM.yyyy HH:mm")})");
                        embedRemnants.AddField($"Heutiger Spieler Peak", $"**{today.Max(p => p.Playercount).ToString("N0", new System.Globalization.CultureInfo("de-DE"))} Spieler** ({today.First(p => p.Playercount == today.Max(x => x.Playercount)).Date.Value.ToString("dd.MM.yyyy HH:mm")})");
                        embedRemnants.AddField($"Vergleich Durchschnitt ({percentOutputAvg}%)", $"{dayYesterday}: **{avgYesteray} Spieler** | Letzte Woche {dayLastWeek}: **{avgLastWeek} Spieler**");
                        embedRemnants.AddField($"Vergleich Peak ({percentOutputPeak}%)", $"{dayYesterday}: **{peakYesteray} Spieler** | Letzte Woche {dayLastWeek}: **{peakLastWeek} Spieler**");
                        embedRemnants.Color = Color.DarkGreen;
                        await ReplyAsync(null, false, embedRemnants.Build());
                    }
                    if (param == "official" || param == "offi" || param == "both")
                    {
                        //Official 
                        var embedOfficial = new EmbedBuilder();

                        var officialDailyPlayer = db.Officialplayer.Where(p => p.Date > DateTime.Now.AddDays(-10)).GroupBy(p => p.Date.Value.ToShortDateString());
                        var officialPlayerPeak = db.Officialplayer.OrderByDescending(p => p.Playercount).FirstOrDefault();
                        var officialFirstDate = db.Officialplayer.OrderBy(p => p.Date).FirstOrDefault();
                        var officialLastDate = db.Officialplayer.OrderByDescending(p => p.Date).FirstOrDefault();

                        var officialToday = officialDailyPlayer.FirstOrDefault(p => p.Key == DateTime.Now.ToShortDateString());
                        IGrouping<string, Officialplayer> officialYesterday = null;
                        var officialYesterdayList = officialDailyPlayer.Where(p => p.Key == DateTime.Now.AddDays(-1).ToShortDateString());
                        if (officialYesterdayList.Any())
                            officialYesterday = officialYesterdayList.FirstOrDefault();

                        IGrouping<string, Officialplayer> officialLastWeek = null;
                        var officialLastWeekList = officialDailyPlayer.Where(p => p.Key == DateTime.Now.AddDays(-8).ToShortDateString());
                        if (officialLastWeekList.Any())
                            officialLastWeek = officialLastWeekList.FirstOrDefault();

                        var culture = new System.Globalization.CultureInfo("de-DE");
                        var dayYesterday = culture.DateTimeFormat.GetDayName(DateTime.Now.AddDays(-1).DayOfWeek);
                        var dayLastWeek = culture.DateTimeFormat.GetDayName(DateTime.Now.AddDays(-8).DayOfWeek);

                        embedOfficial.WithTitle($"S4 League (Official) Spieler Statistiken\nDaten seit dem {officialFirstDate.Date.Value.ToString("dd.MM.yyyy")}");
                        embedOfficial.AddField($"Spieler Online", $"**{officialLastDate.Playercount.ToString("N0", new System.Globalization.CultureInfo("de-DE"))} Spieler** ({officialLastDate.Date.Value.ToString("dd.MM.yyyy HH:mm")})");
                        embedOfficial.AddField($"All Time Spieler Peak", $"**{officialPlayerPeak.Playercount.ToString("N0", new System.Globalization.CultureInfo("de-DE"))} Spieler** ({officialPlayerPeak.Date.Value.ToString("dd.MM.yyyy HH:mm")})");
                        embedOfficial.AddField($"Heutiger Spieler Peak", $"**{officialToday.Max(p => p.Playercount).ToString("N0", new System.Globalization.CultureInfo("de-DE"))} Spieler** ({officialToday.First(p => p.Playercount == officialToday.Max(x => x.Playercount)).Date.Value.ToString("dd.MM.yyyy HH:mm")})");
                        if (officialYesterday != null && officialLastWeek != null)
                        {
                            var officialAvgYesteray = Math.Floor(officialYesterday.Average(p => p.Playercount));
                            var officialAvgLastWeek = Math.Floor(officialLastWeek.Average(p => p.Playercount));

                            var officialPeakYesteray = (double)officialYesterday.Max(p => p.Playercount);
                            var officialPeakLastWeek = (double)officialLastWeek.Max(p => p.Playercount);

                            var officialPercentAvg = Math.Floor(100 / officialAvgLastWeek * officialAvgYesteray) - 100;
                            var officialPercentPeak = Math.Floor(100 / officialPeakLastWeek * officialPeakYesteray) - 100;

                            string officialPercentOutputAvg = officialPercentAvg.ToString();
                            string officialPercentOutputPeak = officialPercentPeak.ToString();
                            if (officialPercentAvg > 0)
                                officialPercentOutputAvg = "+" + officialPercentAvg;
                            if (officialPercentPeak > 0)
                                officialPercentOutputPeak = "+" + officialPercentPeak;

                            embedOfficial.AddField($"Vergleich Durchschnitt ({officialPercentOutputAvg}%)", $"{dayYesterday}: **{officialAvgYesteray} Spieler** | Letzte Woche {dayLastWeek}: **{officialAvgLastWeek} Spieler**");
                            embedOfficial.AddField($"Vergleich Peak ({officialPercentOutputPeak}%)", $"{dayYesterday}: **{officialPeakYesteray} Spieler** | Letzte Woche {dayLastWeek}: **{officialPeakLastWeek} Spieler**");
                        }
                        embedOfficial.Color = Color.DarkRed;
                        await ReplyAsync(null, false, embedOfficial.Build());
                        return;
                    }
                    else
                    {
                        await ReplyAsync("Invalid param. Use `official` or `remnants`.");
                    }
                }
            }
        }
    }
}
