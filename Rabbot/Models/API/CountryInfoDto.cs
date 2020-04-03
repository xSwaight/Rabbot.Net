using System;
using System.Collections.Generic;
using System.Text;

namespace Rabbot.Models.API
{
    public class CountryInfoDto
    {
        public int? _Id { get; set; }
        public double? Lat { get; set; }
        public double? Long { get; set; }
        public string Flag { get; set; }
        public string Iso3 { get; set; }
        public string Iso2 { get; set; }
    }
}
