using System;
using System.Collections.Generic;
using System.Text;

namespace Rabbot.Models
{
    public class CoronaStatsDto
    {
        public string Country { get; set; }
        public CountryInfoDto CountryInfo { get; set; }
        public int? Cases { get; set; }
        public int? TodayCases { get; set; }
        public int? Deaths { get; set; }
        public int? TodayDeaths { get; set; }
        public int? Recovered { get; set; }
        public int? Active { get; set; }
        public int? Critical { get; set; }
        public double? CasesPerOneMillion { get; set; }
        public double? DeathsPerOneMillion { get; set; }

        public override string ToString()
        {
            return Country;
        }
    }
}
