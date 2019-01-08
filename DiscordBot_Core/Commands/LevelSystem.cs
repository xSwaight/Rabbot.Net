using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using DiscordBot_Core.Database;
using DiscordBot_Core.ImageGenerator;
using Microsoft.EntityFrameworkCore;

namespace DiscordBot_Core.Commands
{
    public class LevelSystem : ModuleBase<SocketCommandContext>
    {

        [Command("level")]
        public async Task Level(IGuildUser user = null)
        {
            using (discordbotContext db = new discordbotContext())
            {
                if (user != null)
                {
                    var exp = db.Experience.Where(p => p.UserId == (long)user.Id).FirstOrDefault();
                    if (exp != null)
                    {
                        uint level = Helper.GetLevel(exp.Exp);
                        var neededExp1 = Helper.GetExp(level);
                        var neededExp2 = Helper.GetExp(level + 1);
                        var currentExp = exp.Exp - Helper.GetExp(level);
                        int totalExp = (int)exp.Exp;
                        int currentLevelExp = (int)currentExp;
                        int neededLevelExp = (int)neededExp2 - (int)neededExp1;
                        double dblPercent = ((double)currentLevelExp / (double)neededLevelExp) * 100;
                        int percent = (int)dblPercent;
                        await Context.Channel.SendMessageAsync($"{user.Nickname} ist **Level {level}** mit **{totalExp.ToString("N0")} EXP** und hat bereits **{currentLevelExp.ToString("N0")} | {neededLevelExp.ToString("N0")} EXP ({percent}%)**");
                    }
                    else
                    {
                        await Context.Channel.SendMessageAsync($"{user.Nickname} hat keine EXP!");
                    }
                }
                else
                {
                    var exp = db.Experience.Where(p => p.UserId == (long)Context.User.Id).FirstOrDefault();
                    if (exp != null)
                    {
                        uint level = Helper.GetLevel(exp.Exp);
                        var neededExp1 = Helper.GetExp(level);
                        var neededExp2 = Helper.GetExp(level + 1);
                        var currentExp = exp.Exp - Helper.GetExp(level);
                        int totalExp = (int)exp.Exp;
                        int currentLevelExp = (int)currentExp;
                        int neededLevelExp = (int)neededExp2 - (int)neededExp1;
                        double dblPercent =  ((double)currentLevelExp / (double)neededLevelExp) * 100;
                        int percent = (int)dblPercent;
                        await Context.Channel.SendMessageAsync($"{Context.User.Mention} du bist **Level {level}** mit **{totalExp.ToString("N0")} EXP** und hast bereits **{currentLevelExp.ToString("N0")} | {neededLevelExp.ToString("N0")} EXP ({percent}%)**");
                    }
                    else
                    {
                        await Context.Channel.SendMessageAsync($"{Context.User.Mention} du hast keine EXP!");
                    }
                }
            }
        }

        [Command("ranking")]
        public async Task Ranking()
        {
            using (discordbotContext db = new discordbotContext())
            {
                var top10 = db.Experience.OrderByDescending(p => p.Exp).Take(10);
                EmbedBuilder embed = new EmbedBuilder();
                embed.Description = "Level Ranking";
                embed.WithColor(new Color(239, 220, 7));
                int i = 1;
                foreach (var top in top10)
                {
                    uint level = Helper.GetLevel(top.Exp);
                    var user = db.User.Where(p => p.Id == top.UserId).FirstOrDefault();
                    int exp = (int)top.Exp;
                    embed.AddField("Platz " + i, $"**{user.Name}** \nLevel {level} ({exp.ToString("N0")} EXP)");
                    i++;
                }
                await Context.Channel.SendMessageAsync(null, false, embed.Build());
            }
        }


