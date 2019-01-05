using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot_Core.API.Models
{
    public class Clan
    {
        public bool Success { get; set; }
        public string Name { get; set; }
        public string Master { get; set; }
        public int Member_count { get; set; }
        public string Announcement { get; set; }
        public string Description{ get; set; }
        public int Views { get; set; }
        public int Favorites { get; set; }
        public int Fame { get; set; }
        public int Hate { get; set; }
    }
}
