using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Rabbot.Database.Rabbot
{
    [Table("MutedUsers")]
    public class MutedUserEntity
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
        public DateTime Duration { get; set; }
        [Column]
        public string Roles { get; set; }
    }
}
