using CliWrap;
using CliWrap.Buffered;
using Discord;
using Discord.Commands;
using Rabbot.Services;
using Serilog;
using Serilog.Core;
using System;
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
            if (!(Uri.TryCreate(url, UriKind.Absolute, out Uri uriResult) && uriResult.Scheme == Uri.UriSchemeHttp))
            {
                int querypages = 1;
                VideoSearch videos = new VideoSearch();
                var result = videos.GetVideos(url, querypages).Result.FirstOrDefault();
                if (result == null)
                    await Context.Channel.SendMessageAsync($"Die Suche nach {url} konnte keine Ergebnisse erziehlen.");
                else
                {
                    url = result.getUrl();
                }
            }
            await _service.SendAudioAsync(Context.Guild, Context.Channel, url);
        }
    }
}
