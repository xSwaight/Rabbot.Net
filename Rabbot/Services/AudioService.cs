using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CliWrap;
using CliWrap.Buffered;
using Discord;
using Discord.Audio;
using Discord.WebSocket;
using Serilog;

namespace Rabbot.Services
{
    public class AudioService
    {
        private readonly ConcurrentDictionary<ulong, IAudioClient> ConnectedChannels = new ConcurrentDictionary<ulong, IAudioClient>();
        private readonly DiscordSocketClient _client;
        private static readonly ILogger _logger = Log.ForContext(Serilog.Core.Constants.SourceContextPropertyName, nameof(AudioService));

        public AudioService(DiscordSocketClient client)
        {
            _client = client;
            _client.VoiceServerUpdated += VoiceServerUpdated;
            _client.UserVoiceStateUpdated += UserVoiceStateUpdated;
        }

        private async Task UserVoiceStateUpdated(SocketUser user, SocketVoiceState before, SocketVoiceState after)
        {
            if (user.Id == _client.CurrentUser.Id && after.VoiceChannel == null)
            {
                if(user is SocketGuildUser guildUser)
                {
                    await LeaveAudio(guildUser.Guild);
                }

            }
        }

        private async Task VoiceServerUpdated(SocketVoiceServer arg)
        {
            var test = arg;
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

            ConnectedChannels.TryAdd(guild.Id, audioClient);
        }

        public async Task LeaveAudio(IGuild guild)
        {
            if (ConnectedChannels.TryRemove(guild.Id, out IAudioClient client))
            {
                await client.StopAsync();
                client.Dispose();
            }
        }

        public async Task SendAudioAsync(IGuild guild, IMessageChannel channel, string url)
        {
            if (ConnectedChannels.TryGetValue(guild.Id, out IAudioClient client))
            {
                var streamUrl = GetStreamLink(url).Result.TrimEnd();
                using (var stream = client.CreatePCMStream(AudioApplication.Music))
                {
                    try
                    {
                        var argument = $"-hide_banner -loglevel panic -i \"{streamUrl}\" -ac 2 -f s16le -ar 48000 pipe:1";
                        await Cli.Wrap("ffmpeg.exe").WithArguments(argument).WithStandardOutputPipe(PipeTarget.ToStream(stream)).ExecuteAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, $"Error in {nameof(SendAudioAsync)}");
                    }
                    finally { await stream.FlushAsync(); }
                }
            }
        }

        private async Task<string> GetStreamLink(string url)
        {
            var result = await Cli.Wrap("youtube-dl.exe")
                    .WithArguments($"--format bestaudio[protocol!=http_dash_segments] --youtube-skip-dash-manifest --no-playlist --get-url " + url)
                    .ExecuteBufferedAsync();

            if (result.ExitCode != 0)
            {
                _logger.Error("Something went wrong!");
                return null;
            }
            return result.StandardOutput;
        }
    }
}
