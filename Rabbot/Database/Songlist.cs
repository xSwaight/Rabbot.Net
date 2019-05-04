using System;
using System.Collections.Generic;

namespace Rabbot.Database
{
    public partial class Songlist
    {
        public int Id { get; set; }
        public string Link { get; set; }
        public string Name { get; set; }
        public int Active { get; set; }
    }
}
