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
using Rabbot.API.Models;

namespace Rabbot
{
    public static class Helper
    {
        public static Dictionary<int, Stall> stall = new Dictionary<int, Stall> {
            { 0, new Stall{Level = 1, Name = "Wiese Lv. 1", Capacity = 2000, Attack = 1, Defense = 1, Jackpot = 150, MaxOutput = 40, MaxPot = 300 } },
            { 2, new Stall{Level = 2, Name = "Wiese Lv. 2", Capacity = 2400, Attack = 1, Defense = 1, Jackpot = 200, MaxOutput = 45, MaxPot = 500  } },
            { 5, new Stall{Level = 3, Name = "Wiese Lv. 3", Capacity = 2800, Attack = 2, Defense = 2, Jackpot = 250, MaxOutput = 50, MaxPot = 700  } },
            { 10, new Stall{Level = 4, Name = "Wiese Lv. 4", Capacity = 3200, Attack = 2, Defense = 2, Jackpot = 300, MaxOutput = 55, MaxPot = 900  } },
            { 15, new Stall{Level = 5, Name = "Wiese Lv. 5", Capacity = 3600, Attack = 3, Defense = 3, Jackpot = 350, MaxOutput = 60, MaxPot = 1100  } },
            { 20, new Stall{Level = 6, Name = "Unterstand Lv. 1", Capacity = 4000, Attack = 3, Defense = 3, Jackpot = 400, MaxOutput = 70, MaxPot = 1300  } },
            { 25, new Stall{Level = 7, Name = "Unterstand Lv. 2", Capacity = 4400, Attack = 4, Defense = 4, Jackpot = 450, MaxOutput = 80, MaxPot = 1500  } },
            { 30, new Stall{Level = 8, Name = "Unterstand Lv. 3", Capacity = 4800, Attack = 4, Defense = 4, Jackpot = 500, MaxOutput = 90, MaxPot = 1700  } },
            { 35, new Stall{Level = 9, Name = "Unterstand Lv. 4", Capacity = 5200, Attack = 5, Defense = 5, Jackpot = 550, MaxOutput = 100, MaxPot = 1900  } },
            { 40, new Stall{Level = 10, Name = "Unterstand Lv. 5", Capacity = 5600, Attack = 5, Defense = 5, Jackpot = 600, MaxOutput = 110, MaxPot = 2100  } },
            { 50, new Stall{Level = 11, Name = "Schuppen Lv. 1", Capacity = 6000, Attack = 6, Defense = 6, Jackpot = 650, MaxOutput = 130, MaxPot = 2300  } },
            { 60, new Stall{Level = 12, Name = "Schuppen Lv. 2", Capacity = 6400, Attack = 6, Defense = 6, Jackpot = 700, MaxOutput = 150, MaxPot = 2500  } },
            { 70, new Stall{Level = 13, Name = "Schuppen Lv. 3", Capacity = 6800, Attack = 7, Defense = 7, Jackpot = 750, MaxOutput = 170, MaxPot = 2700  } },
            { 80, new Stall{Level = 14, Name = "Schuppen Lv. 4", Capacity = 7200, Attack = 7, Defense = 7, Jackpot = 800, MaxOutput = 190, MaxPot = 2900  } },
            { 90, new Stall{Level = 15, Name = "Schuppen Lv. 5", Capacity = 7600, Attack = 8, Defense = 8, Jackpot = 850, MaxOutput = 210, MaxPot = 3100  } },
            { 100, new Stall{Level = 16, Name = "Kleiner Stall Lv. 1", Capacity = 8000, Attack = 8, Defense = 8, Jackpot = 900, MaxOutput = 240, MaxPot = 3300  } },
            { 120, new Stall{Level = 17, Name = "Kleiner Stall Lv. 2", Capacity = 8400, Attack = 9, Defense = 9, Jackpot = 950, MaxOutput = 270, MaxPot = 3500  } },
            { 140, new Stall{Level = 18, Name = "Kleiner Stall Lv. 3", Capacity = 8800, Attack = 9, Defense = 9, Jackpot = 1000, MaxOutput = 300, MaxPot = 3700  } },
            { 160, new Stall{Level = 19, Name = "Kleiner Stall Lv. 4", Capacity = 9200, Attack = 10, Defense = 10, Jackpot = 1050, MaxOutput = 330, MaxPot = 3900  } },
            { 180, new Stall{Level = 20, Name = "Kleiner Stall Lv. 5", Capacity = 9600, Attack = 10, Defense = 10, Jackpot = 1100, MaxOutput = 360, MaxPot = 4100  } },
            { 200, new Stall{Level = 21, Name = "Großer Stall Lv. 1", Capacity = 10000, Attack = 11, Defense = 11, Jackpot = 1150, MaxOutput = 400, MaxPot = 4300 } },
            { 240, new Stall{Level = 22, Name = "Großer Stall Lv. 2", Capacity = 10400, Attack = 11, Defense = 11, Jackpot = 1200, MaxOutput = 440, MaxPot = 4500  } },
            { 280, new Stall{Level = 23, Name = "Großer Stall Lv. 3", Capacity = 10800, Attack = 12, Defense = 12, Jackpot = 1250, MaxOutput = 480, MaxPot = 4700  } },
            { 320, new Stall{Level = 24, Name = "Großer Stall Lv. 4", Capacity = 13000, Attack = 12, Defense = 12, Jackpot = 1300, MaxOutput = 520, MaxPot = 4900  } },
            { 360, new Stall{Level = 25, Name = "Großer Stall Lv. 5", Capacity = 16000, Attack = 13, Defense = 13, Jackpot = 1350, MaxOutput = 560, MaxPot = 6000  } },
            { 400, new Stall{Level = 26, Name = "Ziegenhof", Capacity = 30000, Attack = 15, Defense = 15, Jackpot = 1500, MaxOutput = 600, MaxPot = 15000  } }
        };

