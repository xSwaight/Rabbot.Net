using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rabbot.Models;
using Rabbot.Models.API;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;
using SixLabors.Shapes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
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

        public static SocketGuildUser GetGuildUser(this SocketUser user)
        {
            if (user is SocketGuildUser guildUser)
                return guildUser;

            return null;
        }

        // Implements a full image mutating pipeline operating on IImageProcessingContext
        public static IImageProcessingContext ConvertToAvatar(this IImageProcessingContext processingContext, Size size, float cornerRadius)
        {
            return processingContext.Resize(new ResizeOptions
            {
                Size = size,
                Mode = ResizeMode.Crop
            }).ApplyRoundedCorners(cornerRadius);
        }


        // This method can be seen as an inline implementation of an `IImageProcessor`:
        // (The combination of `IImageOperations.Apply()` + this could be replaced with an `IImageProcessor`)
        private static IImageProcessingContext ApplyRoundedCorners(this IImageProcessingContext ctx, float cornerRadius)
        {
            Size size = ctx.GetCurrentSize();
            IPathCollection corners = BuildCorners(size.Width, size.Height, cornerRadius);

            var graphicOptions = new GraphicsOptions()
            {
                Antialias = true,
                AlphaCompositionMode = PixelAlphaCompositionMode.DestOut // enforces that any part of this shape that has color is punched out of the background
            };

            // mutating in here as we already have a cloned original
            // use any color (not Transparent), so the corners will be clipped
            foreach (var c in corners)
            {
                ctx = ctx.Fill(graphicOptions, Color.Red, c);
            }
            return ctx;
        }

        private static IPathCollection BuildCorners(int imageWidth, int imageHeight, float cornerRadius)
        {
            // first create a square
            var rect = new RectangularPolygon(-0.5f, -0.5f, cornerRadius, cornerRadius);

            // then cut out of the square a circle so we are left with a corner
            IPath cornerTopLeft = rect.Clip(new EllipsePolygon(cornerRadius - 0.5f, cornerRadius - 0.5f, cornerRadius));

            // corner is now a corner shape positions top left
            //lets make 3 more positioned correctly, we can do that by translating the original around the center of the image

            float rightPos = imageWidth - cornerTopLeft.Bounds.Width + 1;
            float bottomPos = imageHeight - cornerTopLeft.Bounds.Height + 1;

            // move it across the width of the image - the width of the shape
            IPath cornerTopRight = cornerTopLeft.RotateDegree(90).Translate(rightPos, 0);
            IPath cornerBottomLeft = cornerTopLeft.RotateDegree(-90).Translate(0, bottomPos);
            IPath cornerBottomRight = cornerTopLeft.RotateDegree(180).Translate(rightPos, bottomPos);

            return new PathCollection(cornerTopLeft, cornerBottomLeft, cornerTopRight, cornerBottomRight);
        }
    }
}
