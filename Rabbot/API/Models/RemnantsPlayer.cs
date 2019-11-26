using System;
using System.Collections.Generic;
using System.Text;

namespace Rabbot.API.Models
{
    public class RemnantsPlayer
    {
        public string Name { get; set; }
        public string Clan { get; set; }
        public int Level { get; set; }
        public int Matches { get; set; }
        public int Won { get; set; }
        public int Lost { get; set; }
        public string LastOnline { get; set; }
    }
}
