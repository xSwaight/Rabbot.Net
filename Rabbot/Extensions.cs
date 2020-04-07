﻿using Microsoft.EntityFrameworkCore;
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
        public static int CountWords(this string @this)
        {
            if (string.IsNullOrWhiteSpace(@this))
                return 0;

            return @this.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Count();
        }

        public static string ToFormattedString(this int @this)
        {
            return @this.ToString("N0", new System.Globalization.CultureInfo("de-DE"));
        }

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

        public static int GetValueFromPercent(this int @this, double percent)
        {
            double percentValue = percent / 100;
            double value = Convert.ToDouble(@this);
            return (int)Math.Ceiling((value * (1 + percentValue)) - @this);
        }

        public static YouTubeVideoDto GetFirstVideo(this SyndicationFeed @this)
        {
            var firstItem = @this.Items.FirstOrDefault();
            if (firstItem == null)
                return null;

            return new YouTubeVideoDto { Title = firstItem.Title.Text, UploadDate = firstItem.PublishDate, Id = firstItem.Id.Substring(9), ChannelName = firstItem.Authors.First().Name };
        }

        public static string ToTimeString(this TimeSpan ts, string format)
        {
            return new DateTime(ts.Ticks).ToString(format);
        }

        public static string[] GetArgs(this string @this)
        {
            return @this.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public static string ToHumanReadable(this TimeSpan value)
        {
            var uptime = new StringBuilder();
            if (value.Days > 0)
                uptime.AppendFormat(value.Days > 1 ? "{0} days " : "{0} day ", value.Days);

            if (value.Days > 0 || value.Hours > 0)
                uptime.AppendFormat(value.Hours > 1 ? "{0} hours " : "{0} hour ", value.Hours);

            if (value.Hours > 0 || value.Minutes > 0)
                uptime.AppendFormat(value.Minutes > 1 ? "{0} minutes " : "{0} minute ", value.Minutes);

            if (value.Seconds > 0)
                uptime.AppendFormat(value.Seconds > 1 ? "{0} seconds " : "{0} second ", value.Seconds);

            return uptime.ToString();
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
