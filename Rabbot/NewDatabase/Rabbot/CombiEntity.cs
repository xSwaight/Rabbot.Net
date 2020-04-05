using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Rabbot.NewDatabase.Rabbot
{
    public class CombiEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Column(TypeName = "bigint(20)")]
        public ulong GuildId { get; set; }
        public GuildEntity Guild { get; set; }

        [Column(TypeName = "bigint(20)")]
        public ulong UserId { get; set; }
        public UserEntity User { get; set; }

        [Column(TypeName = "bigint(20)")]
        public ulong CombiUserId { get; set; }
        public UserEntity CombiUser { get; set; }

        [Column]
        public bool Accepted { get; set; }
        [Column(TypeName = "bigint(20)")]
        public ulong MessageId { get; set; }
        [Column]
        public DateTime Date { get; set; }
    }
}
