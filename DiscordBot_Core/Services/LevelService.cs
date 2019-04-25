using Discord;
using Discord.WebSocket;
using DiscordBot_Core.Database;
using DiscordBot_Core.ImageGenerator;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordBot_Core.Services
{
    class LevelService
    {
        public SocketMessage dcMessage { get; set; }
        public SocketGuild dcGuild { get; set; }
        public User User { get; set; }
        public Guild Guild { get; set; }
        public Userfeatures EXP { get; set; }
        public uint OldLevel { get; set; }
        public uint NewLevel { get; set; }

        public LevelService(SocketMessage msg)
        {
            dcMessage = msg;
            dcGuild = ((SocketGuildChannel)msg.Channel).Guild;
            using (swaightContext db = new swaightContext())
            {
                User = db.User.Where(p => p.Id == (long)msg.Author.Id).FirstOrDefault() ?? db.User.AddAsync(new User { Id = (long)msg.Author.Id, Name = msg.Author.Username + "#" + msg.Author.Discriminator }).Result.Entity;
                User.Name = msg.Author.Username + "#" + msg.Author.Discriminator;
                Guild = db.Guild.Where(p => p.ServerId == (long)dcGuild.Id).FirstOrDefault() ?? db.Guild.AddAsync(new Guild { ServerId = (long)dcGuild.Id }).Result.Entity;
                EXP = db.Userfeatures.Where(p => (ulong)p.UserId == msg.Author.Id && p.ServerId == (int)dcGuild.Id).FirstOrDefault() ?? db.Userfeatures.AddAsync(new Userfeatures { Exp = 0, UserId = (long)msg.Author.Id, ServerId = (long)dcGuild.Id }).Result.Entity;
                OldLevel = Helper.GetLevel(EXP.Exp);
                string myMessage = Helper.MessageReplace(msg.Content);
                int textLenght = myMessage.Count();
                Random rnd = new Random();
                int random = rnd.Next(1, 11);
                if (textLenght <= 2)
                    random = 2;
                int exp = 0;
                if (textLenght >= 3 && textLenght < 10 && random > 1)
                    exp = rnd.Next(10, 21);
                if (textLenght >= 10 && textLenght < 20 && random > 1)
                    exp = rnd.Next(20, 31);
                if (textLenght >= 20 && textLenght < 30 && random > 1)
                    exp = rnd.Next(30, 41);
                if (textLenght >= 30 && textLenght < 40 && random > 1)
                    exp = rnd.Next(40, 51);
                if (textLenght >= 40 && textLenght <= 50 && random > 1)
                    exp = rnd.Next(40, 61);
                if (textLenght >= 51 || random == 1)
                    exp = rnd.Next(60, 100);

                var chance = rnd.Next(1, 6);
                if (chance == 3)
                {
                    exp *= rnd.Next(1, 11);
                }

                int multiplier = 1;
                var myEvent = db.Event.FirstOrDefault();
                if (myEvent.Status == 1)
                    multiplier = 2;

                var ranks = db.Musicrank.Where(p => p.ServerId == (long)dcGuild.Id && p.Date.Value.ToShortDateString() == DateTime.Now.ToShortDateString()).OrderByDescending(p => p.Sekunden);
                int rank = 0;
                foreach (var Rank in ranks)
                {
                    rank++;
                    if (Rank.UserId == (long)msg.Author.Id)
                        break;
                }
                double dblExp = exp;
                if (rank == 1)
                    exp = (int)(dblExp * 1.8);
                if (rank == 2)
                    exp = (int)(dblExp * 1.5);
                if (rank == 3)
                    exp = (int)(dblExp * 1.3);

                if (EXP.Gain == 1)
                    EXP.Exp += exp * multiplier;
                NewLevel = Helper.GetLevel(EXP.Exp);
                EXP.Lastmessage = DateTime.Now;
                db.SaveChanges();
            }
        }

        public async Task SendLevelUp()
        {
            if (NewLevel > OldLevel && Guild.Level == 1)
            {
                var template = new HtmlTemplate(Directory.GetCurrentDirectory() + "/RabbotThemeNeon/levelup.html");
                var dcUser = dcGuild.Users.Where(p => p.Id == dcMessage.Author.Id).FirstOrDefault();
                string name = (dcUser as IGuildUser).Nickname ?? dcMessage.Author.Username;
                var html = template.Render(new
                {
                    NAME = name,
                    LEVEL = NewLevel.ToString()
                });

                var path = HtmlToImage.Generate(Helper.RemoveSpecialCharacters(name) + "Level_Up", html, 300, 100);
                await dcMessage.Channel.SendFileAsync(path);
                File.Delete(path);
            }

            if (NewLevel > OldLevel)
            {
                await SetRoles();
            }
        }

        public async Task SetRoles()
        {
            using (swaightContext db = new swaightContext())
            {
                var roles = db.Roles.Where(p => p.ServerId == (long)dcGuild.Id);

                var S4Id = roles.Where(x => x.Description == "S4").FirstOrDefault() ?? new Roles {ServerId = (long)dcGuild.Id, RoleId = 0, Description = "S4" };
                var ProId = roles.Where(x => x.Description == "Pro").FirstOrDefault() ?? new Roles { ServerId = (long)dcGuild.Id, RoleId = 0, Description = "ProId" };
                var SemiId = roles.Where(x => x.Description == "Semi").FirstOrDefault() ?? new Roles { ServerId = (long)dcGuild.Id, RoleId = 0, Description = "SemiId" };
                var AmateurId = roles.Where(x => x.Description == "Amateur").FirstOrDefault() ?? new Roles { ServerId = (long)dcGuild.Id, RoleId = 0, Description = "AmateurId" };
                var RookieId = roles.Where(x => x.Description == "Rookie").FirstOrDefault() ?? new Roles { ServerId = (long)dcGuild.Id, RoleId = 0, Description = "RookieId" };

                var roleS4 = dcGuild.Roles.Where(p => p.Id == (ulong)S4Id.RoleId).FirstOrDefault();
                var rolePro = dcGuild.Roles.Where(p => p.Id == (ulong)ProId.RoleId).FirstOrDefault();
                var roleSemi = dcGuild.Roles.Where(p => p.Id == (ulong)SemiId.RoleId).FirstOrDefault();
                var roleAmateur = dcGuild.Roles.Where(p => p.Id == (ulong)AmateurId.RoleId).FirstOrDefault();
                var roleRookie = dcGuild.Roles.Where(p => p.Id == (ulong)RookieId.RoleId).FirstOrDefault();

                if (roleS4 != null && rolePro != null && roleSemi != null && roleAmateur != null && roleRookie != null)
                {
                    var myUser = dcGuild.Users.Where(p => p.Id == dcMessage.Author.Id).FirstOrDefault();
                    if (NewLevel < 1)
                    {
                        if (myUser.Roles.Where(p => p.Name == "S4" || p.Name == "Pro" || p.Name == "Semi" || p.Name == "Amateur" || p.Name == "Rookie").FirstOrDefault() != null)
                        {
                            await myUser.RemoveRoleAsync(roleS4);
                            await myUser.RemoveRoleAsync(rolePro);
                            await myUser.RemoveRoleAsync(roleSemi);
                            await myUser.RemoveRoleAsync(roleAmateur);
                            await myUser.RemoveRoleAsync(roleRookie);
                        }
                    }
                    if (NewLevel >= 1 && NewLevel <= 19)
                    {
                        if (myUser.Roles.Where(p => p.Name == "S4" || p.Name == "Pro" || p.Name == "Semi" || p.Name == "Amateur").FirstOrDefault() != null)
                        {
                            await myUser.RemoveRoleAsync(roleS4);
                            await myUser.RemoveRoleAsync(rolePro);
                            await myUser.RemoveRoleAsync(roleSemi);
                            await myUser.RemoveRoleAsync(roleAmateur);
                        }
                        if (myUser.Roles.Where(p => p.Name == "Rookie").FirstOrDefault() == null)
                            await myUser.AddRoleAsync(roleRookie);
                    }
                    if (NewLevel >= 20 && NewLevel <= 39)
                    {
                        if (myUser.Roles.Where(p => p.Name == "Rookie").FirstOrDefault() != null)
                            await myUser.RemoveRoleAsync(roleRookie);
                        if (myUser.Roles.Where(p => p.Name == "S4" || p.Name == "Pro" || p.Name == "Semi").FirstOrDefault() != null)
                        {
                            await myUser.RemoveRoleAsync(roleS4);
                            await myUser.RemoveRoleAsync(rolePro);
                            await myUser.RemoveRoleAsync(roleSemi);
                        }
                        if (myUser.Roles.Where(p => p.Name == "Amateur").FirstOrDefault() == null)
                            await myUser.AddRoleAsync(roleAmateur);
                    }
                    if (NewLevel >= 40 && NewLevel <= 59)
                    {
                        if (myUser.Roles.Where(p => p.Name == "Amateur").FirstOrDefault() != null)
                            await myUser.RemoveRoleAsync(roleAmateur);
                        if (myUser.Roles.Where(p => p.Name == "S4" || p.Name == "Pro" || p.Name == "Rookie").FirstOrDefault() != null)
                        {
                            await myUser.RemoveRoleAsync(roleS4);
                            await myUser.RemoveRoleAsync(rolePro);
                            await myUser.RemoveRoleAsync(roleRookie);
                        }
                        if (myUser.Roles.Where(p => p.Name == "Semi").FirstOrDefault() == null)
                            await myUser.AddRoleAsync(roleSemi);
                    }
                    if (NewLevel >= 60 && NewLevel <= 79)
                    {
                        if (myUser.Roles.Where(p => p.Name == "Semi").FirstOrDefault() != null)
                            await myUser.RemoveRoleAsync(roleSemi);
                        if (myUser.Roles.Where(p => p.Name == "S4" || p.Name == "Amateur" || p.Name == "Rookie").FirstOrDefault() != null)
                        {
                            await myUser.RemoveRoleAsync(roleS4);
                            await myUser.RemoveRoleAsync(roleAmateur);
                            await myUser.RemoveRoleAsync(roleRookie);
                        }
                        if (myUser.Roles.Where(p => p.Name == "Pro").FirstOrDefault() == null)
                            await myUser.AddRoleAsync(rolePro);
                    }
                    if (NewLevel >= 80)
                    {
                        if (myUser.Roles.Where(p => p.Name == "Pro").FirstOrDefault() != null)
                            await myUser.RemoveRoleAsync(rolePro);
                        if (myUser.Roles.Where(p => p.Name == "Semi" || p.Name == "Amateur" || p.Name == "Rookie").FirstOrDefault() != null)
                        {
                            await myUser.RemoveRoleAsync(roleSemi);
                            await myUser.RemoveRoleAsync(roleAmateur);
                            await myUser.RemoveRoleAsync(roleRookie);
                        }
                        if (myUser.Roles.Where(p => p.Name == "S4").FirstOrDefault() == null)
                            await myUser.AddRoleAsync(roleS4);
                    }
                }
            }
        }
    }
}
