using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBot_Core.API;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using DiscordBot_Core.Database;
using DiscordBot_Core.API.Models;
using DiscordBot_Core.ImageGenerator;
using System.IO;
using ImageFormat = Discord.ImageFormat;

namespace DiscordBot_Core
{
#pragma warning disable CS1998
    class EventHandler
    {
        DiscordSocketClient _client;
        CommandService _service;
        private IServiceProvider services;

        public async Task InitializeAsync(DiscordSocketClient client)
        {
            _client = client;
            _service = new CommandService();
            services = new ServiceCollection().BuildServiceProvider();
            await _service.AddModulesAsync(Assembly.GetEntryAssembly(), services);
            CancellationTokenSource _cancelationTokenSource = new CancellationTokenSource();
            //new Task(async () => await CheckOnlineUsers(), _cancelationTokenSource.Token, TaskCreationOptions.LongRunning).Start();
            new Task(async () => await CheckBannedUsers(), _cancelationTokenSource.Token, TaskCreationOptions.LongRunning).Start();
            _client.UserJoined += UserJoined;
            _client.UserLeft += UserLeft;
            _client.MessageReceived += MessageReceived;
            _client.MessageDeleted += MessageDeleted;
            _client.JoinedGuild += JoinedGuild;
        }

        private async Task JoinedGuild(SocketGuild guild)
        {
            if (guild.Roles.Where(p => p.Name == "Muted").Count() == 0)
            {
                var mutedPermission = new GuildPermissions(false, false, false, false, false, false, false, false, true, false, false, false, false, false, true, false, false, false, false, false, false, false, false, false, false, false, false, false, false);
                await guild.CreateRoleAsync("Muted", mutedPermission, Color.Red);
            }
            var permission = new OverwritePermissions(PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Inherit, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Inherit, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny);
            foreach (var textChannel in guild.TextChannels)
            {
                var muted = guild.Roles.Where(p => p.Name == "Muted").FirstOrDefault();
                await textChannel.AddPermissionOverwriteAsync(muted, permission, null);
            }
            foreach (var voiceChannel in guild.VoiceChannels)
            {
                var muted = guild.Roles.Where(p => p.Name == "Muted").FirstOrDefault();
                await voiceChannel.AddPermissionOverwriteAsync(muted, permission, null);
            }
            using (discordbotContext db = new discordbotContext())
            {
                var Guild = db.Guild.Where(p => p.ServerId == (long)guild.Id).FirstOrDefault();
                if (Guild == null)
                {
                    await db.Guild.AddAsync(new Guild { ServerId = (long)guild.Id });
                    await db.SaveChangesAsync();
                }
            }
        }

