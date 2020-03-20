using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Threading.Tasks;
using Rabbot.Database;
using Rabbot.Services;
using ImageFormat = Discord.ImageFormat;
using System.Collections.Generic;
using Rabbot.API.Models;
using Rabbot.API;
using System.Runtime.InteropServices;
using Serilog;
using Serilog.Core;

namespace Rabbot
{
    class EventHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commandService;
        private readonly StreakService _streakService;
        private readonly AttackService _attackService;
        private readonly LevelService _levelService;
        private static readonly ILogger _logger = Log.ForContext(Serilog.Core.Constants.SourceContextPropertyName, nameof(EventHandler));

        public EventHandler(DiscordSocketClient client, CommandService commandService, StreakService streakService, AttackService attackService, LevelService levelService)
        {
            _attackService = attackService;
            _streakService = streakService;
            _commandService = commandService;
            _levelService = levelService;
            _client = client;
            InitializeAsync();
        }

        public void InitializeAsync()
        {
            new Task(async () => await CheckBannedUsers(), TaskCreationOptions.LongRunning).Start();
            new Task(async () => await CheckWarnings(), TaskCreationOptions.LongRunning).Start();
            new Task(async () => await CheckSong(), TaskCreationOptions.LongRunning).Start();
            new Task(async () => await CheckDate(), TaskCreationOptions.LongRunning).Start();
            new Task(async () => await CheckAttacks(), TaskCreationOptions.LongRunning).Start();
            new Task(async () => await CheckItems(), TaskCreationOptions.LongRunning).Start();
            new Task(async () => await CheckPlayers(), TaskCreationOptions.LongRunning).Start();
            _logger.Information($"{nameof(EventHandler)}: Loaded successfully");
            _client.UserJoined += UserJoined;
            _client.UserLeft += UserLeft;
            _client.MessageReceived += MessageReceived;
            _client.MessageDeleted += MessageDeleted;
            _client.MessageUpdated += MessageUpdated;
            _client.JoinedGuild += JoinedGuild;
            _client.Connected += _client_Connected;
            _client.ReactionAdded += ReactionAdded;
            _client.ReactionRemoved += ReactionRemoved;
            _client.UserUpdated += UserUpdated;
        }

        private async Task UserUpdated(SocketUser oldUser, SocketUser newUser)
        {
            if (oldUser.Username != newUser.Username)
            {
                using (swaightContext db = new swaightContext())
                {
                    if (db.User.FirstOrDefault(p => p.Id == newUser.Id) == null)
                        await db.User.AddAsync(new User { Id = newUser.Id, Name = $"{newUser.Username}#{newUser.Discriminator}" });
                    if (db.Namechanges.Where(p => p.UserId == newUser.Id).OrderByDescending(p => p.Date).FirstOrDefault()?.NewName == oldUser.Username)
                    {
                        await db.Namechanges.AddAsync(new Namechanges { UserId = newUser.Id, NewName = newUser.Username, Date = DateTime.Now });
                    }
                    else
                    {
                        await db.Namechanges.AddAsync(new Namechanges { UserId = newUser.Id, NewName = oldUser.Username, Date = DateTime.Now.AddMinutes(-1) });
                        await db.Namechanges.AddAsync(new Namechanges { UserId = newUser.Id, NewName = newUser.Username, Date = DateTime.Now });
                    }
                    await db.SaveChangesAsync();
                }
            }
        }

