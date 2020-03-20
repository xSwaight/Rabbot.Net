using System;
using System.Collections.Generic;

namespace Rabbot.Database
{
    public partial class Namechanges
    {
        public int Id { get; set; }
        public ulong UserId { get; set; }
        public string NewName { get; set; }
        public DateTime Date { get; set; }

        public virtual User User { get; set; }
    }
}