        public static Dictionary<int, LevelInfo> exp = new Dictionary<int, LevelInfo> {
            { 0, new LevelInfo{NeededEXP = 0, Reward = 0 } },
            { 1, new LevelInfo{NeededEXP = 300, Reward = 5 } },
            { 2, new LevelInfo{NeededEXP = 600, Reward = 5 } },
            { 3, new LevelInfo{NeededEXP = 1200, Reward = 5 } },
            { 4, new LevelInfo{NeededEXP = 1500, Reward = 5 } },
            { 5, new LevelInfo{NeededEXP = 1800, Reward = 5 } },
            { 6, new LevelInfo{NeededEXP = 2500, Reward = 10 } },
            { 7, new LevelInfo{NeededEXP = 3200, Reward = 10 } },
            { 8, new LevelInfo{NeededEXP = 3900, Reward = 10 } },
            { 9, new LevelInfo{NeededEXP = 4600, Reward = 10 } },
            { 10, new LevelInfo{NeededEXP = 5300, Reward = 10 } },
            { 11, new LevelInfo{NeededEXP = 6500, Reward = 15 } },
            { 12, new LevelInfo{NeededEXP = 7700, Reward = 15 } },
            { 13, new LevelInfo{NeededEXP = 8900, Reward = 15 } },
            { 14, new LevelInfo{NeededEXP = 10100, Reward = 15 } },
            { 15, new LevelInfo{NeededEXP = 11300, Reward = 15 } },
            { 16, new LevelInfo{NeededEXP = 13000, Reward = 20 } },
            { 17, new LevelInfo{NeededEXP = 14700, Reward = 20 } },
            { 18, new LevelInfo{NeededEXP = 16400, Reward = 20 } },
            { 19, new LevelInfo{NeededEXP = 18100, Reward = 20 } },
            { 20, new LevelInfo{NeededEXP = 20400, Reward = 30 } },
            { 21, new LevelInfo{NeededEXP = 22700, Reward = 40 } },
            { 22, new LevelInfo{NeededEXP = 25000, Reward = 50 } },
            { 23, new LevelInfo{NeededEXP = 27300, Reward = 60 } },
            { 24, new LevelInfo{NeededEXP = 29600, Reward = 70 } },
            { 25, new LevelInfo{NeededEXP = 33900, Reward = 80 } },
            { 26, new LevelInfo{NeededEXP = 38200, Reward = 90 } },
            { 27, new LevelInfo{NeededEXP = 42500, Reward = 100 } },
            { 28, new LevelInfo{NeededEXP = 46800, Reward = 110 } },
            { 29, new LevelInfo{NeededEXP = 51100, Reward = 120 } },
            { 30, new LevelInfo{NeededEXP = 60900, Reward = 130 } },
            { 31, new LevelInfo{NeededEXP = 70700, Reward = 140 } },
            { 32, new LevelInfo{NeededEXP = 80500, Reward = 150 } },
            { 33, new LevelInfo{NeededEXP = 90300, Reward = 160 } },
            { 34, new LevelInfo{NeededEXP = 100100, Reward = 170 } },
            { 35, new LevelInfo{NeededEXP = 126900, Reward = 180 } },
            { 36, new LevelInfo{NeededEXP = 153700, Reward = 190 } },
            { 37, new LevelInfo{NeededEXP = 180500, Reward = 200 } },
            { 38, new LevelInfo{NeededEXP = 207300, Reward = 210 } },
            { 39, new LevelInfo{NeededEXP = 234100, Reward = 220 } },
            { 40, new LevelInfo{NeededEXP = 264900, Reward = 230 } },
            { 41, new LevelInfo{NeededEXP = 295700, Reward = 240 } },
            { 42, new LevelInfo{NeededEXP = 326500, Reward = 250 } },
            { 43, new LevelInfo{NeededEXP = 357300, Reward = 260 } },
            { 44, new LevelInfo{NeededEXP = 388100, Reward = 270 } },
            { 45, new LevelInfo{NeededEXP = 428900, Reward = 280 } },
            { 46, new LevelInfo{NeededEXP = 469700, Reward = 290 } },
            { 47, new LevelInfo{NeededEXP = 510500, Reward = 300 } },
            { 48, new LevelInfo{NeededEXP = 551300, Reward = 310 } },
            { 49, new LevelInfo{NeededEXP = 592100, Reward = 320 } },
            { 50, new LevelInfo{NeededEXP = 658900, Reward = 330 } },
            { 51, new LevelInfo{NeededEXP = 725700, Reward = 340 } },
            { 52, new LevelInfo{NeededEXP = 792500, Reward = 350 } },
            { 53, new LevelInfo{NeededEXP = 859300, Reward = 360 } },
            { 54, new LevelInfo{NeededEXP = 926100, Reward = 370 } },
            { 55, new LevelInfo{NeededEXP = 1062900, Reward = 380 } },
            { 56, new LevelInfo{NeededEXP = 1201700, Reward = 390 } },
            { 57, new LevelInfo{NeededEXP = 1342500, Reward = 400 } },
            { 58, new LevelInfo{NeededEXP = 1481300, Reward = 410 } },
            { 59, new LevelInfo{NeededEXP = 1620100, Reward = 420 } },
            { 60, new LevelInfo{NeededEXP = 1762900, Reward = 430 } },
            { 61, new LevelInfo{NeededEXP = 1905700, Reward = 440 } },
            { 62, new LevelInfo{NeededEXP = 2048500, Reward = 450 } },
            { 63, new LevelInfo{NeededEXP = 2191300, Reward = 460 } },
            { 64, new LevelInfo{NeededEXP = 2334100, Reward = 470 } },
            { 65, new LevelInfo{NeededEXP = 2491900, Reward = 480 } },
            { 66, new LevelInfo{NeededEXP = 2649700, Reward = 490 } },
            { 67, new LevelInfo{NeededEXP = 2807500, Reward = 500 } },
            { 68, new LevelInfo{NeededEXP = 2965300, Reward = 510 } },
            { 69, new LevelInfo{NeededEXP = 3123100, Reward = 520 } },
            { 70, new LevelInfo{NeededEXP = 3314900, Reward = 530 } },
            { 71, new LevelInfo{NeededEXP = 3506700, Reward = 540 } },
            { 72, new LevelInfo{NeededEXP = 3698500, Reward = 550 } },
            { 73, new LevelInfo{NeededEXP = 3890300, Reward = 560 } },
            { 74, new LevelInfo{NeededEXP = 4082100, Reward = 570 } },
            { 75, new LevelInfo{NeededEXP = 4345900, Reward = 580 } },
            { 76, new LevelInfo{NeededEXP = 4609700, Reward = 590 } },
            { 77, new LevelInfo{NeededEXP = 4873500, Reward = 600 } },
            { 78, new LevelInfo{NeededEXP = 5137300, Reward = 610 } },
            { 79, new LevelInfo{NeededEXP = 5401100, Reward = 620 } },
            { 80, new LevelInfo{NeededEXP = 5664900, Reward = 1000 } }
        };

