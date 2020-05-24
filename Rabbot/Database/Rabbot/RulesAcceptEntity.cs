using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Rabbot.Database.Rabbot
{
    [Table("RulesAcceptSettings")]
    public class RulesAcceptEntity
    {
        [Key]
        public ulong GuildId { get; set; }
        public GuildEntity Guild { get; set; }

        [Column]
        public ulong ChannelId { get; set; }

        [Column]
        public ulong RoleId { get; set; }

        [Column]
        public string AcceptWord { get; set; }
    }
}
