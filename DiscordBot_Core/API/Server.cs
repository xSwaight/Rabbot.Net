using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot_Core.API
{
    public class Server
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Player_limit { get; set; }
        public int Player_online { get; set; }
        public int State { get; set; }
        public string Last_update { get; set; }
    }
}
