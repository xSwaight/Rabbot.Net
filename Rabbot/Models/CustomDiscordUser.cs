using System;
using System.Collections.Generic;
using System.Text;

namespace Rabbot.Models
{
    public class CustomDiscordUser
    {
        public ulong Id { get; set; }
        public string Username { get; set; }
        public string Avatar { get; set; }
        public string Discriminator { get; set; }
        public int Public_flags { get; set; }
    }
}
