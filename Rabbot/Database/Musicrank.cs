using System;
using System.Collections.Generic;

namespace Rabbot.Database
{
    public partial class Musicrank
    {
        public int Id { get; set; }
        public ulong UserId { get; set; }
        public ulong ServerId { get; set; }
        public long Sekunden { get; set; }
        public DateTime? Date { get; set; }

        public virtual Guild Server { get; set; }
        public virtual User User { get; set; }
    }
}
