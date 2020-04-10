using Microsoft.Extensions.DependencyInjection;
using Rabbot.Models;
using Serilog;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Rabbot.Services
{
    public class ImageService
    {
        private static readonly ILogger _logger = Log.ForContext(Serilog.Core.Constants.SourceContextPropertyName, nameof(ImageService));
        private readonly CacheService _cacheService;

        public ImageService(IServiceProvider services)
        {
            _cacheService = services.GetRequiredService<CacheService>();
        }

        public async Task<MemoryStream> DrawProfileAsync(UserProfileDto profileInfo, bool isAnimated = false)
        {
            MemoryStream outputStream = new MemoryStream();
            var backgroundImage = _cacheService.GetOrAddImage(Path.Combine(AppContext.BaseDirectory, "Resources", "RabbotThemeNeon", "assets", "img", "NeonProfile", "behindProfile.png"));
            var mainImage = _cacheService.GetOrAddImage(Path.Combine(AppContext.BaseDirectory, "Resources", "RabbotThemeNeon", "assets", "img", "NeonProfile", "Profile.png"));
            var levelIcon = _cacheService.GetOrAddImage(Path.Combine(AppContext.BaseDirectory, "Resources", "RabbotThemeNeon", "assets", "img", "NeonLevelIcons", $"{profileInfo.Level}.png"));
            var expBar = _cacheService.GetOrAddImage(Path.Combine(AppContext.BaseDirectory, "Resources", "RabbotThemeNeon", "assets", "img", "NeonProfile", "ExpBar.png"));

            FontCollection fonts = new FontCollection();
            FontFamily frutiger = fonts.Install(Path.Combine(AppContext.BaseDirectory, "Resources", "RabbotThemeNeon", "assets", "fonts", "Frutiger.ttf"));
            FontFamily geometos = fonts.Install(Path.Combine(AppContext.BaseDirectory, "Resources", "RabbotThemeNeon", "assets", "fonts", "Geometos.ttf"));

            var userAvatar = await GetAvatarAsync(profileInfo.AvatarUrl);
            var centerOptions = new TextGraphicsOptions { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            var rightOptions = new TextGraphicsOptions { HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };

            //Fonts
            var nameFont = new Font(frutiger, 24, FontStyle.Regular);
            var levelRankFont = new Font(geometos, 28, FontStyle.Bold);
            var expFont = new Font(geometos, 12, FontStyle.Bold);
            var expInfoFont = new Font(geometos, 11, FontStyle.Bold);
            var goatFont = new Font(geometos, 18, FontStyle.Bold);

            //Gif
            if (isAnimated)
            {
                //userAvatar..Metadata.GetFormatMetadata(GifFormat.Instance).
            }
            profileInfo.Name = Regex.Replace(profileInfo.Name, @"[^\u0000-\u007F]+", string.Empty);
            using (var image = new Image<Rgba32>(300, 175))
            {
                //Short name if it's too long
                while (true)
                {
                    if (TextMeasurer.Measure(profileInfo.Name, new RendererOptions(nameFont)).Width > 200)
                        profileInfo.Name = profileInfo.Name.Substring(0, profileInfo.Name.Length - 1);
                    else
                        break;
                }

                var expBarWidth = (int)(161 * (profileInfo.Percent / 100));
                levelIcon.Mutate(x => x.Resize(26, 26));
                userAvatar.Mutate(x => x.Resize(83, 83));
                expBar.Mutate(x => x.Resize(expBarWidth, 17));
                image.Mutate(x => x
                    .DrawImage(backgroundImage, new Point(0, 0), 1f)
                    .DrawImage(userAvatar, new Point(10, 10), 1f)
                    .DrawImage(expBar, new Point(119, 130), 1f)
                    .DrawImage(mainImage, new Point(0, 0), 1f)
                    .DrawImage(levelIcon, new Point(80, 80), 1f)
                    .DrawText(centerOptions, profileInfo.Name, nameFont, Color.FromHex("#00FFFF"), new PointF(195, 28))
                    .DrawText(centerOptions, profileInfo.Rank, levelRankFont, Color.FromHex("#00FFFF"), new PointF(153, 61))
                    .DrawText(centerOptions, profileInfo.Level, levelRankFont, Color.FromHex("#00FFFF"), new PointF(238, 61))
                    .DrawText(rightOptions, profileInfo.Exp, expFont, Color.FromHex("#00FFFF"), new PointF(110, 122))
                    .DrawText(rightOptions, profileInfo.Goats, goatFont, Color.FromHex("#00FFFF"), new PointF(110, 155))
                    .DrawText(centerOptions, profileInfo.LevelInfo, expInfoFont, Color.FromHex("#00FFFF"), new PointF(204, 155))
                );

                image.SaveAsPng(outputStream);
            }
            outputStream.Position = 0;
            return outputStream;
        }

        private async Task<Image> GetAvatarAsync(string url)
        {
            using (var webClient = new WebClient())
            {
                using (var stream = await webClient.OpenReadTaskAsync(new Uri(url)))
                {
                    return Image.Load(stream);
                }
            }
        }
    }
}
