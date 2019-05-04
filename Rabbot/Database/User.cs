using System;
using System.Collections.Generic;

namespace Rabbot.Database
{
    public partial class User
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public int Notify { get; set; }
    }
}
