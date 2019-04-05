using Discord.Commands;
using Discord.WebSocket;
using DiscordBot_Core.Database;
using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using DiscordBot_Core.API.Models;

namespace DiscordBot_Core
{
    public static class Helper
    {
        public static Dictionary<int, Stall> stall = new Dictionary<int, Stall> {
            { 0, new Stall{Level = 1, Name = "Wiese Lv. 1", Capacity = 600, Attack = 1, Defense = 1, Jackpot = 150, MaxOutput = 40 } },
            { 2, new Stall{Level = 2, Name = "Wiese Lv. 2", Capacity = 1000, Attack = 1, Defense = 1, Jackpot = 200, MaxOutput = 45  } },
            { 5, new Stall{Level = 3, Name = "Wiese Lv. 3", Capacity = 1400, Attack = 2, Defense = 2, Jackpot = 250, MaxOutput = 50  } },
            { 10, new Stall{Level = 4, Name = "Wiese Lv. 4", Capacity = 1800, Attack = 2, Defense = 2, Jackpot = 300, MaxOutput = 55  } },
            { 15, new Stall{Level = 5, Name = "Wiese Lv. 5", Capacity = 2200, Attack = 3, Defense = 3, Jackpot = 350, MaxOutput = 60  } },
            { 20, new Stall{Level = 6, Name = "Unterstand Lv. 1", Capacity = 2600, Attack = 3, Defense = 3, Jackpot = 400, MaxOutput = 70  } },
            { 25, new Stall{Level = 7, Name = "Unterstand Lv. 2", Capacity = 3000, Attack = 4, Defense = 4, Jackpot = 450, MaxOutput = 80  } },
            { 30, new Stall{Level = 8, Name = "Unterstand Lv. 3", Capacity = 3400, Attack = 4, Defense = 4, Jackpot = 500, MaxOutput = 90  } },
            { 35, new Stall{Level = 9, Name = "Unterstand Lv. 4", Capacity = 3800, Attack = 5, Defense = 5, Jackpot = 550, MaxOutput = 100  } },
            { 40, new Stall{Level = 10, Name = "Unterstand Lv. 5", Capacity = 4200, Attack = 5, Defense = 5, Jackpot = 600, MaxOutput = 110  } },
            { 50, new Stall{Level = 11, Name = "Schuppen Lv. 1", Capacity = 4600, Attack = 6, Defense = 6, Jackpot = 650, MaxOutput = 130  } },
            { 60, new Stall{Level = 12, Name = "Schuppen Lv. 2", Capacity = 5000, Attack = 6, Defense = 6, Jackpot = 700, MaxOutput = 150  } },
            { 70, new Stall{Level = 13, Name = "Schuppen Lv. 3", Capacity = 5400, Attack = 7, Defense = 7, Jackpot = 750, MaxOutput = 170  } },
            { 80, new Stall{Level = 14, Name = "Schuppen Lv. 4", Capacity = 5800, Attack = 7, Defense = 7, Jackpot = 800, MaxOutput = 190  } },
            { 90, new Stall{Level = 15, Name = "Schuppen Lv. 5", Capacity = 6200, Attack = 8, Defense = 8, Jackpot = 850, MaxOutput = 210  } },
            { 100, new Stall{Level = 16, Name = "Kleiner Stall Lv. 1", Capacity = 6600, Attack = 8, Defense = 8, Jackpot = 900, MaxOutput = 240  } },
            { 120, new Stall{Level = 17, Name = "Kleiner Stall Lv. 2", Capacity = 7000, Attack = 9, Defense = 9, Jackpot = 950, MaxOutput = 270  } },
            { 140, new Stall{Level = 18, Name = "Kleiner Stall Lv. 3", Capacity = 7400, Attack = 9, Defense = 9, Jackpot = 1000, MaxOutput = 300  } },
            { 160, new Stall{Level = 19, Name = "Kleiner Stall Lv. 4", Capacity = 7800, Attack = 10, Defense = 10, Jackpot = 1050, MaxOutput = 330  } },
            { 180, new Stall{Level = 20, Name = "Kleiner Stall Lv. 5", Capacity = 8200, Attack = 10, Defense = 10, Jackpot = 1100, MaxOutput = 360  } },
            { 200, new Stall{Level = 21, Name = "Großer Stall Lv. 1", Capacity = 8600, Attack = 11, Defense = 11, Jackpot = 1150, MaxOutput = 400 } },
            { 240, new Stall{Level = 22, Name = "Großer Stall Lv. 2", Capacity = 9000, Attack = 11, Defense = 11, Jackpot = 1200, MaxOutput = 440  } },
            { 280, new Stall{Level = 23, Name = "Großer Stall Lv. 3", Capacity = 9400, Attack = 12, Defense = 12, Jackpot = 1250, MaxOutput = 480  } },
            { 320, new Stall{Level = 24, Name = "Großer Stall Lv. 4", Capacity = 9800, Attack = 12, Defense = 12, Jackpot = 1300, MaxOutput = 520  } },
            { 360, new Stall{Level = 25, Name = "Großer Stall Lv. 5", Capacity = 12000, Attack = 13, Defense = 13, Jackpot = 1350, MaxOutput = 560  } },
            { 400, new Stall{Level = 26, Name = "Ziegenhof", Capacity = 30000, Attack = 15, Defense = 15, Jackpot = 1500, MaxOutput = 600  } }
        };

