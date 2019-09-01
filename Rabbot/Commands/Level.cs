using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Rabbot.Database;
using Rabbot.ImageGenerator;
using Microsoft.EntityFrameworkCore;
using Rabbot.Preconditions;
using System.Text;
using Rabbot.Services;

namespace Rabbot.Commands
{
    public class Level : ModuleBase<SocketCommandContext>
    {

        [Command("ranking", RunMode = RunMode.Async), Alias("top")]
        [BotCommand]
        [Cooldown(30)]
        [Summary("Zeigt die Top 10 der User mit den meisten EXP an.")]
        public async Task Ranking()
        {
            using (swaightContext db = new swaightContext())
            {

                var top10 = db.Userfeatures.Where(p => p.ServerId == (long)Context.Guild.Id).OrderByDescending(p => p.Exp).Take(10);
                EmbedBuilder embed = new EmbedBuilder();
                embed.Description = "Level Ranking";
                embed.WithColor(new Color(239, 220, 7));
                int i = 1;
                foreach (var top in top10)
                {
                    try
                    {
                        uint level = Helper.GetLevel(top.Exp);
                        var user = db.User.FirstOrDefault(p => p.Id == top.UserId);
                        int exp = (int)top.Exp;
                        embed.AddField($"{i}. {user.Name}", $"Level {level} ({exp.ToString("N0", new System.Globalization.CultureInfo("de-DE"))} EXP)");
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
        [Summary("Zeigt die Top 10 der User mit den meisten Ziegen an.")]
        public async Task Goats()
        {
            using (swaightContext db = new swaightContext())
            {

                var top10 = db.Userfeatures.Where(p => p.ServerId == (long)Context.Guild.Id && p.UserId != (long)Context.Client.CurrentUser.Id).OrderByDescending(p => p.Goats).Take(10);
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
                        var user = db.User.FirstOrDefault(p => p.Id == top.UserId);
                        var inventory = db.Inventory.Join(db.Items, id => id.ItemId, item => item.Id, (Inventory, Item) => new { Inventory, Item }).Where(p => p.Inventory.FeatureId == top.Id);
                        var stall = Helper.GetStall(top.Wins);
                        var atk = stall.Attack;
                        var def = stall.Defense;

                        if (inventory != null)
                        {
                            foreach (var item in inventory)
                            {
                                atk += item.Item.Atk;
                                def += item.Item.Def;
                            }
                        }
                        embed.AddField($"{i}. {user.Name}", $"**{top.Goats.ToString("N0", new System.Globalization.CultureInfo("de-DE"))} Ziegen** | Stall Level: **{stall.Level}** | ATK: **{atk}0** | DEF: **{def}0**");
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

        [Command("registerSong")]
        [RequireOwner]
        public async Task RegisterSong()
        {
            await Context.Message.DeleteAsync();
            using (swaightContext db = new swaightContext())
            {
                if (Context.User.Activity is SpotifyGame song)
                {
                    await db.Songlist.AddAsync(new Songlist { Name = song.TrackTitle + " - " + song.Artists.First(), Link = song.TrackUrl, Active = 0 });
                    await db.SaveChangesAsync();
                }
            }
        }

        [Command("level")]
        [BotCommand]
        [Summary("Zeigt alle Level und die dazugehörigen Rewards an.")]
        [Cooldown(60)]
        public async Task LevelCmd()
        {
            string msg = "```";
            foreach (var level in Helper.exp)
            {
                    msg += $"Lvl: {level.Key} - {level.Value.Reward} Ziegen\n";
            }
            msg += "```";
            await Context.Channel.SendMessageAsync(msg);
        }

        [Command("musicrank", RunMode = RunMode.Async)]
        [BotCommand]
        [Cooldown(30)]
        [Summary("Zeigt den aktuellen Song und die aktuelle Bestenliste an.")]
        public async Task Musicrank()
        {
            using (swaightContext db = new swaightContext())
            {

                var top10 = db.Musicrank.Where(p => p.ServerId == (long)Context.Guild.Id && p.Date.Value.ToShortDateString() == DateTime.Now.ToShortDateString()).OrderByDescending(p => p.Sekunden).Take(10);
                var song = db.Songlist.Where(p => p.Active == 1);
                if (!song.Any())
                {
                    await Context.Channel.SendMessageAsync("Something went wrong! :(");
                    return;
                }

                var link = song.FirstOrDefault().Link;
                var songId = link.Substring(link.LastIndexOf("/") + 1, 22);

                EmbedBuilder embed = new EmbedBuilder();
                embed.Title = "Daily Musicboost Ranking";
                embed.Url = $"https://open.spotify.com/go?uri=spotify%3Atrack%3A{songId}&product=embed_v2";
                embed.WithColor(new Color(239, 220, 7));
                int i = 1;
                foreach (var top in top10)
                {
                    try
                    {
                        var user = db.User.FirstOrDefault(p => p.Id == top.UserId);
                        TimeSpan time = (DateTime.Now.AddSeconds(top.Sekunden + 1) - DateTime.Now);
                        switch (i)
                        {
                            case 1:
                                    embed.AddField($"{i}. {user.Name}", $"{time.Hours}h {time.Minutes}m {time.Seconds}s (+50% EXP)");
                                break;
                            case 2:
                                    embed.AddField($"{i}. {user.Name}", $"{time.Hours}h {time.Minutes}m {time.Seconds}s (30% EXP)");
                                break;
                            case 3:
                                    embed.AddField($"{i}. {user.Name}", $"{time.Hours}h {time.Minutes}m {time.Seconds}s (+10% EXP)");
                                break;
                            default:
                                    embed.AddField($"{i}. {user.Name}", $"{time.Hours}h {time.Minutes}m {time.Seconds}s");
                                break;
                        }
                        i++;

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message + " " + e.StackTrace);
                    }
                }

                embed.WithFooter($"Hör '{song.FirstOrDefault().Name}' auf Spotify und lass den Sekundencounter wachsen!");
                await Context.Channel.SendMessageAsync($"Heutiger Song: {song.FirstOrDefault().Name}\n{song.FirstOrDefault().Link}", false, embed.Build());
            }
        }

        [Command("setupLevels", RunMode = RunMode.Async)]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        public async Task SetupLevel()
        {
            await Context.Message.DeleteAsync();
            using (swaightContext db = new swaightContext())
            {

                var roles = db.Roles.Where(p => p.ServerId == (long)Context.Guild.Id);
                if (roles.FirstOrDefault(p => p.Description == "S4") == null)
                {
                    Discord.Rest.RestRole roleS4 = await Context.Guild.CreateRoleAsync("S4", null, new Color(239, 69, 50), true);
                    await db.Roles.AddAsync(new Roles { ServerId = (long)Context.Guild.Id, RoleId = (long)roleS4.Id, Description = "S4" });
                }
                if (roles.FirstOrDefault(p => p.Description == "Pro") == null)
                {
                    Discord.Rest.RestRole rolePro = await Context.Guild.CreateRoleAsync("Pro", null, new Color(94, 137, 255), true);
                    await db.Roles.AddAsync(new Roles { ServerId = (long)Context.Guild.Id, RoleId = (long)rolePro.Id, Description = "Pro" });
                }
                if (roles.FirstOrDefault(p => p.Description == "Semi") == null)
                {
                    Discord.Rest.RestRole roleSemi = await Context.Guild.CreateRoleAsync("Semi", null, new Color(21, 216, 102), true);
                    await db.Roles.AddAsync(new Roles { ServerId = (long)Context.Guild.Id, RoleId = (long)roleSemi.Id, Description = "Semi" });
                }
                if (roles.FirstOrDefault(p => p.Description == "Amateur") == null)
                {
                    Discord.Rest.RestRole roleAmateur = await Context.Guild.CreateRoleAsync("Amateur", null, new Color(232, 160, 34), true);
                    await db.Roles.AddAsync(new Roles { ServerId = (long)Context.Guild.Id, RoleId = (long)roleAmateur.Id, Description = "Amateur" });
                }
                if (roles.FirstOrDefault(p => p.Description == "Rookie") == null)
                {
                    Discord.Rest.RestRole roleRookie = await Context.Guild.CreateRoleAsync("Rookie", null, new Color(219, 199, 164), true);
                    await db.Roles.AddAsync(new Roles { ServerId = (long)Context.Guild.Id, RoleId = (long)roleRookie.Id, Description = "Rookie" });
                }
                await db.SaveChangesAsync();
            }
        }

        [Command("levelNotification", RunMode = RunMode.Async)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task LevelNotification()
        {
            await Context.Message.DeleteAsync();
            using (swaightContext db = new swaightContext())
            {

                var guild = db.Guild.FirstOrDefault(p => p.ServerId == (long)Context.Guild.Id);
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
        [Command("setlevel", RunMode = RunMode.Async)]
        public async Task SetLevel(int level, IUser user = null)
        {
            await Context.Message.DeleteAsync();
            const int delay = 2000;
            using (swaightContext db = new swaightContext())
            {
                if (!Helper.exp.TryGetValue(level, out var levelInfo))
                    return;
                if (user != null)
                {
                    if (!db.User.Where(p => p.Id == (long)user.Id).Any())
                    {
                        db.User.Add(new User { Id = (long)user.Id, Name = $"{user.Username}#{user.Discriminator}" });
                        await db.SaveChangesAsync();
                    }
                    var userEXP = db.Userfeatures.FirstOrDefault(p => p.UserId == (long)user.Id && p.ServerId == (long)Context.Guild.Id) ?? db.Userfeatures.AddAsync(new Userfeatures { Exp = 0, ServerId = (long)Context.Guild.Id, UserId = (long)user.Id }).Result.Entity;

                    userEXP.Exp = levelInfo.NeededEXP;
                    await db.SaveChangesAsync();
                    var embedUser = new EmbedBuilder();
                    embedUser.WithDescription($"{user.Username} wurde erfolgreich auf Level {level} gesetzt.");
                    embedUser.WithColor(new Color(90, 92, 96));
                    IUserMessage msg = await ReplyAsync("", false, embedUser.Build());
                    await Task.Delay(delay);
                    await msg.DeleteAsync();
                    return;
                }
                var experience = db.Userfeatures.FirstOrDefault(p => p.UserId == (long)Context.User.Id && p.ServerId == (long)Context.Guild.Id);
                experience.Exp = levelInfo.NeededEXP;
                await db.SaveChangesAsync();
                var embed = new EmbedBuilder();
                embed.WithDescription($"{user.Username} wurde erfolgreich auf Level {level} gesetzt.");
                embed.WithColor(new Color(90, 92, 96));
                IUserMessage m = await ReplyAsync("", false, embed.Build());
                await Task.Delay(delay);
                await m.DeleteAsync();
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
                    if (!db.User.Where(p => p.Id == (long)user.Id).Any())
                    {
                        db.User.Add(new User { Id = (long)user.Id, Name = $"{user.Username}#{user.Discriminator}" });
                        await db.SaveChangesAsync();
                    }
                    var userEXP = db.Userfeatures.FirstOrDefault(p => p.UserId == (long)user.Id && p.ServerId == (long)Context.Guild.Id) ?? db.Userfeatures.AddAsync(new Userfeatures { Exp = 0, ServerId = (long)Context.Guild.Id, UserId = (long)user.Id }).Result.Entity;
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
                var experience = db.Userfeatures.FirstOrDefault(p => p.UserId == (long)Context.User.Id && p.ServerId == (long)Context.Guild.Id);
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
            const int delay = 2000;
            using (swaightContext db = new swaightContext())
            {

                if (user != null)
                {
                    var userEXP = db.Userfeatures.FirstOrDefault(p => p.UserId == (long)user.Id && p.ServerId == (long)Context.Guild.Id);
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
                var experience = db.Userfeatures.FirstOrDefault(p => p.UserId == (long)Context.User.Id && p.ServerId == (long)Context.Guild.Id);
                experience.Exp -= exp;
                await db.SaveChangesAsync();
            }
        }

        [Command("exp")]
        [Cooldown(30)]
        [Summary("Zeigt die benötigten EXP bis zum angegebenen Level an.")]
        public async Task Exp(int level)
        {
            if (level > 80 || level < 1)
            {
                await Context.Channel.SendMessageAsync("Nö.");
                return;
            }
            using (swaightContext db = new swaightContext())
            {
                var user = db.User.Include(p => p.Userfeatures).FirstOrDefault(p => p.Id == (long)Context.User.Id);
                var feature = user.Userfeatures.FirstOrDefault(p => p.ServerId == (long)Context.Guild.Id);
                EmbedBuilder embed = new EmbedBuilder();
                if (Helper.GetLevel(feature.Exp) >= level)
                {
                    embed.Color = Color.Red;
                    embed.Description = $"{Context.User.Mention} du bist bereits über **Level {level}**.";
                    await Context.Channel.SendMessageAsync(null, false, embed.Build());
                    return;
                }
                var neededEXP = (int)Helper.GetEXP(level) - (int)feature.Exp;
                embed.Color = Color.Green;
                embed.Description = $"{Context.User.Mention} du benötigst noch **{neededEXP.ToString("N0", new System.Globalization.CultureInfo("de-DE"))} EXP** bis **Level {level}**.";
                await Context.Channel.SendMessageAsync(null, false, embed.Build());
            }
        }

        [Command("profile", RunMode = RunMode.Async), Alias("rank")]
        [Cooldown(30)]
        [Summary("Zeigt das Profil von dir oder dem markierten User an.")]
        public async Task Profile(SocketUser user = null)
        {
            if (user == null)
            {
                user = Context.User;
            }
            using (swaightContext db = new swaightContext())
            {
                string path = "";
                using (Context.Channel.EnterTypingState())
                {
                    string name = (user as IGuildUser).Nickname ?? user.Username;
                    var dbUser = db.Userfeatures.FirstOrDefault(p => p.UserId == (long)user.Id && p.ServerId == (long)Context.Guild.Id) ?? db.Userfeatures.AddAsync(new Userfeatures { ServerId = (long)Context.Guild.Id, UserId = (long)user.Id, Exp = 0, Goats = 0 }).Result.Entity;
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
                    string progress = $"{currentLevelExp.ToString("N0", new System.Globalization.CultureInfo("de-DE"))} | {neededLevelExp.ToString("N0", new System.Globalization.CultureInfo("de-DE"))}";
                    if (level == Helper.exp.OrderByDescending(p => p.Key).First().Key)
                    {
                        percent = 100;
                        progress = $"{currentLevelExp.ToString("N0", new System.Globalization.CultureInfo("de-DE"))}";
                    }
                    var ranks = db.Userfeatures.Where(p => p.ServerId == (long)Context.Guild.Id).OrderByDescending(p => p.Exp);
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
                        EXP = exp.ToString("N0", new System.Globalization.CultureInfo("de-DE")),
                        PROGRESS = progress,
                        PERCENT = percent.ToString(),
                        GOATCOINS = goat.ToString("N0", new System.Globalization.CultureInfo("de-DE"))
                    });

                    path = HtmlToImage.Generate(Helper.RemoveSpecialCharacters(name) + "_Profile", html, 300, 175);
                    await Context.Channel.SendFileAsync(path);
                    await db.SaveChangesAsync();
                }
                File.Delete(path);
            }
        }
    }
}
