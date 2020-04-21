using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Rabbot.Database;
using Rabbot.Database.Rabbot;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TwitchLib.Api.V5;

namespace Rabbot.Services
{
    class TwitchService
    {
        private readonly DiscordShardedClient _client;
        private readonly DatabaseService _databaseService;
        private static readonly ILogger _logger = Log.ForContext(Serilog.Core.Constants.SourceContextPropertyName, nameof(TwitchService));
        public TwitchService(IServiceProvider services)
        {
            _client = services.GetRequiredService<DiscordShardedClient>();
            _databaseService = services.GetRequiredService<DatabaseService>();
            Task.Run(() =>
            {
                ConfigureLiveMonitorAsync();
            });
        }
        private void ConfigureLiveMonitorAsync()
        {
            try
            {
                var twitchClient = new V5();
                twitchClient.Settings.ClientId = Config.Bot.TwitchToken;
                twitchClient.Settings.AccessToken = Config.Bot.TwitchAccessToken;
                this.OnStreamOnline += Twitch_OnStreamOnline;
                new Task(async () => await CheckStreamStatus(twitchClient, 60), TaskCreationOptions.LongRunning).Start();
                _logger.Information($"{nameof(TwitchService)}: Loaded successfully");
            }
            catch (Exception e)
            {
                _logger.Error(e, $"Error while loading {nameof(TwitchService)}");
            }
        }


        private async Task CheckStreamStatus(V5 twitchClient, int intervallTime)
        {
            List<TwitchLib.Api.V5.Models.Streams.Stream> onlineStreams = new List<TwitchLib.Api.V5.Models.Streams.Stream>();
            while (true)
            {
                try
                {
                    await Task.Delay(intervallTime * 1000);
                    using var db = _databaseService.Open<RabbotContext>();
                    var twitchChannels = db.TwitchChannels.ToList();
                    if (!twitchChannels.Any())
                        continue;

                    foreach (var twitchChannel in twitchChannels)
                    {
                        var userId = twitchClient.Users.GetUserByNameAsync(twitchChannel.ChannelName).Result.Matches?.FirstOrDefault()?.Id;
                        if (userId == null)
                            continue;
                        var stream = twitchClient.Streams?.GetStreamByUserAsync(userId).Result?.Stream;
                        if (stream != null)
                        {
                            if (!onlineStreams.Contains(stream))
                            {
                                onlineStreams.Add(stream);
                                OnStreamOnline?.Invoke(this, new StreamEventArgs { Stream = stream, GuildId = twitchChannel.GuildId });
                            }
                        }
                        else
                        {
                            if (onlineStreams.Contains(stream))
                            {
                                onlineStreams.Remove(stream);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.Error(e, $"Error while checking Twitch streams");
                }

            }
        }

        private void Twitch_OnStreamOnline(object sender, StreamEventArgs e)
        {
            try
            {
                Task.Run(async () =>
                {
                    await StreamOnline(e);
                });

            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error while pushing event");
            }
        }

        private async Task StreamOnline(StreamEventArgs e)
        {
            try
            {
                using var db = _databaseService.Open<RabbotContext>();
                var dbStream = db.Streams.FirstOrDefault(p => p.StreamId == Convert.ToUInt64(e.Stream.Id) && p.AnnouncedGuildId == e.GuildId);
                if (dbStream != null)
                    return;

                await db.Streams.AddAsync(new StreamEntity { StreamId = Convert.ToUInt64(e.Stream.Id), StartTime = e.Stream.CreatedAt, Title = e.Stream.Channel.Status, TwitchUserId = Convert.ToUInt64(e.Stream.Channel.Id), AnnouncedGuildId = e.GuildId });
                await db.SaveChangesAsync();

                var dbGuild = db.Guilds.AsQueryable().FirstOrDefault(p => p.GuildId == e.GuildId);
                if (dbGuild == null)
                    return;

                if (dbGuild.StreamChannelId != null)
                {
                    var guild = _client.Guilds.FirstOrDefault(p => p.Id == dbGuild.GuildId);
                    if (guild == null)
                        return;
                    if (!(guild.Channels.FirstOrDefault(p => p.Id == dbGuild.StreamChannelId) is SocketTextChannel channel))
                        return;

                    var embed = new EmbedBuilder();
                    var author = new EmbedAuthorBuilder
                    {
                        Name = e.Stream.Channel.DisplayName,
                        IconUrl = e.Stream.Channel.Logo
                    };
                    embed.WithAuthor(author);
                    embed.WithTitle(e.Stream.Channel.Status);
                    embed.WithUrl($"https://www.twitch.tv/{e.Stream.Channel.Name}");
                    if (!string.IsNullOrWhiteSpace(e.Stream.Channel.Logo))
                        embed.WithThumbnailUrl(e.Stream.Channel.Logo);
                    if (!string.IsNullOrWhiteSpace(e.Stream.Game))
                        embed.AddField("Game", e.Stream.Game, true);
                    embed.AddField("Viewers", e.Stream.Viewers.ToString(), true);
                    var ThumbnailUrl = e.Stream.Preview.Large.Replace("{width}", "1280").Replace("{height}", "720");
                    if (!string.IsNullOrWhiteSpace(ThumbnailUrl))
                        embed.WithImageUrl(ThumbnailUrl);
                    if (e.Stream.Channel.Name == "swaight")
                        await channel.SendMessageAsync($"Hi {guild.EveryoneRole.Mention}! Ich bin live auf https://www.twitch.tv/{e.Stream.Channel.Name} Schaut mal vorbei :)", false, embed.Build());
                    else
                        await channel.SendMessageAsync($"Hi {guild.EveryoneRole.Mention}! {e.Stream.Channel.DisplayName} ist live auf https://www.twitch.tv/{e.Stream.Channel.Name} Schaut mal vorbei :)", false, embed.Build());

                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error while sending stream announcement");
            }
        }

        public event EventHandler<StreamEventArgs> OnStreamOnline;
    }
    public class StreamEventArgs
    {
        public TwitchLib.Api.V5.Models.Streams.Stream Stream { get; set; }
        public ulong GuildId { get; set; }
    }
}
