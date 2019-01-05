using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace DiscordBot_Core.API
{
    class S4DB
    {
        private HttpClient _httpClient = new HttpClient();

        private static string APIRequest(string url)
        {
            HttpWebResponse response = null;
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
            request.Method = "GET";
            request.Headers.Add("X-S4DB-API-Key", Config.bot.apiToken);
            response = (HttpWebResponse)request.GetResponse();
            using (Stream s = response.GetResponseStream())
            {
                using (StreamReader sr1 = new StreamReader(s))
                {
                    var jsonResponse = sr1.ReadToEnd();
                    return jsonResponse;
                }
            }
        }

        public async Task<string> APIRequestAsync(string url)
        {
            _httpClient.DefaultRequestHeaders.Add("X-S4DB-API-Key", Config.bot.apiToken);
            var task = await _httpClient.GetAsync(url).ConfigureAwait(false);
            task.EnsureSuccessStatusCode();
            var payload = task.Content.ReadAsStringAsync();
            return payload.Result;
        }


        public async Task<Player> GetPlayer(string name)
        {
            if (name.Contains('<')) return null;
            string URL = "https://api.s4db.net/player/" + name;
            var jsonResponse = await APIRequestAsync(URL);
            var player = JsonConvert.DeserializeObject<Player>(jsonResponse);
            if (player.Name == null)
                return null;
            else
                return player;
        }

        public async Task<List<Server>> GetServer()
        {
            string URL = "https://api.s4db.net/server";
            var jsonResponse = await APIRequestAsync(URL);
            var server = JsonConvert.DeserializeObject<List<Server>>(jsonResponse.ToString());
            if (server == null)
                return null;
            else
                return server;
        }
    }
}
