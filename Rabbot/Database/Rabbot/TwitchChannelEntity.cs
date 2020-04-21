using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Rabbot.Database.Rabbot
{
    [Table("TwitchChannels")]
    public class TwitchChannelEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Column]
        public ulong GuildId { get; set; }
        public GuildEntity Guild { get; set; }

        [Column]
        public string ChannelName { get; set; }
    }
}
