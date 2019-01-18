using Discord.Commands;
using Discord.WebSocket;
using DiscordBot_Core.Database;
using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace DiscordBot_Core
{
    public static class Helper
    {

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
            { 56, 1203700 },
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

        public static uint GetLevel(int? exp)
        {
            //double level = Math.Log((double)exp) * Math.Sqrt((double)(exp / 16000));
            uint level = (uint)Math.Sqrt((uint)exp / Config.level.expTableValue);
            return (uint)level;
        }

        public static uint GetExp(uint level)
        {
            var exp = Math.Pow(level, 2) * Config.level.expTableValue;
            //double exp = Math.Pow(Math.E, (level / Math.Sqrt(16000)));
            return (uint)exp;
        }

        public static ulong? GetBotChannel(ICommandContext context)
        {
            using (discordbotContext db = new discordbotContext())
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

        public static uint GetS4Level(int? myExp)
        {
            var level = exp.Where(y => y.Value <= myExp).Max(x => x.Key);
            return (uint)level;
        }

        public static uint GetS4EXP(int? level)
        {
            var myExp = exp.Where(y => y.Key <= level).Max(x => x.Value);
            return (uint)myExp;
        }
    }
}
