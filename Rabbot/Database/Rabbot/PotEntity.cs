using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Rabbot.Database.Rabbot
{
    [Table("Pots")]
    public class PotEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Column(TypeName = "bigint(20)")]
        public ulong UserId { get; set; }
        public UserEntity User { get; set; }

        [Column(TypeName = "bigint(20)")]
        public ulong GuildId { get; set; }
        public GuildEntity Guild { get; set; }

        [Column]
        public int Goats { get; set; }
    }
}
