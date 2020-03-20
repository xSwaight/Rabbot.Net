using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Rabbot.Database;
using Serilog;
using Serilog.Core;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Rabbot.Services
{
    class WarnService
    {
        private SocketGuildUser DcTargetUser { get; set; }
        private SocketGuild DcGuild { get; set; }
        private DiscordSocketClient DcClient { get; set; }

        private static readonly ILogger _logger = Log.ForContext(Serilog.Core.Constants.SourceContextPropertyName, nameof(WarnService));

        public WarnService(DiscordSocketClient client)
        {
            DcClient = client;
        }

        public async Task CheckWarnings()
        {
            using (swaightContext db = new swaightContext())
            {
                if (!db.Warning.Any())
                    return;

                var warnings = db.Warning.ToList();
                foreach (var warn in warnings)
                {
                    if (warn.ActiveUntil < DateTime.Now)
                    {
                        db.Warning.Remove(warn);
                        await db.SaveChangesAsync();
                        continue;
                    }
                    if (warn.Counter >= 3)
                    {
                        DcGuild = DcClient.Guilds.FirstOrDefault(p => p.Id == (ulong)warn.ServerId);
                        DcTargetUser = DcGuild.Users.FirstOrDefault(p => p.Id == (ulong)warn.UserId);
                        var dbUser = db.Userfeatures.FirstOrDefault(p => p.ServerId == warn.ServerId && p.UserId == warn.UserId);
                        MuteService mute = new MuteService(DcClient);
                        await mute.MuteWarnedUser(DcTargetUser, DcGuild);
                        if (dbUser != null)
                        {
                            if (dbUser.Goats >= 100)
                                dbUser.Goats -= 100;
                            else
                                dbUser.Goats = 0;
                        }
                        db.Warning.Remove(warn);
                        await db.SaveChangesAsync();
                        continue;
                    }
                }
            }
        }

        public async Task Warn(IUser user, SocketCommandContext Context)
        {
            using (swaightContext db = new swaightContext())
            {
                var warn = db.Warning.FirstOrDefault(p => p.UserId == user.Id && p.ServerId == Context.Guild.Id) ?? db.Warning.AddAsync(new Warning { ServerId = Context.Guild.Id, UserId = user.Id, ActiveUntil = DateTime.Now.AddHours(1), Counter = 0 }).Result.Entity;
                warn.Counter++;
                if (warn.Counter > 3)
                    return;
                await Context.Message.Channel.SendMessageAsync($"**{user.Mention} du wurdest für schlechtes Benehmen verwarnt. Warnung {warn.Counter}/3**");
                await Logging.Warn(user, Context);
                await db.SaveChangesAsync();
            }
        }
    }
}