        private async Task CheckBannedUsers()
        {
            while (true)
            {
                using (discordbotContext db = new discordbotContext())
                {
                    if (!(db.Muteduser.Count() == 0) && _client.Guilds.Count() > 0)
                    {
                        var mutes = db.Muteduser.ToList();
                        foreach (var ban in mutes)
                        {
                            var guild = _client.Guilds.Where(p => p.Id == (ulong)ban.ServerId).FirstOrDefault();
                            if (guild == null)
                            {
                                db.Muteduser.Remove(ban);
                                await db.SaveChangesAsync();
                                continue;
                            }
                            var mutedRole = guild.Roles.Where(p => p.Name == "Muted").FirstOrDefault();
                            if (guild.CurrentUser == null)
                                continue;
                            var roles = guild.CurrentUser.Roles;
                            int position = 0;
                            foreach (var item in roles)
                            {
                                if (item.Position > position)
                                    position = item.Position;
                            }
                            var user = guild.Users.Where(p => p.Id == (ulong)ban.UserId).FirstOrDefault();
                            if (guild.CurrentUser.GuildPermissions.ManageRoles == true && position > mutedRole.Position)
                            {
                                if (ban.Duration < DateTime.Now)
                                {
                                    db.Muteduser.Remove(ban);
                                    try
                                    {
                                        await user.RemoveRoleAsync(mutedRole);
                                        var oldRoles = ban.Roles.Split('|');
                                        foreach (var oldRole in oldRoles)
                                        {
                                            var role = guild.Roles.Where(p => p.Name == oldRole).FirstOrDefault();
                                            if (role != null)
                                                await user.AddRoleAsync(role);
                                        }

                                        var Guild = db.Guild.Where(p => p.ServerId == (long)guild.Id).FirstOrDefault();
                                        if (Guild.LogchannelId != null && Guild.Log == 1)
                                        {
                                            var embed = new EmbedBuilder();
                                            embed.WithDescription($"{user.Mention} wurde entmuted.");
                                            embed.WithColor(new Color(0, 255, 0));
                                            var logchannel = guild.TextChannels.Where(p => p.Id == (ulong)Guild.LogchannelId).FirstOrDefault();
                                            await logchannel.SendMessageAsync("", false, embed.Build());
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        Console.WriteLine(e.Message);
                                    }
                                }
                                else
                                {
                                    if (user.Roles.Where(p => p.Id == mutedRole.Id).Count() == 0)
                                    {
                                        try
                                        {
                                            var oldRoles = ban.Roles.Split('|');
                                            foreach (var oldRole in oldRoles)
                                            {
                                                if (!oldRole.Contains("everyone"))
                                                {
                                                    var role = guild.Roles.Where(p => p.Name == oldRole).FirstOrDefault();
                                                    if (role != null)
                                                        await user.RemoveRoleAsync(role);
                                                }
                                            }
                                            await user.AddRoleAsync(mutedRole);
                                        }
                                        catch (Exception e)
                                        {
                                            Console.WriteLine(e.Message);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    await db.SaveChangesAsync();
                }
                await Task.Delay(1000);
            }
        }

        #region OnlineUser
        //private async Task CheckOnlineUsers()
        //{
        //    int onlineUsers = -1;
        //    int crashCounter = 0;
        //    DateTime time = DateTime.Now;
        //    while (true)
        //    {
        //        try
        //        {
        //            List<Server> server = new List<Server>();
        //            ApiRequest DB = new ApiRequest();
        //            server = await DB.GetServer();
        //            int onlinecount = 0;
        //            foreach (var item in server)
        //            {
        //                if (item.Player_online >= 0)
        //                    onlinecount += item.Player_online;
        //            }
        //            var percent = (Convert.ToDouble(onlinecount) / Convert.ToDouble(onlineUsers)) * 100;
        //            if ((percent < 80) && onlineUsers != -1 && onlinecount != 0)
        //            {
        //                var embed = new EmbedBuilder();
        //                embed.WithDescription($"***Server Liste:***");
        //                embed.WithColor(new Color(111, 116, 124));
        //                using (discordbotContext db = new discordbotContext())
        //                {
        //                    string crashedServer = "";
        //                    foreach (var item in server)
        //                    {
        //                        if (item.Player_online >= 0)
        //                        {
        //                            string status = "";
        //                            switch (item.State)
        //                            {
        //                                case 0:
        //                                    status = "Offline";
        //                                    crashedServer = item.Name;
        //                                    break;
        //                                case 1:
        //                                    status = "Slow";
        //                                    break;
        //                                case 2:
        //                                    status = "Online";
        //                                    break;
        //                                default:
        //                                    status = "Unknown";
        //                                    break;
        //                            }
        //                            embed.AddField(item.Name, "Status: **" + status + "** | User online: **" + item.Player_online.ToString() + "**", false);
        //                        }
        //                    }
        //                    crashCounter++;
        //                    TimeSpan span = DateTime.Now - time;
        //                    if (db.Guild.Count() > 0)
        //                    {
        //                        foreach (var item in db.Guild)
        //                        {
        //                            if (item.NotificationchannelId != null && item.Notify == 1 && !String.IsNullOrWhiteSpace(crashedServer))
        //                                await _client.Guilds.Where(p => p.Id == (ulong)item.ServerId).FirstOrDefault().TextChannels.Where(p => p.Id == (ulong)item.NotificationchannelId).FirstOrDefault().SendMessageAsync($"**{crashedServer}** ist gecrashed! Das ist der **{crashCounter}.** Crash in den letzten **{span.Days}D {span.Hours}H {span.Minutes}M!**", false, embed.Build());
        //                            else if (item.NotificationchannelId != null && item.Notify == 1)
        //                                await _client.Guilds.Where(p => p.Id == (ulong)item.ServerId).FirstOrDefault().TextChannels.Where(p => p.Id == (ulong)item.NotificationchannelId).FirstOrDefault().SendMessageAsync($"Die Spieleranzahl ist in den letzten **{span.Days}D {span.Hours}H {span.Minutes}M** schon **{crashCounter} mal** eingebrochen!", false, embed.Build());
        //                        }
        //                    }
        //                }
        //            }
        //            onlineUsers = onlinecount;
        //            if (onlinecount > 0)
        //                await _client.SetGameAsync($"{onlinecount} Players online!", null, ActivityType.Watching);
        //            else
        //                await _client.SetGameAsync($"Auth Server is down!", null, ActivityType.Watching);
        //            await Task.Delay(10000);
        //        }
        //        catch (Exception e)
        //        {
        //            Console.WriteLine("Fehler: " + e.Message + "\n" + e.StackTrace);
        //        }
        //    }
        //}
        #endregion
            
        private async Task MessageReceived(SocketMessage msg)
        {
            using (discordbotContext db = new discordbotContext())
            {
                if (msg.Author.IsBot)
                    return;
                var user = db.User.Where(p => (ulong)p.Id == msg.Author.Id).FirstOrDefault();
                if (user == null)
                {
                    await db.User.AddAsync(new User { Id = (long)msg.Author.Id, Name = msg.Author.Username + "#" + msg.Author.Discriminator });
                    user = db.User.Where(p => (ulong)p.Id == msg.Author.Id).FirstOrDefault();
                }
                else
                {
                    user.Name = msg.Author.Username + "#" + msg.Author.Discriminator;
                }
                var experience = db.Experience.Where(p => (ulong)p.UserId == msg.Author.Id).FirstOrDefault();
                if (experience == null)
                {
                    int id;
                    if (db.Experience.Count() != 0)
                    {
                        var lastEntity = db.Experience.OrderBy(p => p.Id).Last();
                        id = lastEntity.Id + 1;
                    }
                    else
                    {
                        id = 1;
                    }
                    await db.Experience.AddAsync(new Experience { Id = id, Exp = 0, UserId = (long)msg.Author.Id });
                }
                else
                {
                    var oldLevel = Helper.GetLevel(experience.Exp);
                    int textLenght = msg.Content.ToString().Replace("*", string.Empty).Count();
                    if (textLenght >= 50)
                        textLenght = 50;
                    experience.Exp += textLenght * Config.level.expMultiplier;
                    var newLevel = Helper.GetLevel(experience.Exp);
                    SocketGuild guild = ((SocketGuildChannel)msg.Channel).Guild;
                    var Guild = db.Guild.Where(p => p.ServerId == (long)guild.Id).FirstOrDefault();
                    if (newLevel > oldLevel && Guild.Level == 1)
                    {
                        //await msg.Channel.SendMessageAsync($"{msg.Author.Mention} Glückwnsch nya~ ≧◡≦! uwu Du bist level up- (ﾉ◕ヮ◕)ﾉ*:･ﾟ✧ **Level {newLevel}** mach weiter so OwO (づ｡◕‿‿◕｡)づ");

                        var template = new HtmlTemplate(Directory.GetCurrentDirectory() + "/LevelUp/levelup.html");
                        var html = template.Render(new
                        {
                            NAME = msg.Author.Username,
                            LEVEL = newLevel.ToString()
                        });

                        var path = HtmlToImage.Generate(Helper.RemoveSpecialCharacters(msg.Author.Username), html, 300, 100);
                        await msg.Channel.SendFileAsync(path);
                        File.Delete(path);
                    }
                    if (newLevel > oldLevel)
                    {
                        foreach (var mutualGuild in msg.Author.MutualGuilds)
                        {
                            var roleS4 = mutualGuild.Roles.Where(p => p.Name == "S4").FirstOrDefault();
                            var rolePro = mutualGuild.Roles.Where(p => p.Name == "Pro").FirstOrDefault();
                            var roleSemi = mutualGuild.Roles.Where(p => p.Name == "Semi").FirstOrDefault();
                            var roleAmateur = mutualGuild.Roles.Where(p => p.Name == "Amateur").FirstOrDefault();
                            var roleRookie = mutualGuild.Roles.Where(p => p.Name == "Rookie").FirstOrDefault();

                            if (roleS4 != null && rolePro != null && roleSemi != null && roleAmateur != null && roleRookie != null)
                            {
                                var myUser = mutualGuild.Users.Where(p => p.Id == msg.Author.Id).FirstOrDefault();
                                if (newLevel >= 1 && newLevel <= 19)
                                {
                                    if (myUser.Roles.Where(p => p.Name == "S4" || p.Name == "Pro" || p.Name == "Semi" || p.Name == "Amateur").FirstOrDefault() != null)
                                    {
                                        await myUser.RemoveRoleAsync(roleS4);
                                        await myUser.RemoveRoleAsync(rolePro);
                                        await myUser.RemoveRoleAsync(roleSemi);
                                        await myUser.RemoveRoleAsync(roleAmateur);
                                    }
                                    if (myUser.Roles.Where(p => p.Name == "Rookie").FirstOrDefault() == null)
                                        await myUser.AddRoleAsync(roleRookie);
                                }
                                if (newLevel >= 20 && newLevel <= 39)
                                {
                                    if (myUser.Roles.Where(p => p.Name == "Rookie").FirstOrDefault() != null)
                                        await myUser.RemoveRoleAsync(roleRookie);
                                    if (myUser.Roles.Where(p => p.Name == "S4" || p.Name == "Pro" || p.Name == "Semi").FirstOrDefault() != null)
                                    {
                                        await myUser.RemoveRoleAsync(roleS4);
                                        await myUser.RemoveRoleAsync(rolePro);
                                        await myUser.RemoveRoleAsync(roleSemi);
                                    }
                                    if (myUser.Roles.Where(p => p.Name == "Amateur").FirstOrDefault() == null)
                                        await myUser.AddRoleAsync(roleAmateur);
                                }
                                if (newLevel >= 40 && newLevel <= 59)
                                {
                                    if (myUser.Roles.Where(p => p.Name == "Amateur").FirstOrDefault() != null)
                                        await myUser.RemoveRoleAsync(roleAmateur);
                                    if (myUser.Roles.Where(p => p.Name == "S4" || p.Name == "Pro" || p.Name == "Rookie").FirstOrDefault() != null)
                                    {
                                        await myUser.RemoveRoleAsync(roleS4);
                                        await myUser.RemoveRoleAsync(rolePro);
                                        await myUser.RemoveRoleAsync(roleRookie);
                                    }
                                    if (myUser.Roles.Where(p => p.Name == "Semi").FirstOrDefault() == null)
                                        await myUser.AddRoleAsync(roleSemi);
                                }
                                if (newLevel >= 60 && newLevel <= 79)
                                {
                                    if (myUser.Roles.Where(p => p.Name == "Semi").FirstOrDefault() != null)
                                        await myUser.RemoveRoleAsync(roleSemi);
                                    if (myUser.Roles.Where(p => p.Name == "S4" || p.Name == "Amateur" || p.Name == "Rookie").FirstOrDefault() != null)
                                    {
                                        await myUser.RemoveRoleAsync(roleS4);
                                        await myUser.RemoveRoleAsync(roleAmateur);
                                        await myUser.RemoveRoleAsync(roleRookie);
                                    }
                                    if (myUser.Roles.Where(p => p.Name == "Pro").FirstOrDefault() == null)
                                        await myUser.AddRoleAsync(rolePro);
                                }
                                if (newLevel >= 80)
                                {
                                    if (myUser.Roles.Where(p => p.Name == "Pro").FirstOrDefault() != null)
                                        await myUser.RemoveRoleAsync(rolePro);
                                    if (myUser.Roles.Where(p => p.Name == "Semi" || p.Name == "Amateur" || p.Name == "Rookie").FirstOrDefault() != null)
                                    {
                                        await myUser.RemoveRoleAsync(roleSemi);
                                        await myUser.RemoveRoleAsync(roleAmateur);
                                        await myUser.RemoveRoleAsync(roleRookie);
                                    }
                                    if (myUser.Roles.Where(p => p.Name == "S4").FirstOrDefault() == null)
                                        await myUser.AddRoleAsync(roleS4);
                                }
                            }
                        }
                    }


                    

                    var myGuild = ((SocketGuildChannel)msg.Channel).Guild;
                    var RoleS4 = myGuild.Roles.Where(p => p.Name == "S4").FirstOrDefault();
                    var RolePro = myGuild.Roles.Where(p => p.Name == "Pro").FirstOrDefault();
                    var RoleSemi = myGuild.Roles.Where(p => p.Name == "Semi").FirstOrDefault();
                    var RoleAmateur = myGuild.Roles.Where(p => p.Name == "Amateur").FirstOrDefault();
                    var RoleRookie = myGuild.Roles.Where(p => p.Name == "Rookie").FirstOrDefault();

                    if (RoleS4 != null && RolePro != null && RoleSemi != null && RoleAmateur != null && RoleRookie != null)
                    {
                        var myUser = myGuild.Users.Where(p => p.Id == msg.Author.Id).FirstOrDefault();
                        if (newLevel >= 1 && newLevel <= 19)
                        {
                            if (myUser.Roles.Where(p => p.Name == "S4" || p.Name == "Pro" || p.Name == "Semi" || p.Name == "Amateur").FirstOrDefault() != null)
                            {
                                await myUser.RemoveRoleAsync(RoleS4);
                                await myUser.RemoveRoleAsync(RolePro);
                                await myUser.RemoveRoleAsync(RoleSemi);
                                await myUser.RemoveRoleAsync(RoleAmateur);
                            }
                            if (myUser.Roles.Where(p => p.Name == "Rookie").FirstOrDefault() == null)
                                await myUser.AddRoleAsync(RoleRookie);
                        }
                        if (newLevel >= 20 && newLevel <= 39)
                        {
                            if (myUser.Roles.Where(p => p.Name == "Rookie").FirstOrDefault() != null)
                                await myUser.RemoveRoleAsync(RoleRookie);
                            if (myUser.Roles.Where(p => p.Name == "S4" || p.Name == "Pro" || p.Name == "Semi").FirstOrDefault() != null)
                            {
                                await myUser.RemoveRoleAsync(RoleS4);
                                await myUser.RemoveRoleAsync(RolePro);
                                await myUser.RemoveRoleAsync(RoleSemi);
                            }
                            if (myUser.Roles.Where(p => p.Name == "Amateur").FirstOrDefault() == null)
                                await myUser.AddRoleAsync(RoleAmateur);
                        }
                        if (newLevel >= 40 && newLevel <= 59)
                        {
                            if (myUser.Roles.Where(p => p.Name == "Amateur").FirstOrDefault() != null)
                                await myUser.RemoveRoleAsync(RoleAmateur);
                            if (myUser.Roles.Where(p => p.Name == "S4" || p.Name == "Pro" || p.Name == "Rookie").FirstOrDefault() != null)
                            {
                                await myUser.RemoveRoleAsync(RoleS4);
                                await myUser.RemoveRoleAsync(RolePro);
                                await myUser.RemoveRoleAsync(RoleRookie);
                            }
                            if (myUser.Roles.Where(p => p.Name == "Semi").FirstOrDefault() == null)
                                await myUser.AddRoleAsync(RoleSemi);
                        }
                        if (newLevel >= 60 && newLevel <= 79)
                        {
                            if (myUser.Roles.Where(p => p.Name == "Semi").FirstOrDefault() != null)
                                await myUser.RemoveRoleAsync(RoleSemi);
                            if (myUser.Roles.Where(p => p.Name == "S4" || p.Name == "Amateur" || p.Name == "Rookie").FirstOrDefault() != null)
                            {
                                await myUser.RemoveRoleAsync(RoleS4);
                                await myUser.RemoveRoleAsync(RoleAmateur);
                                await myUser.RemoveRoleAsync(RoleRookie);
                            }
                            if (myUser.Roles.Where(p => p.Name == "Pro").FirstOrDefault() == null)
                                await myUser.AddRoleAsync(RolePro);
                        }
                        if (newLevel >= 80)
                        {
                            if (myUser.Roles.Where(p => p.Name == "Pro").FirstOrDefault() != null)
                                await myUser.RemoveRoleAsync(RolePro);
                            if (myUser.Roles.Where(p => p.Name == "Semi" || p.Name == "Amateur" || p.Name == "Rookie").FirstOrDefault() != null)
                            {
                                await myUser.RemoveRoleAsync(RoleSemi);
                                await myUser.RemoveRoleAsync(RoleAmateur);
                                await myUser.RemoveRoleAsync(RoleRookie);
                            }
                            if (myUser.Roles.Where(p => p.Name == "S4").FirstOrDefault() == null)
                                await myUser.AddRoleAsync(RoleS4);
                        }
                    }
                }
                await db.SaveChangesAsync();


                if (msg.Content.ToLower().Contains("hurensohn") && msg.Author.Id != _client.CurrentUser.Id)
                    await msg.Channel.SendMessageAsync(msg.Author.Mention + " du bist selber ein Hurensohn.");
            }
        }

        private async Task MessageDeleted(Cacheable<IMessage, ulong> message, ISocketMessageChannel channel)
        {

        }

        private async Task UserLeft(SocketGuildUser user)
        {
            using (discordbotContext db = new discordbotContext())
            {
                if (db.Guild.Where(p => p.ServerId == (long)user.Guild.Id).Count() == 0)
                    return;
                if (db.Guild.Where(p => p.ServerId == (long)user.Guild.Id).FirstOrDefault().Notify == 0)
                    return;
                else
                {
                    var channelId = db.Guild.Where(p => p.ServerId == (long)user.Guild.Id).FirstOrDefault().LogchannelId;
                    var embed = new EmbedBuilder();
                    embed.WithDescription($"{user.Mention} left the server!");
                    embed.WithColor(new Color(255, 0, 0));
                    embed.AddField("User ID", user.Id.ToString(), true);
                    embed.AddField("Username", user.Username + "#" + user.Discriminator, true);
                    embed.ThumbnailUrl = user.GetAvatarUrl(ImageFormat.Png, 1024);
                    embed.AddField("Joined Server at", user.JoinedAt.Value.DateTime.ToShortDateString() + " " + user.JoinedAt.Value.DateTime.ToShortTimeString(), false);
                    await _client.GetGuild(user.Guild.Id).GetTextChannel((ulong)channelId).SendMessageAsync("", false, embed.Build());
                }
            }
        }

        private async Task UserJoined(SocketGuildUser user)
        {
            using (discordbotContext db = new discordbotContext())
            {
                if (db.Guild.Where(p => p.ServerId == (long)user.Guild.Id).Count() == 0)
                    return;
                if (db.Guild.Where(p => p.ServerId == (long)user.Guild.Id).FirstOrDefault().Notify == 0)
                    return;

                var memberRole = _client.Guilds.Where(p => p.Id == user.Guild.Id).FirstOrDefault().Roles.Where(p => p.Name == "Mitglied").FirstOrDefault();
                if (memberRole != null)
                    await user.AddRoleAsync(memberRole);
                var channelId = db.Guild.Where(p => p.ServerId == (long)user.Guild.Id).FirstOrDefault().LogchannelId;
                var embed = new EmbedBuilder();
                embed.WithDescription($"{user.Mention} joined the server!");
                embed.WithColor(new Color(0, 255, 0));
                embed.AddField("User ID", user.Id.ToString(), true);
                embed.AddField("Username", user.Username + "#" + user.Discriminator, true);
                embed.ThumbnailUrl = user.GetAvatarUrl(ImageFormat.Png, 1024);
                embed.AddField("Joined Discord at", user.CreatedAt.DateTime.ToShortDateString() + " " + user.CreatedAt.DateTime.ToShortTimeString(), false);
                await _client.GetGuild(user.Guild.Id).GetTextChannel((ulong)channelId).SendMessageAsync("", false, embed.Build());
            }
        }
    }
}
