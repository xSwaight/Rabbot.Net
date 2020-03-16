using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Rabbot.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rabbot.Models
{
    public class DatabaseUser
    {
        public SocketGuildUser DiscordUser { get; set; }
        public string Name { get; set; }
        public ulong Id { get; set; }
        public bool Notify { get; set; }
        public bool HasLeft { get; set; }
        public int Exp { get; set; }
        public int Goats { get; set; }
        public int CombiExp { get; set; }
        public int Wins { get; set; }
        public int Loses { get; set; }
        public int Trades { get; set; }
        public int Attacks { get; set; }
        public int Spins { get; set; }
        public int Reward { get; set; }
        public bool GainExp { get; set; }
        public DateTime? LastDaily { get; set; }
        public DateTime? LastMessage { get; set; }
        public bool IsLocked { get; set; }

        public DatabaseUser(IUser user)
        {
            DiscordUser = user as SocketGuildUser;
            GetData();
        }

        public DatabaseUser(SocketGuildUser user)
        {
            DiscordUser = user;
            GetData();
        }

        private void GetData()
        {
            using (swaightContext db = new swaightContext())
            {
                var dbUser = db.User.Include(p => p.Userfeatures).FirstOrDefault(p => p.Id == (long)DiscordUser.Id);
                var dbFeatures = dbUser.Userfeatures?.FirstOrDefault();

                if (dbFeatures != null && dbUser != null)
                {
                    Name = dbUser.Name;
                    Id = (ulong)dbUser.Id;
                    Notify = dbUser.Notify == 1 ? true : false;
                    HasLeft = dbFeatures.HasLeft ?? false;
                    Exp = dbFeatures.Exp ?? 0;
                    Goats = dbFeatures.Goats;
                    CombiExp = dbFeatures.CombiExp;
                    Wins = dbFeatures.Wins;
                    Loses = dbFeatures.Loses;
                    Trades = dbFeatures.Trades;
                    Attacks = dbFeatures.Attacks ?? 0;
                    Spins = dbFeatures.Spins;
                    Reward = dbFeatures.Gewinn;
                    GainExp = dbFeatures.Gain == 1 ? true : false;
                    LastDaily = dbFeatures.Lastdaily;
                    LastMessage = dbFeatures.Lastmessage;
                    IsLocked = dbFeatures.Locked == 1 ? true : false;
                }
                else
                {
                    var user = db.User.Add(new User { Name = $"{DiscordUser.Username}+{DiscordUser.Discriminator}", Id = (long)DiscordUser.Id }).Entity;
                    user.Userfeatures.Add(new Userfeatures());
                    db.SaveChanges();
                }
            }
        }
    }
}
