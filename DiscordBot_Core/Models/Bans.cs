using System;
using System.Collections.Generic;

namespace DiscordBot_Core.Models
{
    public partial class Bans
    {
        public int Id { get; set; }
        public long UserId { get; set; }
        public long ServerId { get; set; }
        public DateTime? Duration { get; set; }
        public string Roles { get; set; }
    }
}
