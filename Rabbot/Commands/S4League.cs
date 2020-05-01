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
using Rabbot.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Rabbot.Commands
{
    public class S4League : ModuleBase<SocketCommandContext>
    {
        private static readonly ILogger _logger = Log.ForContext(Serilog.Core.Constants.SourceContextPropertyName, nameof(S4League));
        private DatabaseService Database => DatabaseService.Instance;

        [BotCommand]
        [Summary("Zeigt Statistiken von S4 Remnants oder S4 League Official an. Nutze als Parameter entweder 'official' oder 'remnants'")]
        [Alias("remStats")]
        [Command("s4stats")]
        public async Task S4Stats(string param = "remnants")
        {
            using (var db = Database.Open())
            {
                using (Context.Channel.EnterTypingState())
                {
                    if (param == "remnants" || param == "rem" || param == "both")
                    {
                        //Remnants 
                        var embedRemnants = new EmbedBuilder();

                        var remnantsDailyPlayer = db.RemnantsPlayers.ToList().Where(p => p.Date > DateTime.Now.AddDays(-10)).GroupBy(p => p.Date.ToShortDateString());
                        var remnantsPlayerPeak = db.RemnantsPlayers.AsQueryable().OrderByDescending(p => p.Playercount).FirstOrDefault();
                        var remnantsFirstDate = db.RemnantsPlayers.AsQueryable().OrderBy(p => p.Date).FirstOrDefault();
                        var remnantsLastDate = db.RemnantsPlayers.AsQueryable().OrderByDescending(p => p.Date).FirstOrDefault();

                        var remnantsToday = remnantsDailyPlayer.FirstOrDefault(p => p.Key == DateTime.Now.ToShortDateString());
                        IGrouping<string, RemnantsPlayerEntity> remnantsYesterday = null;
                        var remnantsYesterdayList = remnantsDailyPlayer.Where(p => p.Key == DateTime.Now.AddDays(-1).ToShortDateString());
                        if (remnantsYesterdayList.Any())
                            remnantsYesterday = remnantsYesterdayList.FirstOrDefault();

                        IGrouping<string, RemnantsPlayerEntity> remnantsLastWeek = null;
                        var remnantsLastWeekList = remnantsDailyPlayer.Where(p => p.Key == DateTime.Now.AddDays(-8).ToShortDateString());
                        if (remnantsLastWeekList.Any())
                            remnantsLastWeek = remnantsLastWeekList.FirstOrDefault();

                        var culture = new System.Globalization.CultureInfo("de-DE");
                        var dayYesterday = culture.DateTimeFormat.GetDayName(DateTime.Now.AddDays(-1).DayOfWeek);
                        var dayLastWeek = culture.DateTimeFormat.GetDayName(DateTime.Now.AddDays(-8).DayOfWeek);

                        embedRemnants.WithTitle($"S4 Remnants Spieler Statistiken\nDaten seit dem {remnantsFirstDate.Date.ToString("dd.MM.yyyy")}");
                        embedRemnants.AddField($"Spieler Online", $"**{remnantsLastDate.Playercount.ToFormattedString()} Spieler** ({remnantsLastDate.Date.ToFormattedString()})");
                        embedRemnants.AddField($"All Time Spieler Peak", $"**{remnantsPlayerPeak.Playercount.ToFormattedString()} Spieler** ({remnantsPlayerPeak.Date.ToFormattedString()})");
                        embedRemnants.AddField($"Heutiger Spieler Peak", $"**{remnantsToday.Max(p => p.Playercount).ToFormattedString()} Spieler** ({remnantsToday.First(p => p.Playercount == remnantsToday.Max(x => x.Playercount)).Date.ToFormattedString()})");
                        if (remnantsYesterday != null && remnantsLastWeek != null)
                        {
                            var officialAvgYesteray = Math.Floor(remnantsYesterday.Average(p => p.Playercount));
                            var officialAvgLastWeek = Math.Floor(remnantsLastWeek.Average(p => p.Playercount));

                            var officialPeakYesteray = (double)remnantsYesterday.Max(p => p.Playercount);
                            var officialPeakLastWeek = (double)remnantsLastWeek.Max(p => p.Playercount);

                            var officialPercentAvg = Math.Floor(100 / officialAvgLastWeek * officialAvgYesteray) - 100;
                            var officialPercentPeak = Math.Floor(100 / officialPeakLastWeek * officialPeakYesteray) - 100;

                            string officialPercentOutputAvg = officialPercentAvg.ToString();
                            string officialPercentOutputPeak = officialPercentPeak.ToString();
                            if (officialPercentAvg > 0)
                                officialPercentOutputAvg = "+" + officialPercentAvg;
                            if (officialPercentPeak > 0)
                                officialPercentOutputPeak = "+" + officialPercentPeak;

                            embedRemnants.AddField($"Vergleich Durchschnitt ({officialPercentOutputAvg}%)", $"{dayYesterday}: **{officialAvgYesteray} Spieler** | Letzte Woche {dayLastWeek}: **{officialAvgLastWeek} Spieler**");
                            embedRemnants.AddField($"Vergleich Peak ({officialPercentOutputPeak}%)", $"{dayYesterday}: **{officialPeakYesteray} Spieler** | Letzte Woche {dayLastWeek}: **{officialPeakLastWeek} Spieler**");
                        }
                        embedRemnants.Color = Color.DarkGreen;
                        await ReplyAsync(null, false, embedRemnants.Build());
                    }
                    if (param == "official" || param == "offi" || param == "both")
                    {
                        //Official 
                        var embedOfficial = new EmbedBuilder();

                        var officialDailyPlayer = db.OfficialPlayers.ToList().Where(p => p.Date > DateTime.Now.AddDays(-10)).GroupBy(p => p.Date.ToShortDateString());
                        var officialPlayerPeak = db.OfficialPlayers.AsQueryable().OrderByDescending(p => p.Playercount).FirstOrDefault();
                        var officialFirstDate = db.OfficialPlayers.AsQueryable().OrderBy(p => p.Date).FirstOrDefault();
                        var officialLastDate = db.OfficialPlayers.AsQueryable().OrderByDescending(p => p.Date).FirstOrDefault();

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