        [Command("setupLevels")]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        public async Task SetupLevel()
        {
            await Context.Message.DeleteAsync();
            var roleS4 = Context.Guild.Roles.Where(p => p.Name == "S4").FirstOrDefault();
            var rolePro = Context.Guild.Roles.Where(p => p.Name == "Pro").FirstOrDefault();
            var roleSemi = Context.Guild.Roles.Where(p => p.Name == "Semi").FirstOrDefault();
            var roleAmateur = Context.Guild.Roles.Where(p => p.Name == "Amateur").FirstOrDefault();
            var roleRookie = Context.Guild.Roles.Where(p => p.Name == "Rookie").FirstOrDefault();

            if (roleS4 == null)
                await Context.Guild.CreateRoleAsync("S4", null, new Color(239, 69, 50), true);
            if (rolePro == null)
                await Context.Guild.CreateRoleAsync("Pro", null, new Color(94, 137, 255), true);
            if (roleSemi == null)
                await Context.Guild.CreateRoleAsync("Semi", null, new Color(21, 216, 102), true);
            if (roleAmateur == null)
                await Context.Guild.CreateRoleAsync("Amateur", null, new Color(232, 160, 34), true);
            if (roleRookie == null)
                await Context.Guild.CreateRoleAsync("Rookie", null, new Color(219, 199, 164), true);
        }

        [Command("levelNotification")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task LevelNotification()
        {
            await Context.Message.DeleteAsync();
            using (discordbotContext db = new discordbotContext())
            {
                var guild = db.Guild.Where(p => p.ServerId == (long)Context.Guild.Id).FirstOrDefault();
                if (guild.Level == 1)
                {
                    guild.Level = 0;
                    const int delay = 2000;
                    var embed = new EmbedBuilder();
                    embed.WithDescription("Die Level Benachrichtigungen wurden deaktiviert.");
                    embed.WithColor(new Color(90, 92, 96));
                    IUserMessage m = await ReplyAsync("", false, embed.Build());
                    await Task.Delay(delay);
                    await m.DeleteAsync();
                }
                else
                {
                    guild.Level = 1;
                    const int delay = 2000;
                    var embed = new EmbedBuilder();
                    embed.WithDescription("Die Level Benachrichtigungen wurden aktiviert.");
                    embed.WithColor(new Color(90, 92, 96));
                    IUserMessage m = await ReplyAsync("", false, embed.Build());
                    await Task.Delay(delay);
                    await m.DeleteAsync();
                }
                await db.SaveChangesAsync();
            }
        }

        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Command("addEXP")]
        public async Task AddExp(int exp)
        {
            await Context.Message.DeleteAsync();
            if (Context.User.Id != 128914972829941761)
                return;
            using (discordbotContext db = new discordbotContext())
            {
                var experience = db.Experience.Where(p => p.UserId == (long)Context.User.Id).FirstOrDefault();
                experience.Exp += exp;
                await db.SaveChangesAsync();
                const int delay = 2000;
                var embed = new EmbedBuilder();
                embed.WithDescription("Experience erfolgreich hinzugefügt.");
                embed.WithColor(new Color(90, 92, 96));
                IUserMessage m = await ReplyAsync("", false, embed.Build());
                await Task.Delay(delay);
                await m.DeleteAsync();
            }
        }

        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Command("removeEXP")]
        public async Task RemoveExp(int exp)
        {
            await Context.Message.DeleteAsync();
            if (Context.User.Id != 128914972829941761)
                return;
            using (discordbotContext db = new discordbotContext())
            {
                var experience = db.Experience.Where(p => p.UserId == (long)Context.User.Id).FirstOrDefault();
                experience.Exp -= exp;
                await db.SaveChangesAsync();
                const int delay = 2000;
                var embed = new EmbedBuilder();
                embed.WithDescription("Experience erfolgreich entfernt.");
                embed.WithColor(new Color(90, 92, 96));
                IUserMessage m = await ReplyAsync("", false, embed.Build());
                await Task.Delay(delay);
                await m.DeleteAsync();
            }
        }
    }
}
