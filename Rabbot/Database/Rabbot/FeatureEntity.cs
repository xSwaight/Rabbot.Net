using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Rabbot.Database.Rabbot
{
    [Table("Features")]
    public class FeatureEntity
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

        [Column]
        public bool HasLeft { get; set; }
        [Column]
        public int Exp { get; set; }
        [Column]
        public int Goats { get; set; }
        [Column]
        public int Eggs { get; set; }
        [Column]
        public int StreakLevel { get; set; }
        [Column]
        public int TodaysWords { get; set; }
        [Column]
        public int TotalWords { get; set; }
        [Column]
        public int CombiExp { get; set; }
        [Column]
        public int Wins { get; set; }
        [Column]
        public int Loses { get; set; }
        [Column]
        public int Trades { get; set; }
        [Column]
        public int Attacks { get; set; }
        [Column]
        public int Spins { get; set; }
        [Column]
        public int Gewinn { get; set; }
        [Column]
        public bool GainExp { get; set; }
        [Column]
        public DateTime LastDaily { get; set; }
        [Column]
        public DateTime LastMessage { get; set; }
        [Column]
        public bool Locked { get; set; }

        public List<InventoryEntity> Inventory { get; set; } = new List<InventoryEntity>();
    }
}
