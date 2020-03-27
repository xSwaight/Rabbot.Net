using System;
using System.Collections.Generic;
using System.Text;

namespace Rabbot.Models
{
    class PotUserDto
    {
        public ulong? UserId { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }
        public int Chance { get; set; }
    }
}
