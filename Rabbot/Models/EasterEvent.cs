using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Rabbot.Database;
using Rabbot.Database.Rabbot;
using Rabbot.Services;
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
        public SocketGuild Guild { get; }

        public List<RestUserMessage> CurrentMessages { get; }
        private static readonly ILogger _logger = Log.ForContext(Serilog.Core.Constants.SourceContextPropertyName, nameof(EasterEvent));
        private readonly List<SocketTextChannel> _textChannels;
        private readonly IServiceProvider _services;
        private DatabaseService Database { get; set; } = new DatabaseService();

        public EasterEvent(SocketGuild guild, IServiceProvider services)
        {
            Guild = guild;
            _services = services;
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
                            using (var db = Database.Open())
                            {
                                var easterevent = db.EasterEvents.FirstOrDefault(p => p.MessageId == message.Id);
                                if(easterevent != null)
                                {
                                    easterevent.DespawnTime = DateTime.Now;
                                }
                                await db.SaveChangesAsync();
                            }
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
            using (var db = Database.Open())
            {
                await db.EasterEvents.AddAsync(new EasterEventEntity { MessageId = message.Id, SpawnTime = DateTime.Now });
                await db.SaveChangesAsync();
            }
        }

        public async Task CatchEgg(ulong userId, ulong messageId)
        {
            var message = CurrentMessages.FirstOrDefault(p => p.Id == messageId);
            if (message == null)
                return;

            var channel = message.Channel;
            var author = Guild.Users.FirstOrDefault(p => p.Id == userId);

            int eggs = 0;
            using (var db = Database.Open())
            {
                var user = db.Users.FirstOrDefault(p => p.Id == userId) ?? db.Users.AddAsync(new UserEntity { Id = userId, Name = $"{author.Username}#{author.Discriminator}" }).Result.Entity;
                var feature = db.Features.FirstOrDefault(p => p.GuildId == Guild.Id && p.UserId == userId) ?? db.Features.AddAsync(new FeatureEntity { UserId = userId, GuildId = Guild.Id }).Result.Entity;
                if (feature == null)
                    return;

                feature.Eggs++;
                eggs = feature.Eggs;
                var easterevent = db.EasterEvents.FirstOrDefault(p => p.MessageId == messageId);
                if (easterevent != null)
                {
                    easterevent.UserId = userId;
                    easterevent.CatchTime = DateTime.Now;
                }

                await db.SaveChangesAsync();
            }
            const int delay = 8000;
            var embed = new EmbedBuilder();
            embed.WithDescription($"Glückwunsch! {author.Mention} du hast jetzt {eggs} {Constants.EggGoatR} gefangen!");
            embed.WithColor(new Color(90, 92, 96));
            CurrentMessages.Remove(message);
            await message.ModifyAsync(p =>
            {
                p.Content = "";
                p.Embed = embed.Build();
            });
            await Task.Delay(delay);
            await message.DeleteAsync();
        }
    }
}
