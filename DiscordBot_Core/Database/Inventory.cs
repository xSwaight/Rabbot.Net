using System;
using System.Collections.Generic;

namespace DiscordBot_Core.Database
{
    public partial class Inventory
    {
        public int Id { get; set; }
        public int FeatureId { get; set; }
        public int ItemId { get; set; }
        public DateTime? Duration { get; set; }
    }
}
