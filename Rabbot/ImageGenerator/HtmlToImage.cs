using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Rabbot.ImageGenerator
{
    public static class HtmlToImage
    {
        public static string Generate(string name, string html, int width, int height)
        {
            var converter = new HtmlConverter();
            var bytes = converter.FromHtmlString(html, width, height, ImageFormat.Jpg, 90);
            File.WriteAllBytes($"{name}.jpg", bytes);
            return Directory.GetCurrentDirectory() + $"/{name}.jpg";
        }
    }
}
