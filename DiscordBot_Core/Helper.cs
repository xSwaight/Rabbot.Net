using Discord.Commands;
using Discord.WebSocket;
using DiscordBot_Core.Database;
using System;
using System.Text;
using System.Linq;

namespace DiscordBot_Core
{
    public static class Helper
    {
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
            uint level = (uint)Math.Sqrt((uint)exp / Config.level.expTableValue);
            return level;
        }

        public static uint GetExp(uint level)
        {
            var exp = Math.Pow(level, 2) * Config.level.expTableValue;
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
    }
}
