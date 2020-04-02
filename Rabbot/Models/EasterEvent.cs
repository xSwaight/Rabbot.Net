using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Rabbot.Database;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rabbot.Models
{
    public class EasterEvent
    {
        private static readonly ILogger _logger = Log.ForContext(Serilog.Core.Constants.SourceContextPropertyName, nameof(EasterEvent));
        public SocketGuild Guild { get; }

        private readonly List<SocketTextChannel> _textChannels;
        public List<RestUserMessage> CurrentMessages { get; }

        public EasterEvent(SocketGuild guild)
        {
            Guild = guild;
            _textChannels = GetTextChannels(Constants.EasterMinChannelUser);
            CurrentMessages = new List<RestUserMessage>();
            new Task(async () => await CheckMessageDate(Constants.EasterDespawnTime), TaskCreationOptions.LongRunning).Start();
        }

        private List<SocketTextChannel> GetTextChannels(int minimalUserCount)
        {
            return Guild.TextChannels.Where(p => p.Users.Count >= minimalUserCount).ToList();
        }

        private SocketTextChannel GetRandomChannel()
        {
            var rndIndex = new Random().Next(0, _textChannels.Count() - 1);
            return _textChannels[rndIndex];
        }

        private async Task CheckMessageDate(int timeoutInMin)
        {
            while (true)
            {
                await Task.Delay(5000);
                try
                {
                    foreach (var message in CurrentMessages.ToList())
                    {
                        if (message.CreatedAt.DateTime.ToLocalTime().AddMinutes(timeoutInMin) < DateTime.Now)
                        {
                            CurrentMessages.Remove(message);
                            await message.DeleteAsync();
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Can't remove a message");
                }
            }
        }

        public async Task SendEasterEgg()
        {
            var textChannel = GetRandomChannel();
            var message = await textChannel.SendMessageAsync($"{Constants.EggGoatR}");
            await message.AddReactionAsync(Constants.EggGoatL);
            CurrentMessages.Add(message);
        }

        public async Task CatchEgg(ulong userId, ulong messageId)
        {
            var message = CurrentMessages.FirstOrDefault(p => p.Id == messageId);
            if (message == null)
                return;

            var channel = message.Channel;
            var author = Guild.Users.FirstOrDefault(p => p.Id == userId);

            

            int eggs = 0;
            using (rabbotContext db = new rabbotContext())
            {
                var feature = db.Userfeatures.FirstOrDefault(p => p.ServerId == Guild.Id && p.UserId == userId);
                if (feature == null)
                    return;

                feature.Eggs++;
                eggs = feature.Eggs;
                await db.SaveChangesAsync();
            }
            const int delay = 8000;
            var embed = new EmbedBuilder();
            embed.WithDescription($"Glückwunsch! {author.Mention} du hast jetzt {eggs} {Constants.EggGoatR} gefangen!");
            embed.WithColor(new Color(90, 92, 96));
            await message.ModifyAsync(p =>
            {
                p.Content = "";
                p.Embed = embed.Build();
            });
            CurrentMessages.Remove(message);
            await Task.Delay(delay);
            await message.DeleteAsync();
        }
    }
}
