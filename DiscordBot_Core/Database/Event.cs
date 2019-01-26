using System;
using System.Collections.Generic;

namespace DiscordBot_Core.Database
{
    public partial class Event
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Status { get; set; }
    }
}
