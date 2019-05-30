﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Rabbot.Database;
using Rabbot.Services;
using ImageFormat = Discord.ImageFormat;
using System.Collections.Generic;
using Rabbot.API.Models;
using Microsoft.Extensions.Logging;

namespace Rabbot
{
    class EventHandler
    {
        DiscordSocketClient _client;
        CommandService _service;

        public async Task InitializeAsync(DiscordSocketClient client)
        {
            _client = client;
            _service = new CommandService();

            new Task(async () => await CheckBannedUsers(), TaskCreationOptions.LongRunning).Start();
            new Task(async () => await CheckWarnings(), TaskCreationOptions.LongRunning).Start();
            new Task(async () => await CheckSong(), TaskCreationOptions.LongRunning).Start();
            new Task(async () => await CheckDate(), TaskCreationOptions.LongRunning).Start();
            new Task(async () => await CheckAttacks(), TaskCreationOptions.LongRunning).Start();
            _client.UserJoined += UserJoined;
            _client.UserLeft += UserLeft;
            _client.MessageReceived += MessageReceived;
            _client.MessageDeleted += MessageDeleted;
            _client.MessageUpdated += MessageUpdated;
            _client.JoinedGuild += JoinedGuild;
            _client.Connected += _client_Connected;
            _client.ReactionAdded += ReactionAdded;
            _client.ReactionRemoved += ReactionRemoved;
        }

        private async Task ReactionRemoved(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (reaction.User.Value.Id == _client.CurrentUser.Id)
                return;

            using (swaightContext db = new swaightContext())
            {
                if (!db.Attacks.Any())
                    return;

                var dbAtk = db.Attacks.Where(p => p.MessageId == (long)reaction.MessageId).FirstOrDefault();
                if (dbAtk == null)
                    return;

                var atkUserfeature = db.Userfeatures.Where(p => p.UserId == dbAtk.UserId && p.ServerId == dbAtk.ServerId).FirstOrDefault();
                var defUserfeature = db.Userfeatures.Where(p => p.UserId == dbAtk.TargetId && p.ServerId == dbAtk.ServerId).FirstOrDefault();

                var inventoryUser = db.Inventory.Join(db.Items, id => id.ItemId, item => item.Id, (Inventory, Item) => new { Inventory, Item }).Where(p => p.Inventory.FeatureId == atkUserfeature.Id);
                var inventoryTarget = db.Inventory.Join(db.Items, id => id.ItemId, item => item.Id, (Inventory, Item) => new { Inventory, Item }).Where(p => p.Inventory.FeatureId == defUserfeature.Id);
                var zaun = inventoryTarget.Where(p => p.Item.Id == 2).FirstOrDefault();
                var stab = inventoryUser.Where(p => p.Item.Id == 1).FirstOrDefault();

                bool isActiveZaun = false;
                bool isActiveStab = false;

                switch (reaction.Emote.Name)
                {
                    case "🛡":
                        if (reaction.User.Value.Id != (ulong)dbAtk.TargetId)
                            return;
                        if (zaun == null)
                            return;
                        break;
                    case "🗡":
                        if (reaction.User.Value.Id != (ulong)dbAtk.UserId)
                            return;
                        if (stab == null)
                            return;
                        break;
                    default:
                        return;
                }

                foreach (var item in reaction.Message.Value.Reactions)
                {
                    switch (item.Key.Name)
                    {
                        case "🛡":
                            if (item.Value.ReactionCount >= 2)
                                isActiveZaun = true;
                            break;
                        case "🗡":
                            if (item.Value.ReactionCount >= 2)
                                isActiveStab = true;
                            break;
                    }
                }

                var stall = Helper.GetStall(defUserfeature.Wins);
                var atk = stall.Attack;
                var def = stall.Defense;

                if (isActiveZaun)
                    if (inventoryTarget.Count() != 0)
                    {
                        foreach (var item in inventoryTarget)
                        {
                            def += item.Item.Def;
                        }
                    }

                var userStall = Helper.GetStall(atkUserfeature.Wins);
                var userAtk = userStall.Attack;
                var userDef = userStall.Defense;

                if (isActiveStab)
                    if (inventoryUser.Count() != 0)
                    {
                        foreach (var item in inventoryUser)
                        {
                            userAtk += item.Item.Atk;
                        }
                    }

                var sum = userAtk + def;
                var winChance = ((double)userAtk / (double)sum) * 100;

                var atkUsername = db.User.Where(p => p.Id == atkUserfeature.UserId).FirstOrDefault().Name.Split('#')[0];
                var defUsername = db.User.Where(p => p.Id == defUserfeature.UserId).FirstOrDefault().Name.Split('#')[0];

                string chance = $"**{Math.Round(winChance)}% {atkUsername} - {defUsername} {100 - Math.Round(winChance)}%**";

                await reaction.Message.Value.ModifyAsync(msg => msg.Content = chance);
            }
        }

