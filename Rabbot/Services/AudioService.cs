using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using CliWrap;
using CliWrap.Buffered;
using Discord;
using Discord.Audio;
using Discord.WebSocket;
using Rabbot.Models;
using Serilog;

namespace Rabbot.Services
{
    public class AudioService
    {
        private readonly ConcurrentDictionary<ulong, AudioClient> ConnectedChannels = new ConcurrentDictionary<ulong, AudioClient>();
        private readonly DiscordSocketClient _client;
        private static readonly ILogger _logger = Log.ForContext(Serilog.Core.Constants.SourceContextPropertyName, nameof(AudioService));

        public AudioService(DiscordSocketClient client)
        {
            _client = client;
            _client.UserVoiceStateUpdated += UserVoiceStateUpdated;
        }

        private async Task UserVoiceStateUpdated(SocketUser user, SocketVoiceState before, SocketVoiceState after)
        {
            if (user.Id == _client.CurrentUser.Id && after.VoiceChannel == null)
            {
                if (user is SocketGuildUser guildUser)
                {
                    await LeaveAudio(guildUser.Guild);
                }

            }
        }

        public async Task JoinAudio(IGuild guild, IVoiceChannel target)
        {

            if (ConnectedChannels.ContainsKey(guild.Id))
            {
                return;
            }
            if (target.Guild.Id != guild.Id)
            {
                return;
            }

            var audioClient = await target.ConnectAsync();

            ConnectedChannels.TryAdd(guild.Id, new AudioClient(audioClient));
        }

        public async Task LeaveAudio(IGuild guild)
        {
            if (ConnectedChannels.TryRemove(guild.Id, out AudioClient client))
            {
                await client.DiscordAudioClient.StopAsync();
                client.DiscordAudioClient.Dispose();
            }
        }

        public bool Play(IGuild guild, string url)
        {
            if (ConnectedChannels.TryGetValue(guild.Id, out AudioClient client))
            {
                try
                {
                    new Task(async () => await client.Play(url), TaskCreationOptions.LongRunning).Start();
                    return true;
                }
                catch 
                {
                    return false;
                }
            }
            return false;
        }

        public async Task<bool> AddToPlaylist(IGuild guild, string url)
        {
            if (ConnectedChannels.TryGetValue(guild.Id, out AudioClient client))
            {
                return await client.AddToPlaylist(url);
            }
            return false;
        }

        public List<PlaylistItemDto> GetPlaylist(IGuild guild)
        {
            if (ConnectedChannels.TryGetValue(guild.Id, out AudioClient client))
            {
                return client.GetPlaylist();
            }
            return null;
        }

        public string GetSongInfo(IGuild guild)
        {
            if (ConnectedChannels.TryGetValue(guild.Id, out AudioClient client))
            {
                string output = $"Aktueller Song: `{client.CurrentSong.Title}`\n";
                output += $"{client.GetCurrentPositionInSeconds().ToTimeString("mm:ss")} / {client.GetSongLenght().ToTimeString("mm:ss")}";
                return output;
            }
            return null;
        }
    }
}
