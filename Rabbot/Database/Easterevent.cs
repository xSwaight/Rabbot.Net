using System;
using System.Collections.Generic;

namespace Rabbot.Database
{
    public partial class Easterevent
    {
        public ulong MessageId { get; set; }
        public DateTime SpawnTime { get; set; }
        public ulong? UserId { get; set; }
        public DateTime? CatchTime { get; set; }
        public DateTime? DespawnTime { get; set; }

        public virtual User User { get; set; }
    }
}