        private async Task ReactionAdded(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (reaction.User.Value.Id == _client.CurrentUser.Id)
                return;

            using (swaightContext db = new swaightContext())
            {
                if (!db.Attacks.Any())
                    return;

                var dbAtk = db.Attacks.Where(p => p.MessageId == (long)reaction.MessageId).FirstOrDefault();
                if (dbAtk == null)
                    return;

                var atkUserfeature = db.Userfeatures.Where(p => p.UserId == dbAtk.UserId && p.ServerId == dbAtk.ServerId).FirstOrDefault();
                var defUserfeature = db.Userfeatures.Where(p => p.UserId == dbAtk.TargetId && p.ServerId == dbAtk.ServerId).FirstOrDefault();

                var inventoryUser = db.Inventory.Join(db.Items, id => id.ItemId, item => item.Id, (Inventory, Item) => new { Inventory, Item }).Where(p => p.Inventory.FeatureId == atkUserfeature.Id);
                var inventoryTarget = db.Inventory.Join(db.Items, id => id.ItemId, item => item.Id, (Inventory, Item) => new { Inventory, Item }).Where(p => p.Inventory.FeatureId == defUserfeature.Id);
                var zaun = inventoryTarget.Where(p => p.Item.Id == 2).FirstOrDefault();
                var stab = inventoryUser.Where(p => p.Item.Id == 1).FirstOrDefault();

                bool isActiveZaun = false;
                bool isActiveStab = false;

                switch (reaction.Emote.Name)
                {
                    case "🛡":
                        if (reaction.User.Value.Id != (ulong)dbAtk.TargetId)
                        {
                            await reaction.Message.Value.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                            return;
                        }
                        if (zaun == null)
                        {
                            await reaction.Message.Value.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                            return;
                        }
                        break;
                    case "🗡":
                        if (reaction.User.Value.Id != (ulong)dbAtk.UserId)
                        {
                            await reaction.Message.Value.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                            return;
                        }
                        if (stab == null)
                        {
                            await reaction.Message.Value.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                            return;
                        }
                        break;
                    default:
                        await reaction.Message.Value.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                        return;
                }

                foreach (var item in reaction.Message.Value.Reactions)
                {
                    switch (item.Key.Name)
                    {
                        case "🛡":
                            if (item.Value.ReactionCount >= 2)
                                isActiveZaun = true;
                            break;
                        case "🗡":
                            if (item.Value.ReactionCount >= 2)
                                isActiveStab = true;
                            break;
                    }
                }

                var stall = Helper.GetStall(defUserfeature.Wins);
                var atk = stall.Attack;
                var def = stall.Defense;

                if (isActiveZaun)
                    if (inventoryTarget.Count() != 0)
                    {
                        foreach (var item in inventoryTarget)
                        {
                            def += item.Item.Def;
                        }
                    }

                var userStall = Helper.GetStall(atkUserfeature.Wins);
                var userAtk = userStall.Attack;
                var userDef = userStall.Defense;

                if (isActiveStab)
                    if (inventoryUser.Count() != 0)
                    {
                        foreach (var item in inventoryUser)
                        {
                            userAtk += item.Item.Atk;
                        }
                    }

                var sum = userAtk + def;
                var winChance = ((double)userAtk / (double)sum) * 100;

                var atkUsername = db.User.Where(p => p.Id == atkUserfeature.UserId).FirstOrDefault().Name.Split('#')[0];
                var defUsername = db.User.Where(p => p.Id == defUserfeature.UserId).FirstOrDefault().Name.Split('#')[0];

                string chance = $"**{Math.Round(winChance)}% {atkUsername} - {defUsername} {100 - Math.Round(winChance)}%**";

                await reaction.Message.Value.ModifyAsync(msg => msg.Content = chance);
            }
        }

