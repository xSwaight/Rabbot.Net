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
                        await Context.Channel.SendMessageAsync($"{user.Username} ist Level {level} und hat {exp.Exp} EXP!");
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
                        await Context.Channel.SendMessageAsync($"{Context.User.Mention} du bist Level {level} und hast {exp.Exp} EXP!");
                    }
                    else
                    {
                        await Context.Channel.SendMessageAsync($"{Context.User.Mention} du hast keine EXP!");
                    }
                }
            }
        }
    }
}
