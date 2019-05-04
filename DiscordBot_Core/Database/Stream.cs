using System;
using System.Collections.Generic;

namespace DiscordBot_Core.Database
{
    public partial class Stream
    {
        public long StreamId { get; set; }
        public long TwitchUserId { get; set; }
        public DateTime StartTime { get; set; }
        public string Title { get; set; }
    }
}
