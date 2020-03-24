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

        
    }
}
