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
    public class WarnService
    {
        private readonly DiscordSocketClient _client;
        private readonly MuteService _muteService;

        private static readonly ILogger _logger = Log.ForContext(Serilog.Core.Constants.SourceContextPropertyName, nameof(WarnService));

        public WarnService(DiscordSocketClient client, MuteService muteService)
        {
            _muteService = muteService;
            _client = client;
        }

        public async Task CheckWarnings(swaightContext db)
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
                    var dcGuild = _client.Guilds.FirstOrDefault(p => p.Id == (ulong)warn.ServerId);
                    var dcTargetUser = dcGuild.Users.FirstOrDefault(p => p.Id == (ulong)warn.UserId);
                    var dbUser = db.Userfeatures.FirstOrDefault(p => p.ServerId == warn.ServerId && p.UserId == warn.UserId);
                    await _muteService.MuteWarnedUser(db, dcTargetUser, dcGuild);
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

        public async Task Warn(swaightContext db, IUser user, SocketCommandContext Context)
        {
            var warn = db.Warning.FirstOrDefault(p => p.UserId == user.Id && p.ServerId == Context.Guild.Id) ?? db.Warning.AddAsync(new Warning { ServerId = Context.Guild.Id, UserId = user.Id, ActiveUntil = DateTime.Now.AddHours(1), Counter = 0 }).Result.Entity;
            warn.Counter++;
            if (warn.Counter > 3)
                return;
            await Context.Message.Channel.SendMessageAsync($"**{user.Mention} du wurdest für schlechtes Benehmen verwarnt. Warnung {warn.Counter}/3**");
            await Logging.Warn(user, Context);
            await db.SaveChangesAsync();
        }

        public async Task AutoWarn(swaightContext db, SocketMessage msg)
        {
            var myUser = msg.Author as SocketGuildUser;
            if (db.Muteduser.Where(p => p.UserId == msg.Author.Id && p.ServerId == myUser.Guild.Id).Any())
                return;
            if (myUser.Roles.Where(p => p.Name == "Muted").Any())
                return;
            var warn = db.Warning.FirstOrDefault(p => p.UserId == msg.Author.Id && p.ServerId == myUser.Guild.Id) ?? db.Warning.AddAsync(new Warning { ServerId = myUser.Guild.Id, UserId = msg.Author.Id, ActiveUntil = DateTime.Now.AddHours(1), Counter = 0 }).Result.Entity;
            warn.Counter++;
            if (warn.Counter > 3)
                return;
            await msg.Channel.SendMessageAsync($"**{msg.Author.Mention} du wurdest für schlechtes Benehmen verwarnt. Warnung {warn.Counter}/3**");
            var badword = db.Badwords.FirstOrDefault(p => Helper.ReplaceCharacter(msg.Content).Contains(p.BadWord, StringComparison.OrdinalIgnoreCase) && p.ServerId == myUser.Guild.Id).BadWord;
            await Logging.Warning(myUser, msg, badword);
        }
    }
}