        private async Task MessageUpdated(Cacheable<IMessage, ulong> message, SocketMessage newMessage, ISocketMessageChannel channel)
        {
            using (swaightContext db = new swaightContext())
            {
                if (message.Value.Content == newMessage.Content)
                    return;
                var dcUser = message.Value.Author as SocketGuildUser;
                if (dcUser.IsBot)
                    return;
                var dcTextchannel = channel as SocketTextChannel;
                var dbGuild = db.Guild.Where(p => p.ServerId == (long)dcUser.Guild.Id).FirstOrDefault();
                var dcGuild = _client.Guilds.Where(p => p.Id == (ulong)dbGuild.ServerId).FirstOrDefault();
                if (dbGuild.Trash == 0 || dbGuild.TrashchannelId == null)
                    return;
                var dcTrashChannel = dcGuild.TextChannels.Where(p => p.Id == (ulong)dbGuild.TrashchannelId).FirstOrDefault();
                if (dcTrashChannel == null)
                    return;
                EmbedBuilder embed = new EmbedBuilder();
                embed.WithAuthor(dcUser as IUser);
                embed.Description = $"Nachricht von {dcUser.Mention} in {dcTextchannel.Mention} wurde editiert!";
                embed.AddField($"Alte Nachricht:", $"{message.Value.Content}");
                embed.AddField($"Neue Nachricht:", $"{newMessage.Content}");
                DateTime msgTime = message.Value.Timestamp.DateTime.ToLocalTime();
                embed.WithFooter($"Message ID: {message.Value.Id} • {msgTime.ToShortTimeString()} {msgTime.ToShortDateString()}");
                embed.Color = new Color(0, 225, 255);
                await dcTrashChannel.SendMessageAsync(null, false, embed.Build());
            }
        }

        private async Task MessageDeleted(Cacheable<IMessage, ulong> message, ISocketMessageChannel channel)
        {
            using (swaightContext db = new swaightContext())
            {
                var dcUser = message.Value.Author as SocketGuildUser;
                if (dcUser.IsBot)
                    return;
                if (message.Value.Content.StartsWith(Config.bot.cmdPrefix))
                    return;
                var dcTextchannel = channel as SocketTextChannel;
                var dbGuild = db.Guild.Where(p => p.ServerId == (long)dcUser.Guild.Id).FirstOrDefault();
                var dcGuild = _client.Guilds.Where(p => p.Id == (ulong)dbGuild.ServerId).FirstOrDefault();
                if (dbGuild.Trash == 0 || dbGuild.TrashchannelId == null)
                    return;
                var dcTrashChannel = dcGuild.TextChannels.Where(p => p.Id == (ulong)dbGuild.TrashchannelId).FirstOrDefault();
                if (dcTrashChannel == null)
                    return;
                EmbedBuilder embed = new EmbedBuilder();
                embed.WithAuthor(dcUser as IUser);
                embed.Description = $"Nachricht von {dcUser.Mention} in {dcTextchannel.Mention} wurde gelöscht!";
                embed.AddField($"Nachricht:", $"{message.Value.Content}");
                DateTime msgTime = message.Value.Timestamp.DateTime.ToLocalTime();
                embed.WithFooter($"Message ID: {message.Value.Id} • {msgTime.ToShortTimeString()} {msgTime.ToShortDateString()}");
                embed.Color = new Color(255, 0, 0);
                await dcTrashChannel.SendMessageAsync(null, false, embed.Build());
            }
        }

