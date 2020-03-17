using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System;
using Serilog;
using Rabbot.Services;
using Serilog.Events;
using Sentry;

namespace Rabbot
{
    public class Rabbot
    {
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
                        o.SendDefaultPii = true;
                        o.AttachStacktrace = true;

                    })
                    .WriteTo.File("logs/rabbot.log", rollingInterval: RollingInterval.Day)
                    .WriteTo.Console()
                    .CreateLogger();

                var services = new ServiceCollection()
                    .AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
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
                    .AddSingleton<TwitchService>()
                    .AddSingleton<CommandHandler>()
                    .AddSingleton<StartupService>()
                    .AddSingleton<AudioService>()
                    .AddSingleton<LoggingService>()
                    .AddSingleton<EventHandler>();

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
                serviceProvider.GetRequiredService<TwitchService>();
                serviceProvider.GetRequiredService<EventHandler>();


                // Block this program until it is closed.
                await Task.Delay(-1);
            }
            catch (Exception e)
            {
                Log.Fatal(e, "Startup failed");
            }

        }
        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(configure => configure.AddSerilog());
        }
    }
}
