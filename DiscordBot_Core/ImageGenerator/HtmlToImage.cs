using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DiscordBot_Core.ImageGenerator
{
    public static class HtmlToImage
    {
        public static string Generate(string name, string html)
        {
            var converter = new HtmlConverter();
            //var html = "Hello <string>World</strong>";
            var bytes = converter.FromHtmlString(html, 300, 170, ImageFormat.Jpg, 90);
            File.WriteAllBytes($"{name}.jpg", bytes);
            return Directory.GetCurrentDirectory() + $"/{name}.jpg";
        }
    }
}
