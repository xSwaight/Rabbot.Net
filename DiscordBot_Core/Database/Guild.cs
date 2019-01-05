using System;
using System.Collections.Generic;

namespace DiscordBot_Core.Database
{
    public partial class Guild
    {
        public long ServerId { get; set; }
        public long? LogchannelId { get; set; }
        public long? NotificationchannelId { get; set; }
        public int Notify { get; set; }
    }
}
