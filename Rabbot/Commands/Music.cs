using Discord;
using Discord.Commands;
using Rabbot.Services;
using System.Threading.Tasks;
namespace Rabbot.Commands
{
    public class Music : ModuleBase<SocketCommandContext>
    {
        private readonly AudioService _service;

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
        public async Task Play([Remainder] string song)
        {
            await _service.SendAudioAsync(Context.Guild, Context.Channel, song);
        }
    }
}
