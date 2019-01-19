using Discord;
using Discord.WebSocket;
using DiscordBot_Core.Database;
using DiscordBot_Core.ImageGenerator;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot_Core.Systems
{
    class Userlevel
    {
        public SocketMessage dcMessage { get; set; }
        public SocketGuild dcGuild { get; set; }
        public User User { get; set; }
        public Guild Guild { get; set; }
        public Experience EXP { get; set; }
        public uint OldLevel { get; set; }
        public uint NewLevel { get; set; }

        public Userlevel(SocketMessage msg)
        {
            dcMessage = msg;
            dcGuild = ((SocketGuildChannel)msg.Channel).Guild;
            using (swaightContext db = new swaightContext())
            {
                User = db.User.Where(p => p.Id == (long)msg.Author.Id).FirstOrDefault() ?? db.User.AddAsync(new User { Id = (long)msg.Author.Id, Name = msg.Author.Username + "#" + msg.Author.Discriminator }).Result.Entity;
                User.Name = msg.Author.Username + "#" + msg.Author.Discriminator;
                Guild = db.Guild.Where(p => p.ServerId == (long)dcGuild.Id).FirstOrDefault() ?? db.Guild.AddAsync(new Guild {ServerId = (long)dcGuild.Id }).Result.Entity;
                EXP = db.Experience.Where(p => (ulong)p.UserId == msg.Author.Id).FirstOrDefault() ?? db.Experience.AddAsync(new Experience { Exp = 0, UserId = (long)msg.Author.Id }).Result.Entity;
                OldLevel = Helper.GetLevel(EXP.Exp);
                int textLenght = msg.Content.ToString().Replace("*", string.Empty).Count();
                if (textLenght >= 50)
                    textLenght = 50;
                EXP.Exp += textLenght * Config.level.expMultiplier;
                NewLevel = Helper.GetLevel(EXP.Exp);
                db.SaveChanges();
            }
        }

        public async Task SendLevelUp()
        {
            if (NewLevel > OldLevel && Guild.Level == 1)
            {
                var template = new HtmlTemplate(Directory.GetCurrentDirectory() + "/LevelUp/levelup.html");
                var dcUser = dcGuild.Users.Where(p => p.Id == dcMessage.Author.Id).FirstOrDefault();
                string name = (dcUser as IGuildUser).Nickname ?? dcMessage.Author.Username;
                var html = template.Render(new
                {
                    NAME = name,
                    LEVEL = NewLevel.ToString()
                });

                var path = HtmlToImage.Generate(Helper.RemoveSpecialCharacters(name), html, 300, 100);
                await dcMessage.Channel.SendFileAsync(path);
                File.Delete(path);
            }

            if(NewLevel > OldLevel)
            {
                await SetRoles(true);
            }
        }

        public async Task SetRoles(bool currentServer = false)
        {
            foreach (var mutualGuild in dcMessage.Author.MutualGuilds)
            {
                if (mutualGuild.Id != dcGuild.Id && currentServer)
                    continue;
                var roleS4 = mutualGuild.Roles.Where(p => p.Name == "S4").FirstOrDefault();
                var rolePro = mutualGuild.Roles.Where(p => p.Name == "Pro").FirstOrDefault();
                var roleSemi = mutualGuild.Roles.Where(p => p.Name == "Semi").FirstOrDefault();
                var roleAmateur = mutualGuild.Roles.Where(p => p.Name == "Amateur").FirstOrDefault();
                var roleRookie = mutualGuild.Roles.Where(p => p.Name == "Rookie").FirstOrDefault();

                if (roleS4 != null && rolePro != null && roleSemi != null && roleAmateur != null && roleRookie != null)
                {
                    var myUser = mutualGuild.Users.Where(p => p.Id == dcMessage.Author.Id).FirstOrDefault();
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
