using Discord.Net;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Microsoft;
using Rabbot.Services;
using Rabbot.API;

namespace Rabbot
{
    public class Rabbot
    {
        private DiscordSocketClient _client;
        private CommandHandler _handler;
        public static DiscordSocketClient Client;
        private IConfigurationRoot _config;

        public async Task StartAsync()
        {
            var _event = new EventHandler();
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File("logs/rabbot.log", rollingInterval: RollingInterval.Day)
                .WriteTo.Console()
                .CreateLogger();

            //Create the configuration
            var _builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile(path: "config.json");
            _config = _builder.Build();

            var services = new ServiceCollection()
                .AddSingleton(_client = new DiscordSocketClient(new DiscordSocketConfig
                {
                    LogLevel = LogSeverity.Verbose,
                    MessageCacheSize = 1000
                }))
                .AddSingleton(_config)
                .AddSingleton(new CommandService(new CommandServiceConfig
                {
                    DefaultRunMode = RunMode.Async,
                    LogLevel = LogSeverity.Verbose,
                    CaseSensitiveCommands = false,
                    ThrowOnError = false
                }))
                .AddSingleton<Twitch>()
                .AddSingleton<CommandHandler>()
                .AddSingleton<StartupService>()
                .AddSingleton<AudioService>()
                .AddSingleton<LoggingService>();

            //Add logging     
            ConfigureServices(services);

            //Build services
            var serviceProvider = services.BuildServiceProvider();

            //Instantiate logger/tie-in logging
            serviceProvider.GetRequiredService<LoggingService>();

            //Start the bot
            await serviceProvider.GetRequiredService<StartupService>().StartAsync();

            //Load up services
            serviceProvider.GetRequiredService<CommandHandler>();
            serviceProvider.GetRequiredService<Twitch>();

            await _event.InitializeAsync(_client);
            // Block this program until it is closed.
            await Task.Delay(-1);
        }
        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(configure => configure.AddSerilog());
        }
    }
}
