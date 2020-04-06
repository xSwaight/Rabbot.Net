using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rabbot.Database;
using Rabbot.Database.Rabbot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rabbot.Services
{
    public class DatabaseService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly RabbotContext _db;

        public DatabaseService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _db = Open<RabbotContext>();
        }

        public TContext Open<TContext>()
            where TContext : DbContext
        {
            return _serviceProvider.GetRequiredService<TContext>();
        }

        public async Task SaveChangesAsync()
        {
            await _db.SaveChangesAsync();
        }

        public async Task<FeatureEntity> GetFeature(ulong UserId, ulong GuildId)
        {
            if (!_db.Users.Any(p => p.Id == UserId))
                await _db.Users.AddAsync(new UserEntity { Id = UserId, Name = string.Empty });

            if (!_db.Guilds.Any(p => p.GuildId == GuildId))
                await _db.Guilds.AddAsync(new GuildEntity { GuildId = UserId, GuildName = string.Empty });

            await _db.SaveChangesAsync();

            return  _db.Features.Include(p => p.User).Include(p => p.Guild).FirstOrDefault(p => p.UserId == UserId && p.GuildId == GuildId) 
                    ?? _db.AddAsync(new FeatureEntity {GuildId = GuildId, UserId = UserId }).Result.Entity;
        }
    }
}