        private async Task ReactionRemoved(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel channel, SocketReaction reaction)
        {
            await ResetItem(reaction, channel);

            if (reaction.User.Value.IsBot)
                return;
            if (!reaction.Message.IsSpecified)
                return;
            if (!reaction.Message.Value.Embeds.Any())
                return;
            if (reaction.Message.Value.Embeds.First().Description == null)
                return;
            if (!reaction.Message.Value.Embeds.First().Description.Contains("Slot Machine"))
                return;
            if (Helper.cooldown.TryGetValue(reaction.UserId, out DateTime cooldownends))
            {
                if (cooldownends > DateTime.Now)
                    return;
            }

            if (reaction.Emote is Emote emote)
            {
                if (emote.Id == Constants.Sword.Id || emote.Id == Constants.Shield.Id)
                    return;

                if (emote.Id != Constants.slot.Id)
                {
                    await reaction.Message.Value.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                    return;
                }
            }
            else
            {
                await reaction.Message.Value.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
            }

            var guild = (channel as SocketGuildChannel).Guild;
            var user = guild.Users.First(p => p.Id == reaction.UserId);
            var username = user.Nickname ?? user.Username;
            var msg = cache.Value;
            if (user != null)
            {
                if (reaction.Message.Value.Embeds.FirstOrDefault().Description.ToLower().Contains(username.ToLower()))
                {
                    await Helper.UpdateSpin(channel, user, msg, _client, 0, false);
                    if (Helper.cooldown.TryGetValue(reaction.UserId, out DateTime endsAt))
                    {
                        var difference = endsAt.Subtract(DateTime.UtcNow);
                        var time = DateTime.Now.AddSeconds(1);
                        Helper.cooldown.TryUpdate(reaction.UserId, time, endsAt);
                    }
                    else
                    {
                        Helper.cooldown.TryAdd(reaction.UserId, DateTime.Now.AddSeconds(1));
                    }
                    return;
                }
            }

            await reaction.Message.Value.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
        }

        private async Task ReactionAdded(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel channel, SocketReaction reaction)
        {
            switch (reaction.Message.Value?.Embeds.FirstOrDefault()?.Title)
            {
                case "Combi Anfrage":
                    using (swaightContext db = new swaightContext())
                    {
                        var combi = db.Combi.FirstOrDefault(p => p.MessageId == reaction.MessageId);
                        if (combi.CombiUserId != reaction.UserId && !reaction.User.Value.IsBot)
                        {
                            await reaction.Message.Value.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                            return;
                        }
                        if (!reaction.User.Value.IsBot)
                        {
                            switch (reaction.Emote.Name)
                            {
                                case "✅":
                                    combi.Accepted = true;
                                    combi.MessageId = null;

                                    await reaction.Channel.SendMessageAsync($"{reaction.User.Value.Mention} du hast die Anfrage erfolgreich **angenommen**!");
                                    await reaction.Message.Value.DeleteAsync();
                                    break;
                                case "⛔":
                                    db.Combi.Remove(combi);
                                    await reaction.Channel.SendMessageAsync($"{reaction.User.Value.Mention} du hast die Anfrage erfolgreich **abgelehnt**!");
                                    await reaction.Message.Value.DeleteAsync();
                                    break;
                                default:
                                    await reaction.Message.Value.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                                    break;
                            }
                        }
                        await db.SaveChangesAsync();
                    }
                    break;
                default:
                    await SetItem(reaction, channel);

                    if (reaction.User.Value.IsBot)
                        return;


                    if ((reaction.Emote.Name == Constants.thumbsUp.Name || reaction.Emote.Name == Constants.thumbsDown.Name) && reaction.Channel.Name.Contains("blog"))
                    {
                        var messages = await reaction.Channel.GetMessagesAsync(100).FlattenAsync();
                        foreach (IUserMessage message in messages)
                        {
                            if (message.Id != reaction.MessageId)
                                continue;

                            if (message.Reactions.Any(p => p.Key.Name == Constants.thumbsDown.Name) && reaction.Emote.Name == Constants.thumbsUp.Name)
                                await message.RemoveReactionAsync(Constants.thumbsDown, reaction.User.Value);
                            if (message.Reactions.Any(p => p.Key.Name == Constants.thumbsUp.Name) && reaction.Emote.Name == Constants.thumbsDown.Name)
                                await message.RemoveReactionAsync(Constants.thumbsUp, reaction.User.Value);

                            return;
                        }
                    }

                    if (!reaction.Message.IsSpecified)
                        return;
                    if (!reaction.Message.Value.Embeds.Any())
                        return;
                    if (reaction.Message.Value.Embeds.First().Description == null)
                        return;
                    if (!reaction.Message.Value.Embeds.First().Description.Contains("Slot Machine"))
                        return;
                    if (Helper.cooldown.TryGetValue(reaction.UserId, out DateTime cooldownends))
                    {
                        if (cooldownends > DateTime.Now)
                            return;
                    }

                    if (reaction.Emote is Emote emote)
                    {
                        if (emote.Id == Constants.Sword.Id || emote.Id == Constants.Shield.Id)
                            return;

                        if (emote.Id != Constants.slot.Id)
                        {
                            await reaction.Message.Value.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                            return;
                        }
                    }
                    else
                    {
                        await reaction.Message.Value.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                    }

                    var guild = (channel as SocketGuildChannel).Guild;
                    var user = guild.Users.First(p => p.Id == reaction.UserId);
                    var username = user.Nickname ?? user.Username;
                    var msg = cache.Value;
                    if (user != null)
                    {
                        if (reaction.Message.Value.Embeds.FirstOrDefault().Description.ToLower().Contains(username.ToLower()))
                        {
                            await Helper.UpdateSpin(channel, user, msg, _client, 0, false);
                            if (Helper.cooldown.TryGetValue(reaction.UserId, out DateTime endsAt))
                            {
                                var difference = endsAt.Subtract(DateTime.UtcNow);
                                var time = DateTime.Now.AddSeconds(1);
                                Helper.cooldown.TryUpdate(reaction.UserId, time, endsAt);
                            }
                            else
                            {
                                Helper.cooldown.TryAdd(reaction.UserId, DateTime.Now.AddSeconds(1));
                            }
                            return;
                        }
                    }

                    await reaction.Message.Value.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                    break;
            }
        }