        public static Emote Sword = Emote.Parse("<a:sword:593493621400010795>");
        public static Emote Shield = Emote.Parse("<a:shield:593498755441885275>");
        public static Emoji Yes = new Emoji("✅");
        public static Emoji No = new Emoji("❌");

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
            using (swaightContext db = new swaightContext())
            {
                if (db.Guild.Where(p => p.ServerId == (long)context.Guild.Id).Count() != 0)
                {
                    return (ulong?)db.Guild.Where(p => p.ServerId == (long)context.Guild.Id).FirstOrDefault().Botchannelid;
                }
                else
                {
                    return null;
                }
            }
        }

        public static Stall GetStall(int wins)
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

        public static string ToHumanReadable(this TimeSpan value)
        {
            var uptime = new StringBuilder();
            if (value.Days > 0)
                uptime.AppendFormat(value.Days > 1 ? "{0} days " : "{0} day ", value.Days);

            if (value.Days > 0 || value.Hours > 0)
                uptime.AppendFormat(value.Hours > 1 ? "{0} hours " : "{0} hour ", value.Hours);

            if (value.Hours > 0 || value.Minutes > 0)
                uptime.AppendFormat(value.Minutes > 1 ? "{0} minutes " : "{0} minute ", value.Minutes);

            if (value.Seconds > 0)
                uptime.AppendFormat(value.Seconds > 1 ? "{0} seconds " : "{0} second ", value.Seconds);

            return uptime.ToString();
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
    }
}