        public static Dictionary<int, int> exp = new Dictionary<int, int> {
            { 0, 0 },
            { 1, 300 },
            { 2, 600 },
            { 3, 1200 },
            { 4, 1500 },
            { 5, 1800 },
            { 6, 2500 },
            { 7, 3200 },
            { 8, 3900 },
            { 9, 4600 },
            { 10, 5300 },
            { 11, 6500 },
            { 12, 7700 },
            { 13, 8900 },
            { 14, 10100 },
            { 15, 11300 },
            { 16, 13000 },
            { 17, 14700 },
            { 18, 16400 },
            { 19, 18100 },
            { 20, 20400 },
            { 21, 22700 },
            { 22, 25000 },
            { 23, 27300 },
            { 24, 29600 },
            { 25, 33900 },
            { 26, 38200 },
            { 27, 42500 },
            { 28, 46800 },
            { 29, 51100 },
            { 30, 60900 },
            { 31, 70700 },
            { 32, 80500 },
            { 33, 90300 },
            { 34, 100100 },
            { 35, 126900 },
            { 36, 153700 },
            { 37, 180500 },
            { 38, 207300 },
            { 39, 234100 },
            { 40, 264900 },
            { 41, 295700 },
            { 42, 326500 },
            { 43, 357300 },
            { 44, 388100 },
            { 45, 428900 },
            { 46, 469700 },
            { 47, 510500 },
            { 48, 551300 },
            { 49, 592100 },
            { 50, 658900 },
            { 51, 725700 },
            { 52, 792500 },
            { 53, 859300 },
            { 54, 926100 },
            { 55, 1062900 },
            { 56, 1201700 },
            { 57, 1342500 },
            { 58, 1481300 },
            { 59, 1620100 },
            { 60, 1762900 },
            { 61, 1905700 },
            { 62, 2048500 },
            { 63, 2191300 },
            { 64, 2334100 },
            { 65, 2491900 },
            { 66, 2649700 },
            { 67, 2807500 },
            { 68, 2965300 },
            { 69, 3123100 },
            { 70, 3314900 },
            { 71, 3506700 },
            { 72, 3698500 },
            { 73, 3890300 },
            { 74, 4082100 },
            { 75, 4345900 },
            { 76, 4609700 },
            { 77, 4873500 },
            { 78, 5137300 },
            { 79, 5401100 },
            { 80, 5664900 }
        };

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

        public static bool IsFull (int goats, int wins)
        {
            var stall = GetStall(wins);
            if (stall.Capacity < goats)
                return true;
            return false;
        }

        public static uint GetLevel(int? myExp)
        {
            var level = exp.Where(y => y.Value <= myExp).Max(x => x.Key);
            return (uint)level;
        }

        public static uint GetEXP(int? level)
        {
            var myExp = exp.Where(y => y.Key <= level).Max(x => x.Value);
            return (uint)myExp;
        }

        public static string MessageReplace(string myMessage)
        {
            myMessage = new Regex("(https|http)[:\\/a-zA-Z0-9-Z.?!=#%&_+-;]*").Replace(myMessage, ""); //Edit Links
            myMessage = new Regex("<:[a-zA-Z0-9]*:[0-9]{18,18}>").Replace(myMessage, ""); //Edit custom images
            myMessage = new Regex("<a:[a-zA-Z0-9]*:[0-9]{18,18}>").Replace(myMessage, ""); //Edit custom animated images
            myMessage = new Regex("<@[0-9!]{18,19}>").Replace(myMessage, ""); //Edit tags
            myMessage = new Regex("[\\s]{2,}").Replace(myMessage, " "); //Edit every multiple whitespace type to a single whitespace
            return myMessage;
        }

        public static string ReplaceCharacter(string myString)
        {
            myString = myString.Replace(" ", string.Empty);
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
