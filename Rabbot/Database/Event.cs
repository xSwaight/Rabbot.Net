using System;
using System.Collections.Generic;

namespace Rabbot.Database
{
    public partial class Event
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Multiplier { get; set; }
        public int Status { get; set; }
    }
}
