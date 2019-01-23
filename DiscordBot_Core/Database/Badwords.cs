using System;
using System.Collections.Generic;

namespace DiscordBot_Core.Database
{
    public partial class Badwords
    {
        public int Id { get; set; }
        public string BadWord { get; set; }
    }
}
