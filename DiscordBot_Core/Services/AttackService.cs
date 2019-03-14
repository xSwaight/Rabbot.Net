using Discord;
using Discord.WebSocket;
using DiscordBot_Core.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot_Core.Services
{
    class AttackService
    {
        private DiscordSocketClient DcClient { get; set; }

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
            var dcServer = DcClient.Guilds.Where(p => p.Id == (ulong)attack.ServerId).FirstOrDefault();
            if (dcServer == null)
                return;
            var dcTarget = dcServer.Users.Where(p => p.Id == (ulong)attack.TargetId).FirstOrDefault();
            var dcUser = dcServer.Users.Where(p => p.Id == (ulong)attack.UserId).FirstOrDefault();
            var dcChannel = dcServer.Channels.Where(p => p.Id == (ulong)attack.ChannelId).FirstOrDefault() as ISocketMessageChannel;

            using (swaightContext db = new swaightContext())
            {
                var dbTarget = db.Userfeatures.Where(p => p.ServerId == attack.ServerId && p.UserId == attack.TargetId).FirstOrDefault() ?? db.Userfeatures.AddAsync(new Userfeatures { ServerId = attack.ServerId, UserId = (long)attack.TargetId, Exp = 0, Goats = 0 }).Result.Entity;
                var dbUser = db.Userfeatures.Where(p => p.ServerId == attack.ServerId && p.UserId == attack.UserId).FirstOrDefault() ?? db.Userfeatures.AddAsync(new Userfeatures { ServerId = attack.ServerId, UserId = (long)attack.UserId, Exp = 0, Goats = 0 }).Result.Entity;
                var targetStallBefore = Helper.GetStall(dbTarget.Wins);
                var userStallBefore = Helper.GetStall(dbUser.Wins);
                var inventoryUser = db.Inventory.Join(db.Items, id => id.ItemId, item => item.Id, (Inventory, Item) => new { Inventory, Item }).Where(p => p.Inventory.FeatureId == dbUser.Id);
                var inventoryTarget = db.Inventory.Join(db.Items, id => id.ItemId, item => item.Id, (Inventory, Item) => new { Inventory, Item }).Where(p => p.Inventory.FeatureId == dbTarget.Id);
                var atkUser = userStallBefore.Attack;
                var defTarget = targetStallBefore.Defense;

                if (inventoryUser.Count() != 0)
                {
                    foreach (var item in inventoryUser)
                    {
                        atkUser += item.Item.Atk;
                    }
                }

                if (inventoryTarget.Count() != 0)
                {
                    foreach (var item in inventoryTarget)
                    {
                        defTarget += item.Item.Def;
                    }
                }

                Random rnd = new Random();

                var sum = atkUser + defTarget;
                var winChance = ((double)atkUser / (double)sum) * 100;
                var chance = rnd.Next(1, 101);
                EmbedBuilder embed = new EmbedBuilder();
                if (chance <= winChance)
                {
                    int amount = rnd.Next(40, targetStallBefore.MaxOutput + 1);
                    if (!(dcTarget == null || dcUser == null))
                    {
                        if (dcChannel != null)
                        {
                            embed.Color = Color.Green;
                            embed.Description = $"{dcUser.Mention} du hast den **Angriff** gegen {dcTarget.Mention} **gewonnen** und **{amount} Ziegen** erbeutet!";
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
                    if (!(dcTarget == null || dcUser == null))
                    {
                        if (dcChannel != null)
                        {
                            embed.Color = Color.Red;
                            embed.Description = $"{dcUser.Mention} du hast den **Angriff** gegen {dcTarget.Mention} **verloren** und ihm/ihr **{amount} Ziegen** überlassen.. ";
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
