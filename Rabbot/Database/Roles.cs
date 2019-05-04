using System;
using System.Collections.Generic;

namespace Rabbot.Database
{
    public partial class Roles
    {
        public int Id { get; set; }
        public long? ServerId { get; set; }
        public long? RoleId { get; set; }
        public string Description { get; set; }
    }
}
