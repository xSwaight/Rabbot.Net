using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Rabbot.Database;
using Rabbot.Preconditions;
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

        [Command("addCombi", RunMode = RunMode.Async)]
        [BotCommand]
        [Summary("Erstellt eine Combi Anfrage an den markierten User.")]
        [Cooldown(30)]
        public async Task addCombi(SocketGuildUser user)
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

            using (swaightContext db = new swaightContext())
            {
                if (db.Combi.Where(p => p.ServerId == (long)Context.Guild.Id && p.UserId == (long)Context.User.Id).Count() >= 5)
                {
                    await ReplyAsync($"{Context.User.Mention} du hast bereits 5 bestehende Combis.");
                    return;
                }
                if (db.Combi.Where(p => p.ServerId == (long)Context.Guild.Id && p.UserId == (long)user.Id).Count() >= 5)
                {
                    await ReplyAsync($"{user.Mention} hat bereits 5 bestehende Combis.");
                    return;
                }
                if (db.Combi.Any(p => p.ServerId == (long)Context.Guild.Id && (p.UserId == (long)Context.User.Id && p.CombiUserId == (long)user.Id) || p.CombiUserId == (long)Context.User.Id && p.UserId == (long)user.Id))
                {
                    await ReplyAsync($"{Context.User.Mention} du hast bereits eine **bestehende** Combi oder Combi Anfrage mit {user.Mention}.");
                    return;
                }
                EmbedBuilder embed = new EmbedBuilder();
                embed.WithTitle("Combi Anfrage");
                embed.WithDescription($"Du hast Erfolgreich eine Combi Anfrage an {user.Mention} gestellt.");
                embed.WithFooter("Die Anfrage kann über die Reactions angenommen oder abgelehnt werden.");
                var msg = await ReplyAsync($"{user.Mention}", false, embed.Build());
                await db.Combi.AddAsync(new Combi { ServerId = (long)Context.Guild.Id, UserId = (long)Context.User.Id, CombiUserId = (long)user.Id, Accepted = false, MessageId = (long)msg.Id, Date = DateTime.Now });
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
            using (swaightContext db = new swaightContext())
            {
                var combis = db.Combi.Include(p => p.User).Include(p => p.CombiUser).Where(p => p.ServerId == (long)Context.Guild.Id && (p.UserId == (long)Context.User.Id || p.CombiUserId == (long)Context.User.Id));

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

                    if (combi.CombiUserId == (long)Context.User.Id)
                        embed.AddField($"[{count}] {combi.User.Name}", $"Status: {status}\nBesteht seit: {combi.Date.Value.ToString("dd.MM.yyyy")}");
                    if (combi.UserId == (long)Context.User.Id)
                        embed.AddField($"[{count}] {combi.CombiUser.Name}", $"Status: {status}\nBesteht seit: {combi.Date.Value.ToString("dd.MM.yyyy")}");

                    count++;
                }
                await ReplyAsync(null, false, embed.Build());
            }
        }

        [Command("delCombi", RunMode = RunMode.Async)]
        [BotCommand]
        [Summary("Löscht die Combi mit der entsprechenden ID aus der Liste >combis")]
        [Cooldown(30)]
        public async Task delCombi(int id)
        {
            using (swaightContext db = new swaightContext())
            {
                var combi = db.Combi.Include(p => p.User).Include(p => p.CombiUser).Where(p => p.ServerId == (long)Context.Guild.Id && (p.UserId == (long)Context.User.Id || p.CombiUserId == (long)Context.User.Id)).Skip(id - 1)?.FirstOrDefault();

                if (combi == null)
                {
                    await ReplyAsync($"{Context.User.Mention} die Combi mit der ID {id} kann nicht gefunden werden.");
                    return;
                }

                db.Combi.Remove(combi);
                await db.SaveChangesAsync();

                if (combi.CombiUserId == (long)Context.User.Id)
                    await ReplyAsync($"{Context.User.Mention} du hast erfolgreich die Combi mit **{combi.User.Name}** gelöscht.");
                if (combi.UserId == (long)Context.User.Id)
                    await ReplyAsync($"{Context.User.Mention} du hast erfolgreich die Combi mit **{combi.CombiUser.Name}** gelöscht.");
            }
        }

        [Command("acceptCombi", RunMode = RunMode.Async)]
        [BotCommand]
        [Summary("Akzeptiert eine ausstehende Anfrage mit der entsprechenden ID aus der Liste >combis")]
        [Cooldown(30)]
        public async Task acceptCombi(int id)
        {
            using (swaightContext db = new swaightContext())
            {
                var combi = db.Combi.Include(p => p.User).Include(p => p.CombiUser).Where(p => p.ServerId == (long)Context.Guild.Id && (p.UserId == (long)Context.User.Id || p.CombiUserId == (long)Context.User.Id)).Skip(id - 1)?.FirstOrDefault();

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

                if (combi.CombiUserId == (long)Context.User.Id)
                    await ReplyAsync($"{Context.User.Mention} du hast erfolgreich die Combi mit **{combi.User.Name}** angenommen.");
                if (combi.UserId == (long)Context.User.Id)
                    await ReplyAsync($"{Context.User.Mention} du hast erfolgreich die Combi mit **{combi.CombiUser.Name}** angenommen.");
            }
        }

    }
}
