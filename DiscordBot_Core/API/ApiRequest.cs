using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using DiscordBot_Core.API.Models;
using Newtonsoft.Json.Linq;

namespace DiscordBot_Core.API
{
    class ApiRequest
    {

        public async Task<string> APIRequestAsync(string url)
        {
            try
            {
                HttpClient _httpClient = new HttpClient();
                _httpClient.DefaultRequestHeaders.Add("X-S4DB-API-Key", Config.bot.apiToken);
                var task = await _httpClient.GetAsync(url).ConfigureAwait(false);
                task.EnsureSuccessStatusCode();
                var payload = await task.Content.ReadAsStringAsync();
                return payload;
            }
            catch
            {
                return "";
            }
        }


        public async Task<Player> GetPlayer(string name)
        {
            string URL = "https://api.s4db.net/player/" + name;
            var jsonResponse = await APIRequestAsync(URL);
            if (string.IsNullOrWhiteSpace(jsonResponse))
                return null;
            var player = JsonConvert.DeserializeObject<Player>(jsonResponse);
            if (player.Name == null)
                return null;
            else
                return player;
        }

        public async Task<Clan> GetClan(string name)
        {
            string URL = "https://api.s4db.net/clan/" + name;
            var jsonResponse = await APIRequestAsync(URL);
            if (string.IsNullOrWhiteSpace(jsonResponse))
                return null;
            var clan = JsonConvert.DeserializeObject<Clan>(jsonResponse);
            if (clan.Name == null)
                return null;
            else
                return clan;
        }

        public async Task<List<Server>> GetServer()
        {
            string URL = "https://api.s4db.net/server";
            var jsonResponse = await APIRequestAsync(URL);
            if (string.IsNullOrWhiteSpace(jsonResponse))
                return null;
            var server = JsonConvert.DeserializeObject<List<Server>>(jsonResponse.ToString());
            if (server == null)
                return null;
            else
                return server;
        }
    }
}
