using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Rabbot.Database.Rabbot
{
    [Table("Items")]
    public class ItemEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Column]
        public string Name { get; set; }
        [Column]
        public int Atk { get; set; }
        [Column]
        public int Def { get; set; }

        public List<InventoryEntity> Inventory { get; set; } = new List<InventoryEntity>();
    }
}
