using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rabbot.Database.Rabbot
{
    [Table("Guilds")]
    public class GuildEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public ulong GuildId { get; set; }
        [Column]
        public string GuildName { get; set; }
        [Column]
        public ulong? LogChannelId { get; set; }
        [Column]
        public ulong? NotificationChannelId { get; set; }
        [Column]
        public ulong? BotChannelId { get; set; }
        [Column]
        public ulong? TrashChannelId { get; set; }
        [Column]
        public ulong? StreamChannelId { get; set; }
        [Column]
        public ulong? LevelChannelId { get; set; }
        [Column]
        public bool Notify { get; set; }
        [Column]
        public bool Log { get; set; }
        [Column]
        public bool Trash { get; set; }
        [Column]
        public bool Level { get; set; }

        public List<FeatureEntity> Features { get; set; } = new List<FeatureEntity>();
        public List<WarningEntity> Warnings { get; set; } = new List<WarningEntity>();
        public List<PotEntity> Pots { get; set; } = new List<PotEntity>();
        public List<CombiEntity> Combis { get; set; } = new List<CombiEntity>();
        public List<BadWordEntity> BadWords { get; set; } = new List<BadWordEntity>();
        public List<MusicrankEntity> Musicranks { get; set; } = new List<MusicrankEntity>();
        public List<MutedUserEntity> MutedUsers { get; set; } = new List<MutedUserEntity>();
        public List<RoleEntity> Roles { get; set; } = new List<RoleEntity>();
        public List<AttackEntity> Attacks { get; set; } = new List<AttackEntity>();
    }
}
