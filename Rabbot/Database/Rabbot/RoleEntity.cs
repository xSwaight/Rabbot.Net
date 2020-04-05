using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Rabbot.Database.Rabbot
{
    [Table("Roles")]
    public class RoleEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Column(TypeName = "bigint(20)")]
        public ulong GuildId { get; set; }
        public GuildEntity Guild { get; set; }

        [Column(TypeName = "bigint(20)")]
        public ulong RoleId { get; set; }
        [Column]
        public string Description { get; set; }
    }
}
