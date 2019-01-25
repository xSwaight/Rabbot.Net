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

        public static string replaceCharacter(string myString)
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

        public static async Task SendLogUnmuted(SocketGuild guild, IUser user)
        {
            using (swaightContext db = new swaightContext())
            {
                var Guild = db.Guild.Where(p => p.ServerId == (long)guild.Id).FirstOrDefault();
                if (Guild.LogchannelId != null && Guild.Log == 1)
                {
                    var logchannel = guild.TextChannels.Where(p => p.Id == (ulong)Guild.LogchannelId).FirstOrDefault();
                    var embed = new EmbedBuilder();
                    embed.WithDescription($"{user.Mention} wurde unmuted.");
                    embed.WithColor(new Color(0, 255, 0));
                    await logchannel.SendMessageAsync("", false, embed.Build());
                }
            }
        }

        public static async Task SendLogUnmuted(SocketGuildUser user)
        {
            using (swaightContext db = new swaightContext())
            {
                var Guild = db.Guild.Where(p => p.ServerId == (long)user.Guild.Id).FirstOrDefault();
                if (Guild.LogchannelId != null && Guild.Log == 1)
                {
                    var embed = new EmbedBuilder();
                    embed.WithDescription($"{user.Mention} wurde entmuted.");
                    embed.WithColor(new Color(0, 255, 0));
                    var logchannel = user.Guild.TextChannels.Where(p => p.Id == (ulong)Guild.LogchannelId).FirstOrDefault();
                    await logchannel.SendMessageAsync("", false, embed.Build());
                }
            }
        }

        public static async Task SendLogMute(SocketCommandContext context, IUser user, string duration)
        {
            using (swaightContext db = new swaightContext())
            {
                var Guild = db.Guild.Where(p => p.ServerId == (long)context.Guild.Id).FirstOrDefault();
                if (Guild.LogchannelId != null && Guild.Log == 1)
                {
                    var logchannel = context.Guild.TextChannels.Where(p => p.Id == (ulong)Guild.LogchannelId).FirstOrDefault();
                    var embed = new EmbedBuilder();
                    embed.WithDescription($"{context.User.Username} hat {user.Mention} für {duration} gemuted.");
                    embed.WithColor(new Color(255, 0, 0));
                    await logchannel.SendMessageAsync("", false, embed.Build());
                }
            }
        }

        public static async Task SendLogDelete(SocketCommandContext context, int amount)
        {
            using (swaightContext db = new swaightContext())
            {
                var Guild = db.Guild.Where(p => p.ServerId == (long)context.Guild.Id).FirstOrDefault();
                if (Guild.LogchannelId != null && Guild.Log == 1)
                {
                    var logchannel = context.Guild.TextChannels.Where(p => p.Id == (ulong)Guild.LogchannelId).FirstOrDefault();
                    var logEmbed = new EmbedBuilder();
                    logEmbed.WithDescription($"{context.User.Username} hat die letzten {amount} Nachrichten in {(context.Channel as ITextChannel).Mention} gelöscht.");
                    logEmbed.WithColor(new Color(255, 0, 0));
                    await logchannel.SendMessageAsync("", false, logEmbed.Build());
                }
            }
        }

        public static async Task SendLogDelete(IUser user, SocketCommandContext context, int amount)
        {
            using (swaightContext db = new swaightContext())
            {
                var Guild = db.Guild.Where(p => p.ServerId == (long)context.Guild.Id).FirstOrDefault();
                if (Guild.LogchannelId != null && Guild.Log == 1)
                {
                    var logchannel = context.Guild.TextChannels.Where(p => p.Id == (ulong)Guild.LogchannelId).FirstOrDefault();
                    var logEmbed = new EmbedBuilder();
                    logEmbed.WithDescription($"{context.User.Username} hat die letzten {amount} Nachrichten von {user.Mention} in {(context.Channel as ITextChannel).Mention} gelöscht und {100 * amount} EXP abgezogen.");
                    logEmbed.WithColor(new Color(255, 0, 0));
                    await logchannel.SendMessageAsync("", false, logEmbed.Build());
                }
            }
        }

        public static async Task SendLogWarn(IUser user, SocketCommandContext context)
        {
            using (swaightContext db = new swaightContext())
            {
                var Guild = db.Guild.Where(p => p.ServerId == (long)context.Guild.Id).FirstOrDefault();
                if (Guild.LogchannelId != null && Guild.Log == 1)
                {
                    var channelId = db.Guild.Where(p => p.ServerId == (long)context.Guild.Id).FirstOrDefault().LogchannelId;
                    var embed = new EmbedBuilder();
                    embed.WithDescription($"{user.Mention} wurde von {context.User.Username} verwarnt!");
                    embed.WithColor(new Color(255, 0, 0));
                    embed.AddField("Time", DateTime.Now.ToShortTimeString(), false);
                    embed.ThumbnailUrl = user.GetAvatarUrl(ImageFormat.Auto, 1024);
                    await context.Guild.GetTextChannel((ulong)channelId).SendMessageAsync("", false, embed.Build());
                }
            }
        }

        public static async Task SendLogS4Role(SocketCommandContext context)
        {
            using (swaightContext db = new swaightContext())
            {
                var Guild = db.Guild.Where(p => p.ServerId == (long)context.Guild.Id).FirstOrDefault();
                if (Guild.LogchannelId != null && Guild.Log == 1)
                {
                    var logchannel = context.Guild.TextChannels.Where(p => p.Id == (ulong)Guild.LogchannelId).FirstOrDefault();
                    var embed = new EmbedBuilder();
                    embed.WithDescription($"{context.User.Mention} hat sich die S4 League Rolle gegeben.");
                    embed.WithColor(new Color(0, 255, 0));
                    await logchannel.SendMessageAsync("", false, embed.Build());
                }
            }
        }

        public static async Task SendLogWarningMute(SocketGuildUser user)
        {
            using (swaightContext db = new swaightContext())
            {
                var Guild = db.Guild.Where(p => p.ServerId == (long)user.Guild.Id).FirstOrDefault();
                if (Guild.LogchannelId != null && Guild.Log == 1)
                {
                    var logchannel = user.Guild.TextChannels.Where(p => p.Id == (ulong)Guild.LogchannelId).FirstOrDefault();
                    var embed = new EmbedBuilder();
                    embed.WithDescription($"{user.Mention} wurde aufgrund von 3 Warnings für 1 Stunde gemuted.");
                    embed.WithColor(new Color(255, 0, 0));
                    await logchannel.SendMessageAsync("", false, embed.Build());
                }
            }
        }

        public static async Task SendLogWarning(SocketGuildUser user, SocketMessage msg)
        {
            using (swaightContext db = new swaightContext())
            {
                var Guild = db.Guild.Where(p => p.ServerId == (long)user.Guild.Id).FirstOrDefault();
                if (Guild.LogchannelId != null && Guild.Log == 1)
                {
                    var channelId = db.Guild.Where(p => p.ServerId == (long)user.Guild.Id).FirstOrDefault().LogchannelId;
                    var embed = new EmbedBuilder();
                    embed.WithDescription($"{user.Mention} wurde aufgrund folgender Nachricht verwarnt!");
                    embed.WithColor(new Color(255, 0, 0));
                    embed.AddField("Message", msg.Content.ToString(), false);
                    embed.AddField("Time", msg.CreatedAt.DateTime.ToShortTimeString(), false);
                    embed.ThumbnailUrl = user.GetAvatarUrl(ImageFormat.Auto, 1024);
                    await user.Guild.GetTextChannel((ulong)channelId).SendMessageAsync("", false, embed.Build());
                }
            }
        }

        public static async Task SendLogCooldownMute(ICommandContext context)
        {
            using (swaightContext db = new swaightContext())
            {
                var Guild = db.Guild.Where(p => p.ServerId == (long)context.Guild.Id).FirstOrDefault();
                if (Guild.LogchannelId != null && Guild.Log == 1)
                {
                    var channelId = db.Guild.Where(p => p.ServerId == (long)context.Guild.Id).FirstOrDefault().LogchannelId;
                    var embed = new EmbedBuilder();
                    embed.WithDescription($"{context.User.Mention} wurde aufgrund von Cooldownspam für **10 Minuten** gemuted!");
                    embed.WithColor(new Color(255, 0, 0));
                    await (context.Guild as SocketGuild).GetTextChannel((ulong)channelId).SendMessageAsync("", false, embed.Build());
                }
            }
        }

        public static async Task SendLogBotCommandMute(ICommandContext context)
        {
            using (swaightContext db = new swaightContext())
            {
                var Guild = db.Guild.Where(p => p.ServerId == (long)context.Guild.Id).FirstOrDefault();
                if (Guild.LogchannelId != null && Guild.Log == 1)
                {
                    var channelId = db.Guild.Where(p => p.ServerId == (long)context.Guild.Id).FirstOrDefault().LogchannelId;
                    var embed = new EmbedBuilder();
                    embed.WithDescription($"{context.User.Mention} wurde aufgrund von Spam für **10 Minuten** gemuted!");
                    embed.WithColor(new Color(255, 0, 0));
                    await (context.Guild as SocketGuild).GetTextChannel((ulong)channelId).SendMessageAsync("", false, embed.Build());
                }
            }
        }
    }
}
