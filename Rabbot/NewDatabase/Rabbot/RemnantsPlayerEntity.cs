using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Rabbot.NewDatabase.Rabbot
{
    [Table("Remnantsplayers")]
    public class RemnantsPlayerEntity
    {
        [Key]
        [Column(TypeName = "bigint(20)")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public ulong Id { get; set; }
        [Column]
        public int Playercount { get; set; }
        [Column]
        public DateTime Date { get; set; }
    }
}
