﻿using System;
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
using Rabbot.Database.Rabbot;
using Microsoft.Extensions.DependencyInjection;
using Rabbot.Models;
using Discord.Addons.Interactive;
using System.Collections.Generic;

namespace Rabbot.Commands
{
    public class Level : InteractiveBase
    {
        private static readonly ILogger _logger = Log.ForContext(Serilog.Core.Constants.SourceContextPropertyName, nameof(Level));
        private readonly StreakService _streakService;
        private DatabaseService Database => DatabaseService.Instance;
        private readonly ImageService _imageService;

        public Level(IServiceProvider services)
        {
            _streakService = services.GetRequiredService<StreakService>();
            _imageService = services.GetRequiredService<ImageService>();
        }

        [Command("ranking", RunMode = RunMode.Async), Alias("top")]
        [BotCommand]
        [Summary("Zeigt die Top User sortiert nach EXP an.")]
        public async Task Ranking()
        {
            using (var db = Database.Open())
            {
                var ranking = db.Features.AsQueryable().Include(p => p.User).Where(p => p.GuildId == Context.Guild.Id && p.HasLeft == false).OrderByDescending(p => p.Exp);

                List<string> pages = new List<string>();
                int pageSize = 10;
                var pageCount = (int)Math.Ceiling((ranking.Count() / (double)pageSize));
                for (int i = 1; i <= pageCount; i++)
                {
                    var users = ranking.ToPagedList(i, pageSize);
                    string pageContent = "";

                    pageContent += $"**Level Ranking**\n\n\n";
                    int count = users.PageSize * users.PageNumber - (users.PageSize - 1);
                    foreach (var user in users)
                    {
                        int level = Helper.GetLevel(user.Exp);
                        pageContent += $"{count}. **{user.User.Name}**\nLevel {level} ({user.Exp.ToFormattedString()} EXP)\n\n";
                        count++;
                    }
                    pages.Add(pageContent);
                }

                var paginatedMessage = new PaginatedMessage()
                {
                    Pages = pages.ToArray(),
                    Options = Globals.PaginatorOptions,
                    Color = new Color(239, 220, 7)
                };
                await PagedReplyAsync(paginatedMessage);
            }
        }

