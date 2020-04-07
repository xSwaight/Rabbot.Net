using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;
using System.Collections.Generic;
using System.Text;

namespace Rabbot.Database
{
    public class DesignTimeGameRabbotFactory : IDesignTimeDbContextFactory<RabbotContext>
    {
        public RabbotContext CreateDbContext(string[] args)
        {
            // local connection string
            var connectionString = "server=localhost;database=rabbot;user=root";

            return new RabbotContext(
                new DbContextOptionsBuilder<RabbotContext>()
                    .UseMySql(connectionString)
                    .Options
            );
        }
    }
}
