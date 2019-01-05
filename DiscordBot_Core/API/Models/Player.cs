namespace DiscordBot_Core.API.Models
{
    public class Player
    {
        public bool Success { get; set; }
        public string Name { get; set; }
        public string Clan { get; set; }
        public int Level { get; set; }
        public string Levelbar { get; set; }
        public int Exp { get; set; }
        public int Playtime { get; set; }
        public double Tdrate { get; set; }
        public double Kdrate { get; set; }
        public int Matches_played { get; set; }
        public int Matches_won { get; set; }
        public int Matches_lost { get; set; }
        public string Last_online { get; set; }
        public int Views { get; set; }
        public int Favorites { get; set; }
        public int Fame { get; set; }
        public int Hate { get; set; }
    }
}
