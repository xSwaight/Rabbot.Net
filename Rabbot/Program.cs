using Discord;
using Discord.WebSocket;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Rabbot
{
    class Program
    {
        public static async Task Main()
        {
            while (true)
            {
                try
                {
                    await new Rabbot().StartAsync();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Error in {nameof(Program)}");
                }
            }
        }
    }
}
