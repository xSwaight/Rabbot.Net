using Serilog;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Text;

namespace Rabbot.Services
{
    public class ImageService
    {
        private static readonly ILogger _logger = Log.ForContext(Serilog.Core.Constants.SourceContextPropertyName, nameof(ImageService));

        public string DownloadImage(string url)
        {
            if (url.ToLower().EndsWith(".jpg"))
                return SaveImage(url.Replace("jpeg", "jpg"), ImageFormat.Jpeg);
            else if (url.ToLower().EndsWith(".png"))
                return SaveImage(url, ImageFormat.Png);
            else if (url.ToLower().EndsWith(".gif"))
                return SaveImage(url, ImageFormat.Gif);

            return string.Empty;
        }

        private string SaveImage(string url, ImageFormat format)
        {
            try
            {
                var path = Path.Combine(AppContext.BaseDirectory, $"{Guid.NewGuid()}.{format.ToString().ToLower()}");

                WebClient client = new WebClient();
                Stream stream = client.OpenRead(url);
                Bitmap bitmap; bitmap = new Bitmap(stream);

                if (bitmap != null)
                {
                    bitmap.Save(path, format);
                }

                stream.Flush();
                stream.Close();
                client.Dispose();
                return path;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Something went wrong in {nameof(SaveImage)}");
                return string.Empty;
            }
        }
    }
}
