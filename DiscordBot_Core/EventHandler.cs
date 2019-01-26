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
using DiscordBot_Core.Systems;
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
            new Task(async () => await CheckWarnings(), _cancelationTokenSource.Token, TaskCreationOptions.LongRunning).Start();
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
            using (swaightContext db = new swaightContext())
            {
                var Guild = db.Guild.Where(p => p.ServerId == (long)guild.Id).FirstOrDefault();
                if (Guild == null)
                {
                    await db.Guild.AddAsync(new Guild { ServerId = (long)guild.Id });
                    await db.SaveChangesAsync();
                }
            }
        }

        private async Task CheckWarnings()
        {
            while (true)
            {
                WarnService warn = new WarnService(_client);
                await warn.CheckWarnings();
                await Task.Delay(1000);
            }
        }

        private async Task CheckBannedUsers()
        {
            while (true)
            {
                MuteService mute = new MuteService(_client);
                await mute.CheckMutes();
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
        //                using (swaightContext db = new swaightContext())
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
            if (msg.Author.IsBot)
                return;
            using (swaightContext db = new swaightContext())
            {
                if (db.Badwords.Any(p => Helper.ReplaceCharacter(msg.Content).Contains(p.BadWord, StringComparison.OrdinalIgnoreCase)) && !(msg.Author as SocketGuildUser).GuildPermissions.ManageMessages)
                {
                    await msg.DeleteAsync();
                    SocketGuild dcGuild = ((SocketGuildChannel)msg.Channel).Guild;
                    if (!db.Warning.Where(p => p.UserId == (long)msg.Author.Id && p.ServerId == (long)dcGuild.Id).Any())
                    {
                        await db.Warning.AddAsync(new Warning { ServerId = (long)dcGuild.Id, UserId = (long)msg.Author.Id, ActiveUntil = DateTime.Now.AddHours(1), Counter = 1 });
                        await msg.Channel.SendMessageAsync($"**{msg.Author.Mention} du wurdest für schlechtes Benehmen verwarnt. Warnung 1/3**");
                    }
                    else
                    {
                        var warn = db.Warning.Where(p => p.UserId == (long)msg.Author.Id && p.ServerId == (long)dcGuild.Id).FirstOrDefault();
                        warn.Counter++;
                        await msg.Channel.SendMessageAsync($"**{msg.Author.Mention} du wurdest für schlechtes Benehmen verwarnt. Warnung {warn.Counter}/3**");
                    }
                    var myUser = msg.Author as SocketGuildUser;
                    await Log.Warning(myUser, msg);
                }
                await db.SaveChangesAsync();
            }

            LevelService User = new LevelService(msg);
            await User.SendLevelUp();
            await User.SetRoles();
        }

        private async Task MessageDeleted(Cacheable<IMessage, ulong> message, ISocketMessageChannel channel)
        {

        }

        private async Task UserLeft(SocketGuildUser user)
        {
            using (swaightContext db = new swaightContext())
            {
                if (db.Guild.Where(p => p.ServerId == (long)user.Guild.Id).Count() == 0)
                    return;
                if (db.Guild.Where(p => p.ServerId == (long)user.Guild.Id).FirstOrDefault().Log == 0)
                    return;
                else
                {
                    var channelId = db.Guild.Where(p => p.ServerId == (long)user.Guild.Id).FirstOrDefault().LogchannelId;
                    var embed = new EmbedBuilder();
                    embed.WithDescription($"{user.Mention} left the server!");
                    embed.WithColor(new Color(255, 0, 0));
                    embed.AddField("User ID", user.Id.ToString(), true);
                    embed.AddField("Username", user.Username + "#" + user.Discriminator, true);
                    embed.ThumbnailUrl = user.GetAvatarUrl(ImageFormat.Auto, 1024);
                    embed.AddField("Joined Server at", user.JoinedAt.Value.DateTime.ToShortDateString() + " " + user.JoinedAt.Value.DateTime.ToShortTimeString(), false);
                    await _client.GetGuild(user.Guild.Id).GetTextChannel((ulong)channelId).SendMessageAsync("", false, embed.Build());
                }
            }
        }

        private async Task UserJoined(SocketGuildUser user)
        {
            using (swaightContext db = new swaightContext())
            {
                if (db.Guild.Where(p => p.ServerId == (long)user.Guild.Id).Count() == 0)
                    return;
                if (db.Guild.Where(p => p.ServerId == (long)user.Guild.Id).FirstOrDefault().Log == 0)
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
                embed.ThumbnailUrl = user.GetAvatarUrl(ImageFormat.Auto, 1024);
                embed.AddField("Joined Discord at", user.CreatedAt.DateTime.ToShortDateString() + " " + user.CreatedAt.DateTime.ToShortTimeString(), false);
                await _client.GetGuild(user.Guild.Id).GetTextChannel((ulong)channelId).SendMessageAsync("", false, embed.Build());
            }
        }
    }
}
