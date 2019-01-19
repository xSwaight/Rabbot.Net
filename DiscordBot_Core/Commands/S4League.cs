using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using DiscordBot_Core.API;
using DiscordBot_Core.Database;
using DiscordBot_Core.ImageGenerator;
using DiscordBot_Core.API.Models;
using Discord.WebSocket;
using DiscordBot_Core.Preconditions;

namespace DiscordBot_Core.Commands
{
    public class S4League : ModuleBase<SocketCommandContext>
    {

        [Command("player", RunMode = RunMode.Async)]
        [Cooldown(10)]
        public async Task Player([Remainder]string arg)
        {

            if (!String.IsNullOrWhiteSpace(arg))
            {
                Player player = new Player();
                ApiRequest DB = new ApiRequest();
                player = await DB.GetPlayer(arg);
                if (player == null)
                {
                    var embed = new EmbedBuilder();
                    embed.WithTitle("Fehler");
                    embed.WithDescription("Spieler nicht gefunden ¯\\_(ツ)_/¯");
                    embed.WithColor(new Color(255, 0, 0));
                    await Context.Channel.SendMessageAsync("", false, embed.Build());
                }
                else
                {
                    var embedInfo = new EmbedBuilder();
                    embedInfo.WithColor(new Color(42, 46, 53));
                    embedInfo.AddField("Name", player.Name, true);
                    if (player.Clan != null)
                        embedInfo.AddField("Clan", player.Clan, true);
                    else
                        embedInfo.AddField("Clan", "-", true);
                    embedInfo.AddField("Level", player.Level.ToString(), true);
                    string[] exp = player.Levelbar.Split(':');
                    var percent = (Convert.ToDecimal(exp[0]) / Convert.ToDecimal(exp[1])) * 100;
                    embedInfo.AddField("Percent", Math.Round(percent, 2).ToString() + "%", true);
                    embedInfo.AddField("EXP", player.Exp.ToString("N0"), true);
                    TimeSpan time = DateTime.Now.AddSeconds(player.Playtime) - DateTime.Now;
                    string playtime = time.Days + "D " + time.Hours + "H " + time.Minutes + "M ";
                    embedInfo.AddField("Playtime", playtime, true);
                    embedInfo.AddField("TD Rate", player.Tdrate.ToString(), true);
                    embedInfo.AddField("KD Rate", player.Kdrate.ToString(), true);
                    embedInfo.AddField("Matches played", player.Matches_played.ToString("N0"), true);
                    embedInfo.AddField("Matches won", player.Matches_won.ToString("N0"), true);
                    embedInfo.AddField("Matches lost", player.Matches_lost.ToString("N0"), true);
                    embedInfo.AddField("Last online", Convert.ToDateTime(player.Last_online).ToShortDateString(), true);
                    embedInfo.AddField("Views", player.Views.ToString("N0"), true);
                    embedInfo.AddField("Favorites", player.Favorites.ToString("N0"), true);
                    embedInfo.AddField("Fame", player.Fame.ToString() + "%", true);
                    embedInfo.AddField("Hate", player.Hate.ToString() + "%", true);
                    embedInfo.ThumbnailUrl = "https://s4db.net/assets/img/icon192.png";
                    await Context.Channel.SendMessageAsync("", false, embedInfo.Build());
                }
            }
        }

        [Command("clan", RunMode = RunMode.Async)]
        [Cooldown(10)]
        public async Task Clan([Remainder]string name)
        {

            if (!String.IsNullOrWhiteSpace(name))
            {
                Clan clan = new Clan();
                ApiRequest DB = new ApiRequest();
                clan = await DB.GetClan(name);
                if (clan == null)
                {
                    var embed = new EmbedBuilder();
                    embed.WithTitle("Fehler");
                    embed.WithDescription("Clan nicht gefunden ¯\\_(ツ)_/¯");
                    embed.WithColor(new Color(255, 0, 0));
                    await Context.Channel.SendMessageAsync("", false, embed.Build());
                }
                else
                {
                    var embedInfo = new EmbedBuilder();
                    embedInfo.WithColor(new Color(42, 46, 53));
                    embedInfo.AddField("Name", clan.Name, true);
                    embedInfo.AddField("Master", clan.Master, true);
                    embedInfo.AddField("Member Count", clan.Member_count.ToString(), true);
                    if (!String.IsNullOrWhiteSpace(clan.Announcement))
                        embedInfo.AddField("Announcement", clan.Announcement, true);
                    if (!String.IsNullOrWhiteSpace(clan.Description))
                        embedInfo.AddField("Description", clan.Description, true);
                    embedInfo.AddField("Views", clan.Views.ToString("N0"), true);
                    embedInfo.AddField("Favorites", clan.Favorites.ToString(), true);
                    embedInfo.AddField("Fame", clan.Fame.ToString() + "%", true);
                    embedInfo.AddField("Hate", clan.Hate.ToString() + "%", true);
                    embedInfo.ThumbnailUrl = "https://s4db.net/assets/img/icon192.png";
                    await Context.Channel.SendMessageAsync("", false, embedInfo.Build());
                }
            }
        }

