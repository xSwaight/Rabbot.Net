using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Rabbot.API.Models;
using Newtonsoft.Json.Linq;
using System.Net;
using System.IO;
using System.Text;

namespace Rabbot.API
{
    static class ApiRequest
    {

        public static async Task<string> APIRequestAsync(string url)
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

        public static string RemDB_APIRequest()
        {
            string url = "https://api.remdb.net/playercount";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Stream receiveStream = response.GetResponseStream();
                StreamReader readStream = null;

                if (response.CharacterSet == null)
                {
                    readStream = new StreamReader(receiveStream);
                }
                else
                {
                    readStream = new StreamReader(receiveStream, Encoding.GetEncoding(response.CharacterSet));
                }

                string data = readStream.ReadToEnd();

                response.Close();
                readStream.Close();
                return data;
            }
            return "";
        }

        public static int Official_APIRequest()
        {
            string url = Config.bot.officialPlayerURL;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Stream receiveStream = response.GetResponseStream();
                StreamReader readStream = null;

                readStream = new StreamReader(receiveStream, Encoding.GetEncoding("UTF-8"));
                string data = readStream.ReadToEnd();

                response.Close();
                readStream.Close();
                var playerCount = JsonConvert.DeserializeObject<OfficialPlayerCount>(data);
                if (playerCount.Success)
                    return playerCount.PlayerCount;
            }
            return 0;
        }

        public static async Task<Player> GetPlayer(string name)
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

        public static async Task<Clan> GetClan(string name)
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

        public static async Task<List<Server>> GetServer()
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
