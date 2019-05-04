using System;
using System.Collections.Generic;

namespace Rabbot.Database
{
    public partial class Guild
    {
        public long ServerId { get; set; }
        public long? LogchannelId { get; set; }
        public long? NotificationchannelId { get; set; }
        public long? Botchannelid { get; set; }
        public long? TrashchannelId { get; set; }
        public long? StreamchannelId { get; set; }
        public int Notify { get; set; }
        public int Log { get; set; }
        public int Trash { get; set; }
        public int Level { get; set; }
    }
}
