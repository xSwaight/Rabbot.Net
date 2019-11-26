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

namespace Rabbot.Commands
{
    public class S4League : ModuleBase<SocketCommandContext>
    {

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
        [Summary("Zeigt Statistiken von S4 Remnants an.")]
        [Command("remStats")]
        public async Task RemStats()
        {
            using (swaightContext db = new swaightContext())
            {
                var dailyPlayer = db.Remnantsplayer.GroupBy(p => p.Date.Value.ToShortDateString());
                var playerPeak = db.Remnantsplayer.OrderByDescending(p => p.Playercount).FirstOrDefault();
                var firstDate = db.Remnantsplayer.OrderBy(p => p.Date).FirstOrDefault();
                var lastDate = db.Remnantsplayer.OrderByDescending(p => p.Date).FirstOrDefault();
                var list = new List<double>();
                foreach (var daily in dailyPlayer)
                {
                    list.Add(daily.Average(p => p.Playercount));
                }

                var yesterday = dailyPlayer.FirstOrDefault(p => p.Key ==  DateTime.Now.AddDays(-1).ToShortDateString());
                var today = dailyPlayer.FirstOrDefault(p => p.Key ==  DateTime.Now.ToShortDateString());
                var lastWeek = dailyPlayer.FirstOrDefault(p => p.Key == DateTime.Now.AddDays(-8).ToShortDateString());

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

                var embed = new EmbedBuilder();
                embed.WithTitle($"S4 Remnants Spieler Statistiken (Daten seit dem {firstDate.Date.Value.ToString("dd.MM.yyyy")})");
                embed.AddField($"Spieler Online", $"**{lastDate.Playercount} Spieler** ({lastDate.Date.Value.ToString("dd.MM.yyyy HH:mm")})");
                embed.AddField($"All Time Spieler Peak", $"**{playerPeak.Playercount} Spieler** ({playerPeak.Date.Value.ToString("dd.MM.yyyy HH:mm")})");
                embed.AddField($"Heutiger Spieler Peak", $"**{today.Max(p => p.Playercount)} Spieler** ({today.First(p => p.Playercount == today.Max(x => x.Playercount)).Date.Value.ToString("dd.MM.yyyy HH:mm")})");
                embed.AddField($"Täglicher Durchschnitt", $"**{Math.Floor(list.Average())}** Spieler");
                embed.AddField($"Vergleich Durchschnitt ({percentOutputAvg}%)", $"{dayYesterday}: **{avgYesteray} Spieler** | Letzte Woche {dayLastWeek}: **{avgLastWeek} Spieler**");
                embed.AddField($"Vergleich Peak ({percentOutputPeak}%)", $"{dayYesterday}: **{peakYesteray} Spieler** | Letzte Woche {dayLastWeek}: **{peakLastWeek} Spieler**");
                embed.Color = Color.DarkGreen;
                await ReplyAsync(null, false, embed.Build());
            }
        }
    }
}
