using CliWrap;
using CliWrap.Buffered;
using Discord;
using Discord.Commands;
using MediaToolkit;
using MediaToolkit.Model;
using Rabbot.Services;
using Serilog;
using Serilog.Core;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using VideoLibrary;

namespace Rabbot.Commands
{
    public class Music : ModuleBase<SocketCommandContext>
    {
        private readonly AudioService _service;
        private static readonly ILogger _logger = Log.ForContext(Serilog.Core.Constants.SourceContextPropertyName, nameof(Music));

        public Music(AudioService service)
        {
            _service = service;
        }

        [RequireOwner]
        [Command("join", RunMode = RunMode.Async)]
        public async Task Join()
        {
            await _service.JoinAudio(Context.Guild, (Context.User as IVoiceState).VoiceChannel);
        }

        [RequireOwner]
        [Command("leave", RunMode = RunMode.Async)]
        public async Task Leave()
        {
            await _service.LeaveAudio(Context.Guild);
        }

        [RequireOwner]
        [Command("play", RunMode = RunMode.Async)]
        public async Task Play([Remainder] string url)
        {
            //var youtube = YouTube.Default;
            //var vid = youtube.GetVideo(url);
            //if (vid == null)
            //    return;
            //var song = await GetMp3(vid);
            await _service.SendAudioAsync(Context.Guild, Context.Channel, url);
        }
    }
}
