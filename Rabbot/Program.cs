using Discord;
using Discord.WebSocket;
using Rabbot.API;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Rabbot
{
    class Program
    {
        public static void Main(string[] args)
        {
            while (true)
            {
                try
                {
                    new Rabbot().StartAsync().GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Error in {nameof(Program)}");
                }
            }
        }
    }
}
