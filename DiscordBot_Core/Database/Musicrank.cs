using System;
using System.Collections.Generic;

namespace DiscordBot_Core.Database
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
