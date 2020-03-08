using System;
using System.Collections.Generic;

namespace Rabbot.Database
{
    public partial class Officialplayer
    {
        public long Id { get; set; }
        public int Playercount { get; set; }
        public DateTime? Date { get; set; }
    }
}
