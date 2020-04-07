using CliWrap;
using CliWrap.Buffered;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using Rabbot.Services;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YouTubeSearch;
using Log = Serilog.Log;

namespace Rabbot.Commands
{
    public class Music : ModuleBase<SocketCommandContext>
    {
        private readonly AudioService _audioService;
        private static readonly ILogger _logger = Log.ForContext(Serilog.Core.Constants.SourceContextPropertyName, nameof(Music));

        public Music(IServiceProvider services)
        {
            _audioService = services.GetRequiredService<AudioService>();
        }

        [RequireOwner]
        [Command("join", RunMode = RunMode.Async)]
        public async Task Join()
        {
            await _audioService.JoinAudio(Context.Guild, (Context.User as IVoiceState).VoiceChannel);
        }

        [RequireOwner]
        [Command("leave", RunMode = RunMode.Async)]
        public async Task Leave()
        {
            await _audioService.LeaveAudio(Context.Guild);
        }

        [RequireOwner]
        [Command("play", RunMode = RunMode.Async)]
        public async Task Play([Remainder] string url)
        {
            if (!(Uri.TryCreate(url, UriKind.Absolute, out Uri uriResult) && uriResult.Scheme == Uri.UriSchemeHttp))
            {
                int querypages = 1;
                VideoSearch videos = new VideoSearch();
                var result = videos.GetVideos(url, querypages).Result.FirstOrDefault();
                if (result == null)
                    await Context.Channel.SendMessageAsync($"Die Suche nach `{url}` konnte keine Ergebnisse erziehlen.");
                else
                {
                    url = result.getUrl();
                }
            }
            if (_audioService.Play(Context.Guild, url))
            {
                try
                {
                    var videoInfos = DownloadUrlResolver.GetDownloadUrls(url, false).FirstOrDefault();
                    if (videoInfos != null)
                    {
                        await Context.Channel.SendMessageAsync($"`{videoInfos.Title}` wird abgespielt.");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, $"Error in {nameof(Play)}");
                    await Context.Channel.SendMessageAsync($"Ups, da ging was schief.");
                    return;
                }
            }
            await Context.Channel.SendMessageAsync($"Ups, da ging was schief.");
        }

        [RequireOwner]
        [Command("addSong", RunMode = RunMode.Async)]
        public async Task AddSong([Remainder] string url)
        {
            if (!(Uri.TryCreate(url, UriKind.Absolute, out Uri uriResult) && uriResult.Scheme == Uri.UriSchemeHttp))
            {
                int querypages = 1;
                VideoSearch videos = new VideoSearch();
                var result = videos.GetVideos(url, querypages).Result.FirstOrDefault();
                if (result == null)
                    await Context.Channel.SendMessageAsync($"Die Suche nach `{url}` konnte keine Ergebnisse erziehlen.");
                else
                {
                    url = result.getUrl();
                }
            }
            if (await _audioService.AddToPlaylist(Context.Guild, url))
            {
                var videoInfos = DownloadUrlResolver.GetDownloadUrls(url, false).FirstOrDefault();
                if (videoInfos != null)
                {
                    await Context.Channel.SendMessageAsync($"`{videoInfos.Title}` wurde erfolgreich zur Playlist hinzugefügt.");
                    return;
                }
            }
            await Context.Channel.SendMessageAsync($"Ups, da ging was schief.");
        }

        [RequireOwner]
        [Command("playlist", RunMode = RunMode.Async)]
        public async Task Playlist()
        {
            var playlist = _audioService.GetPlaylist(Context.Guild);
            int counter = 1;
            string output = "**Aktuelle Playlist**\n";
            foreach (var song in playlist)
            {
                output += $"{counter}. `{song.Title}`\n";
                counter++;
            }
            await Context.Channel.SendMessageAsync(output);
        }

        [RequireOwner]
        [Command("currentsong", RunMode = RunMode.Async)]
        public async Task Currentsong()
        {
            var info = _audioService.GetSongInfo(Context.Guild);
            if (!string.IsNullOrWhiteSpace(info))
                await Context.Channel.SendMessageAsync(info);
        }
    }
}