        private async Task _client_Connected()
        {
            using (swaightContext db = new swaightContext())
            {
                if (!db.Event.Where(p => p.Status == 1).Any())
                    return;
                var myEvent = db.Event.FirstOrDefault(p => p.Status == 1);
                await _client.SetGameAsync($"{myEvent.Name} Event aktiv!", null, ActivityType.Watching);
            }
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

        private async Task CheckDate()
        {
            while (true)
            {
                using (swaightContext db = new swaightContext())
                {
                    if (db.Currentday.Any())
                    {
                        if (db.Currentday.FirstOrDefault().Date.ToShortDateString() == DateTime.Now.ToShortDateString())
                        {
                            await Task.Delay(1000);
                            continue;
                        }
                        else
                        {
                            db.Currentday.FirstOrDefault().Date = DateTime.Now;
                            new Task(async () => await NewDay(), TaskCreationOptions.LongRunning).Start();
                        }
                    }
                    else
                        await db.Currentday.AddAsync(new Currentday { Date = DateTime.Now });

                    await db.SaveChangesAsync();
                }
                await Task.Delay(1000);
            }
        }

        private async Task NewDay()
        {
            await RipGoat();
            using (swaightContext db = new swaightContext())
            {
                if (db.Songlist.Any())
                {
                    var songs = db.Songlist;
                    Random rnd = new Random();
                    int counter = 1;
                    int random = rnd.Next(1, songs.Count() + 1);
                    foreach (var song in songs)
                    {
                        song.Active = 0;
                        if (counter == random)
                        {
                            song.Active = 1;
                        }
                        counter++;
                    }
                }
                var trades = db.Userfeatures.Where(p => p.Trades > 0);
                foreach (var trade in trades)
                {
                    trade.Trades = 0;
                }

                var attacks = db.Userfeatures.Where(p => p.Attacks > 0);
                foreach (var attack in attacks)
                {
                    attack.Attacks = 0;
                }
                var users = db.Userfeatures.Where(p => p.NamechangeUntil != null);
                foreach (var user in users)
                {
                    if (user.NamechangeUntil.Value.ToShortDateString() == DateTime.Now.ToShortDateString())
                    {
                        user.NamechangeUntil = null;
                        var dcUser = _client.Guilds.Where(p => p.Id == (ulong)user.ServerId).FirstOrDefault().Users.Where(p => p.Id == (ulong)user.UserId).FirstOrDefault() as SocketGuildUser;
                        await dcUser.SendMessageAsync($"Dein Namechange zu **{dcUser.Nickname}** auf dem **{dcUser.Guild.Name}** Server ist abgelaufen.");
                        await dcUser.ModifyAsync(p => p.Nickname = null);
                    }
                }
                try
                {
                    if (db.Pot.Any())
                    {
                        var servers = db.Pot.GroupBy(p => p.ServerId).Select(p => p.Key).ToList();
                        foreach (var serverId in servers)
                        {
                            var pot = db.Pot.Where(p => p.ServerId == serverId);
                            var sum = pot.Sum(p => p.Goats);
                            double min = 0;
                            List<PotUser> myList = new List<PotUser>();
                            foreach (var item in pot)
                            {
                                var chance = (double)item.Goats / (double)sum * 100;
                                myList.Add(new PotUser { UserId = (long)item.UserId, Min = min + 1, Max = chance + min, Chance = (int)Math.Round(chance) });
                                min = chance + min;
                            }
                            foreach (var item in myList)
                            {
                                item.Max = Math.Floor(item.Max);
                                item.Min = Math.Floor(item.Min);
                            }

                            Random rnd = new Random();
                            var luck = rnd.Next(1, 101);
                            var botChannelId = db.Guild.Where(p => p.ServerId == (long)serverId).FirstOrDefault().Botchannelid;
                            var dcServer = _client.Guilds.Where(p => p.Id == (ulong)serverId).FirstOrDefault();
                            var dcBotChannel = dcServer.TextChannels.Where(p => p.Id == (ulong)botChannelId).FirstOrDefault();

                            foreach (var item in myList)
                            {
                                if (item.Min <= luck && item.Max >= luck)
                                {
                                    var dcUser = dcServer.Users.Where(p => p.Id == (ulong)item.UserId).FirstOrDefault();
                                    var dbUserfeature = db.Userfeatures.Where(p => p.ServerId == (long)dcServer.Id && p.UserId == item.UserId).FirstOrDefault();
                                    EmbedBuilder embed = new EmbedBuilder();
                                    embed.Color = Color.Green;
                                    var stall = Helper.GetStall(dbUserfeature.Wins);
                                    if (Helper.IsFull(dbUserfeature.Goats + sum, dbUserfeature.Wins))
                                    {
                                        if (dcUser != null)
                                            embed.Description = $"Der **Gewinner** von **{sum} Ziegen** aus dem Pot ist {dcUser.Mention} mit einer Chance von **{item.Chance}%**!\nLeider passen in deinen Stall nur **{stall.Capacity} Ziegen**, deswegen sind dir **{sum - stall.Capacity} Ziegen** wieder **entlaufen**..";
                                        dbUserfeature.Goats = stall.Capacity;
                                    }
                                    else
                                    {
                                        if (dcUser != null)
                                            embed.Description = $"Der **Gewinner** von **{sum} Ziegen** aus dem Pot ist {dcUser.Mention} mit einer Chance von **{item.Chance}%**!";
                                        dbUserfeature.Goats += sum;

                                    }
                                    foreach (var myPot in pot)
                                    {
                                        db.Pot.Remove(myPot);
                                    }
                                    await db.SaveChangesAsync();
                                    await dcBotChannel.SendMessageAsync(null, false, embed.Build());

                                }
                            }

                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message + " " + e.StackTrace);
                }
                await db.SaveChangesAsync();
            }
        }

        private async Task RipGoat()
        {
            try
            {
                using (swaightContext db = new swaightContext())
                {
                    foreach (var guild in _client.Guilds)
                    {
                        foreach (var user in guild.Users)
                        {
                            var dbUser = db.Userfeatures.Where(p => p.UserId == (long)user.Id && p.ServerId == (long)guild.Id).FirstOrDefault();
                            if (dbUser == null)
                                continue;
                            if (dbUser.Goats == 0)
                                continue;
                            if (dbUser.Lastmessage == null)
                                continue;

                            if (dbUser.Lastmessage.Value.ToShortDateString() != DateTime.Now.AddDays(-1).ToShortDateString() && !(DateTime.Now.ToShortDateString() == dbUser.Lastmessage.Value.ToShortDateString()))
                            {
                                Random rnd = new Random();
                                int deadGoats;
                                var mainUser = db.User.Where(p => p.Id == dbUser.UserId).FirstOrDefault();
                                if (dbUser.Goats > 4 && dbUser.Goats <= 15)
                                {
                                    deadGoats = rnd.Next(5, dbUser.Goats + 1);
                                    dbUser.Goats -= deadGoats;
                                    if (mainUser.Notify == 1)
                                        await user.SendMessageAsync($"Hey, du musst mal auf deine Ziegen aufpassen und wieder auf **{guild.Name}** vorbei schauen..\nHeute sind wegen Inaktivität **{deadGoats} Ziegen** gestorben..\nFalls ich dich **nerve**, kannst du mich mit **'{Config.bot.cmdPrefix}hdf'** stumm schalten.");
                                    await db.SaveChangesAsync();
                                }
                                else if (dbUser.Goats > 15)
                                {
                                    deadGoats = rnd.Next(5, 16);
                                    dbUser.Goats -= deadGoats;
                                    if (mainUser.Notify == 1)
                                        await user.SendMessageAsync($"Hey, du musst mal auf deine Ziegen aufpassen und wieder auf **{guild.Name}** vorbei schauen..\nHeute sind wegen Inaktivität **{deadGoats} Ziegen** gestorben..\nFalls ich dich **nerve**, kannst du mich mit **'{Config.bot.cmdPrefix}hdf'** stumm schalten.");
                                    await db.SaveChangesAsync();
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + "\n" + e.StackTrace);
            }

        }

        private async Task CheckSong()
        {
            while (true)
            {
                foreach (var guild in _client.Guilds)
                {
                    foreach (var user in guild.Users)
                    {
                        if (user.Activity is SpotifyGame song)
                        {
                            using (swaightContext db = new swaightContext())
                            {
                                if (!db.Songlist.Where(p => p.Active == 1).Any())
                                    continue;

                                if (song.TrackUrl == db.Songlist.Where(p => p.Active == 1).FirstOrDefault().Link)
                                {
                                    var musicrank = db.Musicrank.Where(p => p.UserId == (long)user.Id && p.ServerId == (long)guild.Id).FirstOrDefault() ?? db.Musicrank.AddAsync(new Musicrank { UserId = (long)user.Id, ServerId = (long)guild.Id, Sekunden = 0, Date = DateTime.Now }).Result.Entity;
                                    if (musicrank.Date.Value.ToShortDateString() != DateTime.Now.ToShortDateString())
                                    {
                                        musicrank.Sekunden = 0;
                                        musicrank.Date = DateTime.Now;
                                    }
                                    musicrank.Sekunden += 10;
                                    await db.SaveChangesAsync();
                                    if (musicrank.Sekunden == 300)
                                    {
                                        var channel = await user.GetOrCreateDMChannelAsync();
                                        var msgs = channel.GetMessagesAsync(1).Flatten();
                                        var exp = db.Userfeatures.Where(p => p.ServerId == (long)guild.Id && p.UserId == (long)user.Id).FirstOrDefault();
                                        if (msgs != null)
                                        {
                                            var msg = await msgs.FirstOrDefault() as IMessage;
                                            if (msg != null)
                                                if (!(msg.Content.Contains("Glückwunsch, du hast einen Bonus") && msg.Timestamp.DateTime.ToShortDateString() == DateTime.Now.ToShortDateString()))
                                                {
                                                    var songToday = db.Songlist.Where(p => p.Active == 1).FirstOrDefault();
                                                    if (exp != null)
                                                        await channel.SendMessageAsync($"Glückwunsch, du hast einen Bonus von **10 Ziegen** für das Hören von '**{songToday.Name}**' erhalten!");
                                                }
                                        }
                                        if (exp != null)
                                            exp.Goats += 10;
                                        await db.SaveChangesAsync();
                                    }
                                }
                            }
                        }
                    }
                }
                await Task.Delay(10000);
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

        private async Task CheckAttacks()
        {
            while (true)
            {
                AttackService attacks = new AttackService(_client);
                await attacks.CheckAttacks();
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
                    await Logging.Warning(myUser, msg);
                }
                await db.SaveChangesAsync();
            }

            LevelService User = new LevelService(msg);
            await User.SendLevelUp();
            await User.SetRoles();
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