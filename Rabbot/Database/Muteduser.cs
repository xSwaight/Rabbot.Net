using System;
using System.Collections.Generic;

namespace Rabbot.Database
{
    public partial class Muteduser
    {
        public int Id { get; set; }
        public long UserId { get; set; }
        public long ServerId { get; set; }
        public DateTime? Duration { get; set; }
        public string Roles { get; set; }

        public virtual Guild Server { get; set; }
        public virtual User User { get; set; }
    }
}
