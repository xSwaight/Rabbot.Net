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
using System.Runtime.InteropServices;
using Serilog;
using Serilog.Core;
using Rabbot.Models;
using Discord.Rest;
using Rabbot.Database.Rabbot;
using Microsoft.Extensions.DependencyInjection;

namespace Rabbot.Services
{
    class EventService
    {
        private readonly DiscordShardedClient _client;
        private readonly CommandService _commandService;
        private readonly StreakService _streakService;
        private readonly AttackService _attackService;
        private readonly LevelService _levelService;
        private readonly WarnService _warnService;
        private readonly MuteService _muteService;
        private readonly ApiService _apiService;
        private DatabaseService Database => DatabaseService.Instance;
        private readonly EasterEventService _easterEventService;
        private static readonly ILogger _logger = Log.ForContext(Serilog.Core.Constants.SourceContextPropertyName, nameof(EventService));
        private bool _eventRegistered = false;

        public EventService(IServiceProvider services)
        {
            _attackService = services.GetRequiredService<AttackService>();
            _streakService = services.GetRequiredService<StreakService>();
            _commandService = services.GetRequiredService<CommandService>();
            _levelService = services.GetRequiredService<LevelService>();
            _warnService = services.GetRequiredService<WarnService>();
            _muteService = services.GetRequiredService<MuteService>();
            _apiService = services.GetRequiredService<ApiService>();
            _easterEventService = services.GetRequiredService<EasterEventService>();
            _client = services.GetRequiredService<DiscordShardedClient>();
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
            _logger.Information($"{nameof(EventService)}: Loaded successfully");
            _client.UserJoined += UserJoined;
            _client.UserLeft += UserLeft;
            _client.MessageReceived += MessageReceived;
            _client.MessageDeleted += MessageDeleted;
            _client.MessageUpdated += MessageUpdated;
            _client.JoinedGuild += JoinedGuild;
            _client.ShardConnected += ClientConnected;
            _client.ReactionAdded += ReactionAdded;
            _client.ReactionRemoved += ReactionRemoved;
            _client.UserUpdated += UserUpdated;
            _client.ChannelCreated += ChannelCreated;
            _client.GuildUpdated += GuildUpdated;
        }

        private async Task GuildUpdated(SocketGuild oldGuild, SocketGuild newGuild)
        {
            using (var db = Database.Open())
            {
                var dbGuild = db.Guilds.FirstOrDefault(p => p.GuildId == newGuild.Id);
                if (dbGuild != null)
                {
                    dbGuild.GuildName = newGuild.Name;
                    await db.SaveChangesAsync();
                }
            }
        }

