using System;
using System.Collections.Generic;

namespace Rabbot.Database
{
    public partial class Items
    {
        public Items()
        {
            Inventory = new HashSet<Inventory>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public int Atk { get; set; }
        public int Def { get; set; }

        public virtual ICollection<Inventory> Inventory { get; set; }
    }
}
