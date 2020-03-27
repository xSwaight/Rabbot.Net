using Newtonsoft.Json;
using Rabbot.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace Rabbot.Services
{
    public class ApiService
    {
        private readonly ImageService _imageService;
        private static readonly ILogger _logger = Log.ForContext(Serilog.Core.Constants.SourceContextPropertyName, nameof(ApiService));
        public ApiService(ImageService imageService)
        {
            _imageService = imageService;
        }

        public string GetDogImage()
        {
            var (json, success) = ApiRequest("https://dog.ceo/api/breeds/image/random");
            if (success)
            {
                var dog = DeserializeJson<DogDto>(json);
                if (dog.Status == "success")
                    return _imageService.DownloadImage(dog.Message);
            }
            return string.Empty;
        }

        public string GetCatImage()
        {
            var (json, success) = ApiRequest("http://aws.random.cat/meow");
            if (success)
            {
                var cat = DeserializeJson<CatDto>(json);
                return _imageService.DownloadImage(cat.File);
            }
            return string.Empty;
        }

        public List<CoronaStatsDto> GetCoronaRanking(int count)
        {
            var (json, success) = ApiRequest("https://corona.lmao.ninja/countries");
            if (success)
            {
                return DeserializeJson<CoronaStatsDto[]>(json).OrderByDescending(p => p.Cases).Take(count).ToList();
            }
            return null;
        }

        public CoronaStatsDto GetCoronaCountry(string country)
        {
            var (json, success) = ApiRequest("https://corona.lmao.ninja/countries");
            if (success)
            {
                return DeserializeJson<CoronaStatsDto[]>(json).FirstOrDefault(p => p.Country.Contains(country, StringComparison.InvariantCultureIgnoreCase));
            }
            return null;
        }

        public static (string json, bool success) ApiRequest(string url)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Stream receiveStream = response.GetResponseStream();
                    StreamReader readStream = new StreamReader(receiveStream, Encoding.GetEncoding("UTF-8"));
                    string data = readStream.ReadToEnd();

                    response.Close();
                    readStream.Close();

                    return (data, true);
                }
                return (string.Empty, false);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Something went wrong in {nameof(ApiRequest)}");
                return (string.Empty, false);
            }
        }

        private T DeserializeJson<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json);
        }

    }
}
