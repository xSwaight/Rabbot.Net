using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Rabbot.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rabbot
{
    public static class Logging
    {
        public static async Task Unmuted(SocketCommandContext context, IUser user)
        {
            using (swaightContext db = new swaightContext())
            {
                var Guild = db.Guild.FirstOrDefault(p => p.ServerId == context.Guild.Id);
                if (Guild.LogchannelId != null && Guild.Log == 1)
                {
                    var logchannel = context.Guild.TextChannels.FirstOrDefault(p => p.Id == (ulong)Guild.LogchannelId);
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
                var Guild = db.Guild.FirstOrDefault(p => p.ServerId == user.Guild.Id);
                if (Guild.LogchannelId != null && Guild.Log == 1)
                {
                    var embed = new EmbedBuilder();
                    embed.WithDescription($"{user.Mention} wurde automatisch entmuted.");
                    embed.WithColor(new Color(0, 255, 0));
                    var logchannel = user.Guild.TextChannels.FirstOrDefault(p => p.Id == (ulong)Guild.LogchannelId);
                    await logchannel.SendMessageAsync("", false, embed.Build());
                }
            }
        }

        public static async Task Mute(SocketCommandContext context, IUser user, string duration)
        {
            using (swaightContext db = new swaightContext())
            {
                var Guild = db.Guild.FirstOrDefault(p => p.ServerId == context.Guild.Id);
                if (Guild.LogchannelId != null && Guild.Log == 1)
                {
                    var logchannel = context.Guild.TextChannels.FirstOrDefault(p => p.Id == (ulong)Guild.LogchannelId);
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
                var Guild = db.Guild.FirstOrDefault(p => p.ServerId == context.Guild.Id);
                if (Guild.LogchannelId != null && Guild.Log == 1)
                {
                    var logchannel = context.Guild.TextChannels.FirstOrDefault(p => p.Id == (ulong)Guild.LogchannelId);
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
                var Guild = db.Guild.FirstOrDefault(p => p.ServerId == context.Guild.Id);
                if (Guild.LogchannelId != null && Guild.Log == 1)
                {
                    var logchannel = context.Guild.TextChannels.FirstOrDefault(p => p.Id == (ulong)Guild.LogchannelId);
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
                var Guild = db.Guild.FirstOrDefault(p => p.ServerId == context.Guild.Id);
                if (Guild.LogchannelId != null && Guild.Log == 1)
                {
                    var channelId = db.Guild.FirstOrDefault(p => p.ServerId == context.Guild.Id).LogchannelId;
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
                var Guild = db.Guild.FirstOrDefault(p => p.ServerId == context.Guild.Id);
                if (Guild.LogchannelId != null && Guild.Log == 1)
                {
                    var logchannel = context.Guild.TextChannels.FirstOrDefault(p => p.Id == (ulong)Guild.LogchannelId);
                    var embed = new EmbedBuilder();
                    embed.WithDescription($"{context.User.Mention} hat sich die S4 League Rolle gegeben.");
                    embed.WithColor(new Color(0, 255, 0));
                    await logchannel.SendMessageAsync("", false, embed.Build());
                }
            }
        }

        public static async Task PsbatRole(SocketCommandContext context)
        {
            using (swaightContext db = new swaightContext())
            {
                var Guild = db.Guild.FirstOrDefault(p => p.ServerId == context.Guild.Id);
                if (Guild.LogchannelId != null && Guild.Log == 1)
                {
                    var logchannel = context.Guild.TextChannels.FirstOrDefault(p => p.Id == (ulong)Guild.LogchannelId);
                    var embed = new EmbedBuilder();
                    embed.WithDescription($"{context.User.Mention} hat sich die PS & Bat Rolle gegeben.");
                    embed.WithColor(new Color(0, 255, 0));
                    await logchannel.SendMessageAsync("", false, embed.Build());
                }
            }
        }

        public static async Task WarningMute(SocketGuildUser user)
        {
            using (swaightContext db = new swaightContext())
            {
                var Guild = db.Guild.FirstOrDefault(p => p.ServerId == user.Guild.Id);
                if (Guild.LogchannelId != null && Guild.Log == 1)
                {
                    var logchannel = user.Guild.TextChannels.FirstOrDefault(p => p.Id == (ulong)Guild.LogchannelId);
                    var embed = new EmbedBuilder();
                    embed.WithDescription($"{user.Mention} wurde aufgrund von 3 Warnings für 1 Stunde gemuted.");
                    embed.WithColor(new Color(255, 0, 0));
                    await logchannel.SendMessageAsync("", false, embed.Build());
                }
            }
        }

        public static async Task Warning(SocketGuildUser user, SocketMessage msg, string badword)
        {
            using (swaightContext db = new swaightContext())
            {
                var Guild = db.Guild.FirstOrDefault(p => p.ServerId == user.Guild.Id);
                if (Guild.LogchannelId != null && Guild.Log == 1)
                {
                    var channelId = db.Guild.FirstOrDefault(p => p.ServerId == user.Guild.Id).LogchannelId;
                    var embed = new EmbedBuilder();
                    embed.WithDescription($"{user.Mention} wurde aufgrund folgender Nachricht verwarnt!");
                    embed.WithColor(new Color(255, 0, 0));
                    embed.AddField("Message", msg.Content.ToString(), false);
                    embed.AddField("Badword", badword, false);
                    await user.Guild.GetTextChannel((ulong)channelId).SendMessageAsync("", false, embed.Build());
                }
            }
        }

        public static async Task CooldownMute(ICommandContext context)
        {
            using (swaightContext db = new swaightContext())
            {
                var Guild = db.Guild.FirstOrDefault(p => p.ServerId == context.Guild.Id);
                if (Guild.LogchannelId != null && Guild.Log == 1)
                {
                    var channelId = db.Guild.FirstOrDefault(p => p.ServerId == context.Guild.Id).LogchannelId;
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
                var Guild = db.Guild.FirstOrDefault(p => p.ServerId == context.Guild.Id);
                if (Guild.LogchannelId != null && Guild.Log == 1)
                {
                    var channelId = db.Guild.FirstOrDefault(p => p.ServerId == context.Guild.Id).LogchannelId;
                    var embed = new EmbedBuilder();
                    embed.WithDescription($"{context.User.Mention} wurde aufgrund von Spam für **10 Minuten** gemuted!");
                    embed.WithColor(new Color(255, 0, 0));
                    await (context.Guild as SocketGuild).GetTextChannel((ulong)channelId).SendMessageAsync("", false, embed.Build());
                }
            }
        }
    }
}
