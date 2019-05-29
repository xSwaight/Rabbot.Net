using Discord;
using Discord.Commands;
using MediaToolkit;
using MediaToolkit.Model;
using Rabbot.Services;
using System;
using System.IO;
using System.Threading.Tasks;
using VideoLibrary;

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
        public async Task Play([Remainder] string url)
        {
            var youtube = YouTube.Default;
            var vid = youtube.GetVideo(url);
            if (vid == null)
                return;
            var song = await GetMp3(vid);
            await _service.SendAudioAsync(Context.Guild, Context.Channel, song);
            File.Delete(song);
        }


        public async Task<string> GetMp3(YouTubeVideo video)
        {
            var source = AppContext.BaseDirectory;
            var test = await video.GetBytesAsync();
            File.WriteAllBytes(source + Helper.RemoveSpecialCharacters(video.FullName), test);

            var inputFile = new MediaFile { Filename = source + Helper.RemoveSpecialCharacters(video.FullName) };
            var outputFile = new MediaFile { Filename = $"{source + Helper.RemoveSpecialCharacters(video.FullName)}.mp3" };

            using (var engine = new Engine(AppContext.BaseDirectory + "ffmpeg.exe"))
            {
                engine.GetMetadata(inputFile);

                engine.Convert(inputFile, outputFile);
                File.Delete(inputFile.Filename);
            }
            return outputFile.Filename;
        }
    }
}
