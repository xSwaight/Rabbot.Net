using System;
using System.Collections.Generic;

namespace DiscordBot_Core.Database
{
    public partial class Warning
    {
        public int Id { get; set; }
        public long UserId { get; set; }
        public long ServerId { get; set; }
        public DateTime ActiveUntil { get; set; }
        public int Counter { get; set; }
    }
}
