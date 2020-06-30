using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Rabbot.Database;
using Rabbot.Database.Rabbot;
using Rabbot.Models;
using Rabbot.Models.API;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Rabbot.Services
{
    public class YouTubeVideoService
    {
        private static readonly ILogger _logger = Log.ForContext(Serilog.Core.Constants.SourceContextPropertyName, nameof(YouTubeVideoService));
        private readonly DiscordShardedClient _client;
        private DatabaseService Database => DatabaseService.Instance;

        public YouTubeVideoService(IServiceProvider services)
        {
            _client = services.GetRequiredService<DiscordShardedClient>();

            Task.Run(() =>
            {
                ConfigureYouTubeMonitor();
            });

        }
        private void ConfigureYouTubeMonitor()
        {
            List<string> channelIds = new List<string> { "UCS5FgjEjPYp9Ul7IU7VrfEQ" };
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
                        YouTubeVideoDto video = null;
                        using (XmlReader reader = XmlReader.Create($"https://www.youtube.com/feeds/videos.xml?channel_id={channelId}"))
                        {
                            bool skip = false;
                            using (XmlReader descriptionReader = XmlReader.Create($"https://www.youtube.com/feeds/videos.xml?channel_id={channelId}"))
                            {
                                while (descriptionReader.Read())
                                if (descriptionReader.NodeType == XmlNodeType.Element)
                                    if (descriptionReader.Name == "media:description")
                                    {
                                        XElement el = XNode.ReadFrom(descriptionReader) as XElement;
                                        var description = el.FirstNode.ToString();

                                        if (description.Contains(Constants.AnnouncementIgnoreTag))
                                            skip = true;
                                    }
                            }

                            video = SyndicationFeed.Load(reader).GetFirstVideo();

                            if (skip)
                                continue;

                        }
                        if (video != null)
                        {
                            using (var db = Database.Open())
                            {
                                var dbVideo = db.YouTubeVideos.FirstOrDefault(p => p.VideoId == video.Id);
                                if (dbVideo == null)
                                {
                                    await db.YouTubeVideos.AddAsync(new YouTubeVideoEntity { VideoId = video.Id, VideoTitle = video.Title });
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

        private async Task NewVideo(RabbotContext db, YouTubeVideoDto video)
        {
            foreach (var dbGuild in db.Guilds)
            {
                try
                {
                    if (dbGuild.StreamChannelId == null)
                        continue;

                    var guild = _client.Guilds.FirstOrDefault(p => p.Id == dbGuild.GuildId);
                    if (guild == null)
                        continue;
                    if (!(guild.Channels.FirstOrDefault(p => p.Id == dbGuild.StreamChannelId) is SocketTextChannel channel))
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
