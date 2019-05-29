using System;
using System.Collections.Generic;

namespace Rabbot.Database
{
    public partial class Guild
    {
        public Guild()
        {
            Attacks = new HashSet<Attacks>();
            Musicrank = new HashSet<Musicrank>();
            Muteduser = new HashSet<Muteduser>();
            Pot = new HashSet<Pot>();
            Roles = new HashSet<Roles>();
            Userfeatures = new HashSet<Userfeatures>();
            Warning = new HashSet<Warning>();
        }

        public long ServerId { get; set; }
        public long? LogchannelId { get; set; }
        public long? NotificationchannelId { get; set; }
        public long? Botchannelid { get; set; }
        public long? TrashchannelId { get; set; }
        public long? StreamchannelId { get; set; }
        public int Notify { get; set; }
        public int Log { get; set; }
        public int Trash { get; set; }
        public int Level { get; set; }

        public virtual ICollection<Attacks> Attacks { get; set; }
        public virtual ICollection<Musicrank> Musicrank { get; set; }
        public virtual ICollection<Muteduser> Muteduser { get; set; }
        public virtual ICollection<Pot> Pot { get; set; }
        public virtual ICollection<Roles> Roles { get; set; }
        public virtual ICollection<Userfeatures> Userfeatures { get; set; }
        public virtual ICollection<Warning> Warning { get; set; }
    }
}
