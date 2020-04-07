using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rabbot.Database.Rabbot
{
    public class EasterEventEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public ulong MessageId { get; set; }
        [Column]
        public DateTime SpawnTime { get; set; }
        [Column]
        public ulong? UserId { get; set; }
        public UserEntity User { get; set; }

        [Column]
        public DateTime? CatchTime { get; set; }
        [Column]
        public DateTime? DespawnTime { get; set; }
    }
}
