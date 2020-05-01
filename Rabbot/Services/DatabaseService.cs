using Microsoft.EntityFrameworkCore;
using Rabbot.Database;
using System;
using System.Collections.Generic;
using System.Text;

namespace Rabbot.Services
{
    public class DatabaseService
    {
        public static DatabaseService Instance { get; set; } = new DatabaseService();
        private DbContextOptions<RabbotContext> options;

        public DatabaseService()
        {
            var builder = new DbContextOptionsBuilder<RabbotContext>();
            builder.UseMySql(Config.Bot.ConnectionString);
            options = builder.Options;
        }

        public RabbotContext Open()
        {
            return new RabbotContext(options);
        }
    }
}
