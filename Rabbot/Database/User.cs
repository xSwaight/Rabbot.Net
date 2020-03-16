using System;
using System.Collections.Generic;

namespace Rabbot.Database
{
    public partial class User
    {
        public User()
        {
            Attacks = new HashSet<Attacks>();
            CombiCombiUser = new HashSet<Combi>();
            CombiUser = new HashSet<Combi>();
            Musicrank = new HashSet<Musicrank>();
            Muteduser = new HashSet<Muteduser>();
            Namechanges = new HashSet<Namechanges>();
            Pot = new HashSet<Pot>();
            Userfeatures = new HashSet<Userfeatures>();
            Warning = new HashSet<Warning>();
        }

        public long Id { get; set; }
        public string Name { get; set; }
        public int Notify { get; set; }

        public virtual ICollection<Attacks> Attacks { get; set; }
        public virtual ICollection<Combi> CombiCombiUser { get; set; }
        public virtual ICollection<Combi> CombiUser { get; set; }
        public virtual ICollection<Musicrank> Musicrank { get; set; }
        public virtual ICollection<Muteduser> Muteduser { get; set; }
        public virtual ICollection<Namechanges> Namechanges { get; set; }
        public virtual ICollection<Pot> Pot { get; set; }
        public virtual ICollection<Userfeatures> Userfeatures { get; set; }
        public virtual ICollection<Warning> Warning { get; set; }
    }
}
