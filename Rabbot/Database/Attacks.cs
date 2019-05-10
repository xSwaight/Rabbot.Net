using System;
using System.Collections.Generic;

namespace Rabbot.Database
{
    public partial class Attacks
    {
        public int Id { get; set; }
        public long? UserId { get; set; }
        public long? ServerId { get; set; }
        public long? ChannelId { get; set; }
        public long? MessageId { get; set; }
        public long? TargetId { get; set; }
        public DateTime? AttackEnds { get; set; }

        public virtual Guild Server { get; set; }
        public virtual User User { get; set; }
    }
}
