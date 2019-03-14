using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot_Core.API.Models
{
    class PotUser
    {
        public long UserId { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }
        public int Chance { get; set; }
    }
}
