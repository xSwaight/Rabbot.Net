using System;
using System.Collections.Generic;

namespace Rabbot.Database
{
    public partial class Roles
    {
        public int Id { get; set; }
        public ulong? ServerId { get; set; }
        public ulong? RoleId { get; set; }
        public string Description { get; set; }

        public virtual Guild Server { get; set; }
    }
}
