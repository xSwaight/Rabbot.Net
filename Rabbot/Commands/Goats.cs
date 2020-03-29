using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using PagedList;
using Rabbot.Database;
using Rabbot.Preconditions;
using Rabbot.Services;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rabbot.Commands
{
    public class Goats : ModuleBase<SocketCommandContext>
    {
        private static readonly ILogger _logger = Log.ForContext(Serilog.Core.Constants.SourceContextPropertyName, nameof(Goats));
        private readonly StreakService _streakService;
        private readonly LevelService _levelService;

        public Goats(StreakService streakService, LevelService levelService)
        {
            _streakService = streakService;
            _levelService = levelService;
        }

        [Command("daily", RunMode = RunMode.Async)]
        [BotCommand]
        [Summary("Du kannst 1x täglich eine Belohnung bekommen.")]
        public async Task Daily()
        {
            using (rabbotContext db = new rabbotContext())
            {
                var user = db.User.FirstOrDefault(p => p.Id == Context.User.Id) ?? db.User.AddAsync(new User { Id = Context.User.Id, Name = $"{Context.User.Username}#{Context.User.Discriminator}" }).Result.Entity;
                var dbUser = db.Userfeatures.FirstOrDefault(p => p.UserId == Context.User.Id && p.ServerId == Context.Guild.Id);
                if (dbUser == null)
                    return;
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
            using (rabbotContext db = new rabbotContext())
            {

                if (chance > 1)
                {
                    var dbUser = db.Userfeatures.FirstOrDefault(p => p.UserId == Context.User.Id && p.ServerId == Context.Guild.Id);
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
                        var rabbotUser = db.Userfeatures.FirstOrDefault(p => p.ServerId == Context.Guild.Id && p.UserId == Context.Client.CurrentUser.Id) ?? db.AddAsync(new Userfeatures { ServerId = Context.Guild.Id, UserId = Context.Client.CurrentUser.Id, Goats = 0, Exp = 0 }).Result.Entity;
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
                                        .FirstOrDefault(p => p.UserId == Context.User.Id && p.ServerId == Context.Guild.Id);

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
                    else if (bonus == 3)
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
                using (rabbotContext db = new rabbotContext())
                {

                    var embed = new EmbedBuilder();
                    var senderUser = db.Userfeatures.FirstOrDefault(p => p.ServerId == Context.Guild.Id && p.UserId == Context.User.Id);
                    if (senderUser == null)
                        return;
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

                    var targetUser = db.Userfeatures.FirstOrDefault(p => p.ServerId == Context.Guild.Id && p.UserId == user.Id) ?? db.AddAsync(new Userfeatures { ServerId = Context.Guild.Id, UserId = user.Id, Goats = 0, Exp = 0 }).Result.Entity;
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

                    var rabbotUser = db.Userfeatures.FirstOrDefault(p => p.ServerId == Context.Guild.Id && p.UserId == Context.Client.CurrentUser.Id) ?? db.AddAsync(new Userfeatures { ServerId = Context.Guild.Id, UserId = Context.Client.CurrentUser.Id, Goats = 0, Exp = 0 }).Result.Entity;
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

        [Command("angriff", RunMode = RunMode.Async)]
        [Alias("attack", "atk")]
        [BotCommand]
        [Cooldown(1)]
        [Summary("Du kannst den markierten User angreifen und Ziegen gewinnen oder verlieren.")]
        public async Task Angriff(IUser target)
        {
            if (Helper.AttackActive)
                return;

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
            using (rabbotContext db = new rabbotContext())
            {

                var dbTarget = db.Userfeatures.FirstOrDefault(p => p.ServerId == Context.Guild.Id && p.UserId == target.Id) ?? db.Userfeatures.AddAsync(new Userfeatures { ServerId = Context.Guild.Id, UserId = target.Id, Exp = 0, Goats = 0 }).Result.Entity;
                var dbUser = db.Userfeatures.FirstOrDefault(p => p.ServerId == Context.Guild.Id && p.UserId == Context.User.Id);
                if (dbUser == null)
                    return;
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
                Helper.AttackActive = true;
                dbUser.Attacks++;
                embed.Color = new Color(242, 255, 0);
                embed.Description = $"{Context.User.Mention} du hast erfolgreich einen Angriff gegen {target.Mention} gestartet!\nDu kannst heute noch **{5 - dbUser.Attacks} mal** angreifen.";
                embed.Description += $"\n\n**Interaktionen:**";
                embed.Description += $"\n{Constants.Sword} = Hirtenstab       (Nur Angreifer)";
                embed.Description += $"\n{Constants.Shield} = Stacheldrahtzaun (Nur Angegriffener)";
                embed.WithFooter("Der Angriff dauert 3 Minuten!");

                var stallTarget = Helper.GetStall(dbTarget.Wins);
                var stallUser = Helper.GetStall(dbUser.Wins);
                var atk = stallUser.Attack;
                var def = stallTarget.Defense;

                var sum = atk + def;
                var winChance = ((double)atk / (double)sum) * 100;

                string chance = $"**{Math.Round(winChance)}% {Context.User.Mention} - {target.Mention} {100 - Math.Round(winChance)}%**";

                dbUser.Locked = 1;
                dbTarget.Locked = 1;
                var msg = await Context.Channel.SendMessageAsync(chance, false, embed.Build());
                await db.Attacks.AddAsync(new Attacks { ServerId = Context.Guild.Id, UserId = Context.User.Id, ChannelId = Context.Channel.Id, MessageId = msg.Id, TargetId = target.Id, AttackEnds = DateTime.Now.AddMinutes(3) });
                await msg.AddReactionAsync(Constants.Sword);
                await msg.AddReactionAsync(Constants.Shield);
                await db.SaveChangesAsync();
                Helper.AttackActive = false;
            }
        }


        [Command("shop", RunMode = RunMode.Async)]
        [BotCommand]
        [Cooldown(10)]
        [Summary("Im Shop kannst du Items für Kämpfe und EXP Bonus kaufen.")]
        public async Task Shop()
        {
            var embed = new EmbedBuilder();
            embed.WithTitle("Willkommen im Shop!");
            embed.WithDescription("Gönn dir.");
            embed.WithColor(new Color(241, 242, 222));
            embed.AddField($"{Config.Bot.CmdPrefix}hirtenstab", $"Hirtenstab (+ 20 ATK) | 7 Benutzungen\n**Preis: 75 Ziegen**");
            embed.AddField($"{Config.Bot.CmdPrefix}zaun", $"Stacheldrahtzaun (+ 30 DEF) | 7 Benutzungen\n**Preis: 75 Ziegen**");
            embed.AddField($"{Config.Bot.CmdPrefix}expboost", $"EXP +50% für 24 Stunden\n**Preis: 250 Ziegen**");
            embed.AddField($"{Config.Bot.CmdPrefix}namechange [Name]", $"Ändert den Namen\n**Preis: 100 Ziegen**");
            await Context.Channel.SendMessageAsync(null, false, embed.Build());
        }

        [Command("hirtenstab", RunMode = RunMode.Async)]
        [BotCommand]
        public async Task Hirtenstab()
        {
            using (rabbotContext db = new rabbotContext())
            {

                EmbedBuilder embed = new EmbedBuilder();
                var features = db.Userfeatures
                    .Include(p => p.Inventory)
                    .ThenInclude(p => p.Item)
                    .FirstOrDefault(p => p.UserId == Context.User.Id && p.ServerId == Context.Guild.Id);

                if (features.Locked == 1)
                {
                    embed.Color = Color.Red;
                    embed.Description = $"{Context.User.Mention} du hast gerade eine **Trading Sperre**!";
                    await Context.Channel.SendMessageAsync(null, false, embed.Build());
                    return;
                }

                if (features.Goats < 75)
                {
                    embed.Color = Color.Red;
                    embed.Description = $"{Context.User.Mention} du hast nicht **ausreichend Ziegen**!";
                    await Context.Channel.SendMessageAsync(null, false, embed.Build());
                    return;
                }

                var hirtenstab = features.Inventory.FirstOrDefault(p => p.ItemId == 1);



                if (hirtenstab != null)
                    hirtenstab.Durability += 7;
                else
                    hirtenstab = db.Inventory.AddAsync(new Inventory { FeatureId = features.Id, ItemId = 1, Durability = 7 }).Result.Entity;

                if (hirtenstab.Durability > 50)
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
            using (rabbotContext db = new rabbotContext())
            {

                var features = db.Userfeatures
               .Include(p => p.Inventory)
               .ThenInclude(p => p.Item)
               .FirstOrDefault(p => p.UserId == Context.User.Id && p.ServerId == Context.Guild.Id);

                if (features.Locked == 1)
                {
                    embed.Color = Color.Red;
                    embed.Description = $"{Context.User.Mention} du hast gerade eine **Trading Sperre**!";
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

                var zaun = features.Inventory.FirstOrDefault(p => p.ItemId == 2);


                if (zaun != null)
                    zaun.Durability += 7;
                else
                    zaun = db.Inventory.AddAsync(new Inventory { FeatureId = features.Id, ItemId = 2, Durability = 7 }).Result.Entity;

                if (zaun.Durability > 50)
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

        [Command("expboost", RunMode = RunMode.Async)]
        [BotCommand]
        public async Task Expboost()
        {
            EmbedBuilder embed = new EmbedBuilder();
            using (rabbotContext db = new rabbotContext())
            {

                var features = db.Userfeatures
               .Include(p => p.Inventory)
               .ThenInclude(p => p.Item)
               .FirstOrDefault(p => p.UserId == Context.User.Id && p.ServerId == Context.Guild.Id);

                if (features.Locked == 1)
                {
                    embed.Color = Color.Red;
                    embed.Description = $"{Context.User.Mention} du hast gerade eine **Trading Sperre**!";
                    await Context.Channel.SendMessageAsync(null, false, embed.Build());
                    return;
                }

                if (features.Goats < 250)
                {
                    embed.Color = Color.Red;
                    embed.Description = $"{Context.User.Mention} du hast nicht ausreichend Ziegen!";
                    await Context.Channel.SendMessageAsync(null, false, embed.Build());
                    return;
                }

                var expBoost = features.Inventory.FirstOrDefault(p => p.ItemId == 3);


                if (expBoost != null)
                    if (expBoost.ExpirationDate.Value < DateTime.Now.AddDays(6))
                        expBoost.ExpirationDate = expBoost.ExpirationDate.Value.AddDays(1);
                    else
                    {
                        await ReplyAsync($"{Context.User.Mention} du kannst maximal **7 Tage** EXP Boost auf Vorrat kaufen!");
                        return;
                    }
                else
                {
                    expBoost = db.Inventory.AddAsync(new Inventory { FeatureId = features.Id, ItemId = 3, ExpirationDate = DateTime.Now.AddDays(1) }).Result.Entity;
                }


                features.Goats -= 250;
                await db.SaveChangesAsync();
                embed.Color = Color.Green;
                embed.Description = $"{Context.User.Mention} du hast dir erfolgreich für **250 Ziegen** einen **EXP +50% Booster** gekauft.\nDu bekommst bei jeder Nachricht **+50%** mehr EXP für **24 Stunden**!";
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

            using (rabbotContext db = new rabbotContext())
            {
                var embed = new EmbedBuilder();
                var dbUser = db.Userfeatures.FirstOrDefault(p => p.UserId == user.Id && p.ServerId == Context.Guild.Id);
                if (dbUser == null)
                    return;
                var stall = Helper.GetStall(dbUser.Wins);
                embed.WithTitle($"Stall von {user.Username}");
                embed.WithDescription(stall.Name);
                embed.WithColor(new Color(241, 242, 222));
                embed.AddField($"Level", $"{stall.Level} / 26");
                var percent = ((double)dbUser.Goats / (double)stall.Capacity) * 100;
                embed.AddField($"Kapazität", $"{dbUser.Goats.ToFormattedString()} / {stall.Capacity.ToFormattedString()} Ziegen ({Math.Round(percent, 0)}%)");
                embed.AddField($"Stats", $"ATK: **{stall.Attack.ToFormattedString()}0** | DEF: **{stall.Defense.ToFormattedString()}0**");
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
            using (rabbotContext db = new rabbotContext())
            {
                var dbUser = db.Userfeatures.FirstOrDefault(p => p.UserId == user.Id && p.ServerId == Context.Guild.Id);
                if (dbUser == null)
                    return;
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
                embed.AddField($"Battle", $"**{(dbUser.Loses + dbUser.Wins).ToFormattedString()}** Kämpfe | **{dbUser.Wins.ToFormattedString()}** Siege | **{dbUser.Loses.ToFormattedString()}** Niederlagen");
                var percent = ((double)dbUser.Goats / (double)stall.Capacity) * 100;
                embed.AddField($"Aktueller Stall", $"{stall.Name} | **{dbUser.Goats.ToFormattedString()}** / **{stall.Capacity.ToFormattedString()}** Ziegen (**{Math.Round(percent, 0)}%**)");
                embed.AddField($"Heutige Kämpfe", $"Noch **{fights}** {kaempfe} übrig");

                var combiLevel = Helper.GetCombiLevel(dbUser.CombiExp);
                var neededExp1 = Helper.GetCombiEXP((int)combiLevel);
                var neededExp2 = Helper.GetCombiEXP((int)combiLevel + 1);
                var currentExp = dbUser.CombiExp - Helper.GetCombiEXP((int)combiLevel);
                embed.AddField($"Combi", $" **Level: {combiLevel} | {currentExp}/{neededExp2} EXP**");

                Emoji emote = null;
                if (dbUser.Lastdaily != null)
                {
                    if (dbUser.Lastdaily.Value.ToShortDateString() == DateTime.Now.ToShortDateString())
                    {
                        emote = Constants.Yes;
                    }
                    else
                    {
                        emote = Constants.No;
                    }
                }
                else
                {
                    emote = Constants.No;
                }

                embed.AddField($"Daily", $"Heute abgeholt: {emote}");
                embed.AddField($"Streak", $"{Constants.Fire} **{_streakService.GetStreakLevel(dbUser).ToFormattedString()}**");
                embed.AddField($"Wortcounter", $"Heute: **{_streakService.GetWordsToday(dbUser).ToFormattedString()} {(_streakService.GetWordsToday(dbUser) == 1 ? "Wort" : "Wörter")}**\nTotal: **{_streakService.GetWordsTotal(dbUser).ToFormattedString()} {(_streakService.GetWordsTotal(dbUser) == 1 ? "Wort" : "Wörter")}**");
                var timespan = DateTime.Now - myUser.JoinedAt.Value.DateTime;
                embed.AddField($"Server beigetreten", $"Vor **{Math.Floor(timespan.TotalDays)} Tagen** ({myUser.JoinedAt.Value.DateTime.ToFormattedString()})");
                embed.AddField($"Stats", $"ATK: **{stall.Attack.ToFormattedString()}0** | DEF: **{stall.Defense.ToFormattedString()}0**");
                embed.AddField($"Slot Machine", $"Spins Gesamt: **{dbUser.Spins.ToFormattedString()}** | Gewinn Gesamt: **{dbUser.Gewinn.ToFormattedString()}**");
                embed.AddField($"EXP Bonus", $"{_levelService.GetBonusEXP(db, myUser).bonusInfo}");
                if (inventory.Count() != 0)
                {
                    string items = "";
                    foreach (var item in inventory)
                    {
                        if (item.Inventory.Durability > 0)
                            items += $"**{item.Item.Name}** - übrige Benutzungen: **{item.Inventory.Durability}**\n";
                        else
                            items += $"**{item.Item.Name}** - Haltbar bis: **{item.Inventory.ExpirationDate.Value.ToFormattedString()}**\n";

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
                    embed.AddField($"Level {stall.Value.Level} | {stall.Value.Name}", $"Benötigte Siege: **{stall.Key.ToFormattedString()}** | Kapazität: **{stall.Value.Capacity.ToFormattedString()}** Ziegen");
                }
                catch
                {
                    embed2.AddField($"Level {stall.Value.Level} | {stall.Value.Name}", $"Benötigte Siege: **{stall.Key.ToFormattedString()}** | Kapazität: **{stall.Value.Capacity.ToFormattedString()}** Ziegen");
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
            using (rabbotContext db = new rabbotContext())
            {

                var top25 = db.Userfeatures.Include(p => p.User).Where(p => p.Wins != 0 && p.ServerId == Context.Guild.Id && p.HasLeft == false).OrderByDescending(p => p.Wins).Take(25);
                EmbedBuilder embed = new EmbedBuilder();
                int counter = 1;
                foreach (var top in top25)
                {
                    if (!Context.Guild.Users.Where(p => p.Id == (ulong)top.User.Id).Any())
                        continue;

                    var stall = Helper.GetStall(top.Wins);
                    embed.AddField($"{counter}. {top.User.Name}", $"**{top.Wins.ToFormattedString()}** Siege | **{top.Loses}** Niederlagen | Stall Level: **{stall.Level}** | Ziegen: **{top.Goats}**");
                    counter++;
                }
                await Context.Channel.SendMessageAsync(null, false, embed.Build());
            }
        }

        [Command("namechange", RunMode = RunMode.Async)]
        [BotCommand]
        [Cooldown(10)]
        [Summary("Du kannst dir für 100 Ziegen einen neuen Namen kaufen.")]
        [RequireBotPermission(GuildPermission.ManageNicknames)]
        public async Task Namechange([Remainder]string Name)
        {
            using (rabbotContext db = new rabbotContext())
            {
                var dbUser = db.Userfeatures.FirstOrDefault(p => p.ServerId == Context.Guild.Id && p.UserId == Context.User.Id);

                if (dbUser.Locked == 1)
                {
                    EmbedBuilder embed = new EmbedBuilder();
                    embed.Color = Color.Red;
                    embed.Description = $"{Context.User.Mention} du hast gerade eine **Trading Sperre**!";
                    await Context.Channel.SendMessageAsync(null, false, embed.Build());
                    return;
                }

                if (dbUser.Goats < 100)
                {
                    await ReplyAsync("Du hast **leider** nicht ausreichend Ziegen.");
                    return;
                }

                if (Name.Length > 32)
                {
                    await ReplyAsync("Der Name ist zu lang du **Kek**.");
                    return;
                }

                if (Name.Contains("[GM]") || Name.Contains("[CM]") || Name.Contains("[PM]"))
                {
                    await ReplyAsync("**Nö.**");
                    return;
                }

                if (Context.User is SocketGuildUser user)
                {
                    dbUser.Goats -= 100;
                    await user.ModifyAsync(p => p.Nickname = Name);
                    await ReplyAsync($"Dein Nickname wurde erfolgreich zu **{Name}** geändert!");
                }
                await db.SaveChangesAsync();
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
                using (rabbotContext db = new rabbotContext())
                {

                    EmbedBuilder embed = new EmbedBuilder();
                    if (amount < 10)
                    {
                        embed.Color = Color.Red;
                        embed.Description = $"{Context.User.Mention} du musst mindestens **10 Ziegen** in den Pot stecken!";
                        await Context.Channel.SendMessageAsync(null, false, embed.Build());
                        return;
                    }
                    var myUser = db.Userfeatures.FirstOrDefault(p => p.UserId == Context.User.Id && p.ServerId == Context.Guild.Id);
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
                        embed.Description = $"{Context.User.Mention} du kannst auf deinem **Stall Level** maximal **{stall.MaxPot.ToFormattedString()} Ziegen** in den Pot stecken!";
                        await Context.Channel.SendMessageAsync(null, false, embed.Build());
                        return;
                    }

                    var myPot = db.Pot.FirstOrDefault(p => p.UserId == Context.User.Id && p.ServerId == Context.Guild.Id) ?? db.Pot.AddAsync(new Pot { UserId = Context.User.Id, ServerId = Context.Guild.Id, Goats = 0 }).Result.Entity;
                    if (amount + myPot.Goats > stall.MaxPot)
                    {
                        embed.Color = Color.Red;
                        embed.Description = $"{Context.User.Mention} du kannst auf deinem **Stall Level** maximal **{stall.MaxPot.ToFormattedString()} Ziegen** in den Pot stecken!";
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
                _logger.Error(e, $"Error in command {nameof(Pot)}");
            }

        }

        [Command("chance", RunMode = RunMode.Async)]
        [BotCommand]
        [Cooldown(10)]
        [Summary("Zeigt die Chancen im aktuellen Pot an.")]
        public async Task Chance()
        {
            using (rabbotContext db = new rabbotContext())
            {

                var sum = db.Pot.Where(p => p.ServerId == Context.Guild.Id).OrderByDescending(p => p.Goats).Sum(p => p.Goats);
                var pot = db.Pot.Where(p => p.ServerId == Context.Guild.Id).OrderByDescending(p => p.Goats).Take(25);
                EmbedBuilder embed = new EmbedBuilder();
                int counter = 1;
                foreach (var item in pot)
                {
                    var chance = (double)item.Goats / (double)sum * 100;
                    var user = db.User.FirstOrDefault(p => p.Id == item.UserId);
                    embed.Title = $"{sum.ToFormattedString()} Ziegen im Pot!";
                    embed.AddField($"{counter}. {user.Name}", $"**{item.Goats.ToFormattedString()}** Ziegen | **{Math.Round(chance)}%** Chance");
                    counter++;
                }
                embed.WithFooter("Glücksspiel kann süchtig machen! Sucht Hotline: 089 / 28 28 22");
                await Context.Channel.SendMessageAsync(null, false, embed.Build());
            }
        }


        [Command("spin", RunMode = RunMode.Async)]
        [BotCommand]
        [Cooldown(20)]
        [Summary("Spin das Rad für 20 Ziegen und gewinne mit Glück bis zu 500 Ziegen!")]
        public async Task Spin(int einsatz = 0)
        {
            if (einsatz == 0)
            {
                await ReplyAsync("Du musst einen Einsatz angeben und mindestens **5 Ziegen** bis **200 Ziegen** setzen.");
                return;
            }

            if (einsatz < 5 || einsatz > 200)
            {
                await ReplyAsync("Du musst mindestens **5 Ziegen** setzen und darfst maximal **200 Ziegen** setzen.");
                return;
            }
            var user = Context.User as SocketGuildUser;
            await Helper.UpdateSpin(Context.Channel, user, Context.Message, Context.Client, einsatz);
        }

        [Command("combiRanking", RunMode = RunMode.Async), Alias("combiranks")]
        [BotCommand]
        [Summary("Zeigt die Top User sortiert nach Combi EXP an.")]
        public async Task CombiRanking(int page = 1)
        {
            if (page < 1)
                return;
            using (rabbotContext db = new rabbotContext())
            {

                var ranking = db.Userfeatures.Where(p => p.ServerId == Context.Guild.Id && p.HasLeft == false).OrderByDescending(p => p.CombiExp).ToPagedList(page, 10);
                if (page > ranking.PageCount)
                    return;
                EmbedBuilder embed = new EmbedBuilder();
                embed.Description = $"Combi Ranking Seite {ranking.PageNumber}/{ranking.PageCount}";
                embed.WithColor(new Color(239, 220, 7));
                int i = ranking.PageSize * ranking.PageNumber - (ranking.PageSize - 1);
                foreach (var top in ranking)
                {
                    try
                    {
                        uint level = Helper.GetCombiLevel(top.CombiExp);
                        var user = db.User.FirstOrDefault(p => p.Id == top.UserId);
                        int exp = (int)top.CombiExp;
                        embed.AddField($"{i}. {user.Name}", $"Level {level} ({exp.ToFormattedString()} EXP)");
                        i++;

                    }
                    catch (Exception e)
                    {
                        _logger.Error(e, $"Error in command {nameof(CombiRanking)}");
                    }
                }
                await Context.Channel.SendMessageAsync(null, false, embed.Build());
            }
        }
    }
}
