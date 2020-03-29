﻿using CliWrap;
using CliWrap.Buffered;
using CSCore;
using CSCore.Codecs;
using Discord.Audio;
using Microsoft.Win32.SafeHandles;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using YouTubeSearch;
using Log = Serilog.Log;

namespace Rabbot.Models
{
    public class AudioClient
    {
        public IAudioClient DiscordAudioClient { get; set; }
        public List<PlaylistItemDto> Playlist { get; set; }
        public PlaylistItemDto CurrentSong
        {
            get
            {
                return Playlist.FirstOrDefault();
            }
        }

        private IWaveSource _codec;
        private static readonly ILogger _logger = Log.ForContext(Serilog.Core.Constants.SourceContextPropertyName, nameof(AudioClient));
        private event EventHandler OnEndOfSong;

        public AudioClient(IAudioClient audioClient)
        {
            DiscordAudioClient = audioClient;
            Playlist = new List<PlaylistItemDto>();
            this.OnEndOfSong += NextSong;
        }

        private void NextSong(object sender, EventArgs e)
        {
            Task.Run(async () => { await PlaySound(); });
        }

        private async Task SendAudioAsync()
        {
            if (CurrentSong == null)
            {
                await DiscordAudioClient.StopAsync();
                return;
            }

            using (var stream = DiscordAudioClient.CreatePCMStream(AudioApplication.Music))
            {
                try
                {
                    using (_codec = CodecFactory.Instance.GetCodec(new Uri(CurrentSong.StreamUrl)))
                    {
                        _codec.WriteToStream(stream);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, $"Error in {nameof(SendAudioAsync)}");
                }
                finally
                {
                    await stream.FlushAsync();
                    Playlist.Remove(CurrentSong);
                }
            }
        }

        public async Task Play(string url)
        {
            await AddToPlaylist(url);
            await PlaySound();
        }

        private async Task PlaySound()
        {
            await SendAudioAsync();
            OnEndOfSong?.Invoke(this, EventArgs.Empty);
        }

        public async Task<bool> AddToPlaylist(string url)
        {
            if (Playlist.Where(p => p.YouTubeUrl == url).Any())
                return false;

            var streamUrl = await GetStreamLink(url);

            var videoInfos = DownloadUrlResolver.GetDownloadUrls(url, false).FirstOrDefault();
            if (videoInfos == null)
                return false;

            Playlist.Add(new PlaylistItemDto {StreamUrl = streamUrl, YouTubeUrl = url, Title = videoInfos.Title });
            return true;
        }

        private async Task<string> GetStreamLink(string url)
        {
            var result = await Cli.Wrap(Helper.GetFilePath("youtube-dl"))
                    .WithArguments($"--format bestaudio[protocol!=http_dash_segments] --youtube-skip-dash-manifest --no-playlist --get-url " + url)
                    .ExecuteBufferedAsync();
            if (result.ExitCode != 0)
            {
                _logger.Error("Something went wrong!");
                return null;
            }
            return result.StandardOutput;
        }

        public List<PlaylistItemDto> GetPlaylist()
        {
            return Playlist;
        }

        public TimeSpan GetCurrentPositionInSeconds()
        {
            return _codec.GetPosition();
        }

        public TimeSpan GetSongLenght()
        {
            return _codec.GetLength();
        }
    }
}
