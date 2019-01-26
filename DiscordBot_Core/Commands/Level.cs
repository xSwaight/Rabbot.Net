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
        [Cooldown(60)]
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
        [Cooldown(60)]
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
                        embed.AddField("Platz " + i, $"**{user.Name}** \nLevel {level} ({exp.ToString("N0")} EXP)");
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
                    var userEXP = db.Experience.Where(p => p.UserId == (long)user.Id && p.ServerId == (long)Context.Guild.Id).FirstOrDefault() ?? db.Experience.AddAsync(new Experience {Exp = 0, ServerId = (long)Context.Guild.Id, UserId = (long)user.Id }).Result.Entity;
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
        [Cooldown(60)]
        public async Task Profile(IUser user = null)
        {

            using (swaightContext db = new swaightContext())
            {
                if (user == null)
                {
                    string name = (Context.User as IGuildUser).Nickname ?? Context.User.Username;
                    int exp = db.Experience.Where(p => p.UserId == (long)Context.User.Id && p.ServerId == (long)Context.Guild.Id).FirstOrDefault().Exp ?? 0;
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
                    var template = new HtmlTemplate(Directory.GetCurrentDirectory() + "/RabbotTheme/Profile.html");
                    var html = template.Render(new
                    {
                        AVATAR = profilePicture,
                        NAME = name,
                        LEVEL = level.ToString(),
                        RANK = rank.ToString(),
                        EXP = exp.ToString("N0"),
                        PROGRESS = $"{currentLevelExp.ToString("N0")} | {neededLevelExp.ToString("N0")}",
                        PERCENT = percent.ToString()
                    });

                    var path = HtmlToImage.Generate(Helper.RemoveSpecialCharacters(name), html, 300, 151);
                    await Context.Channel.SendFileAsync(path);
                    File.Delete(path);
                }
                else
                {
                    string name = (user as IGuildUser).Nickname ?? user.Username;
                    int exp = (int)db.Experience.Where(p => p.UserId == (long)user.Id && p.ServerId == (long)Context.Guild.Id).FirstOrDefault().Exp;
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
                    var template = new HtmlTemplate(Directory.GetCurrentDirectory() + "/RabbotTheme/Profile.html");
                    var html = template.Render(new
                    {
                        AVATAR = profilePicture,
                        NAME = name,
                        LEVEL = level.ToString(),
                        RANK = rank.ToString(),
                        EXP = exp.ToString("N0"),
                        PROGRESS = $"{currentLevelExp.ToString("N0")} | {neededLevelExp.ToString("N0")}",
                        PERCENT = percent.ToString()
                    });

                    var path = HtmlToImage.Generate(Helper.RemoveSpecialCharacters(name), html, 300, 151);
                    await Context.Channel.SendFileAsync(path);
                    File.Delete(path);
                }
            }
        }
    }
}
