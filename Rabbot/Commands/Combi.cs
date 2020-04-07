using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PagedList;
using Rabbot.Database;
using Rabbot.Database.Rabbot;
using Rabbot.Preconditions;
using Rabbot.Services;
using Serilog;
using Serilog.Core;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Rabbot.Commands
{
    public class CombiCmd : ModuleBase<SocketCommandContext>
    {
        private static readonly ILogger _logger = Log.ForContext(Serilog.Core.Constants.SourceContextPropertyName, nameof(CombiCmd));
        private readonly DatabaseService _databaseService;

        public CombiCmd(IServiceProvider services)
        {
            _databaseService = services.GetRequiredService<DatabaseService>();
        }

        [Command("addCombi", RunMode = RunMode.Async)]
        [BotCommand]
        [Summary("Erstellt eine Combi Anfrage an den markierten User.")]
        [Cooldown(30)]
        public async Task AddCombi(SocketGuildUser user)
        {
            if (user.IsBot)
            {
                await ReplyAsync("Nö.");
                return;
            }
            if (user.Id == Context.User.Id)
            {
                await ReplyAsync("Hättest du wohl gerne.");
                return;
            }

            using (var db = _databaseService.Open<RabbotContext>())
            {
                if (db.Combis.Where(p => p.GuildId == Context.Guild.Id && p.UserId == Context.User.Id).Count() >= 5)
                {
                    await ReplyAsync($"{Context.User.Mention} du hast bereits 5 bestehende Combis.");
                    return;
                }
                if (db.Combis.Where(p => p.GuildId == Context.Guild.Id && p.UserId == user.Id).Count() >= 5)
                {
                    await ReplyAsync($"{user.Mention} hat bereits 5 bestehende Combis.");
                    return;
                }
                if (db.Combis.Any(p => p.GuildId == Context.Guild.Id && (p.UserId == Context.User.Id && p.CombiUserId == user.Id) || p.CombiUserId == Context.User.Id && p.UserId == user.Id))
                {
                    await ReplyAsync($"{Context.User.Mention} du hast bereits eine **bestehende** Combi oder Combi Anfrage mit {user.Mention}.");
                    return;
                }
                EmbedBuilder embed = new EmbedBuilder();
                embed.WithTitle("Combi Anfrage");
                embed.WithDescription($"Du hast Erfolgreich eine Combi Anfrage an {user.Mention} gestellt.");
                embed.WithFooter("Die Anfrage kann über die Reactions angenommen oder abgelehnt werden.");
                var msg = await ReplyAsync($"{user.Mention}", false, embed.Build());
                await db.Combis.AddAsync(new CombiEntity { GuildId = Context.Guild.Id, UserId = Context.User.Id, CombiUserId = user.Id, Accepted = false, MessageId = msg.Id, Date = DateTime.Now });
                await db.SaveChangesAsync();
                var emoteAccept = new Emoji("✅");
                var emoteDeny = new Emoji("⛔");
                await msg.AddReactionAsync(emoteAccept);
                await msg.AddReactionAsync(emoteDeny);
            }
        }

        [Command("combis", RunMode = RunMode.Async)]
        [BotCommand]
        [Summary("Listet deine alle Combis mit dem aktuellen Status auf.")]
        [Cooldown(30)]
        public async Task CombiList()
        {
            using (var db = _databaseService.Open<RabbotContext>())
            {
                var combis = db.Combis.Include(p => p.User).Include(p => p.CombiUser).Where(p => p.GuildId == Context.Guild.Id && (p.UserId == Context.User.Id || p.CombiUserId == Context.User.Id));

                EmbedBuilder embed = new EmbedBuilder();
                embed.WithTitle("Combis");
                int count = 1;
                foreach (var combi in combis)
                {
                    string status = "";
                    if (combi.Accepted == true)
                        status = "Angenommen";
                    else
                        status = "Ausstehend";

                    if (combi.CombiUserId == Context.User.Id)
                        embed.AddField($"[{count}] {combi.User.Name}", $"Status: {status}\nBesteht seit: {combi.Date.ToString("dd.MM.yyyy")}");
                    if (combi.UserId == Context.User.Id)
                        embed.AddField($"[{count}] {combi.CombiUser.Name}", $"Status: {status}\nBesteht seit: {combi.Date.ToString("dd.MM.yyyy")}");

                    count++;
                }
                await ReplyAsync(null, false, embed.Build());
            }
        }

        [Command("delCombi", RunMode = RunMode.Async)]
        [BotCommand]
        [Summary("Löscht die Combi mit der entsprechenden ID aus der Liste >combis")]
        [Cooldown(30)]
        public async Task DelCombi(int id)
        {
            using (var db = _databaseService.Open<RabbotContext>())
            {
                var combi = db.Combis.Include(p => p.User).Include(p => p.CombiUser).Where(p => p.GuildId == Context.Guild.Id && (p.UserId == Context.User.Id || p.CombiUserId == Context.User.Id)).Skip(id - 1)?.FirstOrDefault();

                if (combi == null)
                {
                    await ReplyAsync($"{Context.User.Mention} die Combi mit der ID {id} kann nicht gefunden werden.");
                    return;
                }

                db.Combis.Remove(combi);
                await db.SaveChangesAsync();

                if (combi.CombiUserId == Context.User.Id)
                    await ReplyAsync($"{Context.User.Mention} du hast erfolgreich die Combi mit **{combi.User.Name}** gelöscht.");
                if (combi.UserId == Context.User.Id)
                    await ReplyAsync($"{Context.User.Mention} du hast erfolgreich die Combi mit **{combi.CombiUser.Name}** gelöscht.");
            }
        }

        [Command("acceptCombi", RunMode = RunMode.Async)]
        [BotCommand]
        [Summary("Akzeptiert eine ausstehende Anfrage mit der entsprechenden ID aus der Liste >combis")]
        [Cooldown(30)]
        public async Task AcceptCombi(int id)
        {
            using (var db = _databaseService.Open<RabbotContext>())
            {
                var combi = db.Combis.Include(p => p.User).Include(p => p.CombiUser).Where(p => p.GuildId == Context.Guild.Id && (p.UserId == Context.User.Id || p.CombiUserId == Context.User.Id)).Skip(id - 1)?.FirstOrDefault();

                if (combi == null)
                {
                    await ReplyAsync($"{Context.User.Mention} die Combi mit der ID {id} kann nicht gefunden werden.");
                    return;
                }
                if (combi.Accepted != true)
                    combi.Accepted = true;
                else
                {
                    await ReplyAsync($"{Context.User.Mention} du hast die Combi bereits angenommen.");
                    return;
                }
                await db.SaveChangesAsync();

                if (combi.CombiUserId == Context.User.Id)
                    await ReplyAsync($"{Context.User.Mention} du hast erfolgreich die Combi mit **{combi.User.Name}** angenommen.");
                if (combi.UserId == Context.User.Id)
                    await ReplyAsync($"{Context.User.Mention} du hast erfolgreich die Combi mit **{combi.CombiUser.Name}** angenommen.");
            }
        }

        [Command("combiRanking", RunMode = RunMode.Async), Alias("combiranks")]
        [BotCommand]
        [Summary("Zeigt die Top User sortiert nach Combi EXP an.")]
        public async Task CombiRanking(int page = 1)
        {
            if (page < 1)
                return;
            using (var db = _databaseService.Open<RabbotContext>())
            {

                var ranking = db.Features.Where(p => p.GuildId == Context.Guild.Id && p.HasLeft == false).OrderByDescending(p => p.CombiExp).ToPagedList(page, 10);
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
                        var user = db.Users.FirstOrDefault(p => p.Id == top.UserId);
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
