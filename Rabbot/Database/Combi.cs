using System;
using System.Collections.Generic;

namespace Rabbot.Database
{
    public partial class Combi
    {
        public int Id { get; set; }
        public ulong ServerId { get; set; }
        public ulong UserId { get; set; }
        public ulong CombiUserId { get; set; }
        public bool? Accepted { get; set; }
        public ulong? MessageId { get; set; }
        public DateTime? Date { get; set; }

        public virtual User CombiUser { get; set; }
        public virtual Guild Server { get; set; }
        public virtual User User { get; set; }
    }
}
