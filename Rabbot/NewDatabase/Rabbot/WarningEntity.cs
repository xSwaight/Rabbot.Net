using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Rabbot.NewDatabase.Rabbot
{
    [Table("Warnings")]
    public class WarningEntity
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
        public DateTime Until { get; set; }
        [Column]
        public int Counter { get; set; }
    }
}
