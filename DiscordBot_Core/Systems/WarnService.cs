using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBot_Core.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot_Core.Systems
{
    class WarnService
    {
        private SocketGuildUser DcTargetUser { get; set; }
        private SocketGuild DcGuild { get; set; }
        private DiscordSocketClient DcClient { get; set; }
        private User User { get; set; }
        private Guild Guild { get; set; }
        private Muteduser MuteUser { get; set; }
        private SocketRole MutedRole { get; set; }

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
                    if(warn.Counter >= 3)
                    {
                        DcGuild = DcClient.Guilds.Where(p => p.Id == (ulong)warn.ServerId).FirstOrDefault();
                        DcTargetUser = DcGuild.Users.Where(p => p.Id == (ulong)warn.UserId).FirstOrDefault();
                        MuteService mute = new MuteService(DcClient);
                        await mute.MuteWarnedUser(DcTargetUser, DcGuild);
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
                if (!db.Warning.Where(p => p.UserId == (long)user.Id && p.ServerId == (long)Context.Guild.Id).Any())
                {
                    await db.Warning.AddAsync(new Warning { ServerId = (long)Context.Guild.Id, UserId = (long)user.Id, ActiveUntil = DateTime.Now.AddHours(1), Counter = 1 });
                    await Context.Channel.SendMessageAsync($"**{user.Mention} du wurdest für schlechtes Benehmen verwarnt. Warnung 1/3**");
                }
                else
                {
                    var warn = db.Warning.Where(p => p.UserId == (long)user.Id && p.ServerId == (long)Context.Guild.Id).FirstOrDefault();
                    warn.Counter++;
                    await Context.Channel.SendMessageAsync($"**{user.Mention} du wurdest für schlechtes Benehmen verwarnt. Warnung {warn.Counter}/3**");
                }
                await Log.Warn(user, Context);
                await db.SaveChangesAsync();
            }
        }
    }
}
