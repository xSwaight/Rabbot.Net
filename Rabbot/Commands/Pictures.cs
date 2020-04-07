using Discord;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using Rabbot.Preconditions;
using Rabbot.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rabbot.Commands
{
    [Group("random")]
    public class Pictures : ModuleBase<SocketCommandContext>
    {
        private static ApiService _apiService;
        private readonly CommandService _commandService;

        public Pictures(IServiceProvider services)
        {
            _apiService = services.GetRequiredService<ApiService>();
            _commandService = services.GetRequiredService<CommandService>();
        }

        public class AnimalPictures : ModuleBase<SocketCommandContext>
        {
            [Priority(3)]
            [Command("fox", RunMode = RunMode.Async), Alias("fuchs")]
            [Cooldown(60)]
            public async Task Fox()
            {
                await SendImage(Context, PictureType.Fox);
            }

            [Priority(3)]
            [Command("kitten", RunMode = RunMode.Async), Alias("cat", "katze", "kadse")]
            [Cooldown(60)]
            public async Task Kitten()
            {
                await SendImage(Context, PictureType.Cat);
            }

            [Priority(3)]
            [Command("doggo", RunMode = RunMode.Async), Alias("hund", "dog")]
            [Cooldown(60)]
            public async Task Doggo()
            {
                await SendImage(Context, PictureType.Dog);
            }

            [Priority(3)]
            [Command("shibe", RunMode = RunMode.Async), Alias("shibu")]
            [Cooldown(60)]
            public async Task Shibe()
            {
                await SendImage(Context, PictureType.Shibe);
            }
        }

        [Priority(2)]
        [Command(RunMode = RunMode.Async)]
        [Summary("Random Bild!")]
        [Cooldown(60)]
        public async Task Random()
        {
            await SendImage(Context, PictureType.Random);
        }

        [Priority(1)]
        [Command(RunMode = RunMode.Async)]
        public async Task Help(string parameter)
        {
            List<CommandInfo> commands = _commandService.Commands.Where(p => p.Module.Name == nameof(AnimalPictures)).ToList();
            string output = $"**`{parameter}` ist kein gültiger Parameter**\n\nGültige Parameter: ";

            foreach (var command in commands)
            {
                foreach (var alias in command.Aliases)
                {
                    output += $"`{alias.Replace("random", string.Empty).Trim()}` ";
                }
            }
            output += $"";
            await Context.Channel.SendMessageAsync(output);
        }

        private static async Task SendImage(SocketCommandContext context, PictureType picture)
        {

            string filepath = string.Empty;
            string title = string.Empty;
            using (context.Channel.EnterTypingState())
            {
                switch (picture)
                {
                    case PictureType.Dog:
                        filepath = _apiService.GetDogImage();
                        title = "Random Doggo";
                        break;
                    case PictureType.Cat:
                        filepath = _apiService.GetCatImage();
                        title = "Random Kitty";
                        break;
                    case PictureType.Fox:
                        filepath = _apiService.GetFoxImage();
                        title = "Random Fox";
                        break;
                    case PictureType.Shibe:
                        filepath = _apiService.GetShibeImage();
                        title = "Random Shibe";
                        break;
                    case PictureType.Random:
                        filepath = _apiService.GetRandomImage();
                        title = "Random Picture";
                        break;
                    default:
                        filepath = string.Empty;
                        break;
                }
                if (!string.IsNullOrWhiteSpace(filepath))
                {
                    EmbedBuilder embed = new EmbedBuilder
                    {
                        ImageUrl = filepath,
                        Title = title,
                        Color = new Color((uint)new Random().Next(0x1000000))
                    };
                    await context.Channel.SendMessageAsync("", false, embed.Build());
                }
                else
                    await context.Channel.SendMessageAsync($"Ooopsie. Irgendwas läuft hier gewaltig schief.");
            }
        }
    }
    public enum PictureType
    {
        Dog,
        Cat,
        Fox,
        Shibe,
        Random
    }
}
