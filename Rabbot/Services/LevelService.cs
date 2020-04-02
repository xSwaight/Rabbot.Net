using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Rabbot.Database;
using Rabbot.ImageGenerator;
using Serilog;
using Serilog.Core;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Rabbot.Services
{
    public class LevelService
    {
        private static readonly ILogger _logger = Log.ForContext(Serilog.Core.Constants.SourceContextPropertyName, nameof(LevelService));
        private readonly StreakService _streakService;

        public LevelService(StreakService streakService)
        {
            _streakService = streakService;
        }

        public (string bonusInfo, int bonusPercent) GetBonusEXP(rabbotContext db, SocketGuildUser user)
        {
            var feature = db.Userfeatures.Include(p => p.Inventory).FirstOrDefault(p => p.UserId == user.Id && p.ServerId == user.Guild.Id);
            if (feature == null)
                return ("Kein Bonus :(", 0);

            int bonusPercent = 0;
            string bonusInfo = string.Empty;
            if (user.Roles.Where(p => p.Name == "Nitro Booster" || p.Name == "Twitch Sub" || p.Name == "YouTube Mitglied").Any())
            {
                bonusPercent += 50;
                bonusInfo += $"**+50% EXP** (Supporter Bonus)\n";
            }
            if (feature.Inventory.FirstOrDefault(p => p.ItemId == 3 && p.ExpirationDate > DateTime.Now) != null)
            {
                bonusPercent += 50;
                bonusInfo += $"**+50% EXP** (EXP +50% Item)\n";
            }

            var ranks = db.Musicrank.Where(p => p.ServerId == user.Guild.Id && p.Date.Value.ToShortDateString() == DateTime.Now.ToShortDateString()).OrderByDescending(p => p.Sekunden);
            int rank = 0;
            foreach (var Rank in ranks)
            {
                rank++;
                if (Rank.UserId == user.Id)
                    break;
            }

            if (rank == 1)
            {
                bonusPercent += 50;
                bonusInfo += $"**+50% EXP** (Musicrank 1. Platz)\n";
            }
            if (rank == 2)
            {
                bonusPercent += 30;
                bonusInfo += $"**+30% EXP** (Musicrank 2. Platz)\n";
            }
            if (rank == 3)
            {
                bonusPercent += 10;
                bonusInfo += $"**+10% EXP** (Musicrank 3. Platz)\n";
            }

            if (db.Event.Where(p => p.Status == 1).Any())
            {
                var myEvent = db.Event.FirstOrDefault(p => p.Status == 1);
                bonusPercent += myEvent.BonusPercent;
                bonusInfo += $" **+{myEvent.BonusPercent}% EXP** ({myEvent.Name} Event)\n";
            }

            if (_streakService.GetStreakLevel(feature) > 0)
            {
                bonusPercent += (int)Math.Ceiling(_streakService.GetStreakLevel(feature) * Constants.ExpBoostPerLevel);
                bonusInfo += $"**+{ (Constants.ExpBoostPerLevel * _streakService.GetStreakLevel(feature))}% EXP** ({ Constants.ExpBoostPerLevel}%  × { Constants.Fire} { _streakService.GetStreakLevel(feature).ToFormattedString()})\n";
            }
            if(bonusPercent > 0)
            {
                bonusInfo += $"**{bonusPercent}% EXP Bonus insgesamt**";
            }

            return (bonusInfo, bonusPercent);
        }

        public async Task AddEXP(SocketMessage msg)
        {
            var dcMessage = msg as SocketUserMessage;
            var dcGuild = ((SocketGuildChannel)msg.Channel).Guild;
            using (rabbotContext db = new rabbotContext())
            {
                var guild = db.Guild.FirstOrDefault(p => p.ServerId == dcGuild.Id) ?? db.Guild.AddAsync(new Guild { ServerId = dcGuild.Id }).Result.Entity;
                var EXP = db.Userfeatures.Include(p => p.User).Where(p => (ulong)p.UserId == msg.Author.Id && p.ServerId == dcGuild.Id).Include(p => p.Inventory).FirstOrDefault() ?? db.Userfeatures.AddAsync(new Userfeatures { Exp = 0, UserId = msg.Author.Id, ServerId = dcGuild.Id }).Result.Entity;
                var oldLevel = Helper.GetLevel(EXP.Exp);
                var oldEXP = Convert.ToDouble(EXP.Exp);
                var roundedEXP = Math.Ceiling(oldEXP / 10000d) * 10000;
                string filteredMsg = Helper.MessageReplace(msg.Content);
                int textLenght = filteredMsg.Count();
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

                var chance = rnd.Next(1, 10);
                if (chance == 3)
                {
                    exp *= rnd.Next(1, 5);
                }

                if (dcMessage?.Author is SocketGuildUser user)
                {
                    var (bonusInfo, bonusPercent) = GetBonusEXP(db, user);
                    exp += exp.GetValueFromPercent(bonusPercent);
                }

                if (roundedEXP < EXP.Exp)
                {
                    EXP.Attacks--;
                }
                EXP.Exp += exp;
                var NewLevel = Helper.GetLevel(EXP.Exp);
                await SendLevelUp(dcGuild, guild, dcMessage, oldLevel, NewLevel);
                await SetRoles(dcGuild, dcMessage, NewLevel);
                db.SaveChanges();
            }
        }

        private async Task SendLevelUp(SocketGuild dcGuild, Guild guild, SocketUserMessage dcMessage, uint OldLevel, uint NewLevel)
        {
            var reward = Helper.GetReward((int)NewLevel);
            if (NewLevel > OldLevel && guild.Level == 1)
            {
                string path = "";
                using (dcMessage.Channel.EnterTypingState())
                {
                    var template = new HtmlTemplate(Directory.GetCurrentDirectory() + "/Resources/RabbotThemeNeon/levelup.html");
                    var dcUser = dcGuild.Users.FirstOrDefault(p => p.Id == dcMessage.Author.Id);
                    string name = (dcUser as IGuildUser).Nickname?.Replace("<", "&lt;").Replace(">", "&gt;") ?? dcMessage.Author.Username?.Replace("<", "&lt;").Replace(">", "&gt;");
                    var html = template.Render(new
                    {
                        NAME = name,
                        LEVEL = NewLevel.ToString()
                    });

                    path = HtmlToImage.Generate(Helper.RemoveSpecialCharacters(name) + "Level_Up", html, 300, 100);
                    using (rabbotContext db = new rabbotContext())
                    {
                        var levelChannelId = db.Guild.FirstOrDefault(p => p.ServerId == dcGuild.Id)?.LevelchannelId;
                        if (levelChannelId == null)
                            await dcMessage.Channel.SendFileAsync(path, $"**Glückwunsch! Als Belohnung erhältst du {reward} Ziegen**!");
                        else
                            await dcGuild.TextChannels.FirstOrDefault(p => p.Id == (ulong)levelChannelId)?.SendFileAsync(path, $"**Glückwunsch! Als Belohnung erhältst du {reward} Ziegen**!");

                    }
                }
                File.Delete(path);
            }

            if (NewLevel > OldLevel)
            {
                using (rabbotContext db = new rabbotContext())
                {
                    var feature = db.Userfeatures.FirstOrDefault(p => p.UserId == dcMessage.Author.Id && p.ServerId == dcGuild.Id);

                    if (Helper.IsFull(feature.Goats + reward, feature.Wins))
                        feature.Goats = Helper.GetStall(feature.Wins).Capacity;
                    else
                        feature.Goats += reward;


                    var combis = db.Combi.Include(p => p.User).ThenInclude(p => p.Userfeatures).Include(p => p.CombiUser).ThenInclude(p => p.Userfeatures).Where(p => p.ServerId == dcGuild.Id && (p.UserId == dcMessage.Author.Id || p.CombiUserId == dcMessage.Author.Id));

                    foreach (var combi in combis)
                    {
                        if (combi.Accepted != true)
                            continue;
                        try
                        {
                            if (combi.CombiUserId == dcMessage.Author.Id)
                                combi.User.Userfeatures.FirstOrDefault(p => p.ServerId == dcGuild.Id).CombiExp++;
                            if (combi.UserId == dcMessage.Author.Id)
                                combi.CombiUser.Userfeatures.FirstOrDefault(p => p.ServerId == dcGuild.Id).CombiExp++;
                        }
                        catch { }
                    }

                    await db.SaveChangesAsync();
                }
            }
        }

        private async Task SetRoles(SocketGuild dcGuild, SocketUserMessage dcMessage, uint NewLevel)
        {
            using (rabbotContext db = new rabbotContext())
            {
                var roles = db.Roles.Where(p => p.ServerId == dcGuild.Id);

                var S4Id = roles.FirstOrDefault(x => x.Description == "S4") ?? new Roles { ServerId = dcGuild.Id, RoleId = 0, Description = "S4" };
                var S3Id = roles.FirstOrDefault(x => x.Description == "S3") ?? new Roles { ServerId = dcGuild.Id, RoleId = 0, Description = "S3" };
                var S2Id = roles.FirstOrDefault(x => x.Description == "S2") ?? new Roles { ServerId = dcGuild.Id, RoleId = 0, Description = "S2" };
                var S1Id = roles.FirstOrDefault(x => x.Description == "S1") ?? new Roles { ServerId = dcGuild.Id, RoleId = 0, Description = "S1" };
                var ProId = roles.FirstOrDefault(x => x.Description == "Pro") ?? new Roles { ServerId = dcGuild.Id, RoleId = 0, Description = "Pro" };
                var SemiId = roles.FirstOrDefault(x => x.Description == "Semi") ?? new Roles { ServerId = dcGuild.Id, RoleId = 0, Description = "Semi" };
                var AmateurId = roles.FirstOrDefault(x => x.Description == "Amateur") ?? new Roles { ServerId = dcGuild.Id, RoleId = 0, Description = "Amateur" };
                var RookieId = roles.FirstOrDefault(x => x.Description == "Rookie") ?? new Roles { ServerId = dcGuild.Id, RoleId = 0, Description = "Rookie" };

                var roleS4 = dcGuild.Roles.FirstOrDefault(p => p.Id == (ulong)S4Id.RoleId);
                var roleS3 = dcGuild.Roles.FirstOrDefault(p => p.Id == (ulong)S3Id.RoleId);
                var roleS2 = dcGuild.Roles.FirstOrDefault(p => p.Id == (ulong)S2Id.RoleId);
                var roleS1 = dcGuild.Roles.FirstOrDefault(p => p.Id == (ulong)S1Id.RoleId);
                var rolePro = dcGuild.Roles.FirstOrDefault(p => p.Id == (ulong)ProId.RoleId);
                var roleSemi = dcGuild.Roles.FirstOrDefault(p => p.Id == (ulong)SemiId.RoleId);
                var roleAmateur = dcGuild.Roles.FirstOrDefault(p => p.Id == (ulong)AmateurId.RoleId);
                var roleRookie = dcGuild.Roles.FirstOrDefault(p => p.Id == (ulong)RookieId.RoleId);

                if (roleS4 != null && roleS3 != null && roleS2 != null && roleS1 != null && rolePro != null && roleSemi != null && roleAmateur != null && roleRookie != null)
                {
                    var myUser = dcGuild.Users.FirstOrDefault(p => p.Id == dcMessage?.Author?.Id);
                    if (myUser == null)
                        return;

                    if (NewLevel < 1)
                    {
                        if (myUser.Roles.FirstOrDefault(p => p.Name == roleS4.Name || p.Name == roleS3.Name || p.Name == roleS2.Name || p.Name == roleS1.Name || p.Name == rolePro.Name || p.Name == roleSemi.Name || p.Name == roleAmateur.Name || p.Name == roleRookie.Name) != null)
                        {
                            await myUser.RemoveRoleAsync(roleS4);
                            await myUser.RemoveRoleAsync(roleS3);
                            await myUser.RemoveRoleAsync(roleS2);
                            await myUser.RemoveRoleAsync(roleS1);
                            await myUser.RemoveRoleAsync(rolePro);
                            await myUser.RemoveRoleAsync(roleSemi);
                            await myUser.RemoveRoleAsync(roleAmateur);
                            await myUser.RemoveRoleAsync(roleRookie);
                        }
                    }
                    if (NewLevel >= 1 && NewLevel <= 19)
                    {
                        if (myUser.Roles.FirstOrDefault(p => p.Name == roleS4.Name || p.Name == roleS3.Name || p.Name == roleS2.Name || p.Name == roleS1.Name || p.Name == rolePro.Name || p.Name == roleSemi.Name || p.Name == roleAmateur.Name) != null)
                        {
                            await myUser.RemoveRoleAsync(roleS4);
                            await myUser.RemoveRoleAsync(roleS3);
                            await myUser.RemoveRoleAsync(roleS2);
                            await myUser.RemoveRoleAsync(roleS1);
                            await myUser.RemoveRoleAsync(rolePro);
                            await myUser.RemoveRoleAsync(roleSemi);
                            await myUser.RemoveRoleAsync(roleAmateur);
                        }
                        if (myUser.Roles.FirstOrDefault(p => p.Name == roleRookie.Name) == null)
                            await myUser.AddRoleAsync(roleRookie);
                    }
                    if (NewLevel >= 20 && NewLevel <= 39)
                    {
                        if (myUser.Roles.FirstOrDefault(p => p.Name == roleRookie.Name) != null)
                            await myUser.RemoveRoleAsync(roleRookie);
                        if (myUser.Roles.FirstOrDefault(p => p.Name == roleS4.Name || p.Name == roleS3.Name || p.Name == roleS2.Name || p.Name == roleS1.Name || p.Name == rolePro.Name || p.Name == roleSemi.Name) != null)
                        {
                            await myUser.RemoveRoleAsync(roleS4);
                            await myUser.RemoveRoleAsync(roleS3);
                            await myUser.RemoveRoleAsync(roleS2);
                            await myUser.RemoveRoleAsync(roleS1);
                            await myUser.RemoveRoleAsync(rolePro);
                            await myUser.RemoveRoleAsync(roleSemi);
                        }
                        if (myUser.Roles.FirstOrDefault(p => p.Name == roleAmateur.Name) == null)
                            await myUser.AddRoleAsync(roleAmateur);
                    }
                    if (NewLevel >= 40 && NewLevel <= 59)
                    {
                        if (myUser.Roles.FirstOrDefault(p => p.Name == roleAmateur.Name) != null)
                            await myUser.RemoveRoleAsync(roleAmateur);
                        if (myUser.Roles.FirstOrDefault(p => p.Name == roleS4.Name || p.Name == roleS3.Name || p.Name == roleS2.Name || p.Name == roleS1.Name || p.Name == rolePro.Name || p.Name == roleRookie.Name) != null)
                        {
                            await myUser.RemoveRoleAsync(roleS4);
                            await myUser.RemoveRoleAsync(roleS3);
                            await myUser.RemoveRoleAsync(roleS2);
                            await myUser.RemoveRoleAsync(roleS1);
                            await myUser.RemoveRoleAsync(rolePro);
                            await myUser.RemoveRoleAsync(roleRookie);
                        }
                        if (myUser.Roles.FirstOrDefault(p => p.Name == roleSemi.Name) == null)
                            await myUser.AddRoleAsync(roleSemi);
                    }
                    if (NewLevel >= 60 && NewLevel <= 79)
                    {
                        if (myUser.Roles.FirstOrDefault(p => p.Name == roleSemi.Name) != null)
                            await myUser.RemoveRoleAsync(roleSemi);
                        if (myUser.Roles.FirstOrDefault(p => p.Name == roleS4.Name || p.Name == roleS3.Name || p.Name == roleS2.Name || p.Name == roleS1.Name || p.Name == roleAmateur.Name || p.Name == roleRookie.Name) != null)
                        {
                            await myUser.RemoveRoleAsync(roleS4);
                            await myUser.RemoveRoleAsync(roleS3);
                            await myUser.RemoveRoleAsync(roleS2);
                            await myUser.RemoveRoleAsync(roleS1);
                            await myUser.RemoveRoleAsync(roleAmateur);
                            await myUser.RemoveRoleAsync(roleRookie);
                        }
                        if (myUser.Roles.FirstOrDefault(p => p.Name == rolePro.Name) == null)
                            await myUser.AddRoleAsync(rolePro);
                    }
                    if (NewLevel >= 80 && NewLevel <= 95)
                    {
                        if (myUser.Roles.FirstOrDefault(p => p.Name == rolePro.Name) != null)
                            await myUser.RemoveRoleAsync(rolePro);
                        if (myUser.Roles.FirstOrDefault(p => p.Name == roleSemi.Name || p.Name == roleS3.Name || p.Name == roleS2.Name || p.Name == roleS4.Name || p.Name == roleAmateur.Name || p.Name == roleRookie.Name) != null)
                        {
                            await myUser.RemoveRoleAsync(roleS4);
                            await myUser.RemoveRoleAsync(roleS3);
                            await myUser.RemoveRoleAsync(roleS2);
                            await myUser.RemoveRoleAsync(roleSemi);
                            await myUser.RemoveRoleAsync(roleAmateur);
                            await myUser.RemoveRoleAsync(roleRookie);
                        }
                        if (myUser.Roles.FirstOrDefault(p => p.Name == roleS1.Name) == null)
                            await myUser.AddRoleAsync(roleS1);
                    }
                    if (NewLevel >= 96 && NewLevel <= 112)
                    {
                        if (myUser.Roles.FirstOrDefault(p => p.Name == roleS1.Name) != null)
                            await myUser.RemoveRoleAsync(roleS1);
                        if (myUser.Roles.FirstOrDefault(p => p.Name == roleSemi.Name || p.Name == roleS3.Name || p.Name == roleS4.Name || p.Name == roleS1.Name || p.Name == roleAmateur.Name || p.Name == roleRookie.Name) != null)
                        {
                            await myUser.RemoveRoleAsync(roleS4);
                            await myUser.RemoveRoleAsync(roleS3);
                            await myUser.RemoveRoleAsync(rolePro);
                            await myUser.RemoveRoleAsync(roleSemi);
                            await myUser.RemoveRoleAsync(roleAmateur);
                            await myUser.RemoveRoleAsync(roleRookie);
                        }
                        if (myUser.Roles.FirstOrDefault(p => p.Name == roleS2.Name) == null)
                            await myUser.AddRoleAsync(roleS2);
                    }
                    if (NewLevel >= 113 && NewLevel <= 118)
                    {
                        if (myUser.Roles.FirstOrDefault(p => p.Name == roleS2.Name) != null)
                            await myUser.RemoveRoleAsync(roleS2);
                        if (myUser.Roles.FirstOrDefault(p => p.Name == roleSemi.Name || p.Name == roleS2.Name || p.Name == roleS4.Name || p.Name == roleS1.Name || p.Name == roleAmateur.Name || p.Name == roleRookie.Name) != null)
                        {
                            await myUser.RemoveRoleAsync(roleS4);
                            await myUser.RemoveRoleAsync(roleS1);
                            await myUser.RemoveRoleAsync(rolePro);
                            await myUser.RemoveRoleAsync(roleSemi);
                            await myUser.RemoveRoleAsync(roleAmateur);
                            await myUser.RemoveRoleAsync(roleRookie);
                        }
                        if (myUser.Roles.FirstOrDefault(p => p.Name == roleS3.Name) == null)
                            await myUser.AddRoleAsync(roleS3);
                    }
                    if (NewLevel >= 119)
                    {
                        if (myUser.Roles.FirstOrDefault(p => p.Name == roleS3.Name) != null)
                            await myUser.RemoveRoleAsync(roleS3);
                        if (myUser.Roles.FirstOrDefault(p => p.Name == roleSemi.Name || p.Name == roleS2.Name || p.Name == roleS3.Name || p.Name == roleS1.Name || p.Name == roleAmateur.Name || p.Name == roleRookie.Name) != null)
                        {
                            await myUser.RemoveRoleAsync(roleS2);
                            await myUser.RemoveRoleAsync(roleS1);
                            await myUser.RemoveRoleAsync(rolePro);
                            await myUser.RemoveRoleAsync(roleSemi);
                            await myUser.RemoveRoleAsync(roleAmateur);
                            await myUser.RemoveRoleAsync(roleRookie);
                        }
                        if (myUser.Roles.FirstOrDefault(p => p.Name == roleS4.Name) == null)
                            await myUser.AddRoleAsync(roleS4);
                    }
                }
            }
        }
    }
}
