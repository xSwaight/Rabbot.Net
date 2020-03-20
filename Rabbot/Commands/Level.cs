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
using PagedList;
using Serilog;
using Serilog.Core;
using Rabbot.Services;

namespace Rabbot.Commands
{
    public class Level : ModuleBase<SocketCommandContext>
    {
        private static readonly ILogger _logger = Log.ForContext(Serilog.Core.Constants.SourceContextPropertyName, nameof(Level));
        private readonly StreakService _streakService;

        public Level(StreakService streakService)
        {
            _streakService = streakService;
        }

        [Command("ranking", RunMode = RunMode.Async), Alias("top")]
        [BotCommand]
        [Summary("Zeigt die Top User sortiert nach EXP an.")]
        public async Task Ranking(int page = 1)
        {
            if (page < 1)
                return;
            using (swaightContext db = new swaightContext())
            {

                var ranking = db.Userfeatures.Where(p => p.ServerId == Context.Guild.Id && p.HasLeft == false).OrderByDescending(p => p.Exp).ToPagedList(page, 10);
                if (page > ranking.PageCount)
                    return;
                EmbedBuilder embed = new EmbedBuilder();
                embed.Description = $"Level Ranking Seite {ranking.PageNumber}/{ranking.PageCount}";
                embed.WithColor(new Color(239, 220, 7));
                int i = ranking.PageSize * ranking.PageNumber - (ranking.PageSize - 1);
                foreach (var top in ranking)
                {
                    try
                    {
                        uint level = Helper.GetLevel(top.Exp);
                        var user = db.User.FirstOrDefault(p => p.Id == top.UserId);
                        int exp = (int)top.Exp;
                        embed.AddField($"{i}. {user.Name}", $"Level {level} ({exp.ToFormattedString()} EXP)");
                        i++;

                    }
                    catch (Exception e)
                    {
                        _logger.Error(e, $"Error while adding fields to embed");
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

                var top10 = db.Userfeatures.Where(p => p.ServerId == Context.Guild.Id && p.UserId != Context.Client.CurrentUser.Id).OrderByDescending(p => p.Goats).Take(10);
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
                        embed.AddField($"{i}. {user.Name}", $"**{top.Goats.ToFormattedString()} Ziegen** | Stall Level: **{stall.Level}** | ATK: **{atk}0** | DEF: **{def}0**");
                        i++;

                    }
                    catch (Exception e)
                    {
                        _logger.Error(e, $"Error while adding fields to embed");
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
        public async Task LevelCmd(int page = 1)
        {
            if (page < 1)
                return;
            var levels = Helper.exp.ToPagedList(page, 60);
            if (page > levels.PageCount)
                return;
            string msg = $"```Seite {levels.PageNumber}/{levels.PageCount}\n";
            foreach (var level in levels)
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

                var top10 = db.Musicrank.Where(p => p.ServerId == Context.Guild.Id && p.Date.Value.ToShortDateString() == DateTime.Now.ToShortDateString()).OrderByDescending(p => p.Sekunden).Take(10);
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
                        _logger.Error(e, $"Error while adding fields to embed");
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

                var roles = db.Roles.Where(p => p.ServerId == Context.Guild.Id);
                if (roles.FirstOrDefault(p => p.Description == "S4") == null)
                {
                    Discord.Rest.RestRole roleS4 = await Context.Guild.CreateRoleAsync("S4", null, new Color(239, 69, 50), true);
                    await db.Roles.AddAsync(new Roles { ServerId = Context.Guild.Id, RoleId = roleS4.Id, Description = "S4" });
                }
                if (roles.FirstOrDefault(p => p.Description == "S3") == null)
                {
                    Discord.Rest.RestRole roleS4 = await Context.Guild.CreateRoleAsync("S3", null, new Color(239, 69, 50), true);
                    await db.Roles.AddAsync(new Roles { ServerId = Context.Guild.Id, RoleId = roleS4.Id, Description = "S3" });
                }
                if (roles.FirstOrDefault(p => p.Description == "S2") == null)
                {
                    Discord.Rest.RestRole roleS4 = await Context.Guild.CreateRoleAsync("S2", null, new Color(239, 69, 50), true);
                    await db.Roles.AddAsync(new Roles { ServerId = Context.Guild.Id, RoleId = roleS4.Id, Description = "S2" });
                }
                if (roles.FirstOrDefault(p => p.Description == "S1") == null)
                {
                    Discord.Rest.RestRole roleS4 = await Context.Guild.CreateRoleAsync("S1", null, new Color(239, 69, 50), true);
                    await db.Roles.AddAsync(new Roles { ServerId = Context.Guild.Id, RoleId = roleS4.Id, Description = "S1" });
                }
                if (roles.FirstOrDefault(p => p.Description == "Pro") == null)
                {
                    Discord.Rest.RestRole rolePro = await Context.Guild.CreateRoleAsync("Pro", null, new Color(94, 137, 255), true);
                    await db.Roles.AddAsync(new Roles { ServerId = Context.Guild.Id, RoleId = rolePro.Id, Description = "Pro" });
                }
                if (roles.FirstOrDefault(p => p.Description == "Semi") == null)
                {
                    Discord.Rest.RestRole roleSemi = await Context.Guild.CreateRoleAsync("Semi", null, new Color(21, 216, 102), true);
                    await db.Roles.AddAsync(new Roles { ServerId = Context.Guild.Id, RoleId = roleSemi.Id, Description = "Semi" });
                }
                if (roles.FirstOrDefault(p => p.Description == "Amateur") == null)
                {
                    Discord.Rest.RestRole roleAmateur = await Context.Guild.CreateRoleAsync("Amateur", null, new Color(232, 160, 34), true);
                    await db.Roles.AddAsync(new Roles { ServerId = Context.Guild.Id, RoleId = roleAmateur.Id, Description = "Amateur" });
                }
                if (roles.FirstOrDefault(p => p.Description == "Rookie") == null)
                {
                    Discord.Rest.RestRole roleRookie = await Context.Guild.CreateRoleAsync("Rookie", null, new Color(219, 199, 164), true);
                    await db.Roles.AddAsync(new Roles { ServerId = Context.Guild.Id, RoleId = roleRookie.Id, Description = "Rookie" });
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

                var guild = db.Guild.FirstOrDefault(p => p.ServerId == Context.Guild.Id);
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
                    if (!db.User.Where(p => p.Id == user.Id).Any())
                    {
                        db.User.Add(new User { Id = user.Id, Name = $"{user.Username}#{user.Discriminator}" });
                        await db.SaveChangesAsync();
                    }
                    var userEXP = db.Userfeatures.FirstOrDefault(p => p.UserId == user.Id && p.ServerId == Context.Guild.Id) ?? db.Userfeatures.AddAsync(new Userfeatures { Exp = 0, ServerId = Context.Guild.Id, UserId = user.Id }).Result.Entity;

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
                var experience = db.Userfeatures.FirstOrDefault(p => p.UserId == Context.User.Id && p.ServerId == Context.Guild.Id);
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
                    if (!db.User.Where(p => p.Id == user.Id).Any())
                    {
                        db.User.Add(new User { Id = user.Id, Name = $"{user.Username}#{user.Discriminator}" });
                        await db.SaveChangesAsync();
                    }
                    var userEXP = db.Userfeatures.FirstOrDefault(p => p.UserId == user.Id && p.ServerId == Context.Guild.Id) ?? db.Userfeatures.AddAsync(new Userfeatures { Exp = 0, ServerId = Context.Guild.Id, UserId = user.Id }).Result.Entity;
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
                var experience = db.Userfeatures.FirstOrDefault(p => p.UserId == Context.User.Id && p.ServerId == Context.Guild.Id);
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
                    var userEXP = db.Userfeatures.FirstOrDefault(p => p.UserId == user.Id && p.ServerId == Context.Guild.Id);
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
                var experience = db.Userfeatures.FirstOrDefault(p => p.UserId == Context.User.Id && p.ServerId == Context.Guild.Id);
                experience.Exp -= exp;
                await db.SaveChangesAsync();
            }
        }

        [Command("exp")]
        [Cooldown(30)]
        [Summary("Zeigt die benötigten EXP bis zum angegebenen Level an.")]
        public async Task Exp(int level)
        {
            if (level > 119 || level < 1)
            {
                await Context.Channel.SendMessageAsync("Nö.");
                return;
            }
            using (swaightContext db = new swaightContext())
            {
                var user = db.User.Include(p => p.Userfeatures).FirstOrDefault(p => p.Id == Context.User.Id);
                var feature = user.Userfeatures.FirstOrDefault(p => p.ServerId == Context.Guild.Id);
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
                embed.Description = $"{Context.User.Mention} du benötigst noch **{neededEXP.ToFormattedString()} EXP** bis **Level {level}**.";
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
                    string name = (user as IGuildUser).Nickname?.Replace("<", "&lt;").Replace(">", "&gt;") ?? user.Username?.Replace("<", "&lt;").Replace(">", "&gt;");
                    var dbUser = db.Userfeatures.FirstOrDefault(p => p.UserId == user.Id && p.ServerId == Context.Guild.Id) ?? db.Userfeatures.AddAsync(new Userfeatures { ServerId = Context.Guild.Id, UserId = user.Id, Exp = 0, Goats = 0 }).Result.Entity;
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
                    string progress = $"{currentLevelExp.ToFormattedString()} | {neededLevelExp.ToFormattedString()}";
                    if (level == Helper.exp.OrderByDescending(p => p.Key).First().Key)
                    {
                        percent = 100;
                        progress = $"{currentLevelExp.ToFormattedString()}";
                    }
                    var ranks = db.Userfeatures.Where(p => p.ServerId == Context.Guild.Id && p.HasLeft == false).OrderByDescending(p => p.Exp);
                    int rank = 1;
                    foreach (var Rank in ranks)
                    {
                        if (Rank.UserId == user.Id)
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
                        EXP = exp.ToFormattedString(),
                        PROGRESS = progress,
                        PERCENT = percent.ToString(),
                        GOATCOINS = goat.ToFormattedString()
                    });

                    path = HtmlToImage.Generate(Helper.RemoveSpecialCharacters(name) + "_Profile", html, 300, 175);
                    await Context.Channel.SendFileAsync(path, $"{Constants.Fire} {_streakService.GetStreakLevel(dbUser)}");
                    await db.SaveChangesAsync();
                }
                File.Delete(path);
            }
        }
    }
}
