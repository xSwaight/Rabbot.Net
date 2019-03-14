using System;
using System.Collections.Generic;

namespace DiscordBot_Core.Database
{
    public partial class Pot
    {
        public int Id { get; set; }
        public long? UserId { get; set; }
        public long? ServerId { get; set; }
        public int Goats { get; set; }
    }
}
