using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Rabbot.Database;
using Rabbot.Database.Rabbot;
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

        public WarnService(IServiceProvider services)
        {
            _muteService = services.GetRequiredService<MuteService>();
            _client = services.GetRequiredService<DiscordSocketClient>();
        }

        public async Task CheckWarnings(RabbotContext db)
        {
            if (!db.Warnings.Any())
                return;

            var warnings = db.Warnings.ToList();
            foreach (var warn in warnings)
            {
                if (warn.Until < DateTime.Now)
                {
                    db.Warnings.Remove(warn);
                    await db.SaveChangesAsync();
                    continue;
                }
                if (warn.Counter >= 3)
                {
                    var dcGuild = _client.Guilds.FirstOrDefault(p => p.Id == warn.GuildId);
                    var dcTargetUser = dcGuild.Users.FirstOrDefault(p => p.Id == warn.UserId);
                    var dbUser = db.Features.FirstOrDefault(p => p.GuildId == warn.GuildId && p.UserId == warn.UserId);
                    await _muteService.MuteWarnedUser(db, dcTargetUser, dcGuild);
                    if (dbUser != null)
                    {
                        if (dbUser.Goats >= 100)
                            dbUser.Goats -= 100;
                        else
                            dbUser.Goats = 0;
                    }
                    db.Warnings.Remove(warn);
                    await db.SaveChangesAsync();
                    continue;
                }
            }
        }

        public async Task Warn(RabbotContext db, IUser user, SocketCommandContext Context)
        {
            var warn = db.Warnings.FirstOrDefault(p => p.UserId == user.Id && p.GuildId == Context.Guild.Id) ?? db.Warnings.AddAsync(new WarningEntity { GuildId = Context.Guild.Id, UserId = user.Id, Until = DateTime.Now.AddHours(1), Counter = 0 }).Result.Entity;
            warn.Counter++;
            if (warn.Counter > 3)
                return;
            await Context.Message.Channel.SendMessageAsync($"**{user.Mention} du wurdest für schlechtes Benehmen verwarnt. Warnung {warn.Counter}/3**");
            await Logging.Warn(user, Context);
            await db.SaveChangesAsync();
        }

        public async Task AutoWarn(RabbotContext db, SocketMessage msg)
        {
            var myUser = msg.Author as SocketGuildUser;
            if (db.MutedUsers.AsQueryable().Where(p => p.UserId == msg.Author.Id && p.GuildId == myUser.Guild.Id).Any())
                return;
            if (myUser.Roles.Where(p => p.Name == "Muted").Any())
                return;
            var warn = db.Warnings.FirstOrDefault(p => p.UserId == msg.Author.Id && p.GuildId == myUser.Guild.Id) ?? db.Warnings.AddAsync(new WarningEntity { GuildId = myUser.Guild.Id, UserId = msg.Author.Id, Until = DateTime.Now.AddHours(1), Counter = 0 }).Result.Entity;
            warn.Counter++;
            if (warn.Counter > 3)
                return;
            await msg.Channel.SendMessageAsync($"**{msg.Author.Mention} du wurdest für schlechtes Benehmen verwarnt. Warnung {warn.Counter}/3**");
            var badword = db.BadWords.FirstOrDefault(p => Helper.ReplaceCharacter(msg.Content).Contains(p.BadWord, StringComparison.OrdinalIgnoreCase) && p.GuildId == myUser.Guild.Id).BadWord;
            await Logging.Warning(myUser, msg, badword);
        }
    }
}
