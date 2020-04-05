using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rabbot.Database.Rabbot
{
    [Table("Users")]
    public class UserEntity
    {
        [Key]
        [Column(TypeName = "bigint(20)")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public ulong Id { get; set; }
        [Column]
        public string Name { get; set; }
        [Column]
        public bool Notify { get; set; }

        public List<FeatureEntity> Features { get; set; } = new List<FeatureEntity>();
        public List<WarningEntity> Warnings { get; set; } = new List<WarningEntity>();
        public List<PotEntity> Pots { get; set; } = new List<PotEntity>();
        public List<NamechangeEntity> Namechanges { get; set; } = new List<NamechangeEntity>();
        public List<CombiEntity> CombiCombiUsers { get; set; } = new List<CombiEntity>();
        public List<CombiEntity> CombiUsers { get; set; } = new List<CombiEntity>();
        public List<AttackEntity> AttackUsers { get; set; } = new List<AttackEntity>();
        public List<AttackEntity> AttackTargets { get; set; } = new List<AttackEntity>();
        public List<MusicrankEntity> Musicranks { get; set; } = new List<MusicrankEntity>();
        public List<MutedUserEntity> MutedUsers { get; set; } = new List<MutedUserEntity>();
    }
}
