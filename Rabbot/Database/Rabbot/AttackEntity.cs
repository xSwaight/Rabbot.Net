using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Rabbot.Database.Rabbot
{
    [Table("Attacks")]
    public class AttackEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Column]
        public ulong UserId { get; set; }
        public UserEntity User { get; set; }

        [Column]
        public ulong GuildId { get; set; }
        public GuildEntity Guild { get; set; }

        [Column]
        public ulong TargetId { get; set; }
        public UserEntity Target { get; set; }

        [Column]
        public ulong ChannelId { get; set; }
        [Column]
        public ulong MessageId { get; set; }
        [Column]
        public DateTime EndTime { get; set; }
    }
}