        private async Task UserUpdated(SocketUser oldUser, SocketUser newUser)
        {
            if (oldUser.Username != newUser.Username)
            {
                using (var db = Database.Open())
                {
                    if (db.Users.FirstOrDefault(p => p.Id == newUser.Id) == null)
                        await db.Users.AddAsync(new UserEntity { Id = newUser.Id, Name = $"{newUser.Username}#{newUser.Discriminator}" });
                    if (db.Namechanges.AsQueryable().Where(p => p.UserId == newUser.Id).OrderByDescending(p => p.Date).FirstOrDefault()?.NewName == oldUser.Username)
                    {
                        await db.Namechanges.AddAsync(new NamechangeEntity { UserId = newUser.Id, NewName = newUser.Username, Date = DateTime.Now });
                    }
                    else
                    {
                        await db.Namechanges.AddAsync(new NamechangeEntity { UserId = newUser.Id, NewName = oldUser.Username, Date = DateTime.Now.AddMinutes(-1) });
                        await db.Namechanges.AddAsync(new NamechangeEntity { UserId = newUser.Id, NewName = newUser.Username, Date = DateTime.Now });
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

                if (emote.Id != Constants.Slot.Id)
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
                    await Helper.UpdateSpin(channel, user, msg, _client.GetShardFor(guild), 0, false);
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
            if (!reaction.Message.IsSpecified)
                return;

            switch (reaction.Message.Value.Embeds.FirstOrDefault()?.Title)
            {
                case "Combi Anfrage":
                    using (var db = Database.Open())
                    {
                        var combi = db.Combis.FirstOrDefault(p => p.MessageId == reaction.MessageId);
                        if (combi == null)
                            return;

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
                                    db.Combis.Remove(combi);
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

                        if (emote.Id != Constants.Slot.Id)
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
                            await Helper.UpdateSpin(channel, user, msg, _client.GetShardFor(guild), 0, false);
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

            using (var db = Database.Open())
            {
                if (!db.Attacks.Any())
                    return;

                var dbAtk = db.Attacks.FirstOrDefault(p => p.MessageId == reaction.MessageId);
                if (dbAtk == null)
                    return;

                var atkUserfeature = db.Features.FirstOrDefault(p => p.UserId == dbAtk.UserId && p.GuildId == dbAtk.GuildId);
                var defUserfeature = db.Features.FirstOrDefault(p => p.UserId == dbAtk.TargetId && p.GuildId == dbAtk.GuildId);

                var inventoryUser = db.Inventorys.AsQueryable().Join(db.Items, id => id.ItemId, item => item.Id, (Inventory, Item) => new { Inventory, Item }).Where(p => p.Inventory.FeatureId == atkUserfeature.Id);
                var inventoryTarget = db.Inventorys.AsQueryable().Join(db.Items, id => id.ItemId, item => item.Id, (Inventory, Item) => new { Inventory, Item }).Where(p => p.Inventory.FeatureId == defUserfeature.Id);
                var zaun = inventoryTarget.FirstOrDefault(p => p.Item.Id == 2);
                var stab = inventoryUser.FirstOrDefault(p => p.Item.Id == 1);

                bool isActiveZaun = false;
                bool isActiveStab = false;


                if (emote.Id == Constants.Shield.Id)
                {
                    if (reaction.User.Value.Id != dbAtk.TargetId)
                        return;
                    if (zaun == null)
                        return;
                }
                else if (emote.Id == Constants.Sword.Id)
                {
                    if (reaction.User.Value.Id != dbAtk.UserId)
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

                var atkUser = dcGuild.Users.FirstOrDefault(p => p.Id == dbAtk.UserId);
                var defUser = dcGuild.Users.FirstOrDefault(p => p.Id == dbAtk.TargetId);

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

            using (var db = Database.Open())
            {
                if (!db.Attacks.Any())
                    return;

                var dbAtk = db.Attacks.FirstOrDefault(p => p.MessageId == reaction.MessageId);
                if (dbAtk == null)
                    return;

                var atkUserfeature = db.Features.FirstOrDefault(p => p.UserId == dbAtk.UserId && p.GuildId == dbAtk.GuildId);
                var defUserfeature = db.Features.FirstOrDefault(p => p.UserId == dbAtk.TargetId && p.GuildId == dbAtk.GuildId);

                var inventoryUser = db.Inventorys.AsQueryable().Join(db.Items, id => id.ItemId, item => item.Id, (Inventory, Item) => new { Inventory, Item }).Where(p => p.Inventory.FeatureId == atkUserfeature.Id);
                var inventoryTarget = db.Inventorys.AsQueryable().Join(db.Items, id => id.ItemId, item => item.Id, (Inventory, Item) => new { Inventory, Item }).Where(p => p.Inventory.FeatureId == defUserfeature.Id);
                var zaun = inventoryTarget.FirstOrDefault(p => p.Item.Id == 2);
                var stab = inventoryUser.FirstOrDefault(p => p.Item.Id == 1);

                bool isActiveZaun = false;
                bool isActiveStab = false;

                if (emote.Id == Constants.Shield.Id)
                {
                    if (reaction.User.Value.Id != dbAtk.TargetId)
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
                    if (reaction.User.Value.Id != dbAtk.UserId)
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

                var atkUser = dcGuild.Users.FirstOrDefault(p => p.Id == dbAtk.UserId);
                var defUser = dcGuild.Users.FirstOrDefault(p => p.Id == dbAtk.TargetId);

                string chance = $"**{Math.Round(winChance)}% {atkUser.Mention} - {defUser.Mention} {100 - Math.Round(winChance)}%**";

                await reaction.Message.Value.ModifyAsync(msg => msg.Content = chance);
            }
        }

        private async Task MessageUpdated(Cacheable<IMessage, ulong> message, SocketMessage newMessage, ISocketMessageChannel channel)
        {
            if (newMessage.Author.IsBot)
                return;
            using (var db = Database.Open())
            {
                if (!(newMessage.Channel is SocketGuildChannel guildChannel))
                    return;
                SocketGuild dcServer = guildChannel.Guild;
                if (db.BadWords.AsQueryable().Where(p => p.GuildId == dcServer.Id).Any(p => Helper.ReplaceCharacter(newMessage.Content).Contains(p.BadWord, StringComparison.OrdinalIgnoreCase)) && !(newMessage.Author as SocketGuildUser).GuildPermissions.ManageMessages && !db.GoodWords.AsQueryable().Where(p => p.GuildId == dcServer.Id).Any(p => Helper.ReplaceCharacter(newMessage.Content).Contains(p.GoodWord, StringComparison.OrdinalIgnoreCase)))
                {
                    await newMessage.DeleteAsync();
                    var myUser = newMessage.Author as SocketGuildUser;
                    if (db.MutedUsers.AsQueryable().Where(p => p.UserId == newMessage.Author.Id && p.GuildId == myUser.Guild.Id).Any())
                        return;
                    if (myUser.Roles.Where(p => p.Name == "Muted").Any())
                        return;
                    if (!db.Warnings.AsQueryable().Where(p => p.UserId == newMessage.Author.Id && p.GuildId == dcServer.Id).Any())
                    {
                        await db.Warnings.AddAsync(new WarningEntity { GuildId = dcServer.Id, UserId = newMessage.Author.Id, Until = DateTime.Now.AddHours(1), Counter = 1 });
                        await newMessage.Channel.SendMessageAsync($"**{newMessage.Author.Mention} du wurdest für schlechtes Benehmen verwarnt. Warnung 1/3**");
                    }
                    else
                    {
                        var warn = db.Warnings.FirstOrDefault(p => p.UserId == newMessage.Author.Id && p.GuildId == dcServer.Id);
                        warn.Counter++;
                        await newMessage.Channel.SendMessageAsync($"**{newMessage.Author.Mention} du wurdest für schlechtes Benehmen verwarnt. Warnung {warn.Counter}/3**");
                        await db.SaveChangesAsync();
                    }
                    var badword = db.BadWords.FirstOrDefault(p => Helper.ReplaceCharacter(newMessage.Content).Contains(p.BadWord, StringComparison.OrdinalIgnoreCase) && p.GuildId == dcServer.Id).BadWord;
                    await Logging.Warning(myUser, newMessage, badword);

                }
                if (message.Value.Content == newMessage.Content)
                    return;
                var dcUser = message.Value.Author as SocketGuildUser;
                if (dcUser.IsBot)
                    return;
                var dcTextchannel = channel as SocketTextChannel;
                var dbGuild = db.Guilds.FirstOrDefault(p => p.GuildId == dcUser.Guild.Id);
                var dcGuild = _client.Guilds.FirstOrDefault(p => p.Id == dbGuild.GuildId);
                if (dbGuild.Trash == false || dbGuild.TrashChannelId == null)
                    return;
                var dcTrashChannel = dcGuild.TextChannels.FirstOrDefault(p => p.Id == dbGuild.TrashChannelId);
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
            using (var db = Database.Open())
            {
                if (message.Value == null)
                    return;

                if (message.Value.Author is SocketGuildUser dcUser)
                {
                    if (db.Rule.Any(p => p.GuildId == dcUser.Guild.Id))
                    {
                        if (message.Value.Channel.Id == db.Rule.First(p => p.GuildId == dcUser.Guild.Id).ChannelId)
                            return;
                    }

                    if (dcUser.IsBot)
                        return;
                    if (message.Value.Content.StartsWith(Config.Bot.CmdPrefix))
                        return;
                    var dcTextchannel = channel as SocketTextChannel;
                    var dbGuild = db.Guilds.FirstOrDefault(p => p.GuildId == dcUser.Guild.Id);
                    var dcGuild = _client.Guilds.FirstOrDefault(p => p.Id == dbGuild.GuildId);
                    if (dbGuild.Trash == false || dbGuild.TrashChannelId == null)
                        return;
                    var dcTrashChannel = dcGuild.TextChannels.FirstOrDefault(p => p.Id == dbGuild.TrashChannelId);
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

        private async Task ClientConnected(DiscordSocketClient client)
        {

            using (var db = Database.Open())
            {
                if (!db.Users.Any(p => p.Id == _client.CurrentUser.Id))
                {
                    await db.Users.AddAsync(new UserEntity { Id = _client.CurrentUser.Id, Name = $"{_client.CurrentUser.Username}#{_client.CurrentUser.Discriminator}" });
                    await db.SaveChangesAsync();
                }

                if (!db.Events.AsQueryable().AsQueryable().Where(p => p.Status == true).Any())
                {
                    await _client.SetGameAsync($"{Config.Bot.CmdPrefix}rank", null, ActivityType.Watching);
                }
                else
                {
                    var myEvent = db.Events.FirstOrDefault(p => p.Status == true);
                    await _client.SetGameAsync($"{myEvent.Name} Event aktiv!", null, ActivityType.Watching);
                }
                //new Task(async () =>
                //{
                //    if (!_eventRegistered)
                //    {
                //        await Task.Delay(20000);
                //        _logger.Information("Loading easter event..");
                //        _easterEventService.RegisterServers(432908323042623508);
                //        _easterEventService.RegisterAnnouncementChannel(432908323042623508, 432909025047347200);
                //        new Task(async () => await _easterEventService.StartEventAsync(), TaskCreationOptions.LongRunning).Start();
                //        _eventRegistered = true;
                //    }
                //}).Start();
            }
        }

        private async Task ChannelCreated(SocketChannel newChannel)
        {
            if (newChannel is SocketGuildChannel channel)
            {
                var mutedRole = channel.Guild.Roles.FirstOrDefault(p => p.Name == "Muted");
                if (mutedRole == null)
                    return;

                var permission = new OverwritePermissions(PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Inherit, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Inherit, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny);
                await channel.AddPermissionOverwriteAsync(mutedRole, permission, null);
            }
        }

        private async Task JoinedGuild(SocketGuild guild)
        {
            RestRole mutedRole = null;
            if (!guild.Roles.Where(p => p.Name == "Muted").Any())
            {
                var mutedPermission = new GuildPermissions(false, false, false, false, false, false, false, false, true, false, false, false, false, false, true, false, false, false, false, false, false, false, false, false, false, false, false, false, false);
                mutedRole = await guild.CreateRoleAsync("Muted", mutedPermission, Color.Red, false, false);
            }

            var permission = new OverwritePermissions(PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Inherit, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Inherit, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny);

            if (mutedRole != null)
            {
                foreach (var textChannel in guild.TextChannels)
                {
                    await textChannel.AddPermissionOverwriteAsync(mutedRole, permission, null);
                }
                foreach (var voiceChannel in guild.VoiceChannels)
                {
                    await voiceChannel.AddPermissionOverwriteAsync(mutedRole, permission, null);
                }
            }
            using (var db = Database.Open())
            {
                var Guild = db.Guilds.FirstOrDefault(p => p.GuildId == guild.Id);
                if (Guild == null)
                {
                    await db.Guilds.AddAsync(new GuildEntity { GuildId = guild.Id, GuildName = guild.Name });
                    await db.SaveChangesAsync();
                }
            }
        }

        private async Task CheckPlayers()
        {
            while (true)
            {
                using (var db = Database.Open())
                {
                    try
                    {
                        var remnantsPlayers = _apiService.GetRemnantsPlayerCount();
                        var officialPlayers = _apiService.GetOfficialPlayerCount();
                        await db.OfficialPlayers.AddAsync(new OfficialPlayerEntity { Playercount = officialPlayers, Date = DateTime.Now });
                        if (int.TryParse(remnantsPlayers, out int count))
                        {
                            await db.RemnantsPlayers.AddAsync(new RemnantsPlayerEntity { Playercount = count, Date = DateTime.Now });
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
                using (var db = Database.Open())
                {
                    if (db.CurrentDay.Any())
                    {
                        if (db.CurrentDay.FirstOrDefault().Date.ToShortDateString() == DateTime.Now.ToShortDateString())
                        {
                            await Task.Delay(1000);
                            continue;
                        }
                        else
                        {
                            db.CurrentDay.FirstOrDefault().Date = DateTime.Now;
                            new Task(async () => await NewDay(), TaskCreationOptions.LongRunning).Start();
                        }
                    }
                    else
                        await db.CurrentDay.AddAsync(new CurrentDayEntity { Date = DateTime.Now });

                    await db.SaveChangesAsync();
                }
                await Task.Delay(1000);
            }
        }

        private async Task NewDay()
        {
            using (var db = Database.Open())
            {
                if (db.Songs.Any())
                {
                    var songs = db.Songs;
                    Random rnd = new Random();
                    int counter = 1;
                    int random = rnd.Next(1, songs.Count() + 1);
                    foreach (var song in songs)
                    {
                        song.Active = false;
                        if (counter == random)
                        {
                            song.Active = true;
                        }
                        counter++;
                    }
                }
                var streaks = db.Features.AsQueryable().Where(p => p.TodaysWords > 0 || p.StreakLevel > 0);
                foreach (var streak in streaks)
                {
                    _streakService.CheckTodaysWordcount(streak);
                }
                await db.SaveChangesAsync();

                var trades = db.Features.AsQueryable().AsQueryable().Where(p => p.Trades > 0);
                foreach (var trade in trades)
                {
                    trade.Trades = 0;
                }

                var attacks = db.Features;
                foreach (var attack in attacks)
                {
                    attack.Attacks = 0;
                }
                try
                {
                    if (db.Pots.Any())
                    {
                        var servers = db.Pots.AsQueryable().GroupBy(p => p.GuildId).Select(p => p.Key).ToList();
                        foreach (var serverId in servers)
                        {
                            var pot = db.Pots.AsQueryable().Where(p => p.GuildId == serverId);
                            var sum = pot.Sum(p => p.Goats);
                            double min = 0;
                            List<PotUserDto> myList = new List<PotUserDto>();
                            foreach (var item in pot)
                            {
                                var chance = (double)item.Goats / (double)sum * 100;
                                myList.Add(new PotUserDto { UserId = item.UserId, Min = min + 1, Max = chance + min, Chance = (int)Math.Round(chance) });
                                min = chance + min;
                            }
                            foreach (var item in myList)
                            {
                                item.Max = Math.Floor(item.Max);
                                item.Min = Math.Floor(item.Min);
                            }

                            Random rnd = new Random();
                            var luck = rnd.Next(1, 101);
                            var botChannelId = db.Guilds.FirstOrDefault(p => p.GuildId == serverId).BotChannelId;
                            var dcServer = _client.Guilds.FirstOrDefault(p => p.Id == serverId);

                            if (dcServer == null || botChannelId == null)
                                continue;

                            var dcBotChannel = dcServer.TextChannels.FirstOrDefault(p => p.Id == botChannelId);

                            if (dcBotChannel == null)
                                continue;

                            foreach (var item in myList)
                            {
                                if (item.Min <= luck && item.Max >= luck)
                                {
                                    var dcUser = dcServer.Users.FirstOrDefault(p => p.Id == item.UserId);
                                    var dbUserfeature = db.Features.FirstOrDefault(p => p.GuildId == dcServer.Id && p.UserId == item.UserId);
                                    EmbedBuilder embed = new EmbedBuilder();
                                    embed.Color = Color.Green;
                                    var stall = Helper.GetStall(dbUserfeature.Wins);
                                    if (Helper.IsFull(dbUserfeature.Goats + sum, dbUserfeature.Wins))
                                    {
                                        if (dcUser != null)
                                        {
                                            embed.Description = $"Der **Gewinner** von **{sum} Ziegen** aus dem Pot ist {dcUser.Mention} mit einer Chance von **{item.Chance}%**!\nLeider passen in deinen Stall nur **{stall.Capacity} Ziegen**, deswegen sind **{(sum + dbUserfeature.Goats) - stall.Capacity} Ziegen** zu Rabbot **geflüchtet**..";
                                            var rabbotUser = db.Features.FirstOrDefault(p => p.GuildId == dcServer.Id && p.UserId == _client.CurrentUser.Id) ?? db.AddAsync(new FeatureEntity { GuildId = dcServer.Id, UserId = _client.CurrentUser.Id, Goats = 0, Exp = 0 }).Result.Entity;
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
                                        db.Pots.Remove(myPot);
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
                                using (var db = Database.Open())
                                {
                                    if (!db.Songs.AsQueryable().Where(p => p.Active == true).Any())
                                        continue;

                                    if (song.TrackUrl == db.Songs.FirstOrDefault(p => p.Active == true).Link)
                                    {
                                        var musicrank = db.Musicranks.FirstOrDefault(p => p.UserId == user.Id && p.GuildId == guild.Id) ?? db.Musicranks.AddAsync(new MusicrankEntity { UserId = user.Id, GuildId = guild.Id, Seconds = 0, Date = DateTime.Now }).Result.Entity;
                                        if (musicrank.Date.ToShortDateString() != DateTime.Now.ToShortDateString())
                                        {
                                            musicrank.Seconds = 0;
                                            musicrank.Date = DateTime.Now;
                                        }
                                        musicrank.Seconds += 10;
                                        await db.SaveChangesAsync();
                                        if (musicrank.Seconds == 300)
                                        {
                                            var channel = await user.GetOrCreateDMChannelAsync();
                                            var exp = db.Features.FirstOrDefault(p => p.GuildId == guild.Id && p.UserId == user.Id);

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
                    using (var db = Database.Open())
                        await _warnService.CheckWarnings(db);
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
                using (var db = Database.Open())
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
                    using (var db = Database.Open())
                        await _muteService.CheckMutes(db);
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
                using (var db = Database.Open())
                {
                    if (db.Inventorys.Any(p => p.ExpiryDate != null))
                    {
                        foreach (var item in db.Inventorys)
                        {
                            if (item.ExpiryDate < DateTime.Now)
                                db.Inventorys.Remove(item);

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

            using (var db = Database.Open())
            {
                if (db.Rule.Any(p => p.GuildId == dcUser.Guild.Id))
                {
                    if (msg.Channel.Id == db.Rule.First(p => p.GuildId == dcUser.Guild.Id).ChannelId)
                        return;
                }

                SocketGuild dcGuild = dcUser.Guild;
                if (db.BadWords.AsQueryable().Where(p => p.GuildId == dcGuild.Id).Any(p => Helper.ReplaceCharacter(msg.Content).Contains(p.BadWord, StringComparison.OrdinalIgnoreCase) && !dcUser.GuildPermissions.ManageMessages) && !db.GoodWords.AsQueryable().Where(p => p.GuildId == dcGuild.Id).Any(p => Helper.ReplaceCharacter(msg.Content).Contains(p.GoodWord, StringComparison.OrdinalIgnoreCase)))
                {
                    await msg.DeleteAsync();
                    await _warnService.AutoWarn(db, msg);
                }
                var dbUser = db.Users.FirstOrDefault(p => p.Id == msg.Author.Id) ?? db.Users.AddAsync(new UserEntity { Id = msg.Author.Id, Name = $"{msg.Author.Username}#{msg.Author.Discriminator}" }).Result.Entity;
                dbUser.Name = $"{msg.Author.Username}#{msg.Author.Discriminator}";
                var feature = db.Features.FirstOrDefault(p => p.UserId == msg.Author.Id && p.GuildId == dcGuild.Id);
                if (feature == null && !msg.Content.StartsWith(Config.Bot.CmdPrefix))
                    feature = db.Features.AddAsync(new FeatureEntity { Exp = 0, UserId = msg.Author.Id, GuildId = dcGuild.Id }).Result.Entity;
                else if (feature == null)
                    return;

                feature.LastMessage = DateTime.Now;

                if (!msg.Content.StartsWith(Config.Bot.CmdPrefix))
                    _streakService.AddWords(feature, msg);

                await db.SaveChangesAsync();
            }
            if (msg.Content.StartsWith(Config.Bot.CmdPrefix))
                return;
            await _levelService.AddEXP(msg);
        }

        private async Task UserLeft(SocketGuildUser user)
        {
            using (var db = Database.Open())
            {
                if (!db.Guilds.AsQueryable().AsQueryable().Where(p => p.GuildId == user.Guild.Id).Any())
                    return;
                if (db.Guilds.FirstOrDefault(p => p.GuildId == user.Guild.Id).Log == false)
                    return;

                var channelId = db.Guilds.FirstOrDefault(p => p.GuildId == user.Guild.Id).LogChannelId;
                var embed = new EmbedBuilder();
                embed.WithTitle($"{user.Username + "#" + user.Discriminator} left the server!");
                embed.WithDescription($"User Tag: {user.Mention}");
                embed.WithColor(new Color(255, 0, 0));
                embed.AddField("User ID", user.Id.ToString(), true);
                embed.AddField("Username", user.Username + "#" + user.Discriminator, true);
                embed.ThumbnailUrl = user.GetAvatarUrl(ImageFormat.Auto, 1024);
                embed.AddField("Joined Server at", user.JoinedAt.Value.DateTime.ToCET().ToFormattedString(), false);
                await _client.GetGuild(user.Guild.Id).GetTextChannel(channelId.Value).SendMessageAsync("", false, embed.Build());

                var dbUser = db.Features.AsQueryable().Where(p => p.UserId == user.Id && p.GuildId == user.Guild.Id);
                foreach (var leftUser in dbUser)
                {
                    leftUser.HasLeft = true;
                }
                await db.SaveChangesAsync();
            }
        }

        private async Task UserJoined(SocketGuildUser user)
        {
            using (var db = Database.Open())
            {
                if (!db.Guilds.AsQueryable().Where(p => p.GuildId == user.Guild.Id).Any())
                    return;
                if (db.Guilds.FirstOrDefault(p => p.GuildId == user.Guild.Id).Log == false)
                    return;

                var memberRole = _client.Guilds.FirstOrDefault(p => p.Id == user.Guild.Id).Roles.FirstOrDefault(p => p.Name == "Mitglied");
                if (memberRole != null && !db.Rule.Any(p => p.GuildId == user.Guild.Id))
                    await user.AddRoleAsync(memberRole);

                var channelId = db.Guilds.FirstOrDefault(p => p.GuildId == user.Guild.Id).LogChannelId;
                var embed = new EmbedBuilder();
                embed.WithTitle($"{user.Username + "#" + user.Discriminator} joined the server!");
                embed.WithDescription($"User Tag: {user.Mention}");
                embed.WithColor(new Color(0, 255, 0));
                embed.AddField("User ID", user.Id.ToString(), true);
                embed.AddField("Username", user.Username + "#" + user.Discriminator, true);
                embed.ThumbnailUrl = user.GetAvatarUrl(ImageFormat.Auto, 1024);
                embed.AddField("Joined Discord at", user.CreatedAt.DateTime.ToCET().ToFormattedString(), false);
                await _client.GetGuild(user.Guild.Id).GetTextChannel(channelId.Value).SendMessageAsync("", false, embed.Build());

                var dbUser = db.Features.AsQueryable().Where(p => p.UserId == user.Id && p.GuildId == user.Guild.Id);
                foreach (var joinedUser in dbUser)
                {
                    joinedUser.HasLeft = false;
                }
                await db.SaveChangesAsync();
            }
        }
    }
}
