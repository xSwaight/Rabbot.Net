using System;
using System.Collections.Generic;

namespace Rabbot.Database
{
    public partial class Inventory
    {
        public int Id { get; set; }
        public int FeatureId { get; set; }
        public int ItemId { get; set; }
        public int Durability { get; set; }
        public DateTime? ExpirationDate { get; set; }

        public virtual Userfeatures Feature { get; set; }
        public virtual Items Item { get; set; }
    }
}
