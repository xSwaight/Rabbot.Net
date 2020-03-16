using Discord.Net;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System;
using Serilog;
using Rabbot.Services;
using Rabbot.API;
using Serilog.Events;
using Sentry;
using Microsoft.Extensions.Logging;

namespace Rabbot
{
    public class Rabbot
    {
        private DiscordSocketClient _client;
        private ILogger<Rabbot> _logger;
        public static DiscordSocketClient Client;

        public async Task StartAsync()
        {
            try
            {
                Log.Logger = new LoggerConfiguration()
                    .WriteTo.Sentry(o =>
                    {
                        o.Dsn = new Dsn(Config.bot.sentrydsn);
                        o.Environment = Config.bot.environment;
                        o.MinimumBreadcrumbLevel = LogEventLevel.Debug;
                        o.MinimumEventLevel = LogEventLevel.Warning;
                    })
                    .WriteTo.File("logs/rabbot.log", rollingInterval: RollingInterval.Day)
                    .WriteTo.Console()
                    .CreateLogger();

                var services = new ServiceCollection()
                    .AddSingleton(_client = new DiscordSocketClient(new DiscordSocketConfig
                    {
                        LogLevel = LogSeverity.Verbose,
                        MessageCacheSize = 1000,
                        ExclusiveBulkDelete = true
                    }))
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
                    .AddSingleton<EventHandler>()
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
                serviceProvider.GetRequiredService<EventHandler>();

                _logger = serviceProvider.GetService<ILogger<Rabbot>>();

                // Block this program until it is closed.
                await Task.Delay(-1);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }

        }
        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(configure => configure.AddSerilog());
        }
    }
}
