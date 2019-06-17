using Discord;
using Discord.WebSocket;
using Rabbot.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TwitchLib.Api;
using TwitchLib.Api.Services;
using TwitchLib.Api.Services.Events.LiveStreamMonitor;

namespace Rabbot.API
{
    class Twitch
    {
        private LiveStreamMonitorService Monitor;
        private TwitchAPI API;
        DiscordSocketClient _client;
        public Twitch(DiscordSocketClient client)
        {
            try
            {
                _ = ConfigLiveMonitorAsync(client);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + " " + e.StackTrace);
            }
        }
        public async Task ConfigLiveMonitorAsync(DiscordSocketClient client)
        {
            try
            {
                _client = client;
                API = new TwitchAPI();
                API.Settings.ClientId = Config.bot.twitchToken;

                Monitor = new LiveStreamMonitorService(API, 60);
                List<string> channels = new List<string> { "swaight" };
                Monitor.SetChannelsByName(channels);

                Monitor.OnStreamOnline += Monitor_OnStreamOnline;
                Monitor.OnStreamOffline += Monitor_OnStreamOffline;

                Monitor.Start();
                await Task.Delay(-1);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + " " + e.StackTrace);
            }
        }

        private void Monitor_OnStreamOffline(object sender, OnStreamOfflineArgs e)
        {
            //ToDo
        }


        private void Monitor_OnStreamOnline(object sender, OnStreamOnlineArgs e)
        {
            try
            {
                Task.Run(() => StreamOnline(e));

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + " " + ex.StackTrace);
            }
        }

        private async Task StreamOnline(OnStreamOnlineArgs e)
        {
            try
            {
                var games = await API.Helix.Games.GetGamesAsync(new List<string> { e.Stream.GameId });
                var users = await API.Helix.Users.GetUsersAsync(new List<string> { e.Stream.UserId });

                var game = games.Games.FirstOrDefault();
                var user = users.Users.FirstOrDefault();

                using (swaightContext db = new swaightContext())
                {
                    var dbStream = db.Stream.Where(p => p.StreamId == Convert.ToInt64(e.Stream.Id)).FirstOrDefault();
                    if (dbStream != null)
                        return;

                    await db.Stream.AddAsync(new Stream { StreamId = Convert.ToInt64(e.Stream.Id), StartTime = e.Stream.StartedAt, Title = e.Stream.Title, TwitchUserId = Convert.ToInt64(e.Stream.UserId) });
                    await db.SaveChangesAsync();

                    foreach (var item in db.Guild)
                    {
                        if (item.StreamchannelId != null)
                        {
                            var guild = _client.Guilds.Where(p => p.Id == (ulong)item.ServerId).FirstOrDefault();
                            if (guild == null)
                                continue;
                            if (!(guild.Channels.Where(p => p.Id == (ulong)item.StreamchannelId).FirstOrDefault() is SocketTextChannel channel))
                                continue;

                            var embed = new EmbedBuilder();
                            var author = new EmbedAuthorBuilder
                            {
                                Name = user.DisplayName,
                                IconUrl = user.ProfileImageUrl
                            };
                            embed.WithAuthor(author);
                            embed.WithTitle(e.Stream.Title);
                            embed.WithUrl($"https://www.twitch.tv/{e.Channel}");
                            embed.WithThumbnailUrl(user.ProfileImageUrl);
                            embed.AddField("Game", game.Name, true);
                            embed.AddField("Viewers", e.Stream.ViewerCount, true);
                            var ThumbnailUrl = e.Stream.ThumbnailUrl.Replace("{width}", "1280").Replace("{height}", "720");
                            embed.WithImageUrl(ThumbnailUrl);
                            await channel.SendMessageAsync($"Hi {guild.EveryoneRole.Mention}! Ich bin live auf https://www.twitch.tv/{e.Channel} Schaut mal vorbei :)", false, embed.Build());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + " " + ex.StackTrace);
            }
        }
    }
}
