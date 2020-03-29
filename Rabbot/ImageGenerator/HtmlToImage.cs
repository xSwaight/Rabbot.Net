using System.IO;

namespace Rabbot.ImageGenerator
{
    public static class HtmlToImage
    {
        public static string Generate(string name, string html, int width, int height, ImageFormat format = ImageFormat.Jpg)
        {
            var converter = new HtmlConverter();
            var bytes = converter.FromHtmlString(html, width, height, format, 90);
            File.WriteAllBytes($"{name}.{format.ToString().ToLower()}", bytes);
            return Directory.GetCurrentDirectory() + $"/{name}.{format.ToString().ToLower()}";
        }
    }
}
