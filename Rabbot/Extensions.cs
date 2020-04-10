using AnimatedGif;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rabbot.Models;
using Rabbot.Models.API;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceModel.Syndication;
using System.Text;
using System.Threading.Tasks;

namespace Rabbot
{
    public static class Extensions
    {
        // strings
        public static string[] GetArgs(this string @this)
        {
            return @this.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public static int CountWords(this string @this)
        {
            if (string.IsNullOrWhiteSpace(@this))
                return 0;

            return @this.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Count();
        }

        // int
        public static string ToFormattedString(this int @this)
        {
            return @this.ToString("N0", new System.Globalization.CultureInfo("de-DE"));
        }
        public static int GetValueFromPercent(this int @this, double percent)
        {
            double percentValue = percent / 100;
            double value = Convert.ToDouble(@this);
            return (int)Math.Ceiling((value * (1 + percentValue)) - @this);
        }

        // DateTime
        public static string ToFormattedString(this DateTime @this)
        {
            return @this.ToString("dd.MM.yyyy HH:mm");
        }

        public static DateTime ToCET(this DateTime @this)
        {
            TimeZoneInfo europeTimeZone;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                europeTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Central Europe Standard Time");
            else
                europeTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

            return TimeZoneInfo.ConvertTimeFromUtc(@this, europeTimeZone);
        }
        public static string ToTimeString(this TimeSpan ts, string format)
        {
            return new DateTime(ts.Ticks).ToString(format);
        }

        // Other
        public static YouTubeVideoDto GetFirstVideo(this SyndicationFeed @this)
        {
            var firstItem = @this.Items.FirstOrDefault();
            if (firstItem == null)
                return null;

            return new YouTubeVideoDto { Title = firstItem.Title.Text, UploadDate = firstItem.PublishDate, Id = firstItem.Id.Substring(9), ChannelName = firstItem.Authors.First().Name };
        }

        public static IServiceCollection AddDbContext<TContext>(this IServiceCollection This,
            Action<DbContextOptionsBuilder> optionsBuilder)
            where TContext : DbContext
        {
            return This
                .AddSingleton(x =>
                {
                    var builder = new DbContextOptionsBuilder<TContext>();
                    optionsBuilder(builder);
                    return builder.Options;
                })
                .AddTransient<TContext>()
                .AddTransient<DbContext>(x => x.GetRequiredService<TContext>());
        }

        public static MemoryStream ToStream(this Image image)
        {
            var stream = new MemoryStream();
            image.Save(stream, ImageFormat.Png);
            image.Dispose();
            stream.Position = 0;

            return stream;
        }

        public static async Task<MemoryStream> ToStream(this Bitmap[] frames, int delay = 33)
        {
            var ms = new MemoryStream();
            using (var gif = new AnimatedGifCreator(ms, delay))
            {
                foreach (var image in frames)
                {
                    await gif.AddFrameAsync(image, quality: GifQuality.Bit8);
                }
            }

            ms.Position = 0;

            return ms;
        }
    }
}
