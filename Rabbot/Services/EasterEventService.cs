using Discord;
using Discord.WebSocket;
using Rabbot.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rabbot.Services
{
    public class EasterEventService
    {
        private static readonly ILogger _logger = Log.ForContext(Serilog.Core.Constants.SourceContextPropertyName, nameof(EasterEventService));

        private List<EasterEvent> _events;
        private DiscordSocketClient _client;
        private Dictionary<ulong, ulong> _guildAnnouncementChannels;

        public EasterEventService(DiscordSocketClient client)
        {
            _client = client;
            _events = new List<EasterEvent>();
            _guildAnnouncementChannels = new Dictionary<ulong, ulong>();
            _client.ReactionAdded += ReactionAdded;
        }

        private async Task ReactionAdded(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (reaction.Emote.Name != Constants.EggGoatL.Name || !reaction.User.IsSpecified || reaction.User.Value.IsBot)
                return;

            var guild = (channel as SocketGuildChannel).Guild;
            var @event = _events.FirstOrDefault(p => p.Guild.Id == guild.Id);

            if (@event == null)
                return;

            if (!@event.CurrentMessages.Any(p => p.Id == reaction.MessageId))
                return;

            await @event.CatchEgg(reaction.UserId, reaction.MessageId);
        }

        public void RegisterServers(params ulong[] guildIds)
        {
            foreach (var guildId in guildIds)
            {
                var guild = _client.Guilds.FirstOrDefault(p => p.Id == guildId);
                if (guild != null)
                    _events.Add(new EasterEvent(guild));
            }
        }

        public void RegisterAnnouncementChannel(ulong serverId, ulong channelId)
        {
            _guildAnnouncementChannels.Add(serverId, channelId);
        }

        public async Task StartEventAsync()
        {
            var currentDate = DateTime.Now;

            if (currentDate < Constants.StartTime)
                while (true)
                {
                    if (DateTime.Now > Constants.StartTime)
                    {
                        _logger.Information($"Easter Event active!");
                        foreach (var item in _guildAnnouncementChannels)
                        {
                            var guild = _client.Guilds.FirstOrDefault(p => p.Id == item.Key);
                            if (guild == null)
                                continue;

                            var textchannel = guild.TextChannels.FirstOrDefault(p => p.Id == item.Value);
                            if (textchannel == null)
                                continue;

                            await textchannel.SendMessageAsync($"Hallo {guild.EveryoneRole.Mention}\nDorq hat sich als Osterhase verkleidet und versteckt sich nun ab und an in allen Channeln auf diesem Server! Sucht ihn und fangt ihn über die Reaction ein! Hier ein Phantombild: {Constants.EggGoatR}\nWer bis zum {Constants.EndTime.ToFormattedString()} Uhr die meisten {Constants.EggGoatR} gefunden hat bekommt eine Custom Rolle und eine Überraschung!\nEine aktuelle Rangliste kann man mit dem Command {Config.Bot.CmdPrefix}osterrank einsehen.\n\nFrohe Ostern wünscht euch das Swaight-Team");
                        }
                        break;
                    }
                    await Task.Delay(1000);
                }

            while (true)
            {
                foreach (var @event in _events)
                {
                    await @event.SendEasterEgg();
                }

                var rndNumber = new Random().Next(Constants.EasterMinRespawnTime, Constants.EasterMaxRespawnTime + 1);
                await Task.Delay(rndNumber * 60000);
                if (DateTime.Now > Constants.EndTime)
                    break;
            }
        }

    }
}
