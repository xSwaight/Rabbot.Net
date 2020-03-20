using System;
using System.Collections.Generic;

namespace Rabbot.Database
{
    public partial class Badwords
    {
        public int Id { get; set; }
        public ulong? ServerId { get; set; }
        public string BadWord { get; set; }

        public virtual Guild Server { get; set; }
    }
}
