using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBot_Core.Database;
using DiscordBot_Core.ImageGenerator;
using Microsoft.EntityFrameworkCore;
using DiscordBot_Core.Preconditions;
using System.Text;

namespace DiscordBot_Core.Commands
{
    public class Level : ModuleBase<SocketCommandContext>
    {

        [Command("level", RunMode = RunMode.Async)]
        [Cooldown(30)]
        public async Task level(IGuildUser user = null)
        {
            using (swaightContext db = new swaightContext())
            {
                if (user != null)
                {
                    var exp = db.Experience.Where(p => p.UserId == (long)user.Id && p.ServerId == (long)Context.Guild.Id).FirstOrDefault();
                    if (exp != null)
                    {
                        uint level = Helper.GetLevel(exp.Exp);
                        var neededExp1 = Helper.GetEXP((int)level);
                        var neededExp2 = Helper.GetEXP((int)level + 1);
                        var currentExp = exp.Exp - Helper.GetEXP((int)level);
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
                    var exp = db.Experience.Where(p => p.UserId == (long)Context.User.Id && p.ServerId == (long)Context.Guild.Id).FirstOrDefault();
                    if (exp != null)
                    {
                        uint level = Helper.GetLevel(exp.Exp);
                        var neededExp1 = Helper.GetEXP((int)level);
                        var neededExp2 = Helper.GetEXP((int)level + 1);
                        var currentExp = exp.Exp - Helper.GetEXP((int)level);
                        int totalExp = (int)exp.Exp;
                        int currentLevelExp = (int)currentExp;
                        int neededLevelExp = (int)neededExp2 - (int)neededExp1;
                        double dblPercent = ((double)currentLevelExp / (double)neededLevelExp) * 100;
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

        [Command("ranking", RunMode = RunMode.Async), Alias("top")]
        [BotCommand]
        [Cooldown(30)]
        public async Task Ranking()
        {
            using (swaightContext db = new swaightContext())
            {
                var top10 = db.Experience.Where(p => p.ServerId == (long)Context.Guild.Id).OrderByDescending(p => p.Exp).Take(10);
                EmbedBuilder embed = new EmbedBuilder();
                embed.Description = "Level Ranking";
                embed.WithColor(new Color(239, 220, 7));
                int i = 1;
                foreach (var top in top10)
                {
                    try
                    {
                        uint level = Helper.GetLevel(top.Exp);
                        var user = db.User.Where(p => p.Id == top.UserId).FirstOrDefault();
                        int exp = (int)top.Exp;
                        embed.AddField($"{i}. {user.Name}", $"Level {level} ({exp.ToString("N0")} EXP)");
                        i++;

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message + " " + e.StackTrace);
                    }
                }
                await Context.Channel.SendMessageAsync(null, false, embed.Build());
            }
        }

        [Command("ziegen", RunMode = RunMode.Async), Alias("goats")]
        [BotCommand]
        [Cooldown(30)]
        public async Task Goats()
        {
            using (swaightContext db = new swaightContext())
            {
                var top10 = db.Experience.Where(p => p.ServerId == (long)Context.Guild.Id).OrderByDescending(p => p.Goats).Take(10);
                EmbedBuilder embed = new EmbedBuilder();
                embed.Description = "Ziegen Ranking";
                embed.WithColor(new Color(239, 220, 7));
                int i = 1;
                foreach (var top in top10)
                {
                    if (top.Goats == 0)
                        continue;
                    try
                    {
                        var user = db.User.Where(p => p.Id == top.UserId).FirstOrDefault();
                        embed.AddField($"{i}. {user.Name}", $"**{top.Goats.ToString("N0")} Ziegen**");
                        i++;

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message + " " + e.StackTrace);
                    }
                }
                await Context.Channel.SendMessageAsync(null, false, embed.Build());
            }
        }

        [Command("musicrank", RunMode = RunMode.Async)]
        [BotCommand]
        [Cooldown(30)]
        public async Task Musicrank()
        {
            using (swaightContext db = new swaightContext())
            {
                var top10 = db.Musicrank.Where(p => p.ServerId == (long)Context.Guild.Id && p.Date.Value.ToShortDateString() == DateTime.Now.ToShortDateString()).OrderByDescending(p => p.Sekunden).Take(10);
                EmbedBuilder embed = new EmbedBuilder();
                embed.Description = "Daily Musicboost Ranking";
                embed.WithColor(new Color(239, 220, 7));
                int i = 1;
                foreach (var top in top10)
                {
                    try
                    {
                        var user = db.User.Where(p => p.Id == top.UserId).FirstOrDefault();
                        TimeSpan time = DateTime.Now.AddSeconds(top.Sekunden) - DateTime.Now;
                        switch (i)
                        {
                            case 1:
                                embed.AddField($"{i}. {user.Name}", $"{time.Hours}h {time.Minutes}m {time.Seconds}s (+20% EXP)");
                                break;
                            case 2:
                                embed.AddField($"{i}. {user.Name}", $"{time.Hours}h {time.Minutes}m {time.Seconds}s (+10% EXP)");
                                break;
                            case 3:
                                embed.AddField($"{i}. {user.Name}", $"{time.Hours}h {time.Minutes}m {time.Seconds}s (+5% EXP)");
                                break;
                            default:
                                embed.AddField($"{i}. {user.Name}", $"{time.Hours}h {time.Minutes}m {time.Seconds}s ");
                                break;
                        }
                        i++;

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message + " " + e.StackTrace);
                    }
                }
                var song = db.Songlist.Where(p => p.Active == 1);
                if (!song.Any())
                {
                    await Context.Channel.SendMessageAsync("Something went wrong! :(", false);
                    return;
                }
                embed.WithFooter($"Hör '{song.FirstOrDefault().Name}' auf Spotify und lass den Sekundencounter wachsen!");
                await Context.Channel.SendMessageAsync($"Heutiger Song: {song.FirstOrDefault().Name}\n{song.FirstOrDefault().Link}", false, embed.Build());
            }
        }

        [Command("trade")]
        [BotCommand]
        [Cooldown(30)]
        public async Task Trade(IUser user, int amount)
        {
            if (amount > 0 && (!user.IsBot || user.Id == Context.Client.CurrentUser.Id))
            {
                using (swaightContext db = new swaightContext())
                {
                    var senderUser = db.Experience.Where(p => p.ServerId == (long)Context.Guild.Id && p.UserId == (long)Context.User.Id).FirstOrDefault() ?? db.AddAsync(new Experience { ServerId = (long)Context.Guild.Id, UserId = (long)Context.Guild.Id, Goats = 0, Exp = 0 }).Result.Entity;
                    if (senderUser.Goats < amount)
                    {
                        await Context.Message.DeleteAsync();
                        var msg = await Context.Channel.SendMessageAsync($"Dir fehlen **{amount - senderUser.Goats} Ziegen**..");
                        await Task.Delay(3000);
                        await msg.DeleteAsync();
                        await db.SaveChangesAsync();
                        return;
                    }
                    if(senderUser.Trades >= 5)
                    {
                        await Context.Message.DeleteAsync();
                        var msg = await Context.Channel.SendMessageAsync($"**Du kannst heute nicht mehr traden!**");
                        await Task.Delay(3000);
                        await msg.DeleteAsync();
                        await db.SaveChangesAsync();
                        return;
                    }

                    var targetUser = db.Experience.Where(p => p.ServerId == (long)Context.Guild.Id && p.UserId == (long)user.Id).FirstOrDefault() ?? db.AddAsync(new Experience { ServerId = (long)Context.Guild.Id, UserId = (long)user.Id, Goats = 0, Exp = 0 }).Result.Entity;
                    if (targetUser == senderUser)
                    {
                        await Context.Channel.SendMessageAsync($"Nö.");
                        return;
                    }
                    var rabbotUser = db.Experience.Where(p => p.ServerId == (long)Context.Guild.Id && p.UserId == (long)Context.Client.CurrentUser.Id).FirstOrDefault() ?? db.AddAsync(new Experience { ServerId = (long)Context.Guild.Id, UserId = (long)Context.Client.CurrentUser.Id, Goats = 0, Exp = 0 }).Result.Entity;
                    senderUser.Goats -= amount;
                    int fees = amount / 4;
                    rabbotUser.Goats += fees;
                    targetUser.Goats += amount - fees;
                    senderUser.Trades++;
                    await db.SaveChangesAsync();

                    if (amount > 1)
                        await Context.Channel.SendMessageAsync($"{Context.User.Username} hat {user.Mention} **{amount - fees} Ziegen** (-{fees} Schutzgeldziegens) geschenkt! Du kannst heute noch **{5 - senderUser.Trades} mal** traden.");
                    else
                        await Context.Channel.SendMessageAsync($"{Context.User.Username} hat {user.Mention} **{amount} Ziege** geschenkt!!");

                }
            }
        }

        [Command("setupLevels", RunMode = RunMode.Async)]
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

        [Command("levelNotification", RunMode = RunMode.Async)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task LevelNotification()
        {
            await Context.Message.DeleteAsync();
            using (swaightContext db = new swaightContext())
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

        [RequireOwner]
        [Command("addEXP", RunMode = RunMode.Async)]
        public async Task AddExp(int exp, IUser user = null)
        {
            await Context.Message.DeleteAsync();
            const int delay = 2000;
            using (swaightContext db = new swaightContext())
            {
                if (user != null)
                {
                    var userEXP = db.Experience.Where(p => p.UserId == (long)user.Id && p.ServerId == (long)Context.Guild.Id).FirstOrDefault() ?? db.Experience.AddAsync(new Experience { Exp = 0, ServerId = (long)Context.Guild.Id, UserId = (long)user.Id }).Result.Entity;
                    userEXP.Exp += exp;
                    await db.SaveChangesAsync();
                    var embedUser = new EmbedBuilder();
                    embedUser.WithDescription($"{user.Username} wurden erfolgreich {exp} EXP hinzugefügt.");
                    embedUser.WithColor(new Color(90, 92, 96));
                    IUserMessage msg = await ReplyAsync("", false, embedUser.Build());
                    await Task.Delay(delay);
                    await msg.DeleteAsync();
                    return;
                }
                var experience = db.Experience.Where(p => p.UserId == (long)Context.User.Id && p.ServerId == (long)Context.Guild.Id).FirstOrDefault();
                experience.Exp += exp;
                await db.SaveChangesAsync();
                var embed = new EmbedBuilder();
                embed.WithDescription($"{exp} EXP wurden erfolgreich hinzugefügt.");
                embed.WithColor(new Color(90, 92, 96));
                IUserMessage m = await ReplyAsync("", false, embed.Build());
                await Task.Delay(delay);
                await m.DeleteAsync();
            }
        }

        [RequireOwner]
        [Command("removeEXP", RunMode = RunMode.Async)]
        public async Task RemoveExp(int exp, IUser user = null)
        {
            await Context.Message.DeleteAsync();
            using (swaightContext db = new swaightContext())
            {
                const int delay = 2000;
                if (user != null)
                {
                    var userEXP = db.Experience.Where(p => p.UserId == (long)user.Id && p.ServerId == (long)Context.Guild.Id).FirstOrDefault();
                    userEXP.Exp -= exp;
                    await db.SaveChangesAsync();
                    var embedUser = new EmbedBuilder();
                    embedUser.WithDescription($"{user.Username} wurden erfolgreich {exp} EXP entfernt.");
                    embedUser.WithColor(new Color(90, 92, 96));
                    IUserMessage msg = await ReplyAsync("", false, embedUser.Build());
                    await Task.Delay(delay);
                    await msg.DeleteAsync();
                    return;
                }
                var experience = db.Experience.Where(p => p.UserId == (long)Context.User.Id && p.ServerId == (long)Context.Guild.Id).FirstOrDefault();
                experience.Exp -= exp;
                await db.SaveChangesAsync();

            }
        }

        [Command("profile", RunMode = RunMode.Async), Alias("rank")]
        [Cooldown(30)]
        public async Task Profile(IUser user = null)
        {

            using (swaightContext db = new swaightContext())
            {
                if (user == null)
                {
                    string name = (Context.User as IGuildUser).Nickname ?? Context.User.Username;
                    var dbUser = db.Experience.Where(p => p.UserId == (long)Context.User.Id && p.ServerId == (long)Context.Guild.Id).FirstOrDefault() ?? db.Experience.AddAsync(new Experience { ServerId = (long)Context.Guild.Id, UserId = (long)Context.User.Id, Exp = 0, Goats = 0 }).Result.Entity;
                    int exp = dbUser.Exp ?? 0;
                    int goat = dbUser.Goats;
                    var level = Helper.GetLevel(exp);
                    var neededExp1 = Helper.GetEXP((int)level);
                    var neededExp2 = Helper.GetEXP((int)level + 1);
                    var currentExp = exp - Helper.GetEXP((int)level);
                    int totalExp = (int)exp;
                    int currentLevelExp = (int)currentExp;
                    int neededLevelExp = (int)neededExp2 - (int)neededExp1;
                    double dblPercent = ((double)currentLevelExp / (double)neededLevelExp) * 100;
                    int percent = (int)dblPercent;
                    var ranks = db.Experience.Where(p => p.ServerId == (long)Context.Guild.Id).OrderByDescending(p => p.Exp);
                    int rank = 1;
                    foreach (var Rank in ranks)
                    {
                        if (Rank.UserId == (long)Context.User.Id)
                            break;
                        rank++;
                    }
                    string profilePicture = Context.User.GetAvatarUrl(Discord.ImageFormat.Auto, 128);
                    if (profilePicture == null)
                        profilePicture = Context.User.GetDefaultAvatarUrl();
                    var template = new HtmlTemplate(Directory.GetCurrentDirectory() + "/RabbotThemeNeon/profile.html");
                    var html = template.Render(new
                    {
                        AVATAR = profilePicture,
                        NAME = name,
                        LEVEL = level.ToString(),
                        RANK = rank.ToString(),
                        EXP = exp.ToString("N0"),
                        PROGRESS = $"{currentLevelExp.ToString("N0")} | {neededLevelExp.ToString("N0")}",
                        PERCENT = percent.ToString(),
                        GOATCOINS = goat.ToString("N0")
                    });

                    var path = HtmlToImage.Generate(Helper.RemoveSpecialCharacters(name) + "_Profile", html, 300, 175);
                    await Context.Channel.SendFileAsync(path);
                    File.Delete(path);
                }
                else
                {
                    string name = (user as IGuildUser).Nickname ?? user.Username;
                    var dbUser = db.Experience.Where(p => p.UserId == (long)user.Id && p.ServerId == (long)Context.Guild.Id).FirstOrDefault() ?? db.Experience.AddAsync(new Experience { ServerId = (long)Context.Guild.Id, UserId = (long)user.Id, Exp = 0, Goats = 0 }).Result.Entity;
                    int exp = dbUser.Exp ?? 0;
                    int goat = dbUser.Goats;
                    var level = Helper.GetLevel(exp);
                    var neededExp1 = Helper.GetEXP((int)level);
                    var neededExp2 = Helper.GetEXP((int)level + 1);
                    var currentExp = exp - Helper.GetEXP((int)level);
                    int totalExp = (int)exp;
                    int currentLevelExp = (int)currentExp;
                    int neededLevelExp = (int)neededExp2 - (int)neededExp1;
                    double dblPercent = ((double)currentLevelExp / (double)neededLevelExp) * 100;
                    int percent = (int)dblPercent;
                    var ranks = db.Experience.Where(p => p.ServerId == (long)Context.Guild.Id).OrderByDescending(p => p.Exp);
                    int rank = 1;
                    foreach (var Rank in ranks)
                    {
                        if (Rank.UserId == (long)user.Id)
                            break;
                        rank++;
                    }
                    string profilePicture = user.GetAvatarUrl(Discord.ImageFormat.Auto, 128);
                    if (profilePicture == null)
                        profilePicture = user.GetDefaultAvatarUrl();
                    var template = new HtmlTemplate(Directory.GetCurrentDirectory() + "/RabbotThemeNeon/profile.html");
                    var html = template.Render(new
                    {
                        AVATAR = profilePicture,
                        NAME = name,
                        LEVEL = level.ToString(),
                        RANK = rank.ToString(),
                        EXP = exp.ToString("N0"),
                        PROGRESS = $"{currentLevelExp.ToString("N0")} | {neededLevelExp.ToString("N0")}",
                        PERCENT = percent.ToString(),
                        GOATCOINS = goat.ToString("N0")
                    });

                    var path = HtmlToImage.Generate(Helper.RemoveSpecialCharacters(name) + "_Profile", html, 300, 175);
                    await Context.Channel.SendFileAsync(path);
                    File.Delete(path);
                }
            }
        }

        [Command("daily", RunMode = RunMode.Async)]
        [BotCommand]
        public async Task Daily()
        {
            using (swaightContext db = new swaightContext())
            {
                var dbUser = db.Experience.Where(p => p.UserId == (long)Context.User.Id && p.ServerId == (long)Context.Guild.Id).FirstOrDefault() ?? db.Experience.AddAsync(new Experience { UserId = (long)Context.User.Id, ServerId = (long)Context.Guild.Id, Exp = 0, Goats = 0 }).Result.Entity;
                if (dbUser.Lastdaily != null)
                {
                    if (dbUser.Lastdaily.Value.ToShortDateString() != DateTime.Now.ToShortDateString())
                    {
                        dbUser.Lastdaily = DateTime.Now;
                        await db.SaveChangesAsync();
                        await GetDailyReward(Context);
                    }
                    else
                    {
                        var now = DateTime.Now;
                        var tomorrow = now.AddDays(1).Date;
                        TimeSpan totalTime = (tomorrow - now);
                        await Context.Channel.SendMessageAsync($"**Komm in {totalTime.Hours}h {totalTime.Minutes}m wieder und versuche es erneut!**");
                    }
                }
                else
                {
                    dbUser.Lastdaily = DateTime.Now;
                    await db.SaveChangesAsync();
                    await GetDailyReward(Context);
                }

            }
        }

        private async Task GetDailyReward(SocketCommandContext context)
        {
            Random rnd = new Random();
            int chance = rnd.Next(1, 11);
            int jackpot = rnd.Next(1, 101);
            if (chance > 1)
            {
                using (swaightContext db = new swaightContext())
                {
                    var dbUser = db.Experience.Where(p => p.UserId == (long)Context.User.Id && p.ServerId == (long)Context.Guild.Id).FirstOrDefault();
                    if (jackpot == 1)
                    {
                        dbUser.Goats += 250;
                        await db.SaveChangesAsync();
                        await Context.Channel.SendMessageAsync($"Jackpot! Dir wurden **250 Ziegen** mit einem Transporter geliefert. Verdammter **Glückspilz**!");
                        return;
                    }
                    int goats = 0;
                    goats = rnd.Next(10, 101);
                    dbUser.Goats += goats;
                    await db.SaveChangesAsync();
                    await Context.Channel.SendMessageAsync($"Wow, du konntest heute unfassbare **{goats} Ziegen** einfangen!");
                }
            }
            else
            {
                await Context.Channel.SendMessageAsync($"**Schade**, heute konntest du keine Ziegen einfangen..");
            }
        }
    }
}
