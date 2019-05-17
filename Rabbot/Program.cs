using Discord;
using Discord.WebSocket;
using Rabbot.API;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Rabbot
{
    class Program
    {
        public static DateTime startTime;
        public static void Main(string[] args)
        {
            try
            {
                new Rabbot().StartAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}
