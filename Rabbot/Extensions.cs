using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rabbot.Models;
using Rabbot.Models.API;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceModel.Syndication;
using System.Text;

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
    }
}
