using Discord.Commands;
using Discord.WebSocket;
using Rabbot.Database;
using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using System.Collections.Concurrent;
using Rabbot.Models;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using Rabbot.Services;

namespace Rabbot
{
    public class Helper
    {
        private static DatabaseService _databaseService;
        public Helper(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public readonly static Dictionary<int, StallDto> stall = new Dictionary<int, StallDto> {
            { 0, new StallDto{Level = 1, Name = "Wiese Lv. 1", Capacity = 2000, Attack = 1, Defense = 1, Jackpot = 500, MaxOutput = 40, MaxPot = 1000 } },
            { 2, new StallDto{Level = 2, Name = "Wiese Lv. 2", Capacity = 2400, Attack = 1, Defense = 1, Jackpot = 600, MaxOutput = 45, MaxPot = 1200  } },
            { 5, new StallDto{Level = 3, Name = "Wiese Lv. 3", Capacity = 2800, Attack = 2, Defense = 2, Jackpot = 700, MaxOutput = 50, MaxPot = 1400  } },
            { 10, new StallDto{Level = 4, Name = "Wiese Lv. 4", Capacity = 3200, Attack = 2, Defense = 2, Jackpot = 800, MaxOutput = 55, MaxPot = 1600  } },
            { 15, new StallDto{Level = 5, Name = "Wiese Lv. 5", Capacity = 3600, Attack = 3, Defense = 3, Jackpot = 900, MaxOutput = 60, MaxPot = 1800  } },
            { 20, new StallDto{Level = 6, Name = "Unterstand Lv. 1", Capacity = 4000, Attack = 3, Defense = 3, Jackpot = 1000, MaxOutput = 70, MaxPot = 2000  } },
            { 25, new StallDto{Level = 7, Name = "Unterstand Lv. 2", Capacity = 4400, Attack = 4, Defense = 4, Jackpot = 1100, MaxOutput = 80, MaxPot = 2200  } },
            { 30, new StallDto{Level = 8, Name = "Unterstand Lv. 3", Capacity = 4800, Attack = 4, Defense = 4, Jackpot = 1200, MaxOutput = 90, MaxPot = 2400  } },
            { 35, new StallDto{Level = 9, Name = "Unterstand Lv. 4", Capacity = 5200, Attack = 5, Defense = 5, Jackpot = 1300, MaxOutput = 100, MaxPot = 2600  } },
            { 40, new StallDto{Level = 10, Name = "Unterstand Lv. 5", Capacity = 5600, Attack = 5, Defense = 5, Jackpot = 1400, MaxOutput = 110, MaxPot = 2800  } },
            { 50, new StallDto{Level = 11, Name = "Schuppen Lv. 1", Capacity = 6000, Attack = 6, Defense = 6, Jackpot = 1500, MaxOutput = 130, MaxPot = 3000  } },
            { 60, new StallDto{Level = 12, Name = "Schuppen Lv. 2", Capacity = 6400, Attack = 6, Defense = 6, Jackpot = 1600, MaxOutput = 150, MaxPot = 3200  } },
            { 70, new StallDto{Level = 13, Name = "Schuppen Lv. 3", Capacity = 6800, Attack = 7, Defense = 7, Jackpot = 1700, MaxOutput = 170, MaxPot = 3400  } },
            { 80, new StallDto{Level = 14, Name = "Schuppen Lv. 4", Capacity = 7200, Attack = 7, Defense = 7, Jackpot = 1800, MaxOutput = 190, MaxPot = 3600  } },
            { 90, new StallDto{Level = 15, Name = "Schuppen Lv. 5", Capacity = 7600, Attack = 8, Defense = 8, Jackpot = 1900, MaxOutput = 210, MaxPot = 3800  } },
            { 100, new StallDto{Level = 16, Name = "Kleiner Stall Lv. 1", Capacity = 8000, Attack = 8, Defense = 8, Jackpot = 2000, MaxOutput = 240, MaxPot = 4000  } },
            { 120, new StallDto{Level = 17, Name = "Kleiner Stall Lv. 2", Capacity = 8400, Attack = 9, Defense = 9, Jackpot = 2100, MaxOutput = 270, MaxPot = 4200  } },
            { 140, new StallDto{Level = 18, Name = "Kleiner Stall Lv. 3", Capacity = 8800, Attack = 9, Defense = 9, Jackpot = 2200, MaxOutput = 300, MaxPot = 4400  } },
            { 160, new StallDto{Level = 19, Name = "Kleiner Stall Lv. 4", Capacity = 9200, Attack = 10, Defense = 10, Jackpot = 2300, MaxOutput = 330, MaxPot = 4600  } },
            { 180, new StallDto{Level = 20, Name = "Kleiner Stall Lv. 5", Capacity = 9600, Attack = 10, Defense = 10, Jackpot = 2400, MaxOutput = 360, MaxPot = 4800  } },
            { 200, new StallDto{Level = 21, Name = "Großer Stall Lv. 1", Capacity = 10000, Attack = 11, Defense = 11, Jackpot = 2500, MaxOutput = 400, MaxPot = 5000 } },
            { 240, new StallDto{Level = 22, Name = "Großer Stall Lv. 2", Capacity = 10400, Attack = 11, Defense = 11, Jackpot = 2600, MaxOutput = 440, MaxPot = 5200  } },
            { 280, new StallDto{Level = 23, Name = "Großer Stall Lv. 3", Capacity = 10800, Attack = 12, Defense = 12, Jackpot = 2700, MaxOutput = 480, MaxPot = 5400  } },
            { 320, new StallDto{Level = 24, Name = "Großer Stall Lv. 4", Capacity = 13000, Attack = 12, Defense = 12, Jackpot = 2800, MaxOutput = 520, MaxPot = 6500  } },
            { 360, new StallDto{Level = 25, Name = "Großer Stall Lv. 5", Capacity = 16000, Attack = 13, Defense = 13, Jackpot = 2900, MaxOutput = 560, MaxPot = 8000  } },
            { 400, new StallDto{Level = 26, Name = "Ziegenhof", Capacity = 30000, Attack = 15, Defense = 15, Jackpot = 3000, MaxOutput = 600, MaxPot = 15000  } }
        };

        public readonly static Dictionary<int, CombiLevelDto> combi = new Dictionary<int, CombiLevelDto> {
            { 0, new CombiLevelDto{NeededEXP = 0 } },
            { 1, new CombiLevelDto{NeededEXP = 5 } },
            { 2, new CombiLevelDto{NeededEXP = 10 } },
            { 3, new CombiLevelDto{NeededEXP = 15 } },
            { 4, new CombiLevelDto{NeededEXP = 20 } },
            { 5, new CombiLevelDto{NeededEXP = 25 } },
            { 6, new CombiLevelDto{NeededEXP = 30 } },
            { 7, new CombiLevelDto{NeededEXP = 35 } },
            { 8, new CombiLevelDto{NeededEXP = 40 } },
            { 9, new CombiLevelDto{NeededEXP = 45 } },
            { 10, new CombiLevelDto{NeededEXP = 50 } },
            { 11, new CombiLevelDto{NeededEXP = 60 } },
            { 12, new CombiLevelDto{NeededEXP = 70 } },
            { 13, new CombiLevelDto{NeededEXP = 80 } },
            { 14, new CombiLevelDto{NeededEXP = 90 } },
            { 15, new CombiLevelDto{NeededEXP = 100 } },
            { 16, new CombiLevelDto{NeededEXP = 110 } },
            { 17, new CombiLevelDto{NeededEXP = 120 } },
            { 18, new CombiLevelDto{NeededEXP = 130 } },
            { 19, new CombiLevelDto{NeededEXP = 140 } },
            { 20, new CombiLevelDto{NeededEXP = 150 } },
            { 21, new CombiLevelDto{NeededEXP = 170 } },
            { 22, new CombiLevelDto{NeededEXP = 190 } },
            { 23, new CombiLevelDto{NeededEXP = 210 } },
            { 24, new CombiLevelDto{NeededEXP = 230 } },
            { 25, new CombiLevelDto{NeededEXP = 250 } },
            { 26, new CombiLevelDto{NeededEXP = 270 } },
            { 27, new CombiLevelDto{NeededEXP = 290 } },
            { 28, new CombiLevelDto{NeededEXP = 310 } },
            { 29, new CombiLevelDto{NeededEXP = 330 } },
            { 30, new CombiLevelDto{NeededEXP = 350 } },
            { 31, new CombiLevelDto{NeededEXP = 390 } },
            { 32, new CombiLevelDto{NeededEXP = 430 } },
            { 33, new CombiLevelDto{NeededEXP = 470 } },
            { 34, new CombiLevelDto{NeededEXP = 510 } },
            { 35, new CombiLevelDto{NeededEXP = 550 } },
            { 36, new CombiLevelDto{NeededEXP = 590 } },
            { 37, new CombiLevelDto{NeededEXP = 630 } },
            { 38, new CombiLevelDto{NeededEXP = 670 } },
            { 39, new CombiLevelDto{NeededEXP = 710 } },
            { 40, new CombiLevelDto{NeededEXP = 750 } },
            { 41, new CombiLevelDto{NeededEXP = 830 } },
            { 42, new CombiLevelDto{NeededEXP = 910 } },
            { 43, new CombiLevelDto{NeededEXP = 990 } },
            { 44, new CombiLevelDto{NeededEXP = 1070 } },
            { 45, new CombiLevelDto{NeededEXP = 1150 } },
            { 46, new CombiLevelDto{NeededEXP = 1230 } },
            { 47, new CombiLevelDto{NeededEXP = 1310 } },
            { 48, new CombiLevelDto{NeededEXP = 1390 } },
            { 49, new CombiLevelDto{NeededEXP = 1470 } },
            { 50, new CombiLevelDto{NeededEXP = 1550 } },
        };

        public readonly static Dictionary<int, LevelInfoDto> exp = new Dictionary<int, LevelInfoDto> {
            { 0, new LevelInfoDto{NeededEXP = 0, Reward = 0 } },
            { 1, new LevelInfoDto{NeededEXP = 300, Reward = 5 } },
            { 2, new LevelInfoDto{NeededEXP = 600, Reward = 5 } },
            { 3, new LevelInfoDto{NeededEXP = 1200, Reward = 5 } },
            { 4, new LevelInfoDto{NeededEXP = 1500, Reward = 5 } },
            { 5, new LevelInfoDto{NeededEXP = 1800, Reward = 5 } },
            { 6, new LevelInfoDto{NeededEXP = 2500, Reward = 10 } },
            { 7, new LevelInfoDto{NeededEXP = 3200, Reward = 10 } },
            { 8, new LevelInfoDto{NeededEXP = 3900, Reward = 10 } },
            { 9, new LevelInfoDto{NeededEXP = 4600, Reward = 10 } },
            { 10, new LevelInfoDto{NeededEXP = 5300, Reward = 10 } },
            { 11, new LevelInfoDto{NeededEXP = 6500, Reward = 15 } },
            { 12, new LevelInfoDto{NeededEXP = 7700, Reward = 15 } },
            { 13, new LevelInfoDto{NeededEXP = 8900, Reward = 15 } },
            { 14, new LevelInfoDto{NeededEXP = 10100, Reward = 15 } },
            { 15, new LevelInfoDto{NeededEXP = 11300, Reward = 15 } },
            { 16, new LevelInfoDto{NeededEXP = 13000, Reward = 20 } },
            { 17, new LevelInfoDto{NeededEXP = 14700, Reward = 20 } },
            { 18, new LevelInfoDto{NeededEXP = 16400, Reward = 20 } },
            { 19, new LevelInfoDto{NeededEXP = 18100, Reward = 20 } },
            { 20, new LevelInfoDto{NeededEXP = 20400, Reward = 30 } },
            { 21, new LevelInfoDto{NeededEXP = 22700, Reward = 40 } },
            { 22, new LevelInfoDto{NeededEXP = 25000, Reward = 50 } },
            { 23, new LevelInfoDto{NeededEXP = 27300, Reward = 60 } },
            { 24, new LevelInfoDto{NeededEXP = 29600, Reward = 70 } },
            { 25, new LevelInfoDto{NeededEXP = 33900, Reward = 80 } },
            { 26, new LevelInfoDto{NeededEXP = 38200, Reward = 90 } },
            { 27, new LevelInfoDto{NeededEXP = 42500, Reward = 100 } },
            { 28, new LevelInfoDto{NeededEXP = 46800, Reward = 110 } },
            { 29, new LevelInfoDto{NeededEXP = 51100, Reward = 120 } },
            { 30, new LevelInfoDto{NeededEXP = 60900, Reward = 130 } },
            { 31, new LevelInfoDto{NeededEXP = 70700, Reward = 140 } },
            { 32, new LevelInfoDto{NeededEXP = 80500, Reward = 150 } },
            { 33, new LevelInfoDto{NeededEXP = 90300, Reward = 160 } },
            { 34, new LevelInfoDto{NeededEXP = 100100, Reward = 170 } },
            { 35, new LevelInfoDto{NeededEXP = 126900, Reward = 180 } },
            { 36, new LevelInfoDto{NeededEXP = 153700, Reward = 190 } },
            { 37, new LevelInfoDto{NeededEXP = 180500, Reward = 200 } },
            { 38, new LevelInfoDto{NeededEXP = 207300, Reward = 210 } },
            { 39, new LevelInfoDto{NeededEXP = 234100, Reward = 220 } },
            { 40, new LevelInfoDto{NeededEXP = 264900, Reward = 230 } },
            { 41, new LevelInfoDto{NeededEXP = 295700, Reward = 240 } },
            { 42, new LevelInfoDto{NeededEXP = 326500, Reward = 250 } },
            { 43, new LevelInfoDto{NeededEXP = 357300, Reward = 260 } },
            { 44, new LevelInfoDto{NeededEXP = 388100, Reward = 270 } },
            { 45, new LevelInfoDto{NeededEXP = 428900, Reward = 280 } },
            { 46, new LevelInfoDto{NeededEXP = 469700, Reward = 290 } },
            { 47, new LevelInfoDto{NeededEXP = 510500, Reward = 300 } },
            { 48, new LevelInfoDto{NeededEXP = 551300, Reward = 310 } },
            { 49, new LevelInfoDto{NeededEXP = 592100, Reward = 320 } },
            { 50, new LevelInfoDto{NeededEXP = 658900, Reward = 330 } },
            { 51, new LevelInfoDto{NeededEXP = 725700, Reward = 340 } },
            { 52, new LevelInfoDto{NeededEXP = 792500, Reward = 350 } },
            { 53, new LevelInfoDto{NeededEXP = 859300, Reward = 360 } },
            { 54, new LevelInfoDto{NeededEXP = 926100, Reward = 370 } },
            { 55, new LevelInfoDto{NeededEXP = 1062900, Reward = 380 } },
            { 56, new LevelInfoDto{NeededEXP = 1201700, Reward = 390 } },
            { 57, new LevelInfoDto{NeededEXP = 1342500, Reward = 400 } },
            { 58, new LevelInfoDto{NeededEXP = 1481300, Reward = 410 } },
            { 59, new LevelInfoDto{NeededEXP = 1620100, Reward = 420 } },
            { 60, new LevelInfoDto{NeededEXP = 1762900, Reward = 430 } },
            { 61, new LevelInfoDto{NeededEXP = 1905700, Reward = 440 } },
            { 62, new LevelInfoDto{NeededEXP = 2048500, Reward = 450 } },
            { 63, new LevelInfoDto{NeededEXP = 2191300, Reward = 460 } },
            { 64, new LevelInfoDto{NeededEXP = 2334100, Reward = 470 } },
            { 65, new LevelInfoDto{NeededEXP = 2491900, Reward = 480 } },
            { 66, new LevelInfoDto{NeededEXP = 2649700, Reward = 490 } },
            { 67, new LevelInfoDto{NeededEXP = 2807500, Reward = 500 } },
            { 68, new LevelInfoDto{NeededEXP = 2965300, Reward = 510 } },
            { 69, new LevelInfoDto{NeededEXP = 3123100, Reward = 520 } },
            { 70, new LevelInfoDto{NeededEXP = 3314900, Reward = 530 } },
            { 71, new LevelInfoDto{NeededEXP = 3506700, Reward = 540 } },
            { 72, new LevelInfoDto{NeededEXP = 3698500, Reward = 550 } },
            { 73, new LevelInfoDto{NeededEXP = 3890300, Reward = 560 } },
            { 74, new LevelInfoDto{NeededEXP = 4082100, Reward = 570 } },
            { 75, new LevelInfoDto{NeededEXP = 4345900, Reward = 580 } },
            { 76, new LevelInfoDto{NeededEXP = 4609700, Reward = 590 } },
            { 77, new LevelInfoDto{NeededEXP = 4873500, Reward = 600 } },
            { 78, new LevelInfoDto{NeededEXP = 5137300, Reward = 610 } },
            { 79, new LevelInfoDto{NeededEXP = 5401100, Reward = 620 } },
            { 80, new LevelInfoDto{NeededEXP = 5664900, Reward = 630 } },
            { 81, new LevelInfoDto{NeededEXP = 6000000, Reward = 640 } },
            { 82, new LevelInfoDto{NeededEXP = 6300000, Reward = 650 } },
            { 83, new LevelInfoDto{NeededEXP = 6600000, Reward = 660 } },
            { 84, new LevelInfoDto{NeededEXP = 6900000, Reward = 670 } },
            { 85, new LevelInfoDto{NeededEXP = 7200000, Reward = 680 } },
            { 86, new LevelInfoDto{NeededEXP = 7500000, Reward = 690 } },
            { 87, new LevelInfoDto{NeededEXP = 7900000, Reward = 700 } },
            { 88, new LevelInfoDto{NeededEXP = 8300000, Reward = 710 } },
            { 89, new LevelInfoDto{NeededEXP = 8700000, Reward = 720 } },
            { 90, new LevelInfoDto{NeededEXP = 9100000, Reward = 730 } },
            { 91, new LevelInfoDto{NeededEXP = 9500000, Reward = 740 } },
            { 92, new LevelInfoDto{NeededEXP = 9900000, Reward = 750 } },
            { 93, new LevelInfoDto{NeededEXP = 10300000, Reward = 760 } },
            { 94, new LevelInfoDto{NeededEXP = 10700000, Reward = 770 } },
            { 95, new LevelInfoDto{NeededEXP = 11000000, Reward = 780 } },
            { 96, new LevelInfoDto{NeededEXP = 11400000, Reward = 790 } },
            { 97, new LevelInfoDto{NeededEXP = 11800000, Reward = 800 } },
            { 98, new LevelInfoDto{NeededEXP = 12200000, Reward = 810 } },
            { 99, new LevelInfoDto{NeededEXP = 12700000, Reward = 820 } },
            { 100, new LevelInfoDto{NeededEXP = 13100000, Reward = 830 } },
            { 101, new LevelInfoDto{NeededEXP = 13600000, Reward = 840 } },
            { 102, new LevelInfoDto{NeededEXP = 14100000, Reward = 850 } },
            { 103, new LevelInfoDto{NeededEXP = 14600000, Reward = 860 } },
            { 104, new LevelInfoDto{NeededEXP = 15100000, Reward = 870 } },
            { 105, new LevelInfoDto{NeededEXP = 15600000, Reward = 880 } },
            { 106, new LevelInfoDto{NeededEXP = 16200000, Reward = 890 } },
            { 107, new LevelInfoDto{NeededEXP = 16800000, Reward = 900 } },
            { 108, new LevelInfoDto{NeededEXP = 17400000, Reward = 910 } },
            { 109, new LevelInfoDto{NeededEXP = 18000000, Reward = 920 } },
            { 110, new LevelInfoDto{NeededEXP = 18600000, Reward = 930 } },
            { 111, new LevelInfoDto{NeededEXP = 19200000, Reward = 940 } },
            { 112, new LevelInfoDto{NeededEXP = 19800000, Reward = 950 } },
            { 113, new LevelInfoDto{NeededEXP = 20400000, Reward = 960 } },
            { 114, new LevelInfoDto{NeededEXP = 21000000, Reward = 970 } },
            { 115, new LevelInfoDto{NeededEXP = 21700000, Reward = 980 } },
            { 116, new LevelInfoDto{NeededEXP = 22400000, Reward = 990 } },
            { 117, new LevelInfoDto{NeededEXP = 23100000, Reward = 1000 } },
            { 118, new LevelInfoDto{NeededEXP = 23800000, Reward = 1100 } },
            { 119, new LevelInfoDto{NeededEXP = 24800000, Reward = 2000 } }
        };

        public static bool AttackActive = false;

        public static ConcurrentDictionary<ulong, DateTime> cooldown = new ConcurrentDictionary<ulong, DateTime>();

        public static async Task UpdateSpin(ISocketMessageChannel channel, SocketGuildUser user, IUserMessage message, DiscordSocketClient client, int setEinsatz, bool isNew = true)
        {
            Random random = new Random();
            var glitch = Constants.Glitch;
            var diego = Constants.Diego;
            var shyguy = Constants.Shyguy;
            var goldenziege = Constants.Goldenziege;

            Emote slot1 = null;
            Emote slot2 = null;
            Emote slot3 = null;

            IUserMessage msg = message;


            if (msg != null && !(msg.Embeds.Count == 0))
                if (msg.Author.Id != client.CurrentUser.Id)
                    return;

            using (var db = _databaseService.Open<RabbotContext>())
            {

                var dbUser = db.Features.FirstOrDefault(p => p.GuildId == user.Guild.Id && p.UserId == user.Id);
                if (dbUser == null)
                    return;


                if (dbUser.Locked == true)
                {
                    await channel.SendMessageAsync($"{user.Mention} du bist gerade in einem Angriff!");
                    if (msg != null)
                        await msg.RemoveAllReactionsAsync();
                    return;
                }

                if (msg.Embeds.Any())
                {
                    string title = "";
                    if (msg.Embeds.First().Footer != null)
                    {
                        title = msg.Embeds.First().Title;
                    }
                    if (!string.IsNullOrEmpty(title))
                    {
                        string[] titleText = Regex.Split(title, @"(!?[+-]?\d+(\.\d+)?)");
                        if (Int32.TryParse(titleText[1], out int commitment))
                            setEinsatz = commitment;
                    }
                }

                if (dbUser.Goats < setEinsatz)
                {
                    await channel.SendMessageAsync($"{user.Mention} du hast leider nicht ausreichend Ziegen!");
                    if (msg != null)
                        await msg.RemoveAllReactionsAsync();
                    return;
                }

                int price = -setEinsatz;
                dbUser.Goats -= setEinsatz;
                await db.SaveChangesAsync();


                int chance = random.Next(1, 1001);

                if (chance <= 880)
                {
                    int rnd1 = random.Next(10000, 20000);
                    int rnd2 = random.Next(30000, 40000);
                    int rnd3 = random.Next(50000, 60000);

                    int magic1 = rnd1 % 500;
                    int magic2 = rnd2 % 500;
                    int magic3 = rnd3 % 500;

                    Emote[] slots = new Emote[500];

                    for (int i = 0; i < 230; i++)
                        slots[i] = glitch;
                    for (int i = 230; i < 420; i++)
                        slots[i] = diego;
                    for (int i = 420; i < 480; i++)
                        slots[i] = shyguy;
                    for (int i = 480; i < 500; i++)
                        slots[i] = goldenziege;

                    slot1 = slots[magic1];
                    slot2 = slots[magic2];
                    slot3 = slots[magic3];
                }
                else if (chance > 880 && chance <= 950)
                {
                    slot1 = glitch;
                    slot2 = glitch;
                    slot3 = glitch;
                }
                else if (chance > 950 && chance <= 990)
                {
                    slot1 = diego;
                    slot2 = diego;
                    slot3 = diego;
                }
                else if (chance > 990 && chance <= 997)
                {
                    slot1 = shyguy;
                    slot2 = shyguy;
                    slot3 = shyguy;
                }
                else if (chance > 997 && chance <= 1000)
                {
                    slot1 = goldenziege;
                    slot2 = goldenziege;
                    slot3 = goldenziege;
                }


                string output = $"{slot1} - {slot2} - {slot3}";

                EmbedBuilder embed = new EmbedBuilder();
                if (slot1 == slot2 && slot1 == slot3 && slot2 == slot3)
                    embed.Color = Color.Gold;
                else
                    embed.Color = Color.DarkGrey;
                embed.WithDescription($"**Slot Machine für {user.Nickname ?? user.Username}**\n\nDein Spin:\n\n{output}");
                int einsatz = setEinsatz;
                embed.WithTitle($"Einsatz: {einsatz} Ziegen");

                if ((slot1 == glitch) && (slot2 == glitch) && (slot3 == glitch))
                {
                    if (!IsFull(dbUser.Goats + einsatz * 2, dbUser.Wins))
                        dbUser.Goats += einsatz * 2;
                    embed.AddField("Ergebnis", $"**Du hast {einsatz * 2} Ziegen gewonnen!**");
                    price += einsatz * 2;
                }

                else if ((slot1 == diego) && (slot2 == diego) && (slot3 == diego))
                {
                    if (!IsFull(dbUser.Goats + einsatz * 5, dbUser.Wins))
                        dbUser.Goats += einsatz * 5;
                    embed.AddField("Ergebnis", $"**Du hast {einsatz * 5} Ziegen gewonnen!**");
                    price += einsatz * 5;

                }

                else if ((slot1 == shyguy) && (slot2 == shyguy) && (slot3 == shyguy))
                {
                    if (!IsFull(dbUser.Goats + einsatz * 10, dbUser.Wins))
                        dbUser.Goats += einsatz * 10;
                    embed.AddField("Ergebnis", $"**Du hast {einsatz * 10} Ziegen gewonnen!**");
                    price += einsatz * 10;
                }

                else if ((slot1 == goldenziege) && (slot2 == goldenziege) && (slot3 == goldenziege))
                {
                    if (!IsFull(dbUser.Goats + einsatz * 25, dbUser.Wins))
                        dbUser.Goats += einsatz * 25;
                    embed.AddField("Ergebnis", $"**Du hast {einsatz * 25} Ziegen gewonnen!**");
                    price += einsatz * 25;
                }

                else
                {
                    embed.AddField("Ergebnis", $"War wohl **nichts**.. {Constants.Doggo}");
                }

                if (msg.Embeds.Any())
                {
                    string footer = "";
                    string title = "";
                    if (msg.Embeds.First().Footer != null)
                    {
                        footer = msg.Embeds.First().Footer.Value.Text;
                        title = msg.Embeds.First().Title;
                    }
                    if (!string.IsNullOrEmpty(footer))
                    {
                        string[] numbers = Regex.Split(footer, @"(!?[+-]?\d+(\.\d+)?)");
                        Int32.TryParse(numbers[1], out int spins);
                        Int32.TryParse(numbers[3], out int goats);
                        spins++;
                        goats += price;
                        var newFooter = $"Spins: {spins} | Gesamt: {goats} Ziegen";
                        embed.WithFooter(newFooter);
                    }

                }
                else
                {
                    var newFooter = $"Spins: 1 | Gesamt: {price} Ziegen";
                    embed.WithFooter(newFooter);
                }

                if (isNew)
                {
                    string prices = $"**Über die Reaction kannst du erneut spinnen.**\n";
                    prices += $"3x {glitch} -> Einsatz × 2\n";
                    prices += $"3x {diego} -> Einsatz × 5\n";
                    prices += $"3x {shyguy} -> Einsatz × 10\n";
                    prices += $"3x {goldenziege} -> Einsatz × 25";
                    msg = await channel.SendMessageAsync(prices, false, embed.Build());
                    await msg.AddReactionAsync(Constants.Slot);
                }
                else
                {
                    await msg.ModifyAsync(p => { p.Embed = embed.Build(); p.Content = null; });
                }

                dbUser.Spins++;
                dbUser.Gewinn += price;

                await db.SaveChangesAsync();
            }
        }

        public static string RemoveSpecialCharacters(string str)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in str)
            {
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '.' || c == '_')
                {
                    sb.Append(c);
                }
            }
            if (!String.IsNullOrWhiteSpace(sb.ToString()))
                return sb.ToString();
            else
                return "picture";
        }

        public static ulong? GetBotChannel(ICommandContext context)
        {
            using (var db = _databaseService.Open<RabbotContext>())
            {
                if (!db.Guilds.Where(p => p.GuildId == context.Guild.Id).Any())
                {
                    return (ulong?)db.Guilds.FirstOrDefault(p => p.GuildId == context.Guild.Id).BotChannelId;
                }
                else
                {
                    return null;
                }
            }
        }

        public static StallDto GetStall(int wins)
        {
            var myStall = stall.Where(y => y.Key <= wins).OrderByDescending(x => x.Key).FirstOrDefault().Value;
            return myStall;
        }

        public static bool IsFull(int goats, int wins)
        {
            var stall = GetStall(wins);
            if (stall.Capacity < goats)
                return true;
            return false;
        }

        public static uint GetLevel(int? myExp)
        {
            var level = exp.Where(y => y.Value.NeededEXP <= myExp).Max(x => x.Key);
            return (uint)level;
        }

        public static uint GetEXP(int? level)
        {
            var myExp = exp.Where(y => y.Key <= level).Max(x => x.Value.NeededEXP);
            return (uint)myExp;
        }

        public static int GetReward(int? level)
        {
            var myExp = exp.Where(y => y.Key <= level).Max(x => x.Value.Reward);
            return myExp;
        }

        public static uint GetCombiLevel(int? myExp)
        {
            var level = combi.Where(y => y.Value.NeededEXP <= myExp).Max(x => x.Key);
            return (uint)level;
        }

        public static uint GetCombiEXP(int? level)
        {
            var myExp = combi.Where(y => y.Key <= level).Max(x => x.Value.NeededEXP);
            return (uint)myExp;
        }

        public static string MessageReplace(string myMessage)
        {
            myMessage = new Regex("(https|http)[:\\/a-zA-Z0-9-Z.?!=#%&_+-;]*").Replace(myMessage, ""); //Edit Links
            myMessage = new Regex("<:[a-zA-Z0-9]*:[0-9]{18,18}>").Replace(myMessage, ""); //Edit custom images
            myMessage = new Regex("<a:[a-zA-Z0-9]*:[0-9]{18,18}>").Replace(myMessage, ""); //Edit custom animated images
            myMessage = new Regex("<@[0-9!]{18,19}>").Replace(myMessage, ""); //Edit tags
            myMessage = new Regex("[^\\w\\d\\s]|[\\\\_]").Replace(myMessage, ""); //Edit all special characters
            myMessage = new Regex("[\\s]{2,}").Replace(myMessage, " "); //Edit every multiple whitespace type to a single whitespace
            return myMessage;
        }

        public static string ReplaceCharacter(string myString)
        {
            myString = new Regex("[.?!,\"'+@#$%^&*(){}][/-_|=§‘’`„°•—–¿¡₩€¢¥£​]").Replace(myString, "");
            myString = new Regex("[ÀÁÂÃÅÆàáâãåæĀāĂăΑАаӒӓä]").Replace(myString, "a");
            myString = new Regex("[Ąą]").Replace(myString, "ah");
            myString = new Regex("[ΒВЬ]").Replace(myString, "b");
            myString = new Regex("[ĆćĈĉĊċČčϹСⅭϲсⅽ]").Replace(myString, "c");
            myString = new Regex("[çÇ]").Replace(myString, "ch");
            myString = new Regex("[ÐĎďĐđƉƊԁⅾ]").Replace(myString, "d");
            myString = new Regex("[3ÈÉÊËèéêëĚěĒēĔĕĖėΕЕе]").Replace(myString, "e");
            myString = new Regex("[Ęę]").Replace(myString, "eh");
            myString = new Regex("[Ϝf​]").Replace(myString, "f");
            myString = new Regex("[ĜĝĞğĠġģԌ]").Replace(myString, "g");
            myString = new Regex("[Ģ]").Replace(myString, "gh");
            myString = new Regex("[ĤĥĦħΗНһн]").Replace(myString, "h");
            myString = new Regex("[1ĨĩĪīĬĭĮįİıĲĳÌÍÎÏìíîïÌÍÎÏ¡!ΙІⅠіⅰ]").Replace(myString, "i");
            myString = new Regex("[ĴĵЈј]").Replace(myString, "j");
            myString = new Regex("[ĶķĸΚКK]").Replace(myString, "k");
            myString = new Regex("[ĹĺĻļĽľĿŀŁł]").Replace(myString, "l");
            myString = new Regex("[ΜМⅯⅿ]").Replace(myString, "m");
            myString = new Regex("[ŃńŅņŇňŉŊŋñΝn​и]").Replace(myString, "n");
            myString = new Regex("[0ŌōŎŏŐőŒœòóôõΟОοоӦӧö]").Replace(myString, "o");
            myString = new Regex("[ΡРр₽]").Replace(myString, "p");
            myString = new Regex("[ŔŕŖŗŘřя]").Replace(myString, "r");
            myString = new Regex("[ŚśŜŝŠšЅѕ]").Replace(myString, "s");
            myString = new Regex("[Şş]").Replace(myString, "sh");
            myString = new Regex("[ŢţŤťŦŧΤТ]").Replace(myString, "t");
            myString = new Regex("[ŨũŪūŬŭŮůŰűÙÚÛùúûµüц]").Replace(myString, "u");
            myString = new Regex("[Ųų]").Replace(myString, "uh");
            myString = new Regex("[ѴⅤνѵⅴ]").Replace(myString, "v");
            myString = new Regex("[Ŵŵѡ]").Replace(myString, "w");
            myString = new Regex("[ΧХⅩхⅹ]").Replace(myString, "x");
            myString = new Regex("[ŶŷŸÝýÿΥҮу]").Replace(myString, "y");
            myString = new Regex("[ŹźŻżŽžΖ]").Replace(myString, "z");

            return myString;
        }

        public static string GetFilePath(string filename)
        {
            string directory = Path.Combine(AppContext.BaseDirectory, "Resources");
            string toolFilepath;
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            {
                toolFilepath = Path.Combine(directory, filename + ".exe");

                if (!File.Exists(toolFilepath))
                {
                    var assembly = typeof(Helper).GetTypeInfo().Assembly;
                    var type = typeof(Helper);
                    var ns = type.Namespace;

                    using (var resourceStream = assembly.GetManifestResourceStream($"{ns}.{filename}.exe"))
                    using (var fileStream = File.OpenWrite(toolFilepath))
                    {
                        resourceStream.CopyTo(fileStream);
                    }
                }
                return toolFilepath;
            }
            else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
            {
                //Check if wkhtmltoimage package is installed on this distro in using which command
                Process process = Process.Start(new ProcessStartInfo()
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WorkingDirectory = "/bin/",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    FileName = "/bin/bash",
                    Arguments = $"which {filename}"

                });
                string answer = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                if (!string.IsNullOrEmpty(answer) && answer.Contains(filename))
                {
                    toolFilepath = filename;
                    return toolFilepath;
                }
                else
                {
                    throw new Exception($"{filename} does not appear to be installed on this linux system according to which command;");
                }
            }
            else
            {
                //OSX not implemented
                throw new Exception("OSX Platform not implemented yet");
            }
        }
    }
}
