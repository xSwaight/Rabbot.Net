using System;
using System.Collections.Generic;

namespace Rabbot.Database
{
    public partial class Userfeatures
    {
        public Userfeatures()
        {
            Inventory = new HashSet<Inventory>();
        }

        public int Id { get; set; }
        public long? ServerId { get; set; }
        public long UserId { get; set; }
        public bool? HasLeft { get; set; }
        public int? Exp { get; set; }
        public int Goats { get; set; }
        public int Wins { get; set; }
        public int Loses { get; set; }
        public int Trades { get; set; }
        public int? Attacks { get; set; }
        public int Spins { get; set; }
        public int Gewinn { get; set; }
        public int Gain { get; set; }
        public DateTime? Lastdaily { get; set; }
        public DateTime? Lastmessage { get; set; }
        public DateTime? NamechangeUntil { get; set; }
        public int Locked { get; set; }

        public virtual Guild Server { get; set; }
        public virtual User User { get; set; }
        public virtual ICollection<Inventory> Inventory { get; set; }
    }
}
