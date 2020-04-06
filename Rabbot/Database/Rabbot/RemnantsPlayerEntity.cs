using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Rabbot.Database.Rabbot
{
    [Table("Remnantsplayers")]
    public class RemnantsPlayerEntity
    {
        [Key]
        [Column]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public ulong Id { get; set; }
        [Column]
        public int Playercount { get; set; }
        [Column]
        public DateTime Date { get; set; }
    }
}