        private async Task ResetItem(SocketReaction reaction, ISocketMessageChannel channel)
        {
            if (reaction.User.Value.Id == _client.CurrentUser.Id)
                return;

            var dcGuild = (channel as SocketGuildChannel).Guild;
            var emote = reaction.Emote as Emote;

            using (swaightContext db = new swaightContext())
            {
                if (!db.Attacks.Any())
                    return;

                var dbAtk = db.Attacks.FirstOrDefault(p => p.MessageId == reaction.MessageId);
                if (dbAtk == null)
                    return;

                var atkUserfeature = db.Userfeatures.FirstOrDefault(p => p.UserId == dbAtk.UserId && p.ServerId == dbAtk.ServerId);
                var defUserfeature = db.Userfeatures.FirstOrDefault(p => p.UserId == dbAtk.TargetId && p.ServerId == dbAtk.ServerId);

                var inventoryUser = db.Inventory.Join(db.Items, id => id.ItemId, item => item.Id, (Inventory, Item) => new { Inventory, Item }).Where(p => p.Inventory.FeatureId == atkUserfeature.Id);
                var inventoryTarget = db.Inventory.Join(db.Items, id => id.ItemId, item => item.Id, (Inventory, Item) => new { Inventory, Item }).Where(p => p.Inventory.FeatureId == defUserfeature.Id);
                var zaun = inventoryTarget.FirstOrDefault(p => p.Item.Id == 2);
                var stab = inventoryUser.FirstOrDefault(p => p.Item.Id == 1);

                bool isActiveZaun = false;
                bool isActiveStab = false;


                if (emote.Id == Constants.Shield.Id)
                {
                    if (reaction.User.Value.Id != (ulong)dbAtk.TargetId)
                        return;
                    if (zaun == null)
                        return;
                }
                else if (emote.Id == Constants.Sword.Id)
                {
                    if (reaction.User.Value.Id != (ulong)dbAtk.UserId)
                        return;
                    if (stab == null)
                        return;
                }
                else
                {
                    return;
                }


                foreach (var item in reaction.Message.Value.Reactions)
                {
                    var Emote = item.Key as Emote;
                    if (Emote.Id == Constants.Shield.Id)
                    {
                        if (item.Value.ReactionCount >= 2)
                            isActiveZaun = true;
                    }
                    else if (Emote.Id == Constants.Sword.Id)
                    {
                        if (item.Value.ReactionCount >= 2)
                            isActiveStab = true;
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

                var atkUser = dcGuild.Users.FirstOrDefault(p => p.Id == (ulong)dbAtk.UserId);
                var defUser = dcGuild.Users.FirstOrDefault(p => p.Id == (ulong)dbAtk.TargetId);

                string chance = $"**{Math.Round(winChance)}% {atkUser.Mention} - {defUser.Mention} {100 - Math.Round(winChance)}%**";

                await reaction.Message.Value.ModifyAsync(msg => msg.Content = chance);
            }
        }
        private async Task SetItem(SocketReaction reaction, ISocketMessageChannel channel)
        {
            if (reaction.User.Value.Id == _client.CurrentUser.Id)
                return;

            var dcGuild = (channel as SocketGuildChannel).Guild;
            var emote = reaction.Emote as Emote;

            using (swaightContext db = new swaightContext())
            {
                if (!db.Attacks.Any())
                    return;

                var dbAtk = db.Attacks.FirstOrDefault(p => p.MessageId == reaction.MessageId);
                if (dbAtk == null)
                    return;

                var atkUserfeature = db.Userfeatures.FirstOrDefault(p => p.UserId == dbAtk.UserId && p.ServerId == dbAtk.ServerId);
                var defUserfeature = db.Userfeatures.FirstOrDefault(p => p.UserId == dbAtk.TargetId && p.ServerId == dbAtk.ServerId);

                var inventoryUser = db.Inventory.Join(db.Items, id => id.ItemId, item => item.Id, (Inventory, Item) => new { Inventory, Item }).Where(p => p.Inventory.FeatureId == atkUserfeature.Id);
                var inventoryTarget = db.Inventory.Join(db.Items, id => id.ItemId, item => item.Id, (Inventory, Item) => new { Inventory, Item }).Where(p => p.Inventory.FeatureId == defUserfeature.Id);
                var zaun = inventoryTarget.FirstOrDefault(p => p.Item.Id == 2);
                var stab = inventoryUser.FirstOrDefault(p => p.Item.Id == 1);

                bool isActiveZaun = false;
                bool isActiveStab = false;

                if (emote.Id == Constants.Shield.Id)
                {
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
                }
                else if (emote.Id == Constants.Sword.Id)
                {
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
                }
                else
                {
                    await reaction.Message.Value.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                    return;
                }


                foreach (var item in reaction.Message.Value.Reactions)
                {
                    var Emote = item.Key as Emote;
                    if (Emote.Id == Constants.Shield.Id)
                    {
                        if (item.Value.ReactionCount >= 2)
                            isActiveZaun = true;
                    }
                    else if (Emote.Id == Constants.Sword.Id)
                    {
                        if (item.Value.ReactionCount >= 2)
                            isActiveStab = true;
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

                var atkUser = dcGuild.Users.FirstOrDefault(p => p.Id == (ulong)dbAtk.UserId);
                var defUser = dcGuild.Users.FirstOrDefault(p => p.Id == (ulong)dbAtk.TargetId);

                string chance = $"**{Math.Round(winChance)}% {atkUser.Mention} - {defUser.Mention} {100 - Math.Round(winChance)}%**";

                await reaction.Message.Value.ModifyAsync(msg => msg.Content = chance);
            }
        }

        private async Task MessageUpdated(Cacheable<IMessage, ulong> message, SocketMessage newMessage, ISocketMessageChannel channel)
        {
            using (swaightContext db = new swaightContext())
            {
                SocketGuild dcServer = ((SocketGuildChannel)newMessage.Channel).Guild;
                if (db.Badwords.Where(p => p.ServerId == dcServer.Id).Any(p => Helper.ReplaceCharacter(newMessage.Content).Contains(p.BadWord, StringComparison.OrdinalIgnoreCase)) && !(newMessage.Author as SocketGuildUser).GuildPermissions.ManageMessages)
                {
                    await newMessage.DeleteAsync();
                    var myUser = newMessage.Author as SocketGuildUser;
                    if (db.Muteduser.Where(p => p.UserId == newMessage.Author.Id && p.ServerId == myUser.Guild.Id).Any())
                        return;
                    if (myUser.Roles.Where(p => p.Name == "Muted").Any())
                        return;
                    if (!db.Warning.Where(p => p.UserId == newMessage.Author.Id && p.ServerId == dcServer.Id).Any())
                    {
                        await db.Warning.AddAsync(new Warning { ServerId = dcServer.Id, UserId = newMessage.Author.Id, ActiveUntil = DateTime.Now.AddHours(1), Counter = 1 });
                        await newMessage.Channel.SendMessageAsync($"**{newMessage.Author.Mention} du wurdest für schlechtes Benehmen verwarnt. Warnung 1/3**");
                    }
                    else
                    {
                        var warn = db.Warning.FirstOrDefault(p => p.UserId == newMessage.Author.Id && p.ServerId == dcServer.Id);
                        warn.Counter++;
                        await newMessage.Channel.SendMessageAsync($"**{newMessage.Author.Mention} du wurdest für schlechtes Benehmen verwarnt. Warnung {warn.Counter}/3**");
                    }
                    var badword = db.Badwords.FirstOrDefault(p => Helper.ReplaceCharacter(newMessage.Content).Contains(p.BadWord, StringComparison.OrdinalIgnoreCase) && p.ServerId == dcServer.Id).BadWord;
                    await Logging.Warning(myUser, newMessage, badword);

                }
                if (message.Value.Content == newMessage.Content)
                    return;
                var dcUser = message.Value.Author as SocketGuildUser;
                if (dcUser.IsBot)
                    return;
                var dcTextchannel = channel as SocketTextChannel;
                var dbGuild = db.Guild.FirstOrDefault(p => p.ServerId == dcUser.Guild.Id);
                var dcGuild = _client.Guilds.FirstOrDefault(p => p.Id == (ulong)dbGuild.ServerId);
                if (dbGuild.Trash == 0 || dbGuild.TrashchannelId == null)
                    return;
                var dcTrashChannel = dcGuild.TextChannels.FirstOrDefault(p => p.Id == (ulong)dbGuild.TrashchannelId);
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
                if (message.Value == null)
                    return;

                if (message.Value.Author is SocketGuildUser dcUser)
                {
                    if (dcUser.IsBot)
                        return;
                    if (message.Value.Content.StartsWith(Config.bot.cmdPrefix))
                        return;
                    var dcTextchannel = channel as SocketTextChannel;
                    var dbGuild = db.Guild.FirstOrDefault(p => p.ServerId == dcUser.Guild.Id);
                    var dcGuild = _client.Guilds.FirstOrDefault(p => p.Id == (ulong)dbGuild.ServerId);
                    if (dbGuild.Trash == 0 || dbGuild.TrashchannelId == null)
                        return;
                    var dcTrashChannel = dcGuild.TextChannels.FirstOrDefault(p => p.Id == (ulong)dbGuild.TrashchannelId);
                    if (dcTrashChannel == null)
                        return;
                    EmbedBuilder embed = new EmbedBuilder();
                    embed.WithAuthor(dcUser as IUser);
                    embed.Description = $"Nachricht von {dcUser.Mention} in {dcTextchannel.Mention} wurde gelöscht!";
                    var attachment = message.Value?.Attachments;
                    var attachmentlink = attachment?.FirstOrDefault()?.Url;
                    embed.AddField($"Nachricht:", $"{message.Value?.Content} {(attachmentlink != null ? $"\nAttachment: {attachmentlink}" : $"")}");
                    DateTime msgTime = message.Value.Timestamp.DateTime.ToLocalTime();
                    embed.WithFooter($"Message ID: {message.Value.Id} • {msgTime.ToShortTimeString()} {msgTime.ToShortDateString()}");
                    embed.Color = new Color(255, 0, 0);
                    await dcTrashChannel.SendMessageAsync(null, false, embed.Build());
                }
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
            if (!guild.Roles.Where(p => p.Name == "Muted").Any())
            {
                var mutedPermission = new GuildPermissions(false, false, false, false, false, false, false, false, true, false, false, false, false, false, true, false, false, false, false, false, false, false, false, false, false, false, false, false, false);
                await guild.CreateRoleAsync("Muted", mutedPermission, Color.Red);
            }
            var permission = new OverwritePermissions(PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Inherit, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Inherit, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny);
            foreach (var textChannel in guild.TextChannels)
            {
                var muted = guild.Roles.FirstOrDefault(p => p.Name == "Muted");
                await textChannel.AddPermissionOverwriteAsync(muted, permission, null);
            }
            foreach (var voiceChannel in guild.VoiceChannels)
            {
                var muted = guild.Roles.FirstOrDefault(p => p.Name == "Muted");
                await voiceChannel.AddPermissionOverwriteAsync(muted, permission, null);
            }
            using (swaightContext db = new swaightContext())
            {
                var Guild = db.Guild.FirstOrDefault(p => p.ServerId == guild.Id);
                if (Guild == null)
                {
                    await db.Guild.AddAsync(new Guild { ServerId = guild.Id });
                    await db.SaveChangesAsync();
                }
            }
        }

        private async Task CheckPlayers()
        {
            while (true)
            {
                using (swaightContext db = new swaightContext())
                {
                    try
                    {
                        var remnantsPlayers = ApiRequest.RemDB_APIRequest();
                        var officialPlayers = ApiRequest.Official_APIRequest();
                        await db.Officialplayer.AddAsync(new Officialplayer { Playercount = officialPlayers, Date = DateTime.Now });
                        if (int.TryParse(remnantsPlayers, out int count))
                        {
                            await db.Remnantsplayer.AddAsync(new Remnantsplayer { Playercount = count, Date = DateTime.Now });
                        }
                        await db.SaveChangesAsync();
                    }
                    catch (Exception e)
                    {
                        _logger.Error(e, $"Error while checking playercounts");
                    }
                }
                await Task.Delay(60000);
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
                var streaks = db.Userfeatures.Where(p => p.TodaysWords > 0);
                foreach (var streak in streaks)
                {
                    _streakService.CheckTodaysWordcount(streak);
                }

                var trades = db.Userfeatures.Where(p => p.Trades > 0);
                foreach (var trade in trades)
                {
                    trade.Trades = 0;
                }

                var attacks = db.Userfeatures;
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
                        var dcUser = _client.Guilds.FirstOrDefault(p => p.Id == (ulong)user.ServerId).Users.FirstOrDefault(p => p.Id == (ulong)user.UserId) as SocketGuildUser;
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
                                myList.Add(new PotUser { UserId = item.UserId, Min = min + 1, Max = chance + min, Chance = (int)Math.Round(chance) });
                                min = chance + min;
                            }
                            foreach (var item in myList)
                            {
                                item.Max = Math.Floor(item.Max);
                                item.Min = Math.Floor(item.Min);
                            }

                            Random rnd = new Random();
                            var luck = rnd.Next(1, 101);
                            var botChannelId = db.Guild.FirstOrDefault(p => p.ServerId == serverId).Botchannelid;
                            var dcServer = _client.Guilds.FirstOrDefault(p => p.Id == (ulong)serverId);

                            if (dcServer == null || botChannelId == null)
                                continue;

                            var dcBotChannel = dcServer.TextChannels.FirstOrDefault(p => p.Id == (ulong)botChannelId);

                            if (dcBotChannel == null)
                                continue;

                            foreach (var item in myList)
                            {
                                if (item.Min <= luck && item.Max >= luck)
                                {
                                    var dcUser = dcServer.Users.FirstOrDefault(p => p.Id == (ulong)item.UserId);
                                    var dbUserfeature = db.Userfeatures.FirstOrDefault(p => p.ServerId == dcServer.Id && p.UserId == (ulong)item.UserId);
                                    EmbedBuilder embed = new EmbedBuilder();
                                    embed.Color = Color.Green;
                                    var stall = Helper.GetStall(dbUserfeature.Wins);
                                    if (Helper.IsFull(dbUserfeature.Goats + sum, dbUserfeature.Wins))
                                    {
                                        if (dcUser != null)
                                        {
                                            embed.Description = $"Der **Gewinner** von **{sum} Ziegen** aus dem Pot ist {dcUser.Mention} mit einer Chance von **{item.Chance}%**!\nLeider passen in deinen Stall nur **{stall.Capacity} Ziegen**, deswegen sind **{(sum + dbUserfeature.Goats) - stall.Capacity} Ziegen** zu Rabbot **geflüchtet**..";
                                            var rabbotUser = db.Userfeatures.FirstOrDefault(p => p.ServerId == dcServer.Id && p.UserId == _client.CurrentUser.Id) ?? db.AddAsync(new Userfeatures { ServerId = dcServer.Id, UserId = _client.CurrentUser.Id, Goats = 0, Exp = 0 }).Result.Entity;
                                            rabbotUser.Goats += (sum + dbUserfeature.Goats) - stall.Capacity;
                                        }
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
                    _logger.Error(e, $"Error in {nameof(NewDay)}");
                }
                await db.SaveChangesAsync();
            }
        }

        private async Task CheckSong()
        {
            while (true)
            {
                try
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

                                    if (song.TrackUrl == db.Songlist.FirstOrDefault(p => p.Active == 1).Link)
                                    {
                                        var musicrank = db.Musicrank.FirstOrDefault(p => p.UserId == user.Id && p.ServerId == guild.Id) ?? db.Musicrank.AddAsync(new Musicrank { UserId = user.Id, ServerId = guild.Id, Sekunden = 0, Date = DateTime.Now }).Result.Entity;
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
                                            var exp = db.Userfeatures.FirstOrDefault(p => p.ServerId == guild.Id && p.UserId == user.Id);

                                            if (exp != null)
                                                if (!Helper.IsFull(exp.Goats + 10, exp.Wins))
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
                catch (Exception e)
                {
                    _logger.Error(e, $"Error in {nameof(CheckSong)}");
                }
            }
        }

        private async Task CheckWarnings()
        {
            while (true)
            {
                try
                {
                    WarnService warn = new WarnService(_client);
                    await warn.CheckWarnings();
                    await Task.Delay(1000);
                }
                catch (Exception e)
                {
                    _logger.Error(e, $"Error in {nameof(CheckWarnings)}");
                }
            }

        }

        private async Task CheckAttacks()
        {
            while (true)
            {
                using (swaightContext db = new swaightContext())
                {
                    try
                    {

                        await _attackService.CheckAttacks(db);
                    }
                    catch (Exception e)
                    {
                        _logger.Error(e, $"Error in {nameof(CheckAttacks)}");
                    }
                }
                await Task.Delay(1000);
            }
        }

        private async Task CheckBannedUsers()
        {
            while (true)
            {
                try
                {
                    MuteService mute = new MuteService(_client);
                    await mute.CheckMutes();
                    await Task.Delay(1000);
                }
                catch (Exception e)
                {
                    _logger.Error(e, $"Error in {nameof(CheckBannedUsers)}");
                }
            }
        }

        private async Task CheckItems()
        {
            while (true)
            {
                using (swaightContext db = new swaightContext())
                {
                    if (db.Inventory.Any(p => p.ExpirationDate != null))
                    {
                        foreach (var item in db.Inventory)
                        {
                            if (item.ExpirationDate < DateTime.Now)
                                db.Inventory.Remove(item);

                        }
                        await db.SaveChangesAsync();
                    }
                }
                await Task.Delay(1000);
            }
        }

        private async Task MessageReceived(SocketMessage msg)
        {
            if (msg.Author.IsBot)
                return;

            if (!(msg.Author is SocketGuildUser dcUser))
                return;

            if (msg.Channel.Name.Contains("blog"))
            {
                if (msg is SocketUserMessage message)
                {
                    await message.AddReactionAsync(Constants.thumbsUp);
                    await message.AddReactionAsync(Constants.thumbsDown);
                }
            }

            using (swaightContext db = new swaightContext())
            {
                SocketGuild dcGuild = dcUser.Guild;
                if (db.Badwords.Where(p => p.ServerId == dcGuild.Id).Any(p => Helper.ReplaceCharacter(msg.Content).Contains(p.BadWord, StringComparison.OrdinalIgnoreCase) && !dcUser.GuildPermissions.ManageMessages))
                {
                    await msg.DeleteAsync();
                    var myUser = msg.Author as SocketGuildUser;
                    if (db.Muteduser.Where(p => p.UserId == msg.Author.Id && p.ServerId == myUser.Guild.Id).Any())
                        return;
                    if (myUser.Roles.Where(p => p.Name == "Muted").Any())
                        return;
                    var warn = db.Warning.FirstOrDefault(p => p.UserId == msg.Author.Id && p.ServerId == dcGuild.Id) ?? db.Warning.AddAsync(new Warning { ServerId = dcGuild.Id, UserId = msg.Author.Id, ActiveUntil = DateTime.Now.AddHours(1), Counter = 0 }).Result.Entity;
                    warn.Counter++;
                    if (warn.Counter > 3)
                        return;
                    await msg.Channel.SendMessageAsync($"**{msg.Author.Mention} du wurdest für schlechtes Benehmen verwarnt. Warnung {warn.Counter}/3**");
                    var badword = db.Badwords.FirstOrDefault(p => Helper.ReplaceCharacter(msg.Content).Contains(p.BadWord, StringComparison.OrdinalIgnoreCase) && p.ServerId == dcGuild.Id).BadWord;
                    await Logging.Warning(myUser, msg, badword);
                }
                var dbUser = db.User.FirstOrDefault(p => p.Id == msg.Author.Id) ?? db.User.AddAsync(new User { Id = msg.Author.Id, Name = msg.Author.Username + "#" + msg.Author.Discriminator }).Result.Entity;
                dbUser.Name = msg.Author.Username + "#" + msg.Author.Discriminator;
                var feature = db.Userfeatures.FirstOrDefault(p => (ulong)p.UserId == msg.Author.Id && p.ServerId == dcGuild.Id) ?? db.Userfeatures.AddAsync(new Userfeatures { Exp = 0, UserId = msg.Author.Id, ServerId = dcGuild.Id }).Result.Entity;
                feature.Lastmessage = DateTime.Now;

                _streakService.AddWords(feature, msg.Content);

                await db.SaveChangesAsync();
            }
            if (msg.Content.StartsWith(Config.bot.cmdPrefix))
                return;
            _levelService.AddEXP(msg);
        }

        private async Task UserLeft(SocketGuildUser user)
        {
            using (swaightContext db = new swaightContext())
            {
                if (!db.Guild.Where(p => p.ServerId == user.Guild.Id).Any())
                    return;
                if (db.Guild.FirstOrDefault(p => p.ServerId == user.Guild.Id).Log == 0)
                    return;

                var channelId = db.Guild.FirstOrDefault(p => p.ServerId == user.Guild.Id).LogchannelId;
                var embed = new EmbedBuilder();
                embed.WithTitle($"{user.Username + "#" + user.Discriminator} left the server!");
                embed.WithDescription($"User Tag: {user.Mention}");
                embed.WithColor(new Color(255, 0, 0));
                embed.AddField("User ID", user.Id.ToString(), true);
                embed.AddField("Username", user.Username + "#" + user.Discriminator, true);
                embed.ThumbnailUrl = user.GetAvatarUrl(ImageFormat.Auto, 1024);
                embed.AddField("Joined Server at", user.JoinedAt.Value.DateTime.ToCET().ToFormattedString(), false);
                await _client.GetGuild(user.Guild.Id).GetTextChannel((ulong)channelId).SendMessageAsync("", false, embed.Build());

                var dbUser = db.Userfeatures.Where(p => p.UserId == user.Id && p.ServerId == user.Guild.Id);
                foreach (var leftUser in dbUser)
                {
                    leftUser.HasLeft = true;
                }
                await db.SaveChangesAsync();
            }
        }

        private async Task UserJoined(SocketGuildUser user)
        {
            using (swaightContext db = new swaightContext())
            {
                if (!db.Guild.Where(p => p.ServerId == user.Guild.Id).Any())
                    return;
                if (db.Guild.FirstOrDefault(p => p.ServerId == user.Guild.Id).Log == 0)
                    return;

                var memberRole = _client.Guilds.FirstOrDefault(p => p.Id == user.Guild.Id).Roles.FirstOrDefault(p => p.Name == "Mitglied");
                if (memberRole != null)
                    await user.AddRoleAsync(memberRole);
                var channelId = db.Guild.FirstOrDefault(p => p.ServerId == user.Guild.Id).LogchannelId;
                var embed = new EmbedBuilder();
                embed.WithTitle($"{user.Username + "#" + user.Discriminator} joined the server!");
                embed.WithDescription($"User Tag: {user.Mention}");
                embed.WithColor(new Color(0, 255, 0));
                embed.AddField("User ID", user.Id.ToString(), true);
                embed.AddField("Username", user.Username + "#" + user.Discriminator, true);
                embed.ThumbnailUrl = user.GetAvatarUrl(ImageFormat.Auto, 1024);
                embed.AddField("Joined Discord at", user.CreatedAt.DateTime.ToCET().ToFormattedString(), false);
                await _client.GetGuild(user.Guild.Id).GetTextChannel((ulong)channelId).SendMessageAsync("", false, embed.Build());

                var dbUser = db.Userfeatures.Where(p => p.UserId == user.Id && p.ServerId == user.Guild.Id);
                foreach (var joinedUser in dbUser)
                {
                    joinedUser.HasLeft = false;
                }
                await db.SaveChangesAsync();
            }
        }
    }
}
