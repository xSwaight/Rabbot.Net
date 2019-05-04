using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Rabbot.Database;
using Rabbot.Preconditions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Rabbot.Commands
{
    public class Goats : ModuleBase<SocketCommandContext>
    {

        [Command("daily", RunMode = RunMode.Async)]
        [BotCommand]
        public async Task Daily()
        {
            using (swaightContext db = new swaightContext())
            {
                var dbUser = db.Userfeatures.Where(p => p.UserId == (long)Context.User.Id && p.ServerId == (long)Context.Guild.Id).FirstOrDefault() ?? db.Userfeatures.AddAsync(new Userfeatures { UserId = (long)Context.User.Id, ServerId = (long)Context.Guild.Id, Exp = 0, Goats = 0 }).Result.Entity;
                if (dbUser.Lastdaily != null)
                {
                    if (dbUser.Lastdaily.Value.ToShortDateString() != DateTime.Now.ToShortDateString())
                    {
                        dbUser.Lastdaily = DateTime.Now;
                        await db.SaveChangesAsync();
                        await GetDailyReward(Context);
                    }
                    else
                    {
                        var now = DateTime.Now;
                        var tomorrow = now.AddDays(1).Date;
                        TimeSpan totalTime = (tomorrow - now);
                        await Context.Channel.SendMessageAsync($"Du hast heute schon deine Daily Ziegen bekommen.\n**Komm in {totalTime.Hours}h {totalTime.Minutes}m wieder und versuche es erneut!**");
                    }
                }
                else
                {
                    dbUser.Lastdaily = DateTime.Now;
                    await db.SaveChangesAsync();
                    await GetDailyReward(Context);
                }
            }
        }

        private async Task GetDailyReward(SocketCommandContext context)
        {
            Random rnd = new Random();
            int chance = rnd.Next(1, 16);
            int jackpot = rnd.Next(1, 101);
            EmbedBuilder embed = new EmbedBuilder();
            if (chance > 1)
            {
                using (swaightContext db = new swaightContext())
                {
                    var dbUser = db.Userfeatures.Where(p => p.UserId == (long)Context.User.Id && p.ServerId == (long)Context.Guild.Id).FirstOrDefault();
                    var stall = Helper.GetStall(dbUser.Wins);
                    if (jackpot <= 2)
                    {

                        if (Helper.IsFull(dbUser.Goats + stall.Jackpot, dbUser.Wins))
                        {
                            embed.Color = new Color(0, 203, 255);
                            embed.Description = $"**Jackpot**!! Dorq besucht dich mit seiner Familie und hinterlässt dir **{stall.Jackpot} Ziegen**. Verdammter **Glückspilz**!\nAllerdings ist **dein Stall ({stall.Name}) voll** und **{(dbUser.Goats + stall.Jackpot) - stall.Capacity} Ziegen** sind wieder entlaufen..";
                            await Context.Channel.SendMessageAsync(null, false, embed.Build());
                            dbUser.Goats = stall.Capacity;
                        }
                        else
                        {
                            dbUser.Goats += stall.Jackpot;
                            embed.Color = new Color(0, 203, 255);
                            embed.Description = $"**Jackpot**!! Dorq besucht dich mit seiner Familie und hinterlässt dir **{stall.Jackpot} Ziegen**. Verdammter **Glückspilz**!";
                            await Context.Channel.SendMessageAsync(null, false, embed.Build());
                        }
                        await db.SaveChangesAsync();
                        return;
                    }
                    int goats = 0;
                    goats = rnd.Next(40, 101);
                    if (Helper.IsFull(dbUser.Goats + goats, dbUser.Wins))
                    {
                        embed.Color = Color.Green;
                        embed.Description = $"Wow, du konntest heute unfassbare **{goats} Ziegen** einfangen!\nAllerdings ist **dein Stall ({stall.Name}) voll** und **{(dbUser.Goats + goats) - stall.Capacity} Ziegen** sind wieder entlaufen..";
                        await Context.Channel.SendMessageAsync(null, false, embed.Build());
                        dbUser.Goats = stall.Capacity;
                    }
                    else
                    {
                        dbUser.Goats += goats;
                        embed.Color = Color.Green;
                        embed.Description = $"Wow, du konntest heute unfassbare **{goats} Ziegen** einfangen!";
                        await Context.Channel.SendMessageAsync(null, false, embed.Build());
                    }
                    await db.SaveChangesAsync();
                }
            }
            else
            {
                embed.Color = Color.Red;
                embed.Description = $"Gib dir nächstes mal **mehr Mühe**!";
                await Context.Channel.SendMessageAsync(null, false, embed.Build());
            }
        }

        //[Command("trade")]
        //[BotCommand]
        //[Cooldown(30)]
        //public async Task Trade(IUser user, int amount)
        //{
        //    if (amount > 0 && (!user.IsBot || user.Id == Context.Client.CurrentUser.Id))
        //    {
        //        using (swaightContext db = new swaightContext())
        //        {
        //            var senderUser = db.Userfeatures.Where(p => p.ServerId == (long)Context.Guild.Id && p.UserId == (long)Context.User.Id).FirstOrDefault() ?? db.AddAsync(new Userfeatures { ServerId = (long)Context.Guild.Id, UserId = (long)Context.Guild.Id, Goats = 0, Exp = 0 }).Result.Entity;
        //            if (senderUser.Goats < amount)
        //            {
        //                await Context.Message.DeleteAsync();
        //                var msg = await Context.Channel.SendMessageAsync($"Dir fehlen **{amount - senderUser.Goats} Ziegen**..");
        //                await Task.Delay(3000);
        //                await msg.DeleteAsync();
        //                await db.SaveChangesAsync();
        //                return;
        //            }
        //            if (senderUser.Trades >= 5)
        //            {
        //                await Context.Message.DeleteAsync();
        //                var msg = await Context.Channel.SendMessageAsync($"**Du kannst heute nicht mehr traden!**");
        //                await Task.Delay(3000);
        //                await msg.DeleteAsync();
        //                await db.SaveChangesAsync();
        //                return;
        //            }

        //            var targetUser = db.Userfeatures.Where(p => p.ServerId == (long)Context.Guild.Id && p.UserId == (long)user.Id).FirstOrDefault() ?? db.AddAsync(new Userfeatures { ServerId = (long)Context.Guild.Id, UserId = (long)user.Id, Goats = 0, Exp = 0 }).Result.Entity;
        //            if (targetUser == senderUser)
        //            {
        //                await Context.Channel.SendMessageAsync($"Nö.");
        //                return;
        //            }

        //            if (targetUser.Locked == 1)
        //            {
        //                await Context.Message.DeleteAsync();
        //                var msg = await Context.Channel.SendMessageAsync($"**{user.Mention} hat gerade eine Trading Sperre!**");
        //                await Task.Delay(3000);
        //                await msg.DeleteAsync();
        //                return;
        //            }

        //            if (senderUser.Locked == 1)
        //            {
        //                await Context.Message.DeleteAsync();
        //                var msg = await Context.Channel.SendMessageAsync($"**{Context.User.Mention} du hast gerade eine Trading Sperre!**");
        //                await Task.Delay(3000);
        //                await msg.DeleteAsync();
        //                return;
        //            }



        //            var rabbotUser = db.Userfeatures.Where(p => p.ServerId == (long)Context.Guild.Id && p.UserId == (long)Context.Client.CurrentUser.Id).FirstOrDefault() ?? db.AddAsync(new Userfeatures { ServerId = (long)Context.Guild.Id, UserId = (long)Context.Client.CurrentUser.Id, Goats = 0, Exp = 0 }).Result.Entity;
        //            senderUser.Goats -= amount;
        //            int fees = amount / 4;
        //            if (Helper.IsFull(targetUser.Goats + amount - fees, targetUser.Wins))
        //            {
        //                await Context.Message.DeleteAsync();
        //                var msg = await Context.Channel.SendMessageAsync($"**Leider ist der Stall von {user.Mention} schon zu voll!**");
        //                await Task.Delay(3000);
        //                await msg.DeleteAsync();
        //                return;
        //            }
        //            rabbotUser.Goats += fees;
        //            targetUser.Goats += amount - fees;
        //            senderUser.Trades++;
        //            await db.SaveChangesAsync();

        //            if (amount > 1)
        //                await Context.Channel.SendMessageAsync($"{Context.User.Username} hat {user.Mention} **{amount - fees} Ziegen** (-{fees} Schutzgeldziegens) geschenkt! Du kannst heute noch **{5 - senderUser.Trades} mal** traden.");
        //            else
        //                await Context.Channel.SendMessageAsync($"{Context.User.Username} hat {user.Mention} **{amount} Ziege** geschenkt!");

        //        }
        //    }
        //}


        //[Command("namechange", RunMode = RunMode.Async)]
        //[BotCommand]
        //public async Task Namechange(string newName)
        //{
        //    if (newName.Count() > 32)
        //    {
        //        await Context.Channel.SendMessageAsync($"{Context.User.Mention} der Name darf maximal **32 Zeichen** lang sein.");
        //        return;
        //    }
        //    if (newName.Count() < 4)
        //    {
        //        await Context.Channel.SendMessageAsync($"{Context.User.Mention} der Name muss maximal **4 Zeichen** lang sein.");
        //        return;
        //    }
        //    var test = new Regex("[\\]\\[]");
        //    if (test.IsMatch(newName))
        //    {
        //        await Context.Channel.SendMessageAsync($"{Context.User.Mention} der Name enthält unerlaubte Zeichen!");
        //        return;
        //    }
        //    using (swaightContext db = new swaightContext())
        //    {
        //        var user = Context.User as SocketGuildUser;
        //        var dbUser = db.Experience.Where(p => p.UserId == (long)user.Id && p.ServerId == (long)Context.Guild.Id).FirstOrDefault() ?? db.Experience.AddAsync(new Experience { ServerId = (long)Context.Guild.Id, UserId = (long)user.Id, Exp = 0 }).Result.Entity;
        //        if (dbUser.Goats >= 100)
        //        {
        //            dbUser.Goats -= 100;
        //            await user.ModifyAsync(p => p.Nickname = newName);
        //            dbUser.NamechangeUntil = DateTime.Now.AddDays(8);
        //            await Context.Channel.SendMessageAsync($"{Context.User.Mention} dein Name wurde erfolgreich für **100 Ziegen** zu **{newName}** geändert!\nDein Namechange hält bis zum **{dbUser.NamechangeUntil.Value.ToShortDateString()} 00:00 Uhr**!");
        //            await db.SaveChangesAsync();
        //        }
        //        else
        //        {
        //            await Context.Channel.SendMessageAsync($"{Context.User.Mention} dir fehlen leider **{100 - dbUser.Goats} Ziegen** um deinen Namen zu ändern!");
        //        }
        //    }
        //}

        //[Command("minuten", RunMode = RunMode.Async)]
        //[BotCommand]
        //[Cooldown(60)]
        //public async Task Minuten(int amount)
        //{
        //    if (amount < 1)
        //        return;

        //    using (swaightContext db = new swaightContext())
        //    {
        //        var dbMusic = db.Musicrank.Where(p => p.ServerId == (long)Context.Guild.Id && p.UserId == (long)Context.User.Id).FirstOrDefault() ?? db.Musicrank.AddAsync(new Musicrank { ServerId = (long)Context.Guild.Id, UserId = (long)Context.User.Id, Date = DateTime.Now, Sekunden = 0 }).Result.Entity;
        //        var dbUser = db.Userfeatures.Where(p => p.UserId == (long)Context.User.Id && p.ServerId == (long)Context.Guild.Id).FirstOrDefault() ?? db.Userfeatures.AddAsync(new Userfeatures { ServerId = (long)Context.Guild.Id, UserId = (long)Context.User.Id, Exp = 0 }).Result.Entity;

        //        if (dbUser.Locked == 1)
        //        {
        //            await Context.Message.DeleteAsync();
        //            var msg = await Context.Channel.SendMessageAsync($"**{Context.User.Mention} du hast gerade eine Shop Sperre!**");
        //            await Task.Delay(3000);
        //            await msg.DeleteAsync();
        //            return;
        //        }
        //        if (dbUser.Goats >= amount)
        //        {
        //            if (dbMusic.Date.Value.ToShortDateString() != DateTime.Now.ToShortDateString())
        //            {
        //                dbMusic.Date = DateTime.Now;
        //                dbMusic.Sekunden = 0;
        //            }
        //            dbUser.Goats -= amount;
        //            dbMusic.Sekunden += amount;
        //            await db.SaveChangesAsync();
        //            await Context.Channel.SendMessageAsync($"{Context.User.Mention} du hast dir erfolgreich **{amount} Minuten** im Musicrank gekauft!");
        //        }
        //        else
        //        {
        //            await Context.Channel.SendMessageAsync($"{Context.User.Mention} dir fehlen leider **{amount - dbUser.Goats} Ziegen**!");
        //        }
        //    }
        //}

        [Command("angriff", RunMode = RunMode.Async)]
        [Alias("attack", "atk")]
        [BotCommand]
        public async Task Angriff(IUser targetUser)
        {
            Random rnd = new Random();
            await Task.Delay(rnd.Next(1, 201));
            EmbedBuilder embed = new EmbedBuilder();
            if (Context.User.Id == targetUser.Id || targetUser.IsBot)
            {
                embed.Color = Color.Red;
                embed.Description = "Ne man. Lass mal. Muss nicht sein.";
                await Context.Channel.SendMessageAsync(null, false, embed.Build());
                return;
            }

            using (swaightContext db = new swaightContext())
            {
                var dbTarget = db.Userfeatures.Where(p => p.ServerId == (long)Context.Guild.Id && p.UserId == (long)targetUser.Id).FirstOrDefault() ?? db.Userfeatures.AddAsync(new Userfeatures { ServerId = (long)Context.Guild.Id, UserId = (long)targetUser.Id, Exp = 0, Goats = 0 }).Result.Entity;
                var dbUser = db.Userfeatures.Where(p => p.ServerId == (long)Context.Guild.Id && p.UserId == (long)Context.User.Id).FirstOrDefault() ?? db.Userfeatures.AddAsync(new Userfeatures { ServerId = (long)Context.Guild.Id, UserId = (long)Context.User.Id, Exp = 0, Goats = 0 }).Result.Entity;
                var targetStall = Helper.GetStall(dbTarget.Wins);
                var userStall = Helper.GetStall(dbUser.Wins);

                if (dbUser.Attacks >= 5)
                {
                    embed.Color = Color.Red;
                    embed.Description = $"{Context.User.Mention} du hast deine **heutigen** Kämpfe bereits **verbraucht**!";
                    await Context.Channel.SendMessageAsync(null, false, embed.Build());
                    return;
                }

                if (targetStall.Level < (userStall.Level - 3))
                {
                    embed.Color = Color.Red;
                    embed.Description = $"{Context.User.Mention} das **Stall Level** deines Gegners ist zu **niedrig**.";
                    await Context.Channel.SendMessageAsync(null, false, embed.Build());
                    return;
                }

                if (dbUser.Goats < userStall.MaxOutput)
                {
                    embed.Color = Color.Red;
                    embed.Description = $"{Context.User.Mention} du musst mindestens **{userStall.MaxOutput} Ziegen** haben um jemanden **angreifen** zu können!";
                    await Context.Channel.SendMessageAsync(null, false, embed.Build());
                    return;
                }
                if (dbTarget.Goats < targetStall.MaxOutput)
                {
                    embed.Color = Color.Red;
                    embed.Description = $"{Context.User.Mention} dein Opfer muss mindestens **{targetStall.MaxOutput} Ziegen** haben um **angegriffen** werden zu können!";
                    await Context.Channel.SendMessageAsync(null, false, embed.Build());
                    return;
                }

                if (dbTarget.Locked == 1)
                {
                    embed.Color = Color.Red;
                    embed.Description = $"{Context.User.Mention} dein Opfer ist aktuell schon in einem **Kampf**!";
                    await Context.Channel.SendMessageAsync(null, false, embed.Build());
                    return;
                }
                if (dbUser.Locked == 1)
                {
                    embed.Color = Color.Red;
                    embed.Description = $"{Context.User.Mention} du bist aktuell schon in einem **Kampf**!";
                    await Context.Channel.SendMessageAsync(null, false, embed.Build());
                    return;
                }

                dbUser.Attacks++;
                embed.Color = new Color(242, 255, 0);
                var shield = new Emoji("🛡");
                var dagger = new Emoji("🗡");
                embed.Description = $"{Context.User.Mention} du hast erfolgreich einen Angriff gegen {targetUser.Mention} gestartet!\nDu kannst heute noch **{5 - dbUser.Attacks} mal** angreifen.";
                embed.Description += $"\n\n**Interaktionen:**";
                embed.Description += $"\n{dagger} = Hirtenstab       (Nur Angreifer)";
                embed.Description += $"\n{shield} = Stacheldrahtzaun (Nur Angegriffener)";
                embed.WithFooter("Der Angriff dauert 3 Minuten!");

                var stallTarget = Helper.GetStall(dbTarget.Wins);
                var stallUser = Helper.GetStall(dbUser.Wins);
                var atk = stallUser.Attack;
                var def = stallTarget.Defense;

                var sum = atk + def;
                var winChance = ((double)atk / (double)sum) * 100;

                string chance = $"**{Math.Round(winChance)}% {Context.User.Username} - {targetUser.Username} {100 - Math.Round(winChance)}%**";

                var msg = await Context.Channel.SendMessageAsync(chance, false, embed.Build());
                dbUser.Locked = 1;
                dbTarget.Locked = 1;
                await db.Attacks.AddAsync(new Attacks { ServerId = (long)Context.Guild.Id, UserId = (long)Context.User.Id, ChannelId = (long)Context.Channel.Id, MessageId = (long)msg.Id, TargetId = (long)targetUser.Id, AttackEnds = DateTime.Now.AddMinutes(3) });
                await db.SaveChangesAsync();
                await msg.AddReactionAsync(dagger);
                await msg.AddReactionAsync(shield);
            }
        }


        [Command("shop", RunMode = RunMode.Async)]
        [BotCommand]
        [Cooldown(10)]
        public async Task Shop()
        {
            var embed = new EmbedBuilder();
            embed.WithTitle("Willkommen im Shop!");
            embed.WithDescription("Gönn dir.");
            embed.WithColor(new Color(241, 242, 222));
            //embed.AddField($"{Config.bot.cmdPrefix}minuten [Anzahl]", $"Minuten im Musicrank\nPreis: 1 Ziege für 1 Minute");
            embed.AddField($"{Config.bot.cmdPrefix}hirtenstab", $"Hirtenstab (+ 20 ATK) 5 Benutzungen\nPreis: 100 Ziegen");
            embed.AddField($"{Config.bot.cmdPrefix}zaun", $"Stacheldrahtzaun (+ 30 DEF) 7 Benutzungen\nPreis: 75 Ziegen");
            //embed.AddField($"{Config.bot.cmdPrefix}namechange [Name]", $"7 Tage Namechange\nPreis: 100 Ziegen");
            await Context.Channel.SendMessageAsync(null, false, embed.Build());
        }

        [Command("hirtenstab", RunMode = RunMode.Async)]
        [BotCommand]
        [Cooldown(10)]
        public async Task Hirtenstab()
        {
            EmbedBuilder embed = new EmbedBuilder();
            using (swaightContext db = new swaightContext())
            {
                var features = db.Userfeatures.Where(p => p.UserId == (long)Context.User.Id && p.ServerId == (long)Context.Guild.Id).FirstOrDefault();
                if (features.Locked == 1)
                {
                    embed.Color = Color.Red;
                    embed.Description = $"**{Context.User.Mention} du hast gerade eine Shop Sperre!**";
                    await Context.Channel.SendMessageAsync(null, false, embed.Build());
                    return;
                }
                if (features.Goats < 100)
                {
                    embed.Color = Color.Red;
                    embed.Description = $"{Context.User.Mention} du hast nicht ausreichend Ziegen!";
                    await Context.Channel.SendMessageAsync(null, false, embed.Build());
                    return;
                }

                var inventar = db.Inventory.Where(p => p.FeatureId == features.Id && p.ItemId == 1).FirstOrDefault();
                if (inventar != null)
                    inventar.Durability += 5;
                else
                    await db.Inventory.AddAsync(new Inventory { FeatureId = features.Id, ItemId = 1, Durability = 5 });

                features.Goats -= 100;
                await db.SaveChangesAsync();
            }
            embed.Color = Color.Green;
            embed.Description = $"{Context.User.Mention} du hast dir erfolgreich für **100 Ziegen** einen **Hirtenstab** gekauft.\nDu kannst ihn bei einem Angriff benutzen um **+20 ATK** mehr zu haben!";
            await Context.Channel.SendMessageAsync(null, false, embed.Build());
        }

        [Command("zaun", RunMode = RunMode.Async)]
        [BotCommand]
        [Cooldown(10)]
        public async Task Zaun()
        {
            EmbedBuilder embed = new EmbedBuilder();
            using (swaightContext db = new swaightContext())
            {
                var features = db.Userfeatures.Where(p => p.UserId == (long)Context.User.Id && p.ServerId == (long)Context.Guild.Id).FirstOrDefault();
                if (features.Locked == 1)
                {
                    embed.Color = Color.Red;
                    embed.Description = $"**{Context.User.Mention} du hast gerade eine Shop Sperre!**";
                    await Context.Channel.SendMessageAsync(null, false, embed.Build());
                    return;
                }
                if (features.Goats < 75)
                {
                    embed.Color = Color.Red;
                    embed.Description = $"{Context.User.Mention} du hast nicht ausreichend Ziegen!";
                    await Context.Channel.SendMessageAsync(null, false, embed.Build());
                    return;
                }

                var inventar = db.Inventory.Where(p => p.FeatureId == features.Id && p.ItemId == 2).FirstOrDefault();
                if (inventar != null)
                    inventar.Durability += 7;
                else
                    await db.Inventory.AddAsync(new Inventory { FeatureId = features.Id, ItemId = 2, Durability = 7 });

                features.Goats -= 75;
                await db.SaveChangesAsync();
            }
            embed.Color = Color.Green;
            embed.Description = $"{Context.User.Mention} du hast dir erfolgreich für **75 Ziegen** einen **Stacheldrahtzaun** gekauft.\nDu kannst ihn bei einem Angriff gegen dich benutzen um **+30 DEF** mehr zu haben!";
            await Context.Channel.SendMessageAsync(null, false, embed.Build());
        }

        [Command("stall", RunMode = RunMode.Async)]
        [BotCommand]
        [Cooldown(10)]
        public async Task Stall(IUser target = null)
        {
            using (swaightContext db = new swaightContext())
            {
                var embed = new EmbedBuilder();
                if (target == null)
                {
                    var user = db.Userfeatures.Where(p => p.UserId == (long)Context.User.Id && p.ServerId == (long)Context.Guild.Id).FirstOrDefault();
                    var stall = Helper.GetStall(user.Wins);
                    embed.WithTitle($"Stall von {Context.User.Username}");
                    embed.WithDescription(stall.Name);
                    embed.WithColor(new Color(241, 242, 222));
                    embed.AddField($"Level", $"{stall.Level} / 26");
                    var percent = ((double)user.Goats / (double)stall.Capacity) * 100;
                    embed.AddField($"Kapazität", $"{user.Goats.ToString("N0")} / {stall.Capacity.ToString("N0")} Ziegen ({Math.Round(percent, 0)}%)");
                    embed.AddField($"Stats", $"ATK: **{stall.Attack.ToString("N0")}0** | DEF: **{stall.Defense.ToString("N0")}0**");
                }
                else
                {
                    var user = db.Userfeatures.Where(p => p.UserId == (long)target.Id && p.ServerId == (long)Context.Guild.Id).FirstOrDefault();
                    var stall = Helper.GetStall(user.Wins);
                    embed.WithTitle($"Stall von {target.Username}");
                    embed.WithDescription(stall.Name);
                    embed.WithColor(new Color(241, 242, 222));
                    embed.AddField($"Level", $"{stall.Level} / 26");
                    var percent = ((double)user.Goats / (double)stall.Capacity) * 100;
                    embed.AddField($"Kapazität", $"{user.Goats.ToString("N0")} / {stall.Capacity.ToString("N0")} Ziegen ({Math.Round(percent, 0)}%)");
                    embed.AddField($"Stats", $"ATK: **{stall.Attack.ToString("N0")}0** | DEF: **{stall.Defense.ToString("N0")}0**");
                }
                await Context.Channel.SendMessageAsync(null, false, embed.Build());
            }
        }

        [Command("stats", RunMode = RunMode.Async)]
        [BotCommand]
        [Cooldown(10)]
        public async Task Stats(IUser target = null)
        {
            using (swaightContext db = new swaightContext())
            {
                var embed = new EmbedBuilder();
                if (target == null)
                {
                    var user = db.Userfeatures.Where(p => p.UserId == (long)Context.User.Id && p.ServerId == (long)Context.Guild.Id).FirstOrDefault();
                    var inventory = db.Inventory.Join(db.Items, id => id.ItemId, item => item.Id, (Inventory, Item) => new { Inventory, Item }).Where(p => p.Inventory.FeatureId == user.Id);
                    var stall = Helper.GetStall(user.Wins);
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

                    embed.WithTitle($"Statistiken von {Context.User.Username}");
                    embed.WithColor(new Color(241, 242, 222));
                    embed.AddField($"Battle", $"**{(user.Loses + user.Wins).ToString("N0")}** Kämpfe | **{user.Wins.ToString("N0")}** Siege | **{user.Loses.ToString("N0")}** Niederlagen");
                    embed.AddField($"Aktueller Stall", $"{stall.Name}");
                    embed.AddField($"Stats", $"ATK: **{stall.Attack.ToString("N0")}0** | DEF: **{stall.Defense.ToString("N0")}0**");
                    if (inventory.Count() != 0)
                    {
                        string items = "";
                        foreach (var item in inventory)
                        {
                            items += $"**{item.Item.Name}** - übrige Benutzungen: **{item.Inventory.Durability}**\n";
                        }
                        embed.AddField($"Inventar", items);
                    }
                }
                else
                {
                    try
                    {
                        var myUser = db.Userfeatures.Where(p => p.UserId == (long)Context.User.Id && p.ServerId == (long)Context.Guild.Id).FirstOrDefault();

                        var targetUser = db.Userfeatures.Where(p => p.UserId == (long)target.Id && p.ServerId == (long)Context.Guild.Id).FirstOrDefault();
                        var inventory = db.Inventory.Join(db.Items, id => id.ItemId, item => item.Id, (Inventory, Item) => new { Inventory, Item }).Where(p => p.Inventory.FeatureId == targetUser.Id);
                        var UserInventory = db.Inventory.Join(db.Items, id => id.ItemId, item => item.Id, (Inventory, Item) => new { Inventory, Item }).Where(p => p.Inventory.FeatureId == myUser.Id);
                        var stall = Helper.GetStall(targetUser.Wins);
                        var userStall = Helper.GetStall(myUser.Wins);
                        var atk = stall.Attack;
                        var def = stall.Defense;

                        var sumWithout = stall.Attack + userStall.Defense;
                        var winChanceWithout = ((double)stall.Attack / (double)sumWithout) * 100;

                        if (inventory.Count() != 0)
                        {
                            foreach (var item in inventory)
                            {
                                atk += item.Item.Atk;
                                def += item.Item.Def;
                            }
                        }

                        var userAtk = userStall.Attack;
                        var userDef = userStall.Defense;

                        if (UserInventory.Count() != 0)
                        {
                            foreach (var item in UserInventory)
                            {
                                userAtk += item.Item.Atk;
                                userDef += item.Item.Def;
                            }
                        }

                        var sum = userAtk + def;
                        var winChance = ((double)userAtk / (double)sum) * 100;
                        //var blockChance = ((double)userDef / (double)sum) * 100;

                        embed.WithTitle($"Statistiken von {target.Username}");
                        embed.WithColor(new Color(241, 242, 222));
                        embed.AddField($"Battle", $"**{(targetUser.Loses + targetUser.Wins).ToString("N0")}** Kämpfe | **{targetUser.Wins.ToString("N0")}** Siege | **{targetUser.Loses.ToString("N0")}** Niederlagen");
                        embed.AddField($"Aktueller Stall", $"{stall.Name}");
                        embed.AddField($"Stats", $"ATK: **{stall.Attack.ToString("N0")}0** | DEF: **{stall.Defense.ToString("N0")}0**");
                        embed.AddField($"Gewinnchance ohne Items", $"{Math.Round(winChanceWithout)}%");
                        if (winChance != winChanceWithout)
                            embed.AddField($"Gewinnchance mit Items", $"{Math.Round(winChance)}%");
                        //embed.AddField($"Blockchance gegen {target.Username}", $"{Math.Round(blockChance)}%");
                        if (inventory.Count() != 0)
                        {
                            string items = "";
                            foreach (var item in inventory)
                            {
                                items += $"**{item.Item.Name}** - übrige Benutzungen: **{item.Inventory.Durability}**\n";
                            }
                            embed.AddField($"Inventar", items);
                        }
                    }
                    catch
                    {
                        throw;
                    }
                }
                await Context.Channel.SendMessageAsync(null, false, embed.Build());
            }
        }

        [Command("stalls", RunMode = RunMode.Async)]
        [BotCommand]
        [Cooldown(10)]
        public async Task StallListe()
        {
            var embed = new EmbedBuilder();
            var embed2 = new EmbedBuilder();

            foreach (var stall in Helper.stall)
            {
                try
                {
                    embed.AddField($"Level {stall.Value.Level} | {stall.Value.Name}", $"Benötigte Siege: {stall.Key.ToString("N0")} | Kapazität: {stall.Value.Capacity.ToString("N0")} Ziegen");
                }
                catch
                {
                    embed2.AddField($"Level {stall.Value.Level} | {stall.Value.Name}", $"Benötigte Siege: {stall.Key.ToString("N0")} | Kapazität: {stall.Value.Capacity.ToString("N0")} Ziegen");
                }
            }
            await Context.Channel.SendMessageAsync(null, false, embed.Build());
            await Context.Channel.SendMessageAsync(null, false, embed2.Build());
        }

        [Command("wins", RunMode = RunMode.Async)]
        [BotCommand]
        [Cooldown(10)]
        public async Task Wins()
        {
            using (swaightContext db = new swaightContext())
            {
                var top25 = db.Userfeatures.Where(p => p.Wins != 0 && p.ServerId == (long)Context.Guild.Id).OrderByDescending(p => p.Wins).Take(25);
                EmbedBuilder embed = new EmbedBuilder();
                int counter = 1;
                foreach (var top in top25)
                {
                    var user = db.User.Where(p => p.Id == top.UserId).FirstOrDefault();
                    var stall = Helper.GetStall(top.Wins);
                    embed.AddField($"{counter}. {user.Name}", $"**{top.Wins.ToString("N0")}** Siege | **{top.Loses}** Niederlagen | Stall Level: **{stall.Level}** | Ziegen: **{top.Goats}**");
                    counter++;
                }
                await Context.Channel.SendMessageAsync(null, false, embed.Build());
            }
        }

        [Command("pot", RunMode = RunMode.Async)]
        [BotCommand]
        [Cooldown(10)]
        public async Task Pot(int amount)
        {
            try
            {
                if (amount < 1)
                    return;

                EmbedBuilder embed = new EmbedBuilder();
                if (amount < 10)
                {
                    embed.Color = Color.Red;
                    embed.Description = $"{Context.User.Mention} du musst mindestens **10 Ziegen** in den Pot stecken!";
                    await Context.Channel.SendMessageAsync(null, false, embed.Build());
                    return;
                }

                using (swaightContext db = new swaightContext())
                {
                    var myUser = db.Userfeatures.Where(p => p.UserId == (long)Context.User.Id && p.ServerId == (long)Context.Guild.Id).FirstOrDefault();
                    if (myUser.Goats < amount)
                    {
                        embed.Color = Color.Red;
                        embed.Description = $"{Context.User.Mention} du hast leider **nicht ausreichend Ziegen**.";
                        await Context.Channel.SendMessageAsync(null, false, embed.Build());
                        return;
                    }

                    if (myUser.Locked == 1)
                    {
                        embed.Color = Color.Red;
                        embed.Description = $"{Context.User.Mention} du hast gerade eine **Sperre**, da du in einem Kampf bist.";
                        await Context.Channel.SendMessageAsync(null, false, embed.Build());
                        return;
                    }

                    var stall = Helper.GetStall(myUser.Wins);
                    if(amount > stall.MaxPot)
                    {
                        embed.Color = Color.Red;
                        embed.Description = $"{Context.User.Mention} du kannst auf deinem **Stall Level** maximal **{stall.MaxPot.ToString("N0")} Ziegen** in den Pot stecken!";
                        await Context.Channel.SendMessageAsync(null, false, embed.Build());
                        return;
                    }

                    var myPot = db.Pot.Where(p => p.UserId == (long)Context.User.Id && p.ServerId == (long)Context.Guild.Id).FirstOrDefault() ?? db.Pot.AddAsync(new Pot { UserId = (long)Context.User.Id, ServerId = (long)Context.Guild.Id, Goats = 0 }).Result.Entity;
                    if (amount + myPot.Goats > stall.MaxPot)
                    {
                        embed.Color = Color.Red;
                        embed.Description = $"{Context.User.Mention} du kannst auf deinem **Stall Level** maximal **{stall.MaxPot.ToString("N0")} Ziegen** in den Pot stecken!";
                        await Context.Channel.SendMessageAsync(null, false, embed.Build());
                        return;
                    }

                    myPot.Goats += amount;
                    myUser.Goats -= amount;

                    await db.SaveChangesAsync();

                    embed.Color = Color.Green;
                    embed.Description = $"{Context.User.Mention} du hast erfolgreich **{amount} Ziegen** in den Pot gesteckt!";
                    await Context.Channel.SendMessageAsync(null, false, embed.Build());
                }
            }
            catch (Exception e)
            {

                Console.WriteLine(e.Message + e.StackTrace);
            }

        }

        [Command("chance", RunMode = RunMode.Async)]
        [BotCommand]
        [Cooldown(10)]
        public async Task Chance()
        {
            using (swaightContext db = new swaightContext())
            {
                var sum = db.Pot.Where(p => p.ServerId == (long)Context.Guild.Id).OrderByDescending(p => p.Goats).Sum(p => p.Goats);
                var pot = db.Pot.Where(p => p.ServerId == (long)Context.Guild.Id).OrderByDescending(p => p.Goats).Take(25);
                EmbedBuilder embed = new EmbedBuilder();
                int counter = 1;
                foreach (var item in pot)
                {
                    var chance = (double)item.Goats / (double)sum * 100;
                    var user = db.User.Where(p => p.Id == item.UserId).FirstOrDefault();
                    embed.Title = $"{sum.ToString("N0")} Ziegen im Pot!";
                    embed.AddField($"{counter}. {user.Name}", $"**{item.Goats.ToString("N0")}** Ziegen | **{Math.Round(chance)}%** Chance");
                    counter++;
                }
                embed.WithFooter("Glücksspiel kann süchtig machen! Sucht Hotline: 089 / 28 28 22");
                await Context.Channel.SendMessageAsync(null, false, embed.Build());
            }
        }
    }
}
