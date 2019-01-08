using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace DiscordBot_Core
{
#pragma warning disable CS1998
    class Program
    {
        DiscordSocketClient _client;
        CommandHandler _handler;
        EventHandler _event;

        static void Main(string[] args)
        => new Program().StartAsync().GetAwaiter().GetResult();

        public async Task StartAsync()
        {
            if (String.IsNullOrWhiteSpace(Config.bot.token)) return;
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Verbose
            });
            _client.Log += Log;

            await _client.LoginAsync(TokenType.Bot, Config.bot.token);
            await _client.StartAsync();
            await _client.SetStatusAsync(UserStatus.Online);
            await _client.SetGameAsync($"euer Herz <3", null, ActivityType.Listening);
            _handler = new CommandHandler();
            _event = new EventHandler();
            await _handler.InitializeAsync(_client);
            await _event.InitializeAsync(_client);
            
            await Task.Delay(-1);
        }


        private async Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.Message);
        }
    }
}
