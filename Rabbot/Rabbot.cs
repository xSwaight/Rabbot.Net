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
using Serilog.Core;

namespace Rabbot
{
    public class Rabbot
    {
        DiscordSocketClient _client;
        private static readonly ILogger _logger = Log.ForContext(Serilog.Core.Constants.SourceContextPropertyName, nameof(Rabbot));
        public async Task StartAsync()
        {
            try
            {
                Log.Logger = new LoggerConfiguration()
                    .WriteTo.Sentry(o =>
                    {
                        o.Dsn = new Dsn(Config.bot.sentrydsn);
                        o.Environment = Config.bot.environment;
                        o.MinimumBreadcrumbLevel = LogEventLevel.Verbose;
                        o.MinimumEventLevel = LogEventLevel.Warning;
                        o.SendDefaultPii = true;
                        o.AttachStacktrace = true;

                    })
                    .MinimumLevel.Verbose()
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
                    .AddSingleton<TwitchService>()
                    //.AddSingleton<YouTubeVideoService>()
                    .AddSingleton<CommandHandler>()
                    .AddSingleton<StartupService>()
                    .AddSingleton<AudioService>()
                    .AddSingleton<LoggingService>()
                    .AddSingleton<StreakService>()
                    .AddSingleton<AttackService>()
                    .AddSingleton<LevelService>()
                    .AddSingleton<MuteService>()
                    .AddSingleton<WarnService>()
                    .AddSingleton<EventService>();

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
                //serviceProvider.GetRequiredService<YouTubeVideoService>();
                serviceProvider.GetRequiredService<StreakService>();
                serviceProvider.GetRequiredService<AttackService>();
                serviceProvider.GetRequiredService<LevelService>();
                serviceProvider.GetRequiredService<MuteService>();
                serviceProvider.GetRequiredService<WarnService>();
                serviceProvider.GetRequiredService<EventService>();

                new Task(() => RunConsoleCommand(), TaskCreationOptions.LongRunning).Start();

                // Block this program until it is closed.
                await Task.Delay(-1);
            }
            catch (Exception e)
            {
                _logger.Fatal(e, "Startup failed");
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
                    string input = Console.ReadLine();
                    switch (input)
                    {
                        case "servers":
                            _logger.Information($"Connected Servers: {string.Join(", ", _client.Guilds)}");
                            break;
                        default:
                            _logger.Information($"Command '{input}' not found");
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
