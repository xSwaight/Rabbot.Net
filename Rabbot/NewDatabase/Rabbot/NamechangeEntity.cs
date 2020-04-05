using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Rabbot.NewDatabase.Rabbot
{
    [Table("Namechanges")]
    public class NamechangeEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Column(TypeName = "bigint(20)")]
        public ulong UserId { get; set; }
        public UserEntity User { get; set; }

        [Column]
        public string NewName { get; set; }
        [Column]
        public DateTime Date { get; set; }
    }
}
