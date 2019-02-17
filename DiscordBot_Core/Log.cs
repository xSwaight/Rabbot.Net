using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBot_Core.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot_Core
{
    public static class Log
    {
        public static async Task Unmuted(SocketCommandContext context, IUser user)
        {
            using (swaightContext db = new swaightContext())
            {
                var Guild = db.Guild.Where(p => p.ServerId == (long)context.Guild.Id).FirstOrDefault();
                if (Guild.LogchannelId != null && Guild.Log == 1)
                {
                    var logchannel = context.Guild.TextChannels.Where(p => p.Id == (ulong)Guild.LogchannelId).FirstOrDefault();
                    var embed = new EmbedBuilder();
                    embed.WithDescription($"{context.User.Username} hat {user.Mention} unmuted.");
                    embed.WithColor(new Color(0, 255, 0));
                    await logchannel.SendMessageAsync("", false, embed.Build());
                }
            }
        }

        public static async Task Unmuted(SocketGuildUser user)
        {
            using (swaightContext db = new swaightContext())
            {
                var Guild = db.Guild.Where(p => p.ServerId == (long)user.Guild.Id).FirstOrDefault();
                if (Guild.LogchannelId != null && Guild.Log == 1)
                {
                    var embed = new EmbedBuilder();
                    embed.WithDescription($"{user.Mention} wurde automatisch entmuted.");
                    embed.WithColor(new Color(0, 255, 0));
                    var logchannel = user.Guild.TextChannels.Where(p => p.Id == (ulong)Guild.LogchannelId).FirstOrDefault();
                    await logchannel.SendMessageAsync("", false, embed.Build());
                }
            }
        }

        public static async Task Mute(SocketCommandContext context, IUser user, string duration)
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


        public static async Task Delete(SocketCommandContext context, int amount)
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

        public static async Task Delete(IUser user, SocketCommandContext context, int amount)
        {
            using (swaightContext db = new swaightContext())
            {
                var Guild = db.Guild.Where(p => p.ServerId == (long)context.Guild.Id).FirstOrDefault();
                if (Guild.LogchannelId != null && Guild.Log == 1)
                {
                    var logchannel = context.Guild.TextChannels.Where(p => p.Id == (ulong)Guild.LogchannelId).FirstOrDefault();
                    var logEmbed = new EmbedBuilder();
                    logEmbed.WithDescription($"{context.User.Username} hat die letzten {amount} Nachrichten von {user.Mention} in {(context.Channel as ITextChannel).Mention} gelöscht und {150 * amount} EXP abgezogen.");
                    logEmbed.WithColor(new Color(255, 0, 0));
                    await logchannel.SendMessageAsync("", false, logEmbed.Build());
                }
            }
        }

        public static async Task Warn(IUser user, SocketCommandContext context)
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
                    await context.Guild.GetTextChannel((ulong)channelId).SendMessageAsync("", false, embed.Build());
                }
            }
        }

        public static async Task S4Role(SocketCommandContext context)
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

        public static async Task WarningMute(SocketGuildUser user)
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

        public static async Task Warning(SocketGuildUser user, SocketMessage msg)
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
                    await user.Guild.GetTextChannel((ulong)channelId).SendMessageAsync("", false, embed.Build());
                }
            }
        }

        public static async Task CooldownMute(ICommandContext context)
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

        public static async Task BotCommandMute(ICommandContext context)
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
