using System;
using System.Collections.Generic;

namespace Rabbot.Database
{
    public partial class Attacks
    {
        public int Id { get; set; }
        public ulong? UserId { get; set; }
        public ulong? ServerId { get; set; }
        public ulong? ChannelId { get; set; }
        public ulong? MessageId { get; set; }
        public ulong? TargetId { get; set; }
        public DateTime? AttackEnds { get; set; }

        public virtual Guild Server { get; set; }
        public virtual User Target { get; set; }
        public virtual User User { get; set; }
    }
}
