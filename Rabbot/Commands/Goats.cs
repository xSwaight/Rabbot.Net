using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Rabbot.Database;
using Rabbot.Preconditions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rabbot.Commands
{
    public class Goats : ModuleBase<SocketCommandContext>
    {

        [Command("daily", RunMode = RunMode.Async)]
        [BotCommand]
        [Summary("Du kannst 1x täglich eine Belohnung bekommen.")]
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
            int chance = rnd.Next(1, 18);
            int jackpot = rnd.Next(1, 101);
            EmbedBuilder embed = new EmbedBuilder();
            using (swaightContext db = new swaightContext())
            {

                if (chance > 1)
                {
                    var dbUser = db.Userfeatures.Where(p => p.UserId == (long)Context.User.Id && p.ServerId == (long)Context.Guild.Id).FirstOrDefault();
                    var stall = Helper.GetStall(dbUser.Wins);
                    if (jackpot <= 3)
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

                    int goats = rnd.Next(20, 201);
                    int bonus = rnd.Next(1, 11);
                    if (Helper.IsFull(dbUser.Goats + goats, dbUser.Wins))
                    {
                        var rabbotUser = db.Userfeatures.Where(p => p.ServerId == (long)Context.Guild.Id && p.UserId == (long)Context.Client.CurrentUser.Id).FirstOrDefault() ?? db.AddAsync(new Userfeatures { ServerId = (long)Context.Guild.Id, UserId = (long)Context.Client.CurrentUser.Id, Goats = 0, Exp = 0 }).Result.Entity;
                        embed.Color = Color.Green;
                        embed.Description = $"Wow, du konntest heute unfassbare **{goats} Ziegen** einfangen!\nAllerdings ist **dein Stall ({stall.Name}) voll** und **{(dbUser.Goats + goats) - stall.Capacity} Ziegen** sind zu **Rabbot** geflüchtet..";
                        await Context.Channel.SendMessageAsync(null, false, embed.Build());
                        rabbotUser.Goats += (dbUser.Goats + goats) - stall.Capacity;
                        dbUser.Goats = stall.Capacity;
                    }
                    else
                    {
                        dbUser.Goats += goats;
                        embed.Color = Color.Green;
                        embed.Description = $"Wow, du konntest heute unfassbare **{goats} Ziegen** einfangen!";
                        await Context.Channel.SendMessageAsync(null, false, embed.Build());
                    }
                    if (bonus == 2)
                    {
                        var features = db.Userfeatures
                                        .Include(p => p.Inventory)
                                        .ThenInclude(p => p.Item)
                                        .FirstOrDefault(p => p.UserId == (long)Context.User.Id && p.ServerId == (long)Context.Guild.Id);

                        int rndChance = rnd.Next(1, 3);
                        int usage = rnd.Next(2, 11);
                        if (rndChance == 1)
                        {
                            var stab = features.Inventory.FirstOrDefault(p => p.ItemId == 1);
                            if (stab != null)
                                stab.Durability += usage;
                            else
                                await db.Inventory.AddAsync(new Inventory { FeatureId = features.Id, ItemId = 1, Durability = usage });
                        }
                        else
                        {
                            var zaun = features.Inventory.FirstOrDefault(p => p.ItemId == 2);
                            if (zaun != null)
                                zaun.Durability += usage;
                            else
                                await db.Inventory.AddAsync(new Inventory { FeatureId = features.Id, ItemId = 2, Durability = usage });
                        }
                        embed.Color = Color.LightOrange;
                        string item = rndChance == 1 ? "Hirtenstab" : "Stacheldrahtzaun";
                        embed.Description = $"**Bonus**: Du hast einen **{item}** mit **{usage} Benutzungen** gefunden.";
                        await Context.Channel.SendMessageAsync(null, false, embed.Build());
                    }
                    else if(bonus == 3)
                    {
                        int fights = rnd.Next(1, 6);
                        dbUser.Attacks -= fights;
                        embed.Color = Color.LightOrange;
                        string item = fights == 1 ? "Bonuskampf" : "Bonuskämpfe";
                        embed.Description = $"**Bonus**: Du hast heute **{fights} {item}** gewonnen!";
                        await Context.Channel.SendMessageAsync(null, false, embed.Build());
                    }
                    await db.SaveChangesAsync();
                }
                else
                {
                    embed.Color = Color.Red;
                    embed.Description = $"Gib dir nächstes mal **mehr Mühe**!";
                    await Context.Channel.SendMessageAsync(null, false, embed.Build());
                }
            }
        }

        [Command("trade")]
        [BotCommand]
        [Cooldown(30)]
        [Summary("Du kannst dem Markierten User die angegebene Anzahl an Ziegen schenken.")]
        public async Task Trade(IUser user, int amount)
        {
            if (amount > 0 && (!user.IsBot || user.Id == Context.Client.CurrentUser.Id))
            {
                using (swaightContext db = new swaightContext())
                {

                    var embed = new EmbedBuilder();
                    var senderUser = db.Userfeatures.Where(p => p.ServerId == (long)Context.Guild.Id && p.UserId == (long)Context.User.Id).FirstOrDefault() ?? db.AddAsync(new Userfeatures { ServerId = (long)Context.Guild.Id, UserId = (long)Context.Guild.Id, Goats = 0, Exp = 0 }).Result.Entity;
                    if (senderUser.Goats < amount)
                    {
                        embed.Color = Color.Red;
                        embed.Description = $"Dir fehlen **{amount - senderUser.Goats} Ziegen**..";
                        await Context.Channel.SendMessageAsync(null, false, embed.Build());
                        return;
                    }
                    if (senderUser.Trades >= 5)
                    {
                        embed.Color = Color.Red;
                        embed.Description = $"**Du kannst heute nicht mehr traden!**";
                        await Context.Channel.SendMessageAsync(null, false, embed.Build());
                        return;
                    }

                    var targetUser = db.Userfeatures.Where(p => p.ServerId == (long)Context.Guild.Id && p.UserId == (long)user.Id).FirstOrDefault() ?? db.AddAsync(new Userfeatures { ServerId = (long)Context.Guild.Id, UserId = (long)user.Id, Goats = 0, Exp = 0 }).Result.Entity;
                    if (targetUser == senderUser)
                    {
                        await Context.Channel.SendMessageAsync($"Nö.");
                        return;
                    }

                    if (targetUser.Locked == 1)
                    {
                        embed.Color = Color.Red;
                        embed.Description = $"**{user.Mention} hat gerade eine Trading Sperre!**";
                        await Context.Channel.SendMessageAsync(null, false, embed.Build());
                        return;
                    }

                    if (senderUser.Locked == 1)
                    {
                        embed.Color = Color.Red;
                        embed.Description = $"**{Context.User.Mention} du hast gerade eine Trading Sperre!**";
                        await Context.Channel.SendMessageAsync(null, false, embed.Build());
                        return;
                    }

                    var rabbotUser = db.Userfeatures.Where(p => p.ServerId == (long)Context.Guild.Id && p.UserId == (long)Context.Client.CurrentUser.Id).FirstOrDefault() ?? db.AddAsync(new Userfeatures { ServerId = (long)Context.Guild.Id, UserId = (long)Context.Client.CurrentUser.Id, Goats = 0, Exp = 0 }).Result.Entity;
                    senderUser.Goats -= amount;
                    int fees = amount / 4;
                    if (Helper.IsFull(targetUser.Goats + amount - fees, targetUser.Wins))
                    {
                        embed.Color = Color.Red;
                        embed.Description = $"**Leider ist der Stall von {user.Mention} schon zu voll!**";
                        await Context.Channel.SendMessageAsync(null, false, embed.Build());
                        return;
                    }
                    rabbotUser.Goats += fees;
                    targetUser.Goats += amount - fees;
                    senderUser.Trades++;
                    await db.SaveChangesAsync();

                    if (amount > 1)
                    {
                        embed.Color = Color.Green;
                        embed.Description = $"{Context.User.Username} hat {user.Mention} **{amount - fees} Ziegen** (-{fees} Schutzgeldziegens) geschenkt! Du kannst heute noch **{5 - senderUser.Trades} mal** traden.";
                        await Context.Channel.SendMessageAsync(null, false, embed.Build());
                    }
                    else
                    {
                        embed.Color = Color.Green;
                        embed.Description = $"{Context.User.Username} hat {user.Mention} **{amount} Ziege** geschenkt! Du kannst heute noch **{5 - senderUser.Trades} mal** traden.";
                        await Context.Channel.SendMessageAsync(null, false, embed.Build());
                    }
                }
            }
        }


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
        [Summary("Du kannst den markierten User angreifen und Ziegen gewinnen oder verlieren.")]
        public async Task Angriff(IUser target)
        {
            Random rnd = new Random();
            await Task.Delay(rnd.Next(1, 201));
            EmbedBuilder embed = new EmbedBuilder();
            if (Context.User.Id == target.Id || target.IsBot)
            {
                embed.Color = Color.Red;
                embed.Description = "Ne man. Lass mal. Muss nicht sein.";
                await Context.Channel.SendMessageAsync(null, false, embed.Build());
                return;
            }
            using (swaightContext db = new swaightContext())
            {

                var dbTarget = db.Userfeatures.Where(p => p.ServerId == (long)Context.Guild.Id && p.UserId == (long)target.Id).FirstOrDefault() ?? db.Userfeatures.AddAsync(new Userfeatures { ServerId = (long)Context.Guild.Id, UserId = (long)target.Id, Exp = 0, Goats = 0 }).Result.Entity;
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

                //if (dbUser.Goats < userStall.MaxOutput)
                //{
                //    embed.Color = Color.Red;
                //    embed.Description = $"{Context.User.Mention} du musst mindestens **{userStall.MaxOutput} Ziegen** haben um jemanden **angreifen** zu können!";
                //    await Context.Channel.SendMessageAsync(null, false, embed.Build());
                //    return;
                //}
                //if (dbTarget.Goats < targetStall.MaxOutput)
                //{
                //    embed.Color = Color.Red;
                //    embed.Description = $"{Context.User.Mention} dein Opfer muss mindestens **{targetStall.MaxOutput} Ziegen** haben um **angegriffen** werden zu können!";
                //    await Context.Channel.SendMessageAsync(null, false, embed.Build());
                //    return;
                //}

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
                embed.Description = $"{Context.User.Mention} du hast erfolgreich einen Angriff gegen {target.Mention} gestartet!\nDu kannst heute noch **{5 - dbUser.Attacks} mal** angreifen.";
                embed.Description += $"\n\n**Interaktionen:**";
                embed.Description += $"\n{Helper.Sword} = Hirtenstab       (Nur Angreifer)";
                embed.Description += $"\n{Helper.Shield} = Stacheldrahtzaun (Nur Angegriffener)";
                embed.WithFooter("Der Angriff dauert 3 Minuten!");

                var stallTarget = Helper.GetStall(dbTarget.Wins);
                var stallUser = Helper.GetStall(dbUser.Wins);
                var atk = stallUser.Attack;
                var def = stallTarget.Defense;

                var sum = atk + def;
                var winChance = ((double)atk / (double)sum) * 100;

                string chance = $"**{Math.Round(winChance)}% {Context.User.Mention} - {target.Mention} {100 - Math.Round(winChance)}%**";

                var msg = await Context.Channel.SendMessageAsync(chance, false, embed.Build());
                dbUser.Locked = 1;
                dbTarget.Locked = 1;
                await db.Attacks.AddAsync(new Attacks { ServerId = (long)Context.Guild.Id, UserId = (long)Context.User.Id, ChannelId = (long)Context.Channel.Id, MessageId = (long)msg.Id, TargetId = (long)target.Id, AttackEnds = DateTime.Now.AddMinutes(3) });
                await db.SaveChangesAsync();
                await msg.AddReactionAsync(Helper.Sword);
                await msg.AddReactionAsync(Helper.Shield);
            }
        }


        [Command("shop", RunMode = RunMode.Async)]
        [BotCommand]
        [Cooldown(10)]
        [Summary("Im Shop kannst du Items für Kämpfe kaufen.")]
        public async Task Shop()
        {
            var embed = new EmbedBuilder();
            embed.WithTitle("Willkommen im Shop!");
            embed.WithDescription("Gönn dir.");
            embed.WithColor(new Color(241, 242, 222));
            //embed.AddField($"{Config.bot.cmdPrefix}minuten [Anzahl]", $"Minuten im Musicrank\nPreis: 1 Ziege für 1 Minute");
            embed.AddField($"{Config.bot.cmdPrefix}hirtenstab", $"Hirtenstab (+ 20 ATK) 7 Benutzungen\nPreis: 75 Ziegen");
            embed.AddField($"{Config.bot.cmdPrefix}zaun", $"Stacheldrahtzaun (+ 30 DEF) 7 Benutzungen\nPreis: 75 Ziegen");
            //embed.AddField($"{Config.bot.cmdPrefix}namechange [Name]", $"7 Tage Namechange\nPreis: 100 Ziegen");
            await Context.Channel.SendMessageAsync(null, false, embed.Build());
        }

        [Command("hirtenstab", RunMode = RunMode.Async)]
        [BotCommand]
        public async Task Hirtenstab()
        {
            using (swaightContext db = new swaightContext())
            {

                EmbedBuilder embed = new EmbedBuilder();
                var features = db.Userfeatures
                    .Include(p => p.Inventory)
                    .ThenInclude(p => p.Item)
                    .FirstOrDefault(p => p.UserId == (long)Context.User.Id && p.ServerId == (long)Context.Guild.Id);

                if (features.Goats < 75)
                {
                    embed.Color = Color.Red;
                    embed.Description = $"{Context.User.Mention} du hast nicht ausreichend Ziegen!";
                    await Context.Channel.SendMessageAsync(null, false, embed.Build());
                    return;
                }

                var hirtenstab = features.Inventory.FirstOrDefault(p => p.ItemId == 1);



                if (hirtenstab != null)
                    hirtenstab.Durability += 7;
                else
                    hirtenstab = db.Inventory.AddAsync(new Inventory { FeatureId = features.Id, ItemId = 1, Durability = 7 }).Result.Entity;

                if (hirtenstab.Durability + 7 > 50)
                {
                    embed.Color = Color.Red;
                    embed.Description = $"{Context.User.Mention} du kannst maximal **50 Hirtenstäbe** tragen!";
                    await Context.Channel.SendMessageAsync(null, false, embed.Build());
                    return;
                }

                features.Goats -= 75;
                await db.SaveChangesAsync();
                embed.Color = Color.Green;
                embed.Description = $"{Context.User.Mention} du hast dir erfolgreich für **75 Ziegen** einen **Hirtenstab** gekauft.\nDu kannst ihn bei einem Angriff benutzen um **+20 ATK** mehr zu haben!";
                await Context.Channel.SendMessageAsync(null, false, embed.Build());
            }
        }

        [Command("zaun", RunMode = RunMode.Async)]
        [BotCommand]
        public async Task Zaun()
        {
            EmbedBuilder embed = new EmbedBuilder();
            using (swaightContext db = new swaightContext())
            {

                var features = db.Userfeatures
               .Include(p => p.Inventory)
               .ThenInclude(p => p.Item)
               .FirstOrDefault(p => p.UserId == (long)Context.User.Id && p.ServerId == (long)Context.Guild.Id);

                if (features.Goats < 75)
                {
                    embed.Color = Color.Red;
                    embed.Description = $"{Context.User.Mention} du hast nicht ausreichend Ziegen!";
                    await Context.Channel.SendMessageAsync(null, false, embed.Build());
                    return;
                }

                var zaun = features.Inventory.FirstOrDefault(p => p.ItemId == 2);


                if (zaun != null)
                    zaun.Durability += 7;
                else
                    zaun = db.Inventory.AddAsync(new Inventory { FeatureId = features.Id, ItemId = 2, Durability = 7 }).Result.Entity;

                if (zaun.Durability + 7 > 50)
                {
                    embed.Color = Color.Red;
                    embed.Description = $"{Context.User.Mention} du kannst maximal **50 Stacheldrahtzäune** tragen!";
                    await Context.Channel.SendMessageAsync(null, false, embed.Build());
                    return;
                }

                features.Goats -= 75;
                await db.SaveChangesAsync();
                embed.Color = Color.Green;
                embed.Description = $"{Context.User.Mention} du hast dir erfolgreich für **75 Ziegen** einen **Stacheldrahtzaun** gekauft.\nDu kannst ihn bei einem Angriff gegen dich benutzen um **+30 DEF** mehr zu haben!";
                await Context.Channel.SendMessageAsync(null, false, embed.Build());
            }
        }

        [Command("stall", RunMode = RunMode.Async)]
        [BotCommand]
        [Cooldown(10)]
        [Summary("Du kannst entweder deinen eigenen Stall, oder den Stall des markierten Users sehen.")]
        public async Task Stall(SocketUser user = null)
        {
            if (user == null)
                user = Context.User;
            if (user.IsBot)
                return;

            using (swaightContext db = new swaightContext())
            {
                var embed = new EmbedBuilder();
                var dbUser = db.Userfeatures.Where(p => p.UserId == (long)user.Id && p.ServerId == (long)Context.Guild.Id).FirstOrDefault() ?? db.Userfeatures.AddAsync(new Userfeatures { ServerId = (long)Context.Guild.Id, UserId = (long)user.Id, Exp = 0, Goats = 0 }).Result.Entity;
                var stall = Helper.GetStall(dbUser.Wins);
                embed.WithTitle($"Stall von {user.Username}");
                embed.WithDescription(stall.Name);
                embed.WithColor(new Color(241, 242, 222));
                embed.AddField($"Level", $"{stall.Level} / 26");
                var percent = ((double)dbUser.Goats / (double)stall.Capacity) * 100;
                embed.AddField($"Kapazität", $"{dbUser.Goats.ToString("N0", new System.Globalization.CultureInfo("de-DE"))} / {stall.Capacity.ToString("N0", new System.Globalization.CultureInfo("de-DE"))} Ziegen ({Math.Round(percent, 0)}%)");
                embed.AddField($"Stats", $"ATK: **{stall.Attack.ToString("N0", new System.Globalization.CultureInfo("de-DE"))}0** | DEF: **{stall.Defense.ToString("N0", new System.Globalization.CultureInfo("de-DE"))}0**");
                await Context.Channel.SendMessageAsync(null, false, embed.Build());
                await db.SaveChangesAsync();
            }
        }

        [Command("stats", RunMode = RunMode.Async)]
        [BotCommand]
        [Cooldown(10)]
        [Summary("Du kannst entweder deine eigenen Stats, oder die Stats des markierten Users sehen.")]
        public async Task Stats(SocketUser user = null)
        {
            if (user == null)
                user = Context.User;
            if (user.IsBot)
                return;

            var embed = new EmbedBuilder();
            using (swaightContext db = new swaightContext())
            {
                var dbUser = db.Userfeatures.Where(p => p.UserId == (long)user.Id && p.ServerId == (long)Context.Guild.Id).FirstOrDefault() ?? db.Userfeatures.AddAsync(new Userfeatures { ServerId = (long)Context.Guild.Id, UserId = (long)user.Id, Exp = 0, Goats = 0 }).Result.Entity;
                var inventory = db.Inventory.Join(db.Items, id => id.ItemId, item => item.Id, (Inventory, Item) => new { Inventory, Item }).Where(p => p.Inventory.FeatureId == dbUser.Id);
                var stall = Helper.GetStall(dbUser.Wins);
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
                var fights = 5 - dbUser.Attacks;
                string kaempfe = fights == 1 ? "Kampf" : "Kämpfe";
                var myUser = user as SocketGuildUser;
                embed.WithTitle($"Statistiken von {myUser.Nickname ?? myUser.Username}");
                embed.WithColor(new Color(241, 242, 222));
                embed.AddField($"Battle", $"**{(dbUser.Loses + dbUser.Wins).ToString("N0", new System.Globalization.CultureInfo("de-DE"))}** Kämpfe | **{dbUser.Wins.ToString("N0", new System.Globalization.CultureInfo("de-DE"))}** Siege | **{dbUser.Loses.ToString("N0", new System.Globalization.CultureInfo("de-DE"))}** Niederlagen");
                var percent = ((double)dbUser.Goats / (double)stall.Capacity) * 100;
                embed.AddField($"Aktueller Stall", $"{stall.Name} | **{dbUser.Goats.ToString("N0", new System.Globalization.CultureInfo("de-DE"))}** / **{stall.Capacity.ToString("N0", new System.Globalization.CultureInfo("de-DE"))}** Ziegen (**{Math.Round(percent, 0)}%**)");
                embed.AddField($"Heute Kämpfe", $"Noch **{fights}** {kaempfe} übrig");

                Emoji emote = null;
                if(dbUser.Lastdaily.Value.ToShortDateString() == DateTime.Now.ToShortDateString())
                {
                    emote = Helper.Yes;
                }
                else
                {
                    emote = Helper.No;
                }

                embed.AddField($"Daily", $"Heute abgeholt: {emote}");
                embed.AddField($"Stats", $"ATK: **{stall.Attack.ToString("N0", new System.Globalization.CultureInfo("de-DE"))}0** | DEF: **{stall.Defense.ToString("N0", new System.Globalization.CultureInfo("de-DE"))}0**");
                if (inventory.Count() != 0)
                {
                    string items = "";
                    foreach (var item in inventory)
                    {
                        items += $"**{item.Item.Name}** - übrige Benutzungen: **{item.Inventory.Durability}**\n";
                    }
                    embed.AddField($"Inventar", items);
                }

                await Context.Channel.SendMessageAsync(null, false, embed.Build());
                await db.SaveChangesAsync();
            }
        }

        [Command("stalls", RunMode = RunMode.Async)]
        [BotCommand]
        [Cooldown(10)]
        [Summary("Eine Liste mit allen Ställen")]
        public async Task StallListe()
        {
            var embed = new EmbedBuilder();
            var embed2 = new EmbedBuilder();

            foreach (var stall in Helper.stall)
            {
                try
                {
                    embed.AddField($"Level {stall.Value.Level} | {stall.Value.Name}", $"Benötigte Siege: **{stall.Key.ToString("N0", new System.Globalization.CultureInfo("de-DE"))}** | Kapazität: **{stall.Value.Capacity.ToString("N0", new System.Globalization.CultureInfo("de-DE"))}** Ziegen");
                }
                catch
                {
                    embed2.AddField($"Level {stall.Value.Level} | {stall.Value.Name}", $"Benötigte Siege: **{stall.Key.ToString("N0", new System.Globalization.CultureInfo("de-DE"))}** | Kapazität: **{stall.Value.Capacity.ToString("N0", new System.Globalization.CultureInfo("de-DE"))}** Ziegen");
                }
            }
            await Context.Channel.SendMessageAsync(null, false, embed.Build());
            await Context.Channel.SendMessageAsync(null, false, embed2.Build());
        }

        [Command("wins", RunMode = RunMode.Async)]
        [BotCommand]
        [Cooldown(10)]
        [Summary("Die Bestenliste sortiert nach Siegen")]
        public async Task Wins()
        {
            using (swaightContext db = new swaightContext())
            {

                var top25 = db.Userfeatures.Include(p => p.User).Where(p => p.Wins != 0 && p.ServerId == (long)Context.Guild.Id).OrderByDescending(p => p.Wins).Take(25);
                EmbedBuilder embed = new EmbedBuilder();
                int counter = 1;
                foreach (var top in top25)
                {
                    if (!Context.Guild.Users.Where(p => p.Id == (ulong)top.User.Id).Any())
                        continue;

                    var stall = Helper.GetStall(top.Wins);
                    embed.AddField($"{counter}. {top.User.Name}", $"**{top.Wins.ToString("N0", new System.Globalization.CultureInfo("de-DE"))}** Siege | **{top.Loses}** Niederlagen | Stall Level: **{stall.Level}** | Ziegen: **{top.Goats}**");
                    counter++;
                }
                await Context.Channel.SendMessageAsync(null, false, embed.Build());
            }
        }

        [Command("pot", RunMode = RunMode.Async)]
        [BotCommand]
        [Cooldown(10)]
        [Summary("Du kannst eine bestimmte Anzahl an Ziegen in den Pot werfen, der täglich um 0 Uhr ausgelost wird.")]
        public async Task Pot(int amount)
        {
            try
            {
                if (amount < 1)
                    return;
                using (swaightContext db = new swaightContext())
                {

                    EmbedBuilder embed = new EmbedBuilder();
                    if (amount < 10)
                    {
                        embed.Color = Color.Red;
                        embed.Description = $"{Context.User.Mention} du musst mindestens **10 Ziegen** in den Pot stecken!";
                        await Context.Channel.SendMessageAsync(null, false, embed.Build());
                        return;
                    }
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
                    if (amount > stall.MaxPot)
                    {
                        embed.Color = Color.Red;
                        embed.Description = $"{Context.User.Mention} du kannst auf deinem **Stall Level** maximal **{stall.MaxPot.ToString("N0", new System.Globalization.CultureInfo("de-DE"))} Ziegen** in den Pot stecken!";
                        await Context.Channel.SendMessageAsync(null, false, embed.Build());
                        return;
                    }

                    var myPot = db.Pot.Where(p => p.UserId == (long)Context.User.Id && p.ServerId == (long)Context.Guild.Id).FirstOrDefault() ?? db.Pot.AddAsync(new Pot { UserId = (long)Context.User.Id, ServerId = (long)Context.Guild.Id, Goats = 0 }).Result.Entity;
                    if (amount + myPot.Goats > stall.MaxPot)
                    {
                        embed.Color = Color.Red;
                        embed.Description = $"{Context.User.Mention} du kannst auf deinem **Stall Level** maximal **{stall.MaxPot.ToString("N0", new System.Globalization.CultureInfo("de-DE"))} Ziegen** in den Pot stecken!";
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
        [Summary("Zeigt die Chancen im aktuellen Pot an.")]
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
                    embed.Title = $"{sum.ToString("N0", new System.Globalization.CultureInfo("de-DE"))} Ziegen im Pot!";
                    embed.AddField($"{counter}. {user.Name}", $"**{item.Goats.ToString("N0", new System.Globalization.CultureInfo("de-DE"))}** Ziegen | **{Math.Round(chance)}%** Chance");
                    counter++;
                }
                embed.WithFooter("Glücksspiel kann süchtig machen! Sucht Hotline: 089 / 28 28 22");
                await Context.Channel.SendMessageAsync(null, false, embed.Build());
            }
        }
    }
}
