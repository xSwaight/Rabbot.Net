using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Rabbot.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;

namespace Rabbot.Services
{
    public class DiscordApiService
    {
        private static readonly ILogger _logger = Log.ForContext(Serilog.Core.Constants.SourceContextPropertyName, nameof(DiscordApiService));
        public DiscordApiService(IServiceProvider services)
        {

        }

        public CustomDiscordUser GetUserData(ulong uid)
        {
            var headers = new Dictionary<string, string>();
            headers.Add("Authorization", $"Bot {Config.Bot.Token}");
            var result = ApiService.GetRequest($"https://discord.com/api/v8/users/{uid}", headers);
            if (result.success)
            {
                return JsonConvert.DeserializeObject<CustomDiscordUser>(result.payload);
            }

            return null;
        }
    }
}
