using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Rabbot.Database.Rabbot
{
    [Table("AvatarLogs")]
    public class AvatarLogEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Column]
        public ulong UserId { get; set; }
        [Column]
        public string AvatarId { get; set; }
        [Column]
        public DateTime ChangeDate { get; set; }
    }
}
