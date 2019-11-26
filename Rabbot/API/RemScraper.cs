using HtmlAgilityPack;
using Rabbot.API.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Rabbot.API
{
    public static class RemScraper
    {
        public static RemnantsPlayer Scrape(string name)
        {
            HtmlDocument htmlDoc = new HtmlDocument();
            string url = $"https://remdb.net/player/{name}/";
            string urlResponse = "";
            try
            {
                urlResponse = URLRequest(url);
            }
            catch
            {
                return null;
            }

            if (urlResponse.Contains("Player doesn't exist"))
                return null;

            htmlDoc.LoadHtml(urlResponse);
            try
            {
                var level = htmlDoc.GetElementbyId("s4db-content").SelectNodes("//div").Where(p => p.InnerText.Contains("Level")).ToList()[8].InnerText;
                var clan = htmlDoc.GetElementbyId("s4db-player-view-clan").InnerText;
                var extendedData = htmlDoc.GetElementbyId("s4db-player-view-general").ChildNodes.First(p => p.Name == "div" && p.HasClass("row-fluid")).ChildNodes.First(p => p.Name == "div").ChildNodes.Where(p => p.Name == "div").ToList();

                RemnantsPlayer player = new RemnantsPlayer
                {
                    Matches = Convert.ToInt32(Regex.Match(extendedData[0].InnerText, @"\d+").Value),
                    Won = Convert.ToInt32(Regex.Match(extendedData[1].InnerText, @"\d+").Value),
                    Lost = Convert.ToInt32(Regex.Match(extendedData[2].InnerText, @"\d+").Value),
                    LastOnline = extendedData[3].InnerText.Split(":")[1].Trim(),
                    Clan = clan,
                    Level = Convert.ToInt32(Regex.Match(level, @"\d+").Value),
                    Name = name
                };
                return player;
            }
            catch (Exception)
            {
                return null;
            }
        }

        //General Function to request data from a Server
        static string URLRequest(string url)
        {
            // Prepare the Request
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

            // Set method to GET to retrieve data
            request.Method = "GET";
            request.Timeout = 6000; //60 second timeout
            request.UserAgent = "Mozilla/5.0 (compatible; MSIE 9.0; Windows Phone OS 7.5; Trident/5.0; IEMobile/9.0)";

            string responseContent = null;

            // Get the Response
            using (WebResponse response = request.GetResponse())
            {
                // Retrieve a handle to the Stream
                using (Stream stream = response.GetResponseStream())
                {
                    // Begin reading the Stream
                    using (StreamReader streamreader = new StreamReader(stream))
                    {
                        // Read the Response Stream to the end
                        responseContent = streamreader.ReadToEnd();
                    }
                }
            }

            return (responseContent);
        }
    }
}