        [Command("ziegen", RunMode = RunMode.Async), Alias("goats")]
        [BotCommand]
        [Cooldown(30)]
        [Summary("Zeigt die Top 10 der User mit den meisten Ziegen an.")]
        public async Task Goats()
        {
            using (var db = Database.Open())
            {

                var top10 = db.Features.AsQueryable().Where(p => p.GuildId == Context.Guild.Id && p.UserId != Context.Client.CurrentUser.Id).OrderByDescending(p => p.Goats).Take(10);
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
                        var user = db.Users.FirstOrDefault(p => p.Id == top.UserId);
                        var inventory = db.Inventorys.AsQueryable().Join(db.Items, id => id.ItemId, item => item.Id, (Inventory, Item) => new { Inventory, Item }).Where(p => p.Inventory.FeatureId == top.Id);
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
            using (var db = Database.Open())
            {
                if (Context.User.Activity is SpotifyGame song)
                {
                    await db.Songs.AddAsync(new SongEntity { Name = song.TrackTitle + " - " + song.Artists.First(), Link = song.TrackUrl, Active = false });
                    await db.SaveChangesAsync();
                }
            }
        }

        [Command("level")]
        [BotCommand]
        [Summary("Zeigt alle Level und die dazugehörigen Rewards an.")]
        [Cooldown(5)]
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
            using (var db = Database.Open())
            {

                var top10 = db.Musicranks.AsQueryable().Where(p => p.GuildId == Context.Guild.Id && p.Date.ToShortDateString() == DateTime.Now.ToShortDateString()).OrderByDescending(p => p.Seconds).Take(10);
                var song = db.Songs.AsQueryable().Where(p => p.Active == false);
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
                        var user = db.Users.FirstOrDefault(p => p.Id == top.UserId);
                        TimeSpan time = (DateTime.Now.AddSeconds(top.Seconds + 1) - DateTime.Now);
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
            using (var db = Database.Open())
            {

                var roles = db.Roles.AsQueryable().Where(p => p.GuildId == Context.Guild.Id);
                if (roles.FirstOrDefault(p => p.Description == "Grandmaster") == null)
                {
                    Discord.Rest.RestRole roleS4 = await Context.Guild.CreateRoleAsync("Grandmaster", null, new Color(48, 45, 37), true, false);
                    await db.Roles.AddAsync(new RoleEntity { GuildId = Context.Guild.Id, RoleId = roleS4.Id, Description = "Grandmaster" });
                }
                if (roles.FirstOrDefault(p => p.Description == "Master") == null)
                {
                    Discord.Rest.RestRole roleS4 = await Context.Guild.CreateRoleAsync("Master", null, new Color(239, 69, 50), true, false);
                    await db.Roles.AddAsync(new RoleEntity { GuildId = Context.Guild.Id, RoleId = roleS4.Id, Description = "Master" });
                }
                if (roles.FirstOrDefault(p => p.Description == "Expert") == null)
                {
                    Discord.Rest.RestRole rolePro = await Context.Guild.CreateRoleAsync("Expert", null, new Color(239, 69, 50), true, false);
                    await db.Roles.AddAsync(new RoleEntity { GuildId = Context.Guild.Id, RoleId = rolePro.Id, Description = "Expert" });
                }
                if (roles.FirstOrDefault(p => p.Description == "Pro") == null)
                {
                    Discord.Rest.RestRole roleSemi = await Context.Guild.CreateRoleAsync("Pro", null, new Color(94, 137, 255), true, false);
                    await db.Roles.AddAsync(new RoleEntity { GuildId = Context.Guild.Id, RoleId = roleSemi.Id, Description = "Pro" });
                }
                if (roles.FirstOrDefault(p => p.Description == "Semi") == null)
                {
                    Discord.Rest.RestRole roleAmateur = await Context.Guild.CreateRoleAsync("Semi", null, new Color(21, 216, 102), true, false);
                    await db.Roles.AddAsync(new RoleEntity { GuildId = Context.Guild.Id, RoleId = roleAmateur.Id, Description = "Semi" });
                }
                if (roles.FirstOrDefault(p => p.Description == "Rookie") == null)
                {
                    Discord.Rest.RestRole roleRookie = await Context.Guild.CreateRoleAsync("Rookie", null, new Color(219, 199, 164), true, false);
                    await db.Roles.AddAsync(new RoleEntity { GuildId = Context.Guild.Id, RoleId = roleRookie.Id, Description = "Rookie" });
                }
                await db.SaveChangesAsync();
            }
        }

        [Command("levelNotification", RunMode = RunMode.Async)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task LevelNotification()
        {
            await Context.Message.DeleteAsync();
            using (var db = Database.Open())
            {

                var guild = db.Guilds.FirstOrDefault(p => p.GuildId == Context.Guild.Id);
                if (guild.Level == true)
                {
                    guild.Level = false;
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
                    guild.Level = true;
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
            using (var db = Database.Open())
            {
                if (!Helper.exp.TryGetValue(level, out var levelInfo))
                    return;
                if (user != null)
                {
                    if (!db.Users.AsQueryable().Where(p => p.Id == user.Id).Any())
                    {
                        db.Users.Add(new UserEntity { Id = user.Id, Name = $"{user.Username}#{user.Discriminator}" });
                        await db.SaveChangesAsync();
                    }
                    var userEXP = db.Features.FirstOrDefault(p => p.UserId == user.Id && p.GuildId == Context.Guild.Id) ?? db.Features.AddAsync(new FeatureEntity { Exp = 0, GuildId = Context.Guild.Id, UserId = user.Id }).Result.Entity;

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
                var experience = db.Features.FirstOrDefault(p => p.UserId == Context.User.Id && p.GuildId == Context.Guild.Id);
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
            using (var db = Database.Open())
            {

                if (user != null)
                {
                    if (!db.Users.AsQueryable().Where(p => p.Id == user.Id).Any())
                    {
                        db.Users.Add(new UserEntity { Id = user.Id, Name = $"{user.Username}#{user.Discriminator}" });
                        await db.SaveChangesAsync();
                    }
                    var userEXP = db.Features.FirstOrDefault(p => p.UserId == user.Id && p.GuildId == Context.Guild.Id) ?? db.Features.AddAsync(new FeatureEntity { Exp = 0, GuildId = Context.Guild.Id, UserId = user.Id }).Result.Entity;
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
                var experience = db.Features.FirstOrDefault(p => p.UserId == Context.User.Id && p.GuildId == Context.Guild.Id);
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
            using (var db = Database.Open())
            {

                if (user != null)
                {
                    var userEXP = db.Features.FirstOrDefault(p => p.UserId == user.Id && p.GuildId == Context.Guild.Id);
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
                var experience = db.Features.FirstOrDefault(p => p.UserId == Context.User.Id && p.GuildId == Context.Guild.Id);
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
            using (var db = Database.Open())
            {
                var user = db.Users.Include(p => p.Features).FirstOrDefault(p => p.Id == Context.User.Id);
                var feature = user.Features.FirstOrDefault(p => p.GuildId == Context.Guild.Id);
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
            using (var db = Database.Open())
            {
                string path = "";
                using (Context.Channel.EnterTypingState())
                {
                    string name = (user as IGuildUser).Nickname?.Replace("<", "&lt;").Replace(">", "&gt;") ?? user.Username?.Replace("<", "&lt;").Replace(">", "&gt;");
                    if (!db.Users.Any(p => p.Id == user.Id))
                    {
                        await db.Users.AddAsync(new UserEntity { Id = user.Id, Name = $"{user.Username}#{user.Discriminator}" });
                    }

                    var dbUser = db.Features.FirstOrDefault(p => p.UserId == user.Id && p.GuildId == Context.Guild.Id) ?? db.Features.AddAsync(new FeatureEntity { GuildId = Context.Guild.Id, UserId = user.Id, Exp = 0, Goats = 0 }).Result.Entity;
                    int exp = dbUser.Exp;
                    int goat = dbUser.Goats;
                    var level = Helper.GetLevel(exp);
                    var neededExp1 = Helper.GetEXP(level);
                    var neededExp2 = Helper.GetEXP(level + 1);
                    var currentExp = exp - Helper.GetEXP(level);
                    int totalExp = exp;
                    int currentLevelExp = currentExp;
                    int neededLevelExp = neededExp2 - neededExp1;
                    double dblPercent = ((double)currentLevelExp / (double)neededLevelExp) * 100;
                    int percent = (int)dblPercent;
                    string progress = $"{currentLevelExp.ToFormattedString()} | {neededLevelExp.ToFormattedString()}";
                    if (level == Helper.exp.OrderByDescending(p => p.Key).First().Key)
                    {
                        percent = 100;
                        progress = $"{currentLevelExp.ToFormattedString()}";
                    }
                    var ranks = db.Features.AsQueryable().Where(p => p.GuildId == Context.Guild.Id && p.HasLeft == false).OrderByDescending(p => p.Exp);
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

                    bool isAnimated = profilePicture.Contains(".gif");
                    using (var image = await _imageService.DrawProfileAsync(new UserProfileDto { Exp = exp.ToFormattedString(), AvatarUrl = profilePicture, Name = name, Goats = goat.ToFormattedString(), Level = level.ToString(), LevelInfo = progress, Percent = percent, Rank = rank.ToString() }, isAnimated))
                    {
                        await Context.Channel.SendFileAsync(image, $"{name}.{(isAnimated ? "gif" : "png")}", $"{Constants.Fire} {_streakService.GetStreakLevel(dbUser)}");
                    }
                    await db.SaveChangesAsync();
                }
            }
        }
    }
}
