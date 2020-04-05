using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Rabbot.NewDatabase.Rabbot
{
    [Table("Inventorys")]
    public class InventoryEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Column]
        public int FeatureId { get; set; }
        public FeatureEntity Feature { get; set; }

        [Column]
        public int ItemId { get; set; }
        public ItemEntity Item { get; set; }

        [Column]
        public int Durability { get; set; }
        [Column]
        public DateTime ExpiryDate { get; set; }
    }
}
