using System;
using System.Collections.Generic;

namespace DiscordBot_Core.Database
{
    public partial class Items
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Atk { get; set; }
        public int Def { get; set; }
    }
}
