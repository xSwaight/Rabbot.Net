using System;
using System.Collections.Generic;

namespace DiscordBot_Core.Database
{
    public partial class Songlist
    {
        public int Id { get; set; }
        public string Link { get; set; }
        public string Name { get; set; }
        public int Active { get; set; }
    }
}
