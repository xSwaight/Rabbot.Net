using System;
using System.Collections.Generic;

namespace Rabbot.Database
{
    public partial class User
    {
        public User()
        {
            Attacks = new HashSet<Attacks>();
            Musicrank = new HashSet<Musicrank>();
            Muteduser = new HashSet<Muteduser>();
            Pot = new HashSet<Pot>();
            Userfeatures = new HashSet<Userfeatures>();
            Warning = new HashSet<Warning>();
        }

        public long Id { get; set; }
        public string Name { get; set; }
        public int Notify { get; set; }

        public virtual ICollection<Attacks> Attacks { get; set; }
        public virtual ICollection<Musicrank> Musicrank { get; set; }
        public virtual ICollection<Muteduser> Muteduser { get; set; }
        public virtual ICollection<Pot> Pot { get; set; }
        public virtual ICollection<Userfeatures> Userfeatures { get; set; }
        public virtual ICollection<Warning> Warning { get; set; }
    }
}
