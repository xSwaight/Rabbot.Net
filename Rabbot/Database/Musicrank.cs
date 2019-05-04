using System;
using System.Collections.Generic;

namespace Rabbot.Database
{
    public partial class Musicrank
    {
        public int Id { get; set; }
        public long UserId { get; set; }
        public long ServerId { get; set; }
        public long Sekunden { get; set; }
        public DateTime? Date { get; set; }
    }
}
