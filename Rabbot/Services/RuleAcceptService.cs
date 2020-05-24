using Discord.Commands;
using Discord.WebSocket;
using Rabbot.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rabbot.Services
{
    public class RuleAcceptService
    {
        private readonly DiscordShardedClient _client;
        private readonly DatabaseService _db;
        public RuleAcceptService(DiscordShardedClient client)
        {
            _client = client;
            _db = DatabaseService.Instance;

            _client.MessageReceived += MessageReceived;
        }

        private async Task MessageReceived(SocketMessage message)
        {
            if (!(message is SocketUserMessage msg))
                return;

            var context = new ShardedCommandContext(_client, msg);
            if (!(context.User is SocketGuildUser user))
                return;

            using RabbotContext db = _db.Open();
            if (!db.Rule.Any(p => p.GuildId == context.Guild.Id))
                return;

            var ruleEntity = db.Rule.First(p => p.GuildId == context.Guild.Id);
            if (message.Channel.Id != ruleEntity.ChannelId)
                return;

            await message.DeleteAsync();
            if (!ruleEntity.AcceptWord.Equals(context.Message.Content, StringComparison.OrdinalIgnoreCase))
                return;

            var role = context.Guild.Roles.FirstOrDefault(p => p.Id == ruleEntity.RoleId);
            if (role == null)
                return;

            await user.AddRoleAsync(role);
        }
    }
}
