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

namespace Rabbot.Commands
{
    public class Level : ModuleBase<SocketCommandContext>
    {
        private readonly swaightContext _database;
        public Level(swaightContext database)
        {
            _database = database;
        }

        [Command("ranking", RunMode = RunMode.Async), Alias("top")]
        [BotCommand]
        [Cooldown(30)]
        [Summary("Zeigt die Top 10 der User mit den meisten EXP an.")]
        public async Task Ranking()
        {
            var top10 = _database.Userfeatures.Where(p => p.ServerId == (long)Context.Guild.Id).OrderByDescending(p => p.Exp).Take(10);
            EmbedBuilder embed = new EmbedBuilder();
            embed.Description = "Level Ranking";
            embed.WithColor(new Color(239, 220, 7));
            int i = 1;
            foreach (var top in top10)
            {
                try
                {
                    uint level = Helper.GetLevel(top.Exp);
                    var user = _database.User.Where(p => p.Id == top.UserId).FirstOrDefault();
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

        [Command("ziegen", RunMode = RunMode.Async), Alias("goats")]
        [BotCommand]
        [Cooldown(30)]
        [Summary("Zeigt die Top 10 der User mit den meisten Ziegen an.")]
        public async Task Goats()
        {
            var top10 = _database.Userfeatures.Where(p => p.ServerId == (long)Context.Guild.Id && p.UserId != (long)Context.Client.CurrentUser.Id).OrderByDescending(p => p.Goats).Take(10);
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
                    var user = _database.User.Where(p => p.Id == top.UserId).FirstOrDefault();
                    var inventory = _database.Inventory.Join(_database.Items, id => id.ItemId, item => item.Id, (Inventory, Item) => new { Inventory, Item }).Where(p => p.Inventory.FeatureId == top.Id);
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

        [Command("musicrank", RunMode = RunMode.Async)]
        [BotCommand]
        [Cooldown(30)]
        [Summary("Zeigt den aktuellen Song und die aktuelle Bestenliste an.")]
        public async Task Musicrank()
        {
            var top10 = _database.Musicrank.Where(p => p.ServerId == (long)Context.Guild.Id && p.Date.Value.ToShortDateString() == DateTime.Now.ToShortDateString()).OrderByDescending(p => p.Sekunden).Take(10);
            EmbedBuilder embed = new EmbedBuilder();
            embed.Description = "Daily Musicboost Ranking";
            embed.WithColor(new Color(239, 220, 7));
            int i = 1;
            foreach (var top in top10)
            {
                try
                {
                    var user = _database.User.Where(p => p.Id == top.UserId).FirstOrDefault();
                    TimeSpan time = (DateTime.Now.AddSeconds(top.Sekunden + 1) - DateTime.Now);
                    switch (i)
                    {
                        case 1:
                            if (time.Days > 0)
                                embed.AddField($"{i}. {user.Name}", $"{time.Days}d {time.Hours}h {time.Minutes}m {time.Seconds}s (+80% EXP)");
                            else
                                embed.AddField($"{i}. {user.Name}", $"{time.Hours}h {time.Minutes}m {time.Seconds}s (+80% EXP)");
                            break;
                        case 2:
                            if (time.Days > 0)
                                embed.AddField($"{i}. {user.Name}", $"{time.Days}d {time.Hours}h {time.Minutes}m {time.Seconds}s (+50% EXP)");
                            else
                                embed.AddField($"{i}. {user.Name}", $"{time.Hours}h {time.Minutes}m {time.Seconds}s (+50% EXP)");
                            break;
                        case 3:
                            if (time.Days > 0)
                                embed.AddField($"{i}. {user.Name}", $"{time.Days}d {time.Hours}h {time.Minutes}m {time.Seconds}s (+30% EXP)");
                            else
                                embed.AddField($"{i}. {user.Name}", $"{time.Hours}h {time.Minutes}m {time.Seconds}s (+30% EXP)");
                            break;
                        default:
                            if (time.Days > 0)
                                embed.AddField($"{i}. {user.Name}", $"{time.Days}d {time.Hours}h {time.Minutes}m {time.Seconds}s");
                            else
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
            var song = _database.Songlist.Where(p => p.Active == 1);
            if (!song.Any())
            {
                await Context.Channel.SendMessageAsync("Something went wrong! :(", false);
                return;
            }
            embed.WithFooter($"Hör '{song.FirstOrDefault().Name}' auf Spotify und lass den Sekundencounter wachsen!");
            await Context.Channel.SendMessageAsync($"Heutiger Song: {song.FirstOrDefault().Name}\n{song.FirstOrDefault().Link}", false, embed.Build());
        }

        [Command("setupLevels", RunMode = RunMode.Async)]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        public async Task SetupLevel()
        {
            await Context.Message.DeleteAsync();
            var roles = _database.Roles.Where(p => p.ServerId == (long)Context.Guild.Id);
            if (roles.Where(p => p.Description == "S4").FirstOrDefault() == null)
            {
                Discord.Rest.RestRole roleS4 = await Context.Guild.CreateRoleAsync("S4", null, new Color(239, 69, 50), true);
                await _database.Roles.AddAsync(new Roles { ServerId = (long)Context.Guild.Id, RoleId = (long)roleS4.Id, Description = "S4" });
            }
            if (roles.Where(p => p.Description == "Pro").FirstOrDefault() == null)
            {
                Discord.Rest.RestRole rolePro = await Context.Guild.CreateRoleAsync("Pro", null, new Color(94, 137, 255), true);
                await _database.Roles.AddAsync(new Roles { ServerId = (long)Context.Guild.Id, RoleId = (long)rolePro.Id, Description = "Pro" });
            }
            if (roles.Where(p => p.Description == "Semi").FirstOrDefault() == null)
            {
                Discord.Rest.RestRole roleSemi = await Context.Guild.CreateRoleAsync("Semi", null, new Color(21, 216, 102), true);
                await _database.Roles.AddAsync(new Roles { ServerId = (long)Context.Guild.Id, RoleId = (long)roleSemi.Id, Description = "Semi" });
            }
            if (roles.Where(p => p.Description == "Amateur").FirstOrDefault() == null)
            {
                Discord.Rest.RestRole roleAmateur = await Context.Guild.CreateRoleAsync("Amateur", null, new Color(232, 160, 34), true);
                await _database.Roles.AddAsync(new Roles { ServerId = (long)Context.Guild.Id, RoleId = (long)roleAmateur.Id, Description = "Amateur" });
            }
            if (roles.Where(p => p.Description == "Rookie").FirstOrDefault() == null)
            {
                Discord.Rest.RestRole roleRookie = await Context.Guild.CreateRoleAsync("Rookie", null, new Color(219, 199, 164), true);
                await _database.Roles.AddAsync(new Roles { ServerId = (long)Context.Guild.Id, RoleId = (long)roleRookie.Id, Description = "Rookie" });
            }
            await _database.SaveChangesAsync();
        }

        [Command("levelNotification", RunMode = RunMode.Async)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task LevelNotification()
        {
            await Context.Message.DeleteAsync();
            var guild = _database.Guild.Where(p => p.ServerId == (long)Context.Guild.Id).FirstOrDefault();
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
            await _database.SaveChangesAsync();
        }

        [RequireOwner]
        [Command("addEXP", RunMode = RunMode.Async)]
        public async Task AddExp(int exp, IUser user = null)
        {
            await Context.Message.DeleteAsync();
            const int delay = 2000;
            if (user != null)
            {
                var userEXP = _database.Userfeatures.Where(p => p.UserId == (long)user.Id && p.ServerId == (long)Context.Guild.Id).FirstOrDefault() ?? _database.Userfeatures.AddAsync(new Userfeatures { Exp = 0, ServerId = (long)Context.Guild.Id, UserId = (long)user.Id }).Result.Entity;
                userEXP.Exp += exp;
                await _database.SaveChangesAsync();
                var embedUser = new EmbedBuilder();
                embedUser.WithDescription($"{user.Username} wurden erfolgreich {exp} EXP hinzugefügt.");
                embedUser.WithColor(new Color(90, 92, 96));
                IUserMessage msg = await ReplyAsync("", false, embedUser.Build());
                await Task.Delay(delay);
                await msg.DeleteAsync();
                return;
            }
            var experience = _database.Userfeatures.Where(p => p.UserId == (long)Context.User.Id && p.ServerId == (long)Context.Guild.Id).FirstOrDefault();
            experience.Exp += exp;
            await _database.SaveChangesAsync();
            var embed = new EmbedBuilder();
            embed.WithDescription($"{exp} EXP wurden erfolgreich hinzugefügt.");
            embed.WithColor(new Color(90, 92, 96));
            IUserMessage m = await ReplyAsync("", false, embed.Build());
            await Task.Delay(delay);
            await m.DeleteAsync();
        }

        [RequireOwner]
        [Command("removeEXP", RunMode = RunMode.Async)]
        public async Task RemoveExp(int exp, IUser user = null)
        {
            await Context.Message.DeleteAsync();
            const int delay = 2000;
            if (user != null)
            {
                var userEXP = _database.Userfeatures.Where(p => p.UserId == (long)user.Id && p.ServerId == (long)Context.Guild.Id).FirstOrDefault();
                userEXP.Exp -= exp;
                await _database.SaveChangesAsync();
                var embedUser = new EmbedBuilder();
                embedUser.WithDescription($"{user.Username} wurden erfolgreich {exp} EXP entfernt.");
                embedUser.WithColor(new Color(90, 92, 96));
                IUserMessage msg = await ReplyAsync("", false, embedUser.Build());
                await Task.Delay(delay);
                await msg.DeleteAsync();
                return;
            }
            var experience = _database.Userfeatures.Where(p => p.UserId == (long)Context.User.Id && p.ServerId == (long)Context.Guild.Id).FirstOrDefault();
            experience.Exp -= exp;
            await _database.SaveChangesAsync();
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
            using (Context.Channel.EnterTypingState())
            {
                string name = (user as IGuildUser).Nickname ?? user.Username;
                var dbUser = _database.Userfeatures.Where(p => p.UserId == (long)user.Id && p.ServerId == (long)Context.Guild.Id).FirstOrDefault() ?? _database.Userfeatures.AddAsync(new Userfeatures { ServerId = (long)Context.Guild.Id, UserId = (long)user.Id, Exp = 0, Goats = 0 }).Result.Entity;
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
                var ranks = _database.Userfeatures.Where(p => p.ServerId == (long)Context.Guild.Id).OrderByDescending(p => p.Exp);
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
                    PROGRESS = $"{currentLevelExp.ToString("N0", new System.Globalization.CultureInfo("de-DE"))} | {neededLevelExp.ToString("N0", new System.Globalization.CultureInfo("de-DE"))}",
                    PERCENT = percent.ToString(),
                    GOATCOINS = goat.ToString("N0", new System.Globalization.CultureInfo("de-DE"))
                });

                var path = HtmlToImage.Generate(Helper.RemoveSpecialCharacters(name) + "_Profile", html, 300, 175);
                await Context.Channel.SendFileAsync(path);
                File.Delete(path);
                await _database.SaveChangesAsync();
            }
        }
    }
}
