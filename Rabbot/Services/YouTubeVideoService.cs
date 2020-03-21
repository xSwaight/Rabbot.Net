using Discord.WebSocket;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Rabbot.Database;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rabbot.Services
{
    public class YouTubeVideoService
    {
        private static readonly ILogger _logger = Log.ForContext(Serilog.Core.Constants.SourceContextPropertyName, nameof(YouTubeVideoService));
        private readonly YouTubeService _yt;
        private readonly DiscordSocketClient _client;

        public YouTubeVideoService(DiscordSocketClient client)
        {
            _client = client;
            _yt = new YouTubeService(new BaseClientService.Initializer() { ApiKey = Config.bot.youTubeApiKey });

            Task.Run(() =>
            {
                ConfigureYouTubeMonitorAsync();
            });

        }
        private void ConfigureYouTubeMonitorAsync()
        {
            var searchListRequest = _yt.Search.List("snippet");
            searchListRequest.ChannelId = "UCS5FgjEjPYp9Ul7IU7VrfEQ";
            searchListRequest.Order = SearchResource.ListRequest.OrderEnum.Date;
            searchListRequest.MaxResults = 1;
            new Task(async () => await CheckLastVideo(searchListRequest, 60), TaskCreationOptions.LongRunning).Start();
            _logger.Information($"{nameof(YouTubeVideoService)}: Loaded successfully");
        }

        private async Task CheckLastVideo(SearchResource.ListRequest searchListRequest, int intervallTime)
        {
            while (true)
            {
                try
                {
                    await Task.Delay(intervallTime * 1000);
                    var searchListResult = await searchListRequest.ExecuteAsync();
                    var video = searchListResult.Items.FirstOrDefault();
                    if (video != null)
                    {
                        using (swaightContext db = new swaightContext())
                        {
                            var dbVideo = db.Youtubevideo.FirstOrDefault(p => p.VideoId == video.Id.VideoId);
                            if (dbVideo == null)
                            {
                                if (video.Snippet.Description.Contains(Constants.AnnouncementIgnoreTag))
                                    continue;
                                await db.Youtubevideo.AddAsync(new Youtubevideo { VideoId = video.Id.VideoId, VideoTitle = video.Snippet.Title });
                                await db.SaveChangesAsync();
                                await NewVideo(db, video);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.Error(e, $"Error while checking last YouTube Video");
                }
            }
        }

        private async Task NewVideo(swaightContext db, SearchResult video)
        {
            foreach (var dbGuild in db.Guild)
            {
                try
                {
                    if (dbGuild.StreamchannelId == null)
                        continue;

                    var guild = _client.Guilds.FirstOrDefault(p => p.Id == dbGuild.ServerId);
                    if (guild == null)
                        continue;
                    if (!(guild.Channels.FirstOrDefault(p => p.Id == dbGuild.StreamchannelId) is SocketTextChannel channel))
                        continue;

                    var youTubeUrl = $"https://www.youtube.com/watch?v={video.Id.VideoId}";
                    await channel.SendMessageAsync($"**{video.Snippet.ChannelTitle}** hat ein neues Video hochgeladen! {guild.EveryoneRole.Mention}\n{youTubeUrl}");

                }
                catch (Exception ex)
                {
                    _logger.Error(ex, $"Error while sending YouTube notification");
                }
            }
        }

    }
}