        //[Command("server", RunMode = RunMode.Async)]
        //public async Task Server()
        //{
        //    List<Server> server = new List<Server>();
        //    ApiRequest DB = new ApiRequest();
        //    server = await DB.GetServer();
        //    var embed = new EmbedBuilder();
        //    embed.WithDescription($"***Server Liste:***");
        //    embed.WithColor(new Color(111, 116, 124));
        //    using (swaightContext db = new swaightContext())
        //    {
        //        string crashedServer = "";
        //        foreach (var item in server)
        //        {
        //            if (item.Player_online >= 0)
        //            {
        //                string status = "";
        //                switch (item.State)
        //                {
        //                    case 0:
        //                        status = "Offline";
        //                        crashedServer = item.Name;
        //                        break;
        //                    case 1:
        //                        status = "Slow";
        //                        break;
        //                    case 2:
        //                        status = "Online";
        //                        break;
        //                    default:
        //                        status = "Unknown";
        //                        break;
        //                }
        //                embed.AddField(item.Name, "Status: **" + status + "** | User online: **" + item.Player_online.ToString("N0") + "**", false);
        //            }
        //        }
        //        await Context.Channel.SendMessageAsync(null, false, embed.Build());
        //    }
        //}

        [Command("playercard", RunMode = RunMode.Async)]
        [Cooldown(10)]
        public async Task Playercard([Remainder]string arg)
        {

            Player player = new Player();
            ApiRequest DB = new ApiRequest();
            player = await DB.GetPlayer(arg);
            if (player == null)
            {
                var embed = new EmbedBuilder();
                embed.WithTitle("Fehler");
                embed.WithDescription("Spieler nicht gefunden ¯\\_(ツ)_/¯");
                embed.WithColor(new Color(255, 0, 0));
                await Context.Channel.SendMessageAsync("", false, embed.Build());
            }
            else
            {
                var template = new HtmlTemplate(Directory.GetCurrentDirectory() + "/playercardTemplate.html");
                var html = template.Render(new
                {
                    BACKGROUND = "Background.png",
                    COLOR = "#949494",
                    LEVEL = player.Level.ToString(),
                    IGNAME = player.Name,
                    EXP = player.Exp.ToString("N0"),
                    TOUCHDOWN = player.Tdrate.ToString(),
                    MATCHES = player.Matches_played.ToString("N0"),
                    DEATHMATCH = player.Kdrate.ToString()
                });

                var path = HtmlToImage.Generate(Helper.RemoveSpecialCharacters(arg), html, 300, 170);
                await Context.Channel.SendFileAsync(path);
                File.Delete(path);
            }
        }

        [Command("s4dbcard", RunMode = RunMode.Async)]
        [Cooldown(10)]
        public async Task S4dbcard([Remainder]string arg)
        {

            Player player = new Player();
            ApiRequest DB = new ApiRequest();
            player = await DB.GetPlayer(arg);
            if (player == null)
            {
                var embed = new EmbedBuilder();
                embed.WithTitle("Fehler");
                embed.WithDescription("Spieler nicht gefunden ¯\\_(ツ)_/¯");
                embed.WithColor(new Color(255, 0, 0));
                await Context.Channel.SendMessageAsync("", false, embed.Build());
            }
            else
            {
                var template = new HtmlTemplate(Directory.GetCurrentDirectory() + "/playercardTemplate.html");
                var html = template.Render(new
                {
                    BACKGROUND = "S4DB_background.png",
                    COLOR = "#403e3e",
                    LEVEL = player.Level.ToString(),
                    IGNAME = player.Name,
                    EXP = player.Exp.ToString("N0"),
                    TOUCHDOWN = player.Tdrate.ToString(),
                    MATCHES = player.Matches_played.ToString("N0"),
                    DEATHMATCH = player.Kdrate.ToString()
                });

                var path = HtmlToImage.Generate(Helper.RemoveSpecialCharacters(arg), html, 300, 170);
                await Context.Channel.SendFileAsync(path);
                File.Delete(path);
            }
        }


        [Command("s4")]
        public async Task S4()
        {
            using (swaightContext db = new swaightContext())
            {
                await Context.Message.DeleteAsync();
                var s4Role = Context.Guild.Roles.Where(p => p.Name == "S4 League");
                if (s4Role.Count() == 0)
                    return;

                var user = Context.Guild.Users.Where(p => p.Id == Context.User.Id).FirstOrDefault();
                if (user.Roles.Where(p => p.Name == "S4 League").Count() != 0)
                    return;

                await user.AddRoleAsync(s4Role.FirstOrDefault());
                var guild = db.Guild.Where(p => p.ServerId == (long)Context.Guild.Id).FirstOrDefault();
                if (guild.LogchannelId != null && guild.Log == 1)
                {
                    var logchannel = Context.Guild.TextChannels.Where(p => p.Id == (ulong)guild.LogchannelId).FirstOrDefault();
                    var embed = new EmbedBuilder();
                    embed.WithDescription($"{Context.User.Mention} hat sich die S4 League Rolle gegeben.");
                    embed.WithColor(new Color(0, 255, 0));
                    await logchannel.SendMessageAsync("", false, embed.Build());
                }
            }
        }
    }
}
