﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System;
using Serilog;
using Rabbot.Services;
using Serilog.Events;
using Sentry;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Discord.Addons.Interactive;
using System.Reflection;

namespace Rabbot
{
    public class Rabbot
    {
        DiscordShardedClient _client;
        CommandService _commandService;
        private static readonly ILogger _logger = Log.ForContext(Serilog.Core.Constants.SourceContextPropertyName, nameof(Rabbot));
        public async Task StartAsync()
        {
            try
            {
                Log.Logger = new LoggerConfiguration()
                    .WriteTo.Sentry(o =>
                    {
                        o.Dsn = new Dsn(Config.Bot.SentryDsn);
                        o.Environment = Config.Bot.Environment;
                        o.MinimumBreadcrumbLevel = LogEventLevel.Verbose;
                        o.MinimumEventLevel = LogEventLevel.Error;
                        o.SendDefaultPii = true;
                        o.AttachStacktrace = true;

                    })
                    .MinimumLevel.Verbose()
                    .WriteTo.File("logs/rabbot.log", rollingInterval: RollingInterval.Day)
                    .WriteTo.Console()
                    .CreateLogger();

                var config = new DiscordSocketConfig
                {
                    TotalShards = 1,
                    LogLevel = LogSeverity.Verbose,
                    MessageCacheSize = 1000,
                    ExclusiveBulkDelete = true
                };

                var services = new ServiceCollection()
                    .AddSingleton(_client = new DiscordShardedClient(config))
                    .AddSingleton(_commandService = new CommandService(new CommandServiceConfig
                    {
                        DefaultRunMode = RunMode.Async,
                        LogLevel = LogSeverity.Verbose,
                        CaseSensitiveCommands = false,
                        ThrowOnError = false
                    }))
                    .AddSingleton<TwitchService>()
                    .AddSingleton<YouTubeVideoService>()
                    .AddSingleton<CommandHandler>()
                    .AddSingleton<StartupService>()
                    .AddSingleton<AudioService>()
                    .AddSingleton<LoggingService>()
                    .AddSingleton<Logging>()
                    .AddSingleton<StreakService>()
                    .AddSingleton<AttackService>()
                    .AddSingleton<LevelService>()
                    .AddSingleton<MuteService>()
                    .AddSingleton<WarnService>()
                    .AddSingleton<EventService>()
                    .AddSingleton<ApiService>()
                    .AddSingleton<DiscordApiService>()
                    .AddSingleton<InteractiveService>()
                    .AddSingleton<EasterEventService>()
                    .AddSingleton<ImageService>()
                    .AddSingleton<CacheService>()
                    .AddSingleton<RuleAcceptService>()
                    .AddSingleton<Helper>();



                //Add logging
                ConfigureServices(services);

                //Build services
                var serviceProvider = services.BuildServiceProvider();

                //Instantiate logger/tie-in logging
                serviceProvider.GetRequiredService<LoggingService>();

                // Run Migrations
                var db = DatabaseService.Instance.Open();
                _logger.Information($"Checking database={db.GetType().Name}...");

                using (db)
                {
                    if (db.Database.GetPendingMigrations().Any())
                    {
                        _logger.Information($"Applying database={db.GetType().Name} migrations...");
                        db.Database.Migrate();
                    }
                    else
                    {
                        _logger.Information($"{db.GetType().Name} is up2date!");
                    }
                }

                //Start the bot
                await serviceProvider.GetRequiredService<StartupService>().StartAsync();

                //Load up services
                serviceProvider.GetRequiredService<CommandHandler>();
                serviceProvider.GetRequiredService<TwitchService>();
                serviceProvider.GetRequiredService<YouTubeVideoService>();
                serviceProvider.GetRequiredService<StreakService>();
                serviceProvider.GetRequiredService<AttackService>();
                serviceProvider.GetRequiredService<LevelService>();
                serviceProvider.GetRequiredService<Logging>();
                serviceProvider.GetRequiredService<MuteService>();
                serviceProvider.GetRequiredService<WarnService>();
                serviceProvider.GetRequiredService<EventService>();
                serviceProvider.GetRequiredService<ApiService>();
                serviceProvider.GetRequiredService<EasterEventService>();
                serviceProvider.GetRequiredService<ImageService>();
                serviceProvider.GetRequiredService<CacheService>();
                serviceProvider.GetRequiredService<RuleAcceptService>();
                serviceProvider.GetRequiredService<Helper>();

                new Task(() => RunConsoleCommand(), TaskCreationOptions.LongRunning).Start();

                //Block this program until it is closed.
                await Task.Delay(-1);
            }
            catch (Exception e)
            {
                _logger.Error(e, $"Error in {nameof(StartAsync)}");
                Console.WriteLine($"Startup failed. Please check the config file.");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
                Environment.Exit(0);
            }

        }
        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(configure => configure.AddSerilog());
        }

        private void RunConsoleCommand()
        {
            while (true)
            {
                try
                {
                    var input = Console.ReadLine().GetArgs();
                    switch (input.First())
                    {
                        case "servers":
                            _logger.Information($"Connected Servers: {string.Join(", ", _client.Guilds)}");
                            break;
                        default:
                            _logger.Information($"Command '{input.First()}' not found");
                            break;
                    }
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Error while running a console command");
                }
            }
        }
    }
}
