﻿using Microsoft.EntityFrameworkCore;
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
            var connectionString = "server=localhost;database=newrabbot;user=root";

            return new RabbotContext(
                new DbContextOptionsBuilder<RabbotContext>()
                    .UseMySql(connectionString)
                    .Options
            );
        }
    }
}