using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using SixLabors.ImageSharp;

namespace Rabbot.Services
{
    public class CacheService
    {
        private static readonly ConcurrentDictionary<string, Image> ImageCache = new ConcurrentDictionary<string, Image>();

        public Image GetOrAddImage(string path)
        {
            if (!ImageCache.TryGetValue(path, out var image))
                image = CacheImageFromDisk(path);

            return image;
        }

        public static void ClearImageCache()
        {
            foreach (var image in ImageCache)
                image.Value.Dispose();

            ImageCache.Clear();
        }

        private Image CacheImageFromDisk(string path)
        {
            var image = Image.Load(path);
            ImageCache.TryAdd(path, image);

            return image;
        }
    }
}
