using Discord.WebSocket;
using Rabbot.API.Models;
using Rabbot.Database;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using System.Xml;

namespace Rabbot.Services
{
    public class YouTubeVideoService
    {
        private static readonly ILogger _logger = Log.ForContext(Serilog.Core.Constants.SourceContextPropertyName, nameof(YouTubeVideoService));
        private readonly DiscordSocketClient _client;

        public YouTubeVideoService(DiscordSocketClient client)
        {
            _client = client;

            Task.Run(() =>
            {
                ConfigureYouTubeMonitor();
            });

        }
        private void ConfigureYouTubeMonitor()
        {
            List<string> channelIds = new List<string> { "UCS5FgjEjPYp9Ul7IU7VrfEQ", "UCw8IeYso0n8spjPBwfGrZ8w" };
            new Task(async () => await CheckLastVideo(channelIds, 60), TaskCreationOptions.LongRunning).Start();
            _logger.Information($"{nameof(YouTubeVideoService)}: Loaded successfully");
        }

        private async Task CheckLastVideo(List<string> channelIds, int intervallTime)
        {
            while (true)
            {
                try
                {
                    await Task.Delay(intervallTime * 1000);
                    foreach (var channelId in channelIds)
                    {
                        YouTubeVideo video = null;
                        using (XmlReader reader = XmlReader.Create($"https://www.youtube.com/feeds/videos.xml?channel_id={channelId}"))
                        {
                            video = SyndicationFeed.Load(reader).GetFirstVideo();
                        }
                        if (video != null)
                        {
                            using (swaightContext db = new swaightContext())
                            {
                                var dbVideo = db.Youtubevideo.FirstOrDefault(p => p.VideoId == video.Id);
                                if (dbVideo == null)
                                {
                                    if (video.Title.Contains(Constants.AnnouncementIgnoreTag))
                                        continue;
                                    await db.Youtubevideo.AddAsync(new Youtubevideo { VideoId = video.Id, VideoTitle = video.Title });
                                    await db.SaveChangesAsync();
                                    await NewVideo(db, video);
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.Error(e, $"Error while checking last YouTube video");
                }
            }
        }

        private async Task NewVideo(swaightContext db, YouTubeVideo video)
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

                    var youTubeUrl = $"https://www.youtube.com/watch?v={video.Id}";
                    await channel.SendMessageAsync($"**{video.ChannelName}** hat ein neues Video hochgeladen! {guild.EveryoneRole.Mention}\n{youTubeUrl}");

                }
                catch (Exception ex)
                {
                    _logger.Error(ex, $"Error while sending YouTube notification");
                }
            }
        }

    }
}
