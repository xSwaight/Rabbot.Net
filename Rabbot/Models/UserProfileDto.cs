using System;
using System.Collections.Generic;
using System.Text;

namespace Rabbot.Models
{
    public class UserProfileDto
    {
        public string Name { get; set; }
        public string AvatarUrl { get; set; }
        public string Level { get; set; }
        public string Exp { get; set; }
        public string Rank { get; set; }
        public string Goats { get; set; }
        public double Percent { get; set; }
        public string LevelInfo { get; set; }
    }
}
