using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Rabbot.Models;
using Rabbot.Models.API;
using Serilog;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Rabbot.Services
{
    public class ApiService
    {
        private readonly ImageService _imageService;
        private static readonly ILogger _logger = Log.ForContext(Serilog.Core.Constants.SourceContextPropertyName, nameof(ApiService));
        public ApiService(IServiceProvider services)
        {
            _imageService = services.GetRequiredService<ImageService>();
        }

        public string GetRandomImage()
        {
            var rnd = new Random().Next(0, 4);
            if (rnd == 0)
                return GetFoxImage();
            else if (rnd == 1)
                return GetDogImage();
            else if (rnd == 2)
                return GetCatImage();
            else if (rnd == 3)
                return GetShibeImage();

            return string.Empty;
        }

        public string GetShibeImage()
        {
            var (payload, success) = ApiRequest(Constants.ShibeApi);
            if (success)
            {
                var link = payload.Replace("[\"", string.Empty).Replace("\"]", string.Empty);
                if (string.IsNullOrWhiteSpace(link))
                    return string.Empty;

                return link;
            }
            return string.Empty;
        }

        public string GetFoxImage()
        {
            var (payload, success) = ApiRequest(Constants.FoxApi);
            if (success)
            {
                var fox = DeserializeJson<FoxDto>(payload);
                return fox.Image;
            }
            return string.Empty;
        }

        public string GetDogImage()
        {
            var (payload, success) = ApiRequest(Constants.DogApi);
            if (success)
            {
                var dog = DeserializeJson<DogDto>(payload);
                if (dog.Status == "success")
                    return dog.Message;
            }
            return string.Empty;
        }

        public string GetCatImage()
        {
            var (payload, success) = ApiRequest(Constants.CatApi);
            if (success)
            {
                var cat = DeserializeJson<CatDto>(payload);
                return cat.File;
            }
            return string.Empty;
        }

        public List<CoronaStatsDto> GetCoronaRanking(int count)
        {
            var (payload, success) = ApiRequest(Constants.CoronaApi);
            if (success)
            {
                return DeserializeJson<CoronaStatsDto[]>(payload).OrderByDescending(p => p.Cases).Take(count).ToList();
            }
            return null;
        }

        public CoronaStatsDto GetCoronaCountry(string country)
        {
            var (payload, success) = ApiRequest(Constants.CoronaApi);
            if (success)
            {
                return DeserializeJson<CoronaStatsDto[]>(payload).FirstOrDefault(p => p.Country.Contains(country, StringComparison.InvariantCultureIgnoreCase));
            }
            return null;
        }

        public int GetOfficialPlayerCount()
        {
            var (payload, success) = ApiRequest(Config.Bot.OfficialPlayerURL);
            if (success)
                return DeserializeJson<OfficialPlayerCountDto>(payload).PlayerCount;
            return 0;
        }

        public static (string payload, bool success) ApiRequest(string url, Dictionary<string, string> headers = null)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

                // Add header
                if (headers != null)
                    foreach (var header in headers)
                    {
                        request.Headers.Add(header.Key, header.Value);
                    }

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

        public async Task<(string payload, bool success)> APIRequestAsync(string url, Dictionary<string, string> headers = null)
        {
            try
            {
                HttpClient _httpClient = new HttpClient();

                // Add header
                if (headers != null)
                    foreach (var header in headers)
                    {
                        _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                    }

                var task = await _httpClient.GetAsync(url).ConfigureAwait(false);
                task.EnsureSuccessStatusCode();
                var payload = await task.Content.ReadAsStringAsync();
                return (payload, true);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Something went wrong in {nameof(APIRequestAsync)}");
                return (string.Empty, false);
            }
        }

        private T DeserializeJson<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json);
        }

    }
}
