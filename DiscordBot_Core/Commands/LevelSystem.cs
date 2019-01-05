using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using DiscordBot_Core.Database;
using DiscordBot_Core.ImageGenerator;

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
                        await Context.Channel.SendMessageAsync($"{user.Username} ist **Level {level}** und hat **{exp.Exp} EXP** und braucht noch **{currentExp}/{neededExp2 - neededExp1} EXP** fürs Level up!!");
                    }
                    else
                    {
                        await Context.Channel.SendMessageAsync($"{user.Username} hat keine EXP!");
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
                        await Context.Channel.SendMessageAsync($"{Context.User.Mention} du bist **Level {level}** und hast **{exp.Exp} EXP** und brauchst noch **{currentExp}/{neededExp2 - neededExp1} EXP** fürs Level up!");
                    }
                    else
                    {
                        await Context.Channel.SendMessageAsync($"{Context.User.Mention} du hast keine EXP!");
                    }
                }
            }
        }

        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Command("levelNotification")]
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
