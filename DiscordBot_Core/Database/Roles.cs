using System;
using System.Collections.Generic;

namespace DiscordBot_Core.Database
{
    public partial class Roles
    {
        public int Id { get; set; }
        public long? ServerId { get; set; }
        public long? RoleId { get; set; }
        public string Description { get; set; }
    }
}
