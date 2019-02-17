using System;
using System.Collections.Generic;

namespace DiscordBot_Core.Database
{
    public partial class Experience
    {
        public int Id { get; set; }
        public long? ServerId { get; set; }
        public long UserId { get; set; }
        public int? Exp { get; set; }
        public int Goats { get; set; }
        public int Trades { get; set; }
        public int Gain { get; set; }
        public DateTime? Lastdaily { get; set; }
        public DateTime? Lastmessage { get; set; }
    }
}
