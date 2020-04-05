using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Rabbot.Database;
using Rabbot.ImageGenerator;
using Discord.WebSocket;
using Rabbot.Preconditions;
using Serilog;
using Serilog.Core;
using Rabbot.Database.Rabbot;

namespace Rabbot.Commands
{
    public class S4League : ModuleBase<SocketCommandContext>
    {
        private static readonly ILogger _logger = Log.ForContext(Serilog.Core.Constants.SourceContextPropertyName, nameof(S4League));

        [BotCommand]
        [Summary("Zeigt Statistiken von S4 Remnants oder S4 League Official an. Nutze als Parameter entweder 'official' oder 'remnants'")]
        [Alias("remStats")]
        [Command("s4stats")]
        public async Task S4Stats(string param = "remnants")
        {
            using (RabbotContext db = new RabbotContext())
            {
                using (Context.Channel.EnterTypingState())
                {
                    if (param == "remnants" || param == "rem" || param == "both")
                    {
                        // Remnants 
                        var dailyPlayer = db.RemnantsPlayers.Where(p => p.Date > DateTime.Now.AddDays(-10)).GroupBy(p => p.Date.ToShortDateString());

                        var playerPeak = db.RemnantsPlayers.OrderByDescending(p => p.Playercount).FirstOrDefault();
                        var firstDate = db.RemnantsPlayers.OrderBy(p => p.Date).FirstOrDefault();
                        var lastDate = db.RemnantsPlayers.OrderByDescending(p => p.Date).FirstOrDefault();

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
                        embedRemnants.WithTitle($"S4 Remnants Spieler Statistiken\nDaten seit dem {firstDate.Date.ToString("dd.MM.yyyy")}");
                        embedRemnants.AddField($"Spieler Online", $"**{lastDate.Playercount.ToFormattedString()} Spieler** ({lastDate.Date.ToFormattedString()})");
                        embedRemnants.AddField($"All Time Spieler Peak", $"**{playerPeak.Playercount.ToFormattedString()} Spieler** ({playerPeak.Date.ToFormattedString()})");
                        embedRemnants.AddField($"Heutiger Spieler Peak", $"**{today.Max(p => p.Playercount).ToFormattedString()} Spieler** ({today.First(p => p.Playercount == today.Max(x => x.Playercount)).Date.ToFormattedString()})");
                        embedRemnants.AddField($"Vergleich Durchschnitt ({percentOutputAvg}%)", $"{dayYesterday}: **{avgYesteray} Spieler** | Letzte Woche {dayLastWeek}: **{avgLastWeek} Spieler**");
                        embedRemnants.AddField($"Vergleich Peak ({percentOutputPeak}%)", $"{dayYesterday}: **{peakYesteray} Spieler** | Letzte Woche {dayLastWeek}: **{peakLastWeek} Spieler**");
                        embedRemnants.Color = Color.DarkGreen;
                        await ReplyAsync(null, false, embedRemnants.Build());
                    }
                    if (param == "official" || param == "offi" || param == "both")
                    {
                        //Official 
                        var embedOfficial = new EmbedBuilder();

                        var officialDailyPlayer = db.OfficialPlayers.Where(p => p.Date > DateTime.Now.AddDays(-10)).GroupBy(p => p.Date.ToShortDateString());
                        var officialPlayerPeak = db.OfficialPlayers.OrderByDescending(p => p.Playercount).FirstOrDefault();
                        var officialFirstDate = db.OfficialPlayers.OrderBy(p => p.Date).FirstOrDefault();
                        var officialLastDate = db.OfficialPlayers.OrderByDescending(p => p.Date).FirstOrDefault();

                        var officialToday = officialDailyPlayer.FirstOrDefault(p => p.Key == DateTime.Now.ToShortDateString());
                        IGrouping<string, OfficialPlayerEntity> officialYesterday = null;
                        var officialYesterdayList = officialDailyPlayer.Where(p => p.Key == DateTime.Now.AddDays(-1).ToShortDateString());
                        if (officialYesterdayList.Any())
                            officialYesterday = officialYesterdayList.FirstOrDefault();

                        IGrouping<string, OfficialPlayerEntity> officialLastWeek = null;
                        var officialLastWeekList = officialDailyPlayer.Where(p => p.Key == DateTime.Now.AddDays(-8).ToShortDateString());
                        if (officialLastWeekList.Any())
                            officialLastWeek = officialLastWeekList.FirstOrDefault();

                        var culture = new System.Globalization.CultureInfo("de-DE");
                        var dayYesterday = culture.DateTimeFormat.GetDayName(DateTime.Now.AddDays(-1).DayOfWeek);
                        var dayLastWeek = culture.DateTimeFormat.GetDayName(DateTime.Now.AddDays(-8).DayOfWeek);

                        embedOfficial.WithTitle($"S4 League (Official) Spieler Statistiken\nDaten seit dem {officialFirstDate.Date.ToString("dd.MM.yyyy")}");
                        embedOfficial.AddField($"Spieler Online", $"**{officialLastDate.Playercount.ToFormattedString()} Spieler** ({officialLastDate.Date.ToFormattedString()})");
                        embedOfficial.AddField($"All Time Spieler Peak", $"**{officialPlayerPeak.Playercount.ToFormattedString()} Spieler** ({officialPlayerPeak.Date.ToFormattedString()})");
                        embedOfficial.AddField($"Heutiger Spieler Peak", $"**{officialToday.Max(p => p.Playercount).ToFormattedString()} Spieler** ({officialToday.First(p => p.Playercount == officialToday.Max(x => x.Playercount)).Date.ToFormattedString()})");
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
                }
            }
        }
    }
}
