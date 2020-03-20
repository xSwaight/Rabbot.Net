using Discord;
using Discord.WebSocket;
using Rabbot.Database;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rabbot.Services
{
    class AttackService
    {
        private DiscordSocketClient DcClient { get; set; }

        private static readonly ILogger _logger = Log.ForContext(Serilog.Core.Constants.SourceContextPropertyName, nameof(AttackService));
        public AttackService(DiscordSocketClient client)
        {
            DcClient = client;
        }

        public async Task CheckAttacks()
        {
            using (swaightContext db = new swaightContext())
            {
                if (!db.Attacks.Any())
                    return;

                var attacks = db.Attacks.ToList();
                foreach (var attack in attacks)
                {
                    if (attack.AttackEnds < DateTime.Now)
                    {
                        await AttackResult(attack);
                        db.Attacks.Remove(attack);
                        await db.SaveChangesAsync();
                        continue;
                    }
                }
            }
        }

        public async Task AttackResult(Attacks attack)
        {
            var dcServer = DcClient.Guilds.FirstOrDefault(p => p.Id == (ulong)attack.ServerId);
            if (dcServer == null)
                return;
            var dcTarget = dcServer.Users.FirstOrDefault(p => p.Id == (ulong)attack.TargetId);
            var dcUser = dcServer.Users.FirstOrDefault(p => p.Id == (ulong)attack.UserId);
            var dcChannel = dcServer.Channels.FirstOrDefault(p => p.Id == (ulong)attack.ChannelId) as ISocketMessageChannel;

            using (swaightContext db = new swaightContext())
            {
                var dbTarget = db.Userfeatures.FirstOrDefault(p => p.ServerId == attack.ServerId && p.UserId == attack.TargetId) ?? db.Userfeatures.AddAsync(new Userfeatures { ServerId = attack.ServerId, UserId = (long)attack.TargetId, Exp = 0, Goats = 0 }).Result.Entity;
                var dbUser = db.Userfeatures.FirstOrDefault(p => p.ServerId == attack.ServerId && p.UserId == attack.UserId) ?? db.Userfeatures.AddAsync(new Userfeatures { ServerId = attack.ServerId, UserId = (long)attack.UserId, Exp = 0, Goats = 0 }).Result.Entity;
                var targetStallBefore = Helper.GetStall(dbTarget.Wins);
                var userStallBefore = Helper.GetStall(dbUser.Wins);
                var inventoryUser = db.Inventory.Join(db.Items, id => id.ItemId, item => item.Id, (Inventory, Item) => new { Inventory, Item }).Where(p => p.Inventory.FeatureId == dbUser.Id);
                var inventoryTarget = db.Inventory.Join(db.Items, id => id.ItemId, item => item.Id, (Inventory, Item) => new { Inventory, Item }).Where(p => p.Inventory.FeatureId == dbTarget.Id);
                var atkUser = userStallBefore.Attack;
                var defTarget = targetStallBefore.Defense;
                bool hirtenstab = false;
                bool zaun = false;

                var dcMessage = dcChannel.CachedMessages.FirstOrDefault(p => p.Id == (ulong)attack.MessageId) as SocketUserMessage;

                if (dcMessage != null)
                    foreach (var reaction in dcMessage.Reactions)
                    {
                        var emote = reaction.Key as Emote;

                        if(emote.Id == Constants.Shield.Id)
                        {
                            if (reaction.Value.ReactionCount >= 2)
                                zaun = true;
                        }
                        else if(emote.Id == Constants.Sword.Id)
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
                                    db.Inventory.Remove(item.Inventory);
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
                                    db.Inventory.Remove(item.Inventory);
                            }
                        }
                    }

                Random rnd = new Random();

                var sum = atkUser + defTarget;
                var winChance = ((double)atkUser / (double)sum) * 100;
                var chance = rnd.Next(1, 101);
                EmbedBuilder embed = new EmbedBuilder();
                var rabbotUser = db.Userfeatures.FirstOrDefault(p => p.ServerId == (long)dcServer.Id && p.UserId == (long)DcClient.CurrentUser.Id) ?? db.AddAsync(new Userfeatures { ServerId = (long)dcServer.Id, UserId = (long)DcClient.CurrentUser.Id, Goats = 0, Exp = 0 }).Result.Entity;
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

                dbUser.Locked = 0;
                dbTarget.Locked = 0;
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
}
