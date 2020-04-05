using Discord;
using Discord.WebSocket;
using Rabbot.Database;
using Rabbot.Database.Rabbot;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rabbot.Services
{
    public class AttackService
    {
        private static readonly ILogger _logger = Log.ForContext(Serilog.Core.Constants.SourceContextPropertyName, nameof(AttackService));
        private readonly DiscordSocketClient _client;

        public AttackService(DiscordSocketClient client)
        {
            _client = client;
        }

        public async Task CheckAttacks(RabbotContext db)
        {
            if (!db.Attacks.Any())
                return;

            var attacks = db.Attacks.ToList();
            foreach (var attack in attacks)
            {
                if (attack.EndTime < DateTime.Now)
                {
                    await AttackResult(attack, db);
                    db.Attacks.Remove(attack);
                    await db.SaveChangesAsync();
                    continue;
                }
            }
        }

        private async Task AttackResult(AttackEntity attack, RabbotContext db)
        {
            var dcServer = _client.Guilds.FirstOrDefault(p => p.Id == (ulong)attack.GuildId);
            if (dcServer == null)
                return;
            var dcTarget = dcServer.Users.FirstOrDefault(p => p.Id == (ulong)attack.TargetId);
            var dcUser = dcServer.Users.FirstOrDefault(p => p.Id == (ulong)attack.UserId);
            var dcChannel = dcServer.Channels.FirstOrDefault(p => p.Id == (ulong)attack.ChannelId) as ISocketMessageChannel;

            var dbTarget = db.Features.FirstOrDefault(p => p.GuildId == attack.GuildId && p.UserId == attack.TargetId) ?? db.Features.AddAsync(new FeatureEntity { Guild = attack.Guild, UserId = attack.TargetId, Exp = 0, Goats = 0 }).Result.Entity;
            var dbUser = db.Features.FirstOrDefault(p => p.GuildId == attack.GuildId && p.UserId == attack.UserId) ?? db.Features.AddAsync(new FeatureEntity { GuildId = attack.GuildId, UserId = attack.UserId, Exp = 0, Goats = 0 }).Result.Entity;
            var targetStallBefore = Helper.GetStall(dbTarget.Wins);
            var userStallBefore = Helper.GetStall(dbUser.Wins);
            var inventoryUser = db.Inventorys.Join(db.Items, id => id.ItemId, item => item.Id, (Inventory, Item) => new { Inventory, Item }).Where(p => p.Inventory.FeatureId == dbUser.Id);
            var inventoryTarget = db.Inventorys.Join(db.Items, id => id.ItemId, item => item.Id, (Inventory, Item) => new { Inventory, Item }).Where(p => p.Inventory.FeatureId == dbTarget.Id);
            var atkUser = userStallBefore.Attack;
            var defTarget = targetStallBefore.Defense;
            bool hirtenstab = false;
            bool zaun = false;

            var dcMessage = dcChannel.CachedMessages.FirstOrDefault(p => p.Id == (ulong)attack.MessageId) as SocketUserMessage;

            if (dcMessage != null)
                foreach (var reaction in dcMessage.Reactions)
                {
                    var emote = reaction.Key as Emote;

                    if (emote.Id == Constants.Shield.Id)
                    {
                        if (reaction.Value.ReactionCount >= 2)
                            zaun = true;
                    }
                    else if (emote.Id == Constants.Sword.Id)
                    {
                        if (reaction.Value.ReactionCount >= 2)
                            hirtenstab = true;
                    }
                }

            if (hirtenstab)
                if (inventoryUser.Count() != 0)
                {
                    foreach (var item in inventoryUser)
                    {
                        atkUser += item.Item.Atk;
                        if (item.Inventory.ItemId == 1)
                        {
                            item.Inventory.Durability--;
                            if (item.Inventory.Durability <= 0)
                                db.Inventorys.Remove(item.Inventory);
                        }
                    }

                }

            if (zaun)
                if (inventoryTarget.Count() != 0)
                {
                    foreach (var item in inventoryTarget)
                    {
                        defTarget += item.Item.Def;
                        if (item.Inventory.ItemId == 2)
                        {
                            item.Inventory.Durability--;
                            if (item.Inventory.Durability <= 0)
                                db.Inventorys.Remove(item.Inventory);
                        }
                    }
                }

            Random rnd = new Random();

            var sum = atkUser + defTarget;
            var winChance = ((double)atkUser / (double)sum) * 100;
            var chance = rnd.Next(1, 101);
            EmbedBuilder embed = new EmbedBuilder();
            var rabbotUser = db.Features.FirstOrDefault(p => p.GuildId == dcServer.Id && p.UserId == _client.CurrentUser.Id) ?? db.AddAsync(new FeatureEntity { GuildId = dcServer.Id, UserId = _client.CurrentUser.Id, Goats = 0, Exp = 0 }).Result.Entity;
            if (chance <= winChance)
            {
                int amount = rnd.Next(40, targetStallBefore.MaxOutput + 1);
                if (amount >= dbTarget.Goats)
                    amount = dbTarget.Goats;

                if (!(dcTarget == null || dcUser == null))
                {
                    if (dcChannel != null)
                    {
                        embed.Color = Color.Green;
                        if (!Helper.IsFull(dbUser.Goats + amount, dbUser.Wins))
                            embed.Description = $"{dcUser.Mention} du hast den **Angriff** gegen {dcTarget.Mention} **gewonnen** und **{amount} Ziegen** erbeutet!";
                        else
                        {
                            embed.Description = $"{dcUser.Mention} du hast den **Angriff** gegen {dcTarget.Mention} **gewonnen** und **{amount} Ziegen** erbeutet!\nLeider ist **dein Stall voll**. Deswegen sind **{(dbUser.Goats + amount) - Helper.GetStall(dbUser.Wins).Capacity} Ziegen** zu Rabbot geflüchtet.";
                            rabbotUser.Goats += (dbUser.Goats + amount) - Helper.GetStall(dbUser.Wins).Capacity;
                        }
                        await dcChannel.SendMessageAsync(null, false, embed.Build());
                    }
                }

                dbUser.Wins++;
                if (Helper.IsFull(dbUser.Goats + amount, dbUser.Wins))
                    dbUser.Goats = Helper.GetStall(dbUser.Wins).Capacity;
                else
                    dbUser.Goats += amount;
                dbTarget.Goats -= amount;
                dbTarget.Loses++;
            }
            else
            {
                int amount = rnd.Next(40, userStallBefore.MaxOutput + 1);
                if (amount >= dbUser.Goats)
                    amount = dbUser.Goats;
                if (!(dcTarget == null || dcUser == null))
                {
                    if (dcChannel != null)
                    {
                        embed.Color = Color.Red;
                        if (!Helper.IsFull(dbTarget.Goats + amount, dbTarget.Wins))
                            embed.Description = $"{dcUser.Mention} du hast den **Angriff** gegen {dcTarget.Mention} **verloren** und ihm/ihr **{amount} Ziegen** überlassen..";
                        else
                        {
                            embed.Description = $"{dcUser.Mention} du hast den **Angriff** gegen {dcTarget.Mention} **verloren** und ihm/ihr **{amount} Ziegen** überlassen..\nLeider ist {dcTarget.Nickname ?? dcTarget.Username}'s **Stall voll**. Deswegen sind **{(dbTarget.Goats + amount) - Helper.GetStall(dbTarget.Wins).Capacity} Ziegen** zu Rabbot geflüchtet.";
                            rabbotUser.Goats += (dbTarget.Goats + amount) - Helper.GetStall(dbTarget.Wins).Capacity;
                        }
                        await dcChannel.SendMessageAsync(null, false, embed.Build());
                    }
                }

                dbTarget.Wins++;
                if (Helper.IsFull(dbTarget.Goats + amount, dbTarget.Wins))
                    dbTarget.Goats = Helper.GetStall(dbTarget.Wins).Capacity;
                else
                    dbTarget.Goats += amount;
                dbUser.Goats -= amount;
                dbUser.Loses++;
            }

            dbUser.Locked = false;
            dbTarget.Locked = false;
            await db.SaveChangesAsync();

            var targetStallAfter = Helper.GetStall(dbTarget.Wins);
            var userStallAfter = Helper.GetStall(dbUser.Wins);
            if ((targetStallAfter != targetStallBefore) && dcChannel != null)
            {
                embed.Color = Color.Green;
                embed.Description = $"{dcTarget.Mention} durch deinen **Sieg** hat sich dein Stall vergrößert! (Neuer Stall: **{targetStallAfter.Name}**)";
                await dcChannel.SendMessageAsync(null, false, embed.Build());
            }
            if ((userStallAfter != userStallBefore) && dcChannel != null)
            {
                embed.Color = Color.Green;
                embed.Description = $"{dcUser.Mention} durch deinen **Sieg** hat sich dein Stall vergrößert! (Neuer Stall: **{userStallAfter.Name}**)";
                await dcChannel.SendMessageAsync(null, false, embed.Build());
            }
        }
    }
}
