using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Rabbot.Services
{
    public class DatabaseService
    {
        private readonly IServiceProvider _serviceProvider;

        public DatabaseService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public TContext Open<TContext>()
            where TContext : DbContext
        {
            return _serviceProvider.GetRequiredService<TContext>();
        }
    }
}
